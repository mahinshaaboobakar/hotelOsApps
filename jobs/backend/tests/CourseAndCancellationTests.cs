using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>Hold and resume, amend, cancel with cascade — job.amend and job.cancel, S9 D2, S1 D2.</summary>
[Collection(JobsCollection.Name)]
public class CourseAndCancellationTests(JobsFixture fixture)
{
    [Fact]
    public async Task Hold_needs_a_reason_stops_the_session_and_resume_returns_where_it_was()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var arjun = Uuid7.NewUuid7();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        await h.Work.StartAsync(h.Scope(arjun), job.Id, default);
        job = await h.Db.Jobs.FirstAsync(j => j.Id == job.Id);

        await Assert.ThrowsAsync<InvalidRequestException>(() => h.Course.HoldAsync(h.Scope(),
            new HoldCommand { JobId = job.Id, ExpectedVersion = job.Version, Reason = " " }, default));

        var thursday = new DateTimeOffset(2026, 9, 4, 9, 0, 0, TimeSpan.Zero);
        job = await h.Course.HoldAsync(h.Scope(), new HoldCommand
        {
            JobId = job.Id, ExpectedVersion = job.Version, Reason = "waiting for parts", Until = thursday,
        }, default);

        Assert.Equal((JobStatus.OnHold, "waiting for parts", thursday), (job.JobStatus, job.HoldReason, job.HoldUntil));
        Assert.Equal(("job.amend", "property", h.PropertyId), h.Authorizer.Checks[^1]);
        Assert.Null(await h.Records.OpenSessionAsync(job.Id, default));
        Assert.Contains(EventTypes.JobHeld, h.Events.Types);

