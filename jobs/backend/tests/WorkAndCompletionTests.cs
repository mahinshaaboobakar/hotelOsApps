using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>Accept, sessions, resolve, close, reopen — S4, frame 4, S2 D3.</summary>
[Collection(JobsCollection.Name)]
public class WorkAndCompletionTests(JobsFixture fixture)
{
    [Fact]
    public async Task Only_the_assignee_accepts_and_works_the_job()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var arjun = Guid.CreateVersion7();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);

        await Assert.ThrowsAsync<PermissionDeniedException>(() => h.Assignment.AcceptAsync(h.Scope(), job.Id, job.Version, default));
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        Assert.Equal(JobStatus.Accepted, job.JobStatus);

        await Assert.ThrowsAsync<PermissionDeniedException>(() => h.Work.StartAsync(h.Scope(), job.Id, default));
        var session = await h.Work.StartAsync(h.Scope(arjun), job.Id, default);
        Assert.True(session.IsRunning);
        Assert.Equal(JobStatus.InProgress, (await h.Db.Jobs.FirstAsync(j => j.Id == job.Id)).JobStatus);
        Assert.Contains(EventTypes.JobStarted, h.Events.Types);
    }

    [Fact]
    public async Task Pause_keeps_the_session_stop_ends_it_and_worked_time_excludes_the_pause()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var arjun = Guid.CreateVersion7();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        await h.Work.StartAsync(h.Scope(arjun), job.Id, default);

        h.Clock.Advance(TimeSpan.FromMinutes(10));
        var paused = await h.Work.PauseAsync(h.Scope(arjun), job.Id, "fetch gauge", default);
        Assert.True(paused.IsPaused);
        h.Clock.Advance(TimeSpan.FromMinutes(4));
        var resumed = await h.Work.ResumeAsync(h.Scope(arjun), job.Id, default);
        h.Clock.Advance(TimeSpan.FromMinutes(6));
        var stopped = await h.Work.StopAsync(h.Scope(arjun), job.Id, default);

        Assert.Equal(resumed.Id, stopped.Id);
        var sessions = await h.Db.WorkSessions.Where(s => s.JobId == job.Id).OrderBy(s => s.StartedAt).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(600, sessions[0].WorkedSeconds);
        Assert.Equal(360, sessions[1].WorkedSeconds);
        Assert.Equal(JobStatus.InProgress, (await h.Db.Jobs.FirstAsync(j => j.Id == job.Id)).JobStatus);
    }

    [Fact]
    public async Task Resolve_needs_a_fitting_resolution_or_a_note_for_other_and_stops_the_running_session()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var arjun = Guid.CreateVersion7();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        await h.Work.StartAsync(h.Scope(arjun), job.Id, default);
        job = await h.Db.Jobs.FirstAsync(j => j.Id == job.Id);

        await Assert.ThrowsAsync<InvalidRequestException>(() => h.Completion.ResolveAsync(h.Scope(arjun),
            new ResolveCommand { JobId = job.Id, ExpectedVersion = job.Version }, default));
        await Assert.ThrowsAsync<InvalidRequestException>(() => h.Completion.ResolveAsync(h.Scope(arjun),
            new ResolveCommand { JobId = job.Id, ExpectedVersion = job.Version, ResolutionId = h.Other.Id }, default));

        h.Clock.Advance(TimeSpan.FromMinutes(31));
        job = await h.Completion.ResolveAsync(h.Scope(arjun), new ResolveCommand
        {
            JobId = job.Id, ExpectedVersion = job.Version, ResolutionId = h.RefrigerantToppedUp.Id, Note = "Charged to 68 psi",
        }, default);

        Assert.Equal(JobStatus.Resolved, job.JobStatus);
        Assert.Equal(("job.complete", "property", h.PropertyId), h.Authorizer.Checks[^1]);
        var session = Assert.Single(await h.Db.WorkSessions.Where(s => s.JobId == job.Id).ToListAsync());
        Assert.Equal((false, 31 * 60L), (session.IsRunning, session.WorkedSeconds));
        Assert.Contains(EventTypes.JobResolved, h.Events.Types);
    }

    [Fact]
    public async Task A_resolution_of_another_category_is_refused()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var arjun = Guid.CreateVersion7();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        var delivered = new Domain.Catalogue.Resolution { Id = Guid.CreateVersion7(), OrganizationId = h.OrganizationId, CategoryId = h.StillWater.CategoryId, Name = "Delivered" };
        h.Db.CatalogueResolutions.Add(delivered);
        await h.Db.SaveChangesAsync();

        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(() => h.Completion.ResolveAsync(h.Scope(arjun),
            new ResolveCommand { JobId = job.Id, ExpectedVersion = job.Version, ResolutionId = delivered.Id }, default));
        Assert.Contains("Delivered", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resolved_auto_closes_after_the_policy_hours_and_may_be_reopened_before()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        h.Db.ClosingPolicies.Add(new Domain.Policy.ClosingPolicy { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, DepartmentCode = "ENG", AutoCloseHours = 4 });
        await h.Db.SaveChangesAsync();
        var arjun = Guid.CreateVersion7();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        job = await h.Completion.ResolveAsync(h.Scope(arjun), new ResolveCommand { JobId = job.Id, ExpectedVersion = job.Version, ResolutionId = h.RefrigerantToppedUp.Id }, default);

        h.Clock.Advance(TimeSpan.FromHours(3));
        Assert.Equal(0, await h.AutoClose.RunAsync(h.PropertyId, default));
        job = await h.Completion.ReopenAsync(h.Scope(arjun), job.Id, job.Version, "still warm", default);
        Assert.Equal(JobStatus.Accepted, job.JobStatus);
        Assert.Contains(EventTypes.JobReopened, h.Events.Types);

        job = await h.Completion.ResolveAsync(h.Scope(arjun), new ResolveCommand { JobId = job.Id, ExpectedVersion = job.Version, ResolutionId = h.RefrigerantToppedUp.Id }, default);
        h.Clock.Advance(TimeSpan.FromHours(4));
        Assert.Equal(1, await h.AutoClose.RunAsync(h.PropertyId, default));
        job = await h.Db.Jobs.FirstAsync(j => j.Id == job.Id);
        Assert.Equal(JobStatus.Closed, job.JobStatus);
        Assert.Null(await h.Records.CurrentAssignmentAsync(job.Id, default));
        Assert.Contains(EventTypes.JobClosed, h.Events.Types);
    }

    [Fact]
    public async Task A_stale_version_is_refused()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: Guid.CreateVersion7());

        await Assert.ThrowsAsync<ConcurrencyException>(() => h.Completion.CloseAsync(h.Scope(), job.Id, job.Version + 5, default));
    }
}
