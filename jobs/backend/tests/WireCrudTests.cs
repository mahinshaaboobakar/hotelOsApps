using HotelOS.Jobs.Contracts.V1;
using HotelOS.Jobs.Domain;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// The wire round's create, read and update rows: every operation driven from
/// the RPC a control would call, checked in the database, and read back
/// through the wire.
/// </summary>
[Collection(JobsCollection.Name)]
public class WireCrudTests(JobsFixture fixture)
{
    /// <summary>A category, an item, a resolution and a policy, made over the wire.</summary>
    private static async Task<(string CategoryId, string ItemId, string ResolutionId)> CatalogueAsync(WireHarness h)
    {
        var category = await h.Client.SaveCategoryAsync(new SaveCategoryRequest
        {
            Context = h.Context(), Code = "AC", Name = "Air conditioning", DepartmentCode = "ENG", Active = true,
        });
        var item = await h.Client.SaveItemAsync(new SaveItemRequest
        {
            Context = h.Context(), CategoryId = category.Id, Code = "AC_NOT_COOLING", Name = "Not cooling",
            DefaultPriority = "P2", DueWithinMinutes = 40, PhotoOnCompletion = "OPTIONAL", Active = true,
            GuestRequestable = true, ReplaceAliases = true, Aliases = { "AC not working", "room warm" },
        });
        var resolution = await h.Client.AddResolutionAsync(new AddResolutionRequest
        {
            Context = h.Context(), ItemId = item.Id, Name = "Refrigerant topped up",
        });
        return (category.Id, item.Id, resolution.Id);
    }

    private static RaiseJobRequest Raise(WireHarness h, string itemId, string summary = "Room feels warm since noon") => new()
    {
        Context = h.Context(), ItemId = itemId, LocationId = Guid.CreateVersion7().ToString(), Summary = summary,
        RaisedVia = RaisedVia.App, RaisedKind = RaisedKind.Staff, RaisedById = h.PropertyId.ToString(),
    };

    [Fact]
    public async Task The_catalogue_is_created_over_the_wire_stored_and_read_back()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var (categoryId, itemId, _) = await CatalogueAsync(h);

        await using (var db = h.Db())
        {
            var item = await db.Items.FirstAsync(i => i.Id == Guid.Parse(itemId));
            Assert.Equal(("AC_NOT_COOLING", "Not cooling", 40), (item.Code, item.Name, item.DueWithinMinutes));
            Assert.Equal(2, await db.ItemAliases.CountAsync(a => a.ItemId == item.Id));
        }