        job = await h.Course.ResumeAsync(h.Scope(), job.Id, job.Version, default);
        Assert.Equal((JobStatus.Accepted, null), (job.JobStatus, job.HoldReason));
        Assert.Contains(EventTypes.JobResumed, h.Events.Types);
    }

    [Fact]
    public async Task Amend_sets_priority_by_hand_restricts_links_and_raises_a_scheduled_job_now()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var scheduled = await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: new DateOnly(2026, 9, 10));
        var other = await h.RaiseNotCoolingAsync(h.Scope());

        var job = await h.Course.AmendAsync(h.Scope(), new AmendCommand
        {
            JobId = scheduled.Id, ExpectedVersion = scheduled.Version, Priority = Priority.P3, Restricted = true,
            LinkJobId = other.Id, ScheduledFor = Optional<DateOnly?>.Of(null),
        }, default);

        Assert.Equal((Priority.P3, PriorityDecidedBy.Manual, true, JobStatus.Raised, null),
            (job.Priority, job.PriorityDecidedBy, job.Restricted, job.JobStatus, job.ScheduledFor));
        Assert.Single(await h.Db.Links.Where(l => l.JobId == job.Id && l.LinkedJobId == other.Id).ToListAsync());

        // A second link between the same pair, from the other side, is not a second row.
        await h.Course.AmendAsync(h.Scope(), new AmendCommand { JobId = other.Id, ExpectedVersion = other.Version, LinkJobId = job.Id }, default);
        Assert.Single(await h.Db.Links.Where(l => (l.JobId == job.Id && l.LinkedJobId == other.Id) || (l.JobId == other.Id && l.LinkedJobId == job.Id)).ToListAsync());
    }

    [Fact]
    public async Task A_started_job_cannot_be_scheduled_but_a_scheduled_one_can_move()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var started = await h.RaiseNotCoolingAsync(h.Scope());
        var scheduled = await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: new DateOnly(2026, 9, 10));

        await Assert.ThrowsAsync<InvalidRequestException>(() => h.Course.AmendAsync(h.Scope(), new AmendCommand
        {
            JobId = started.Id, ExpectedVersion = started.Version, ScheduledFor = Optional<DateOnly?>.Of(new DateOnly(2026, 9, 12)),
        }, default));

        var moved = await h.Course.AmendAsync(h.Scope(), new AmendCommand
        {
            JobId = scheduled.Id, ExpectedVersion = scheduled.Version, ScheduledFor = Optional<DateOnly?>.Of(new DateOnly(2026, 9, 12)),
        }, default);
        Assert.Equal((JobStatus.Scheduled, new DateOnly(2026, 9, 12)), (moved.JobStatus, moved.ScheduledFor));
    }

    [Fact]
    public async Task Cancel_needs_a_reason_asks_job_cancel_and_cascades_to_open_steps_but_not_links()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var parent = await h.RaiseNotCoolingAsync(h.Scope());
        var step = await h.Jobs.RaiseAsync(h.Scope(), new RaiseJobCommand
        {
            ItemId = h.NotCooling.Id, LocationId = h.Room1204, Summary = "Leak test", RaisedVia = RaisedVia.App,
            RaisedKind = RaisedKind.Staff, RaisedById = Uuid7.NewUuid7(), ParentJobId = parent.Id,
        }, default);
        var linked = await h.RaiseNotCoolingAsync(h.Scope());
        await h.Course.AmendAsync(h.Scope(), new AmendCommand { JobId = parent.Id, ExpectedVersion = parent.Version, LinkJobId = linked.Id }, default);
        parent = await h.Db.Jobs.FirstAsync(j => j.Id == parent.Id);

        await Assert.ThrowsAsync<InvalidRequestException>(() => h.Cancellation.CancelAsync(h.Scope(),
            new CancelCommand { JobId = parent.Id, ExpectedVersion = parent.Version, Reason = "" }, default));

        parent = await h.Cancellation.CancelAsync(h.Scope(), new CancelCommand
        {
            JobId = parent.Id, ExpectedVersion = parent.Version, Reason = "guest checked out",
        }, default);

        Assert.Equal(JobStatus.Cancelled, parent.JobStatus);
        Assert.Equal(("job.cancel", "property", h.PropertyId), h.Authorizer.Checks[^1]);
        Assert.Equal(JobStatus.Cancelled, (await h.Db.Jobs.FirstAsync(j => j.Id == step.Id)).JobStatus);
        Assert.Equal(JobStatus.Raised, (await h.Db.Jobs.FirstAsync(j => j.Id == linked.Id)).JobStatus);
        Assert.Equal(2, h.Events.Types.Count(t => t == EventTypes.JobCancelled));
    }

    [Fact]
    public async Task Closing_a_parent_never_closes_its_steps_but_resolving_it_unblocks_the_first()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var arjun = Uuid7.NewUuid7();
        var parent = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        var step = await h.Jobs.RaiseAsync(h.Scope(), new RaiseJobCommand
        {
            ItemId = h.NotCooling.Id, LocationId = h.Room1204, Summary = "Leak test", RaisedVia = RaisedVia.App,
            RaisedKind = RaisedKind.Staff, RaisedById = Uuid7.NewUuid7(), ParentJobId = parent.Id,
        }, default);
        parent = await h.Assignment.AcceptAsync(h.Scope(arjun), parent.Id, parent.Version, default);
        parent = await h.Completion.ResolveAsync(h.Scope(arjun), new ResolveCommand { JobId = parent.Id, ExpectedVersion = parent.Version, ResolutionId = h.RefrigerantToppedUp.Id }, default);
        await h.Completion.CloseAsync(h.Scope(), parent.Id, parent.Version, default);

        var after = await h.Db.Jobs.FirstAsync(j => j.Id == step.Id);
        Assert.Equal(JobStatus.Raised, after.JobStatus);
        var unblocked = await h.Db.ConcernHistory.Where(c => c.JobId == step.Id).OrderBy(c => c.Since).ToListAsync();
        Assert.Contains(unblocked, c => c.Reason.StartsWith("unblocked", StringComparison.Ordinal));
    }
}
