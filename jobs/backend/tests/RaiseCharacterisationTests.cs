using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>Raising — frame 3, S1: the number, the priority chain, the guest's stay, the catalogue's defaults, scheduling, steps.</summary>
[Collection(JobsCollection.Name)]
public class RaiseCharacterisationTests(JobsFixture fixture)
{
    [Fact]
    public async Task Raise_numbers_the_job_from_the_property_code_and_the_category_department()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();

        var first = await h.RaiseNotCoolingAsync(h.Scope());
        var second = await h.Jobs.RaiseAsync(h.Scope(), new RaiseJobCommand
        {
            ItemId = h.StillWater.Id, LocationId = h.Room1204, Summary = "Two bottles please",
            RaisedVia = RaisedVia.App, RaisedKind = RaisedKind.Staff, RaisedById = Guid.CreateVersion7(),
        }, default);

        Assert.Equal("ENG", first.DepartmentCode);
        Assert.Equal("HK", second.DepartmentCode);
        Assert.StartsWith("MRN-ENG-", first.JobNumber, StringComparison.Ordinal);
        Assert.StartsWith("MRN-HK-", second.JobNumber, StringComparison.Ordinal);
        var n1 = int.Parse(first.JobNumber.Split('-')[2], System.Globalization.CultureInfo.InvariantCulture);
        var n2 = int.Parse(second.JobNumber.Split('-')[2], System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(n1 + 1, n2);
        Assert.Equal(("job.create", "property", h.PropertyId), h.Authorizer.Checks[0]);
    }

    [Fact]
    public async Task The_priority_chain_is_manual_then_flow_then_catalogue_then_not_triaged()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var scope = h.Scope();

        var flow = await h.RaiseNotCoolingAsync(scope);
        var manual = await h.Jobs.RaiseAsync(scope, Staff(h) with { Priority = Priority.P3, FlowPriority = Priority.P1 }, default);
        var catalogue = await h.Jobs.RaiseAsync(scope, Staff(h), default);