        var read = await h.Client.ListCatalogueAsync(new ListCatalogueRequest { Context = h.Context() });
        Assert.Contains(read.Categories, c => c.Id == categoryId && c.Name == "Air conditioning");
        var back = Assert.Single(read.Items, i => i.Id == itemId);
        Assert.Equal(("P2", 40), (back.DefaultPriority, back.DueWithinMinutes));
        Assert.Contains("AC not working", back.Aliases);
    }

    [Fact]
    public async Task A_job_is_raised_over_the_wire_numbered_stored_and_read_back_whole()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var (_, itemId, _) = await CatalogueAsync(h);

        var raised = await h.Client.RaiseJobAsync(Raise(h, itemId));

        Assert.StartsWith("MRN-ENG-", raised.JobNumber, StringComparison.Ordinal);
        Assert.Equal(("P2", "CATALOGUE", "RAISED"), (raised.Priority, raised.PriorityDecidedBy, raised.JobStatus));
        await using (var db = h.Db())
        {
            var job = await db.Jobs.FirstAsync(j => j.Id == Guid.Parse(raised.Id));
            Assert.Equal(h.PropertyId, job.PropertyId);
            Assert.Equal("ENG", job.DepartmentCode);
            // The birth rows the design promises, written in the same transaction.
            Assert.Equal(1, await db.StatusHistory.CountAsync(s => s.JobId == job.Id));
            Assert.Equal(1, await db.Notes.CountAsync(n => n.JobId == job.Id));
            Assert.Equal(1, await db.ConcernHistory.CountAsync(c => c.JobId == job.Id));
        }

        var detail = await h.Client.GetJobAsync(new GetJobRequest { Context = h.Context(), Id = raised.Id });
        Assert.Equal(raised.JobNumber, detail.Job.JobNumber);
        Assert.Single(detail.StatusHistory);
        Assert.Single(detail.Notes);
        Assert.Single(detail.ConcernHistory);
    }

    [Fact]
    public async Task The_whole_life_of_a_job_crosses_the_wire_and_the_database_agrees()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var (_, itemId, resolutionId) = await CatalogueAsync(h);
        var arjun = Guid.CreateVersion7();
        var job = await h.Client.RaiseJobAsync(Raise(h, itemId));

        job = await h.Client.AssignJobAsync(new AssignJobRequest
        {
            Context = h.Context(), Id = job.Id, ExpectedVersion = job.Version, UserId = arjun.ToString(),
        });
        Assert.Equal("ASSIGNED", job.JobStatus);

        WireCaller.Current = AuthenticatedCaller.ForUser(arjun, Guid.NewGuid(), TransportPrincipalKind.Application, "jobs");
        try
        {
            job = await h.Client.AcceptJobAsync(new JobVersionRequest { Context = h.Context(), Id = job.Id, ExpectedVersion = job.Version });
            Assert.Equal("ACCEPTED", job.JobStatus);

            var session = await h.Client.StartWorkAsync(new JobRequest { Context = h.Context(), Id = job.Id });
            Assert.NotNull(session.StartedAt);
            Assert.Null(session.StoppedAt);

            var paused = await h.Client.PauseWorkAsync(new PauseWorkRequest { Context = h.Context(), Id = job.Id, Reason = "fetch gauge" });
            Assert.Equal("fetch gauge", paused.PauseReason);
            await h.Client.ResumeWorkAsync(new JobRequest { Context = h.Context(), Id = job.Id });
            var stopped = await h.Client.StopWorkAsync(new JobRequest { Context = h.Context(), Id = job.Id });
            Assert.NotNull(stopped.StoppedAt);

            job = await h.Client.GetJobAsync(new GetJobRequest { Context = h.Context(), Id = job.Id }).ResponseAsync
                .ContinueWith(t => t.Result.Job);
            job = await h.Client.ResolveJobAsync(new ResolveJobRequest
            {
                Context = h.Context(), Id = job.Id, ExpectedVersion = job.Version,
                ResolutionId = resolutionId, Note = "Charged to 68 psi",
            });
            Assert.Equal("RESOLVED", job.JobStatus);

            job = await h.Client.CloseJobAsync(new JobVersionRequest { Context = h.Context(), Id = job.Id, ExpectedVersion = job.Version });
            Assert.Equal("CLOSED", job.JobStatus);
        }
        finally
        {
            WireCaller.Current = AuthenticatedCaller.ForService("jobs");
        }

        await using var db = h.Db();
        var stored = await db.Jobs.FirstAsync(j => j.Id == Guid.Parse(job.Id));
        Assert.Equal(JobStatus.Closed, stored.JobStatus);
        Assert.Equal(2, await db.WorkSessions.CountAsync(s => s.JobId == stored.Id));
        Assert.Equal(1, await db.Resolutions.CountAsync(r => r.JobId == stored.Id));
        // Every step announced, in the caller's own transaction.
        Assert.True(await db.Set<HotelOS.Platform.StoredEvent>().CountAsync(e => e.AggregateId == stored.Id) >= 6);
    }

    [Fact]
    public async Task A_note_a_reminder_and_a_rating_are_written_and_read_back()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var (_, itemId, resolutionId) = await CatalogueAsync(h);
        var stay = Guid.CreateVersion7();
        var arjun = Guid.CreateVersion7();
        var request = Raise(h, itemId);
        request.RaisedKind = RaisedKind.Guest;
        request.RaisedById = string.Empty;
        request.StayId = stay.ToString();
        request.RaisedVia = RaisedVia.GuestApp;
        var job = await h.Client.RaiseJobAsync(request);

        WireCaller.Current = AuthenticatedCaller.ForUser(arjun, Guid.NewGuid(), TransportPrincipalKind.Application, "jobs");
        try
        {
            await h.Client.AddNoteAsync(new AddNoteRequest { Context = h.Context(), Id = job.Id, Text = "Suction pressure low." });
            var reminder = await h.Client.RemindMeAsync(new RemindMeRequest
            {
                Context = h.Context(), Id = job.Id, Note = "check the leak test",
                At = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(1)),
            });
            Assert.NotEqual(string.Empty, reminder.ReminderId);

            job = await h.Client.AssignJobAsync(new AssignJobRequest { Context = h.Context(), Id = job.Id, ExpectedVersion = job.Version, UserId = arjun.ToString() });
            job = await h.Client.AcceptJobAsync(new JobVersionRequest { Context = h.Context(), Id = job.Id, ExpectedVersion = job.Version });
            job = await h.Client.ResolveJobAsync(new ResolveJobRequest { Context = h.Context(), Id = job.Id, ExpectedVersion = job.Version, ResolutionId = resolutionId });
            job = await h.Client.CloseJobAsync(new JobVersionRequest { Context = h.Context(), Id = job.Id, ExpectedVersion = job.Version });

            var rating = await h.Client.RateJobAsync(new RateJobRequest
            {
                Context = h.Context(), Id = job.Id, StayId = stay.ToString(), Stars = 5, Text = "Six minutes. Thank you.",
            });
            Assert.Equal(5, rating.Stars);
        }
        finally
        {
            WireCaller.Current = AuthenticatedCaller.ForService("jobs");
        }

        var detail = await h.Client.GetJobAsync(new GetJobRequest { Context = h.Context(), Id = job.Id });
        Assert.Equal(5, detail.Rating.Stars);
        Assert.Contains(detail.Notes, n => n.Text == "Suction pressure low.");
    }

    [Fact]
    public async Task A_policy_and_a_presence_switch_are_saved_and_the_database_holds_them()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var saved = await h.Client.SaveConcernPolicyAsync(new SaveConcernPolicyRequest
        {
            Context = h.Context(), Name = "Engineering", DepartmentCode = "ENG", UntriagedStuckMinutes = 15,
            Rules = { new ConcernRule { Priority = "P1", DueWithinMinutes = 40, AtRiskPercent = 75, NotAcceptedMinutes = 8, NoSessionMinutes = 15, ManagerAtRisk = true, RunsOutsidePresence = true } },
            Ladder = { new LadderStep { Priority = "P1", StepNo = 1, Role = "ASSIGNEE", Trigger = "AT_RISK", DelayMinutes = 0 } },
        });
        var presence = await h.Client.SavePresenceAsync(new SavePresenceRequest
        {
            Context = h.Context(), DepartmentCode = "ENG", Enabled = true, FollowShifts = true,
        });

        Assert.Equal("ENG", presence.DepartmentCode);
        await using var db = h.Db();
        var policy = await db.ConcernPolicies.FirstAsync(p => p.Id == Guid.Parse(saved.Id));
        Assert.Equal(("Engineering", "ENG"), (policy.Name, policy.DepartmentCode));
        Assert.Equal(1, await db.ConcernRules.CountAsync(r => r.PolicyId == policy.Id));
        Assert.Equal(1, await db.LadderSteps.CountAsync(s => s.PolicyId == policy.Id));

        var read = await h.Client.ListPresenceAsync(new ListPresenceRequest { Context = h.Context() });
        Assert.Contains(read.Departments, d => d.DepartmentCode == "ENG" && d.FollowShifts);
    }
}
