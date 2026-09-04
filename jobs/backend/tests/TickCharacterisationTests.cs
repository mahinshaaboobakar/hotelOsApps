using HotelOS.Jobs.Application.Concerns;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// The tick itself — <c>TEMPORAL-Q1</c>. What the Schedule starts every minute:
/// the discovery of properties from open jobs, the three passes in order, and
/// the isolation that keeps one property's failure off the others.
/// </summary>
/// <remarks>
/// <b>This had no test before the Temporal migration.</b> The three passes were
/// each covered and the loop around them was not, so the try/catch the design
/// calls load-bearing was asserted nowhere. Driving the activity rather than
/// the timer is the point of the redirect: the ticker is now Temporal's, and
/// this body is what both triggers call.
/// </remarks>
[Collection(JobsCollection.Name)]
public class TickCharacterisationTests(JobsFixture fixture)
{
    /// <summary>
    /// The activity over one harness's services — the same three passes the
    /// host resolves per scope, and the identity <c>ForBackgroundWork</c> needs.
    /// </summary>
    private static ConcernActivities TickOver(JobsHarness h)
    {
        var provider = new ServiceCollection()
            .AddSingleton(h.Db)
            .AddSingleton(h.Sweep)
            .AddSingleton(h.DayStart)
            .AddSingleton(h.AutoClose)
            .AddSingleton(new ServiceIdentity("jobs"))
            .AddLogging()
            .BuildServiceProvider();
        return new ConcernActivities(() => provider);
    }

    [Fact]
    public async Task One_tick_runs_the_three_passes_in_order_at_a_property_with_jobs()
    {
        var h = new JobsHarness(fixture, new DateTimeOffset(2026, 9, 2, 21, 30, 0, TimeSpan.Zero));
        await h.SeedCatalogueAsync();
        await h.SeedEngineeringPolicyAsync();
        h.Db.ClosingPolicies.Add(new ClosingPolicy { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, DepartmentCode = "ENG", AutoCloseHours = 4 });
        await h.Db.SaveChangesAsync();
        var arjun = Guid.CreateVersion7();
        h.Directory.Roles[LadderRole.Manager] = [Guid.CreateVersion7()];

        // One for each pass: a scheduled job whose day has come, a running job
        // that will breach, and a resolved job past its closing hours.
        var scheduled = await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: new DateOnly(2026, 9, 3));
        var running = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        running = await h.Assignment.AcceptAsync(h.Scope(arjun), running.Id, running.Version, default);
        await h.Work.StartAsync(h.Scope(arjun), running.Id, default);
        var resolved = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        resolved = await h.Assignment.AcceptAsync(h.Scope(arjun), resolved.Id, resolved.Version, default);
        resolved = await h.Completion.ResolveAsync(
            h.Scope(arjun),
            new Application.Jobs.ResolveCommand { JobId = resolved.Id, ExpectedVersion = resolved.Version, ResolutionId = h.RefrigerantToppedUp.Id },
            default);

        h.Clock.Advance(TimeSpan.FromHours(5));
        await TickOver(h).SweepAsync(default);

        Assert.Equal(JobStatus.Raised, (await h.Db.Jobs.FirstAsync(j => j.Id == scheduled.Id)).JobStatus);
        Assert.Equal(JobStatus.Closed, (await h.Db.Jobs.FirstAsync(j => j.Id == resolved.Id)).JobStatus);
        Assert.Equal(
            Concern.Breached,
            await h.Db.ConcernHistory.Where(c => c.JobId == running.Id).OrderByDescending(c => c.Since).Select(c => c.Concern).FirstAsync());
    }

    [Fact]
    public async Task A_property_the_directory_cannot_answer_for_does_not_stop_the_others()
    {
        var swept = new JobsHarness(fixture);
        await swept.SeedCatalogueAsync();
        await swept.SeedEngineeringPolicyAsync();
        swept.Directory.Roles[LadderRole.Manager] = [Guid.CreateVersion7()];
        var arjun = Guid.CreateVersion7();
        var job = await swept.RaiseNotCoolingAsync(swept.Scope(), assignTo: arjun);
        job = await swept.Assignment.AcceptAsync(swept.Scope(arjun), job.Id, job.Version, default);
        await swept.Work.StartAsync(swept.Scope(arjun), job.Id, default);

        // A second property, with an open job of its own, whose Master Data has
        // gone away. The tick discovers both; the order it finds them in is the
        // database's, so this proves the isolation either way round — a leaked
        // exception either fails the tick or stops the other property short.
        var failing = new JobsHarness(fixture);
        await failing.SeedCatalogueAsync();
        await failing.RaiseNotCoolingAsync(failing.Scope());
        swept.Directory.Unreachable.Add(failing.PropertyId);

        swept.Clock.Advance(TimeSpan.FromMinutes(31));
        await TickOver(swept).SweepAsync(default);

        Assert.Equal(
            Concern.AtRisk,
            await swept.Db.ConcernHistory.Where(c => c.JobId == job.Id).OrderByDescending(c => c.Since).Select(c => c.Concern).FirstAsync());
    }
}