        Assert.Equal((Priority.P1, PriorityDecidedBy.Flow), (flow.Priority, flow.PriorityDecidedBy));
        Assert.Equal((Priority.P3, PriorityDecidedBy.Manual), (manual.Priority, manual.PriorityDecidedBy));
        Assert.Equal((Priority.P2, PriorityDecidedBy.Catalogue), (catalogue.Priority, catalogue.PriorityDecidedBy));
        Assert.Equal(h.Clock.GetUtcNow().AddMinutes(40), catalogue.DueAt);
    }

    [Fact]
    public async Task A_guest_raised_job_needs_the_stay_and_the_stay_is_the_guest()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();

        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(() => h.Jobs.RaiseAsync(
            h.Scope(), Staff(h) with { RaisedKind = RaisedKind.Guest, RaisedById = null, StayId = null }, default));
        Assert.Contains("stay_id", refusal.Message, StringComparison.Ordinal);

        var stay = Guid.CreateVersion7();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), stay);
        Assert.Equal(stay, job.StayId);
        Assert.Null(job.RaisedById);
        Assert.Equal(RaisedVia.GuestApp, job.RaisedVia);
    }

    [Fact]
    public async Task Raise_writes_the_birth_rows_and_announces_job_created_in_the_same_transaction()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();

        var job = await h.RaiseNotCoolingAsync(h.Scope());

        Assert.Equal([EventTypes.JobCreated], h.Events.Types);
        var history = await h.Db.StatusHistory.Where(x => x.JobId == job.Id).ToListAsync();
        Assert.Equal(("", JobStatus.Raised), (Assert.Single(history).FromStatus, history[0].ToStatus));
        var note = Assert.Single(await h.Db.Notes.Where(n => n.JobId == job.Id).ToListAsync());
        Assert.Equal((RaisedKind.Guest, job.Summary), (note.AuthorKind, note.Text));
        var concern = Assert.Single(await h.Db.ConcernHistory.Where(c => c.JobId == job.Id).ToListAsync());
        Assert.Equal(Concern.OnTrack, concern.Concern);
    }

    [Fact]
    public async Task Nobody_on_shift_leaves_the_job_raised_and_auto_pending()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();

        var job = await h.RaiseNotCoolingAsync(h.Scope());

        Assert.Equal(JobStatus.Raised, job.JobStatus);
        Assert.Null(await h.Records.CurrentAssignmentAsync(job.Id, default));
    }

    [Fact]
    public async Task Auto_picks_the_person_on_shift_with_the_fewest_open_jobs()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var arjun = Guid.CreateVersion7();
        h.Directory.OnShift.Add(new(Guid.CreateVersion7(), "Deepak Rao", 3));
        h.Directory.OnShift.Add(new(arjun, "Arjun Menon", 1));

        var job = await h.RaiseNotCoolingAsync(h.Scope());

        Assert.Equal(JobStatus.Assigned, job.JobStatus);
        var assignment = await h.Records.CurrentAssignmentAsync(job.Id, default);
        Assert.Equal((arjun, AssignmentHow.Auto), (assignment!.AssigneeUserId, assignment.How));
        Assert.Equal([EventTypes.JobCreated, EventTypes.JobAssigned], h.Events.Types);
    }

    [Fact]
    public async Task A_scheduled_job_waits_for_its_day_and_its_clock_starts_then()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        h.Directory.OnShift.Add(new(Guid.CreateVersion7(), "Arjun Menon", 0));
        var wednesday = new DateOnly(2026, 9, 3);

        var job = await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: wednesday);

        Assert.Equal(JobStatus.Scheduled, job.JobStatus);
        Assert.Null(await h.Records.CurrentAssignmentAsync(job.Id, default));
        Assert.Equal(new DateTimeOffset(2026, 9, 3, 0, 40, 0, TimeSpan.Zero), job.DueAt);

        h.Clock.Set(new DateTimeOffset(2026, 9, 2, 21, 30, 0, TimeSpan.Zero)); // 3 Sep 00:30 in Asia/Qatar (UTC+3), still 2 Sep in UTC
        Assert.Equal(1, await h.DayStart.RunAsync(h.PropertyId, default));
        Assert.Equal(JobStatus.Raised, (await h.Db.Jobs.FirstAsync(j => j.Id == job.Id)).JobStatus);
    }

    [Fact]
    public async Task A_step_takes_the_next_number_under_its_parent_and_a_step_cannot_have_steps()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var parent = await h.RaiseNotCoolingAsync(h.Scope());

        var one = await h.Jobs.RaiseAsync(h.Scope(), Staff(h) with { ParentJobId = parent.Id }, default);
        var two = await h.Jobs.RaiseAsync(h.Scope(), Staff(h) with { ParentJobId = parent.Id }, default);
        Assert.Equal((1, 2), (one.StepNo, two.StepNo));

        await Assert.ThrowsAsync<InvalidRequestException>(() => h.Jobs.RaiseAsync(h.Scope(), Staff(h) with { ParentJobId = one.Id }, default));
    }

    [Fact]
    public async Task An_item_switched_off_at_this_property_cannot_be_raised()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        h.Db.ItemPolicies.Add(new Domain.Policy.PropertyItemPolicy { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, ItemId = h.NotCooling.Id, ActiveHere = false });
        await h.Db.SaveChangesAsync();

        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(() => h.RaiseNotCoolingAsync(h.Scope()));
        Assert.Contains("not offered", refusal.Message, StringComparison.Ordinal);
    }

    private static RaiseJobCommand Staff(JobsHarness h) => new()
    {
        ItemId = h.NotCooling.Id, LocationId = h.Room1204, Summary = "Bedside lamp dead",
        RaisedVia = RaisedVia.App, RaisedKind = RaisedKind.Staff, RaisedById = Guid.CreateVersion7(),
    };
}
