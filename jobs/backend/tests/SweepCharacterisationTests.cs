using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Events;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>The sweep against the real tables — S5: rows only on change, the accountable climb, nudges by subscription, presence.</summary>
[Collection(JobsCollection.Name)]
public class SweepCharacterisationTests(JobsFixture fixture)
{
    [Fact]
    public async Task The_sweep_writes_a_concern_row_only_when_the_verdict_changes_and_announces_it()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        await h.SeedEngineeringPolicyAsync();
        var arjun = Guid.CreateVersion7();
        var kiran = Guid.CreateVersion7();
        h.Directory.Roles[LadderRole.Manager] = [kiran];
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        await h.Work.StartAsync(h.Scope(arjun), job.Id, default);
        var announced = h.Events.Types.Count(t => t == EventTypes.JobConcernChanged);

        h.Clock.Advance(TimeSpan.FromMinutes(20));
        Assert.Equal(0, await h.Sweep.RunAsync(h.PropertyId, default));
        h.Clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Equal(1, await h.Sweep.RunAsync(h.PropertyId, default));
        Assert.Equal(0, await h.Sweep.RunAsync(h.PropertyId, default));
        h.Clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Equal(1, await h.Sweep.RunAsync(h.PropertyId, default));

        var rows = await h.Db.ConcernHistory.Where(c => c.JobId == job.Id).OrderBy(c => c.Since).ToListAsync();
        Assert.Equal([Concern.OnTrack, Concern.AtRisk, Concern.Breached], rows.Select(r => r.Concern));
        Assert.Equal([LadderRole.Assignee, LadderRole.Manager, LadderRole.Manager], rows.Select(r => r.AccountableRole)); // manager-at-risk is on for P1
        Assert.Equal(kiran, rows[1].AccountableUserId); // the manager, resolved through the directory
        Assert.Equal(announced + 2, h.Events.Types.Count(t => t == EventTypes.JobConcernChanged));
        Assert.Contains(LadderRole.Manager, h.Directory.RoleLookups);
    }

    [Fact]
    public async Task Nudges_follow_the_subscriptions_and_repeat_at_their_interval()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        await h.SeedEngineeringPolicyAsync();
        var arjun = Guid.CreateVersion7();
        var priya = Guid.CreateVersion7();
        h.Directory.Roles[LadderRole.Supervisor] = [priya];
        h.Db.Subscriptions.AddRange(
            new ConcernSubscription { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, Role = LadderRole.Assignee, Concern = Concern.AtRisk, RepeatMinutes = 5 },
            new ConcernSubscription { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, Role = LadderRole.Supervisor, Concern = Concern.Breached, DepartmentCode = "ENG" });
        await h.Db.SaveChangesAsync();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        await h.Work.StartAsync(h.Scope(arjun), job.Id, default);

        h.Clock.Advance(TimeSpan.FromMinutes(31));
        await h.Sweep.RunAsync(h.PropertyId, default);
        h.Clock.Advance(TimeSpan.FromMinutes(3));
        await h.Sweep.RunAsync(h.PropertyId, default);
        Assert.Equal(1, await h.Db.Nudges.CountAsync(n => n.JobId == job.Id && n.ToUserId == arjun));
        h.Clock.Advance(TimeSpan.FromMinutes(3));
        await h.Sweep.RunAsync(h.PropertyId, default);
        Assert.Equal(2, await h.Db.Nudges.CountAsync(n => n.JobId == job.Id && n.ToUserId == arjun && n.Concern == Concern.AtRisk));

        h.Clock.Advance(TimeSpan.FromMinutes(10));
        await h.Sweep.RunAsync(h.PropertyId, default);
        var toPriya = Assert.Single(await h.Db.Nudges.Where(n => n.JobId == job.Id && n.ToUserId == priya).ToListAsync());
        Assert.Equal((Concern.Breached, LadderRole.Supervisor), (toPriya.Concern, toPriya.AsRole));
        await h.Sweep.RunAsync(h.PropertyId, default);
        Assert.Equal(1, await h.Db.Nudges.CountAsync(n => n.JobId == job.Id && n.ToUserId == priya));
    }

    [Fact]
    public async Task An_absent_department_pauses_a_P2_clock_and_the_shift_fan_out_resumes_it()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        await h.SeedEngineeringPolicyAsync();
        var arjun = Guid.CreateVersion7();
        var job = await h.Jobs.RaiseAsync(h.Scope(), new Application.Jobs.RaiseJobCommand
        {
            ItemId = h.NotCooling.Id, LocationId = h.Room1204, Summary = "Noisy unit", RaisedVia = RaisedVia.App,
            RaisedKind = RaisedKind.Staff, RaisedById = Guid.CreateVersion7(), AssignToUserId = arjun,
        }, default);
        Assert.Equal(Priority.P2, job.Priority);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        await h.Work.StartAsync(h.Scope(arjun), job.Id, default);
        await h.Presence.ShiftEndedAsync(h.PropertyId, "ENG", h.Clock.GetUtcNow(), default);

        h.Clock.Advance(TimeSpan.FromMinutes(45));
        await h.Sweep.RunAsync(h.PropertyId, default);
        Assert.Equal(Concern.OnTrack, (await Latest(h, job.Id)).Concern);

        await h.Presence.ShiftStartedAsync(h.PropertyId, "ENG", 4, h.Clock.GetUtcNow(), default);
        await h.Sweep.RunAsync(h.PropertyId, default);
        Assert.Equal(Concern.Breached, (await Latest(h, job.Id)).Concern);
    }

    private static async Task<JobConcernHistory> Latest(JobsHarness h, Guid jobId) =>
        await h.Db.ConcernHistory.Where(c => c.JobId == jobId).OrderByDescending(c => c.Since).FirstAsync();
}
