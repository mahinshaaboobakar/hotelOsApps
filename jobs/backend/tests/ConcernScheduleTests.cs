using HotelOS.Jobs.Application.Concerns;
using HotelOS.Jobs.Domain;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Temporalio.Api.Enums.V1;
using ITemporalClient = Temporalio.Client.ITemporalClient;
using ScheduleActionStartWorkflow = Temporalio.Client.Schedules.ScheduleActionStartWorkflow;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// The Schedule firing this application's sweep, against a real Temporal —
/// <c>TEMPORAL-Q1</c>, page 62a.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the step that has to pass before the timer may be deleted.</b>
/// Page 62a's order is keep the timer, add the workflow, confirm the Schedule
/// fires, then remove the timer in its own commit — and "confirm" means a
/// property's job moved because Temporal started the workflow, not because a
/// test called the activity.
/// </para>
/// <para>
/// <b>An absent server fails the run</b> — ADR 0053, and the SDK's own
/// <c>TemporalScheduleTests</c> take the same line: a suite that skipped would
/// report <c>ok</c> having asserted nothing about the one thing it exists for.
/// </para>
/// <para>
/// The schedule is named per run rather than <see cref="ConcernSweepWorkflow.ScheduleId"/>,
/// so a test never contends with a real installation's, and fires every ten
/// seconds rather than every minute so the run is not a minute long. What the
/// property will actually hold — the id and the cadence — is asserted from the
/// constants the wiring uses.
/// </para>
/// </remarks>
[Collection(JobsCollection.Name)]
public sealed class ConcernScheduleTests(JobsFixture fixture)
{
    private static string Address =>
        Environment.GetEnvironmentVariable("HOTELOS_TEST_TEMPORAL_ADDRESS") ?? "127.0.0.1:27233";

    private static string Namespace =>
        Environment.GetEnvironmentVariable("HOTELOS_TEST_TEMPORAL_NAMESPACE") ?? "hotelos";

    [Fact]
    public void The_property_gets_the_cadence_the_walkthrough_locked()
    {
        Assert.Equal("jobs-concern-sweep", ConcernSweepWorkflow.ScheduleId);
        Assert.Equal(TimeSpan.FromSeconds(60), ConcernSweepWorkflow.Cadence);
        Assert.Equal(ConcernSweepWorkflow.Cadence, ConcernSweepWorkflow.Ceiling);
    }

    [Fact]
    public async Task A_schedule_fires_the_workflow_and_the_sweep_happens()
    {
        // A job that is already at risk when the sweep reaches it.
        var raising = new JobsHarness(fixture);
        await raising.SeedCatalogueAsync();
        await raising.SeedEngineeringPolicyAsync();
        var arjun = Guid.CreateVersion7();
        var job = await raising.RaiseNotCoolingAsync(raising.Scope(), assignTo: arjun);
        job = await raising.Assignment.AcceptAsync(raising.Scope(arjun), job.Id, job.Version, default);
        await raising.Work.StartAsync(raising.Scope(arjun), job.Id, default);

        // The worker's own context and clock — the activity runs on Temporal's
        // thread while this test polls, and one DbContext cannot serve both.
        var worker = new JobsHarness(fixture, raising.Clock.GetUtcNow().AddMinutes(31));

        Environment.SetEnvironmentVariable(TemporalConnection.AddressVariable, Address);
        Environment.SetEnvironmentVariable(TemporalConnection.NamespaceVariable, Namespace);
        var client = await Connect();
        var id = $"jobs-concern-sweep-{Guid.NewGuid():n}";

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(worker.Db);
        builder.Services.AddSingleton(worker.Sweep);
        builder.Services.AddSingleton(worker.DayStart);
        builder.Services.AddSingleton(worker.AutoClose);
        builder.Services.AddSingleton(new ServiceIdentity("jobs"));
        IHost? host = null;
        var activities = new ConcernActivities(() => host!.Services);
        builder.Services.AddTemporal(temporal => temporal
            .Workflow<ConcernSweepWorkflow>()
            .Activities(activities)
            .Schedule(id, TimeSpan.FromSeconds(10), nameof(ConcernSweepWorkflow)));
        host = builder.Build();

        await host.StartAsync();
        try
        {
            var description = await client.GetScheduleHandle(id).DescribeAsync();
            Assert.Equal(ScheduleOverlapPolicy.Skip, description.Schedule.Policy.Overlap);
            Assert.Equal(
                nameof(ConcernSweepWorkflow),
                Assert.IsType<ScheduleActionStartWorkflow>(description.Schedule.Action).Workflow);

            // ON_TRACK is on the job from the moment it is raised, so waiting
            // for "a row" would pass before Temporal did anything. The proof is
            // the verdict *changing* — which only a sweep can do.
            string? concern = await Eventually(async () => await raising.Db.ConcernHistory
                .Where(c => c.JobId == job.Id)
                .OrderByDescending(c => c.Since)
                .Select(c => c.Concern)
                .FirstOrDefaultAsync());

            Assert.Equal(Concern.AtRisk, concern);
        }
        finally
        {
            await client.GetScheduleHandle(id).DeleteAsync();
            await host.StopAsync();
            host.Dispose();
        }
    }

    private static async Task<ITemporalClient> Connect()
    {
        try
        {
            return await new TemporalConnection(Address, Namespace).ConnectAsync();
        }
        catch (Exception failure)
        {
            throw new InvalidOperationException(
                $"No Temporal answering at {Address} in namespace {Namespace} — run "
                + "`make temporal-up` in HosPilotOS, or set HOTELOS_TEST_TEMPORAL_ADDRESS. "
                + "27233 is the development convention; 7233 is what an installed server holds.",
                failure);
        }
    }

    /// <summary>Poll until the sweep has moved the job, or say how long it was waited for.</summary>
    private static async Task<string?> Eventually(Func<Task<string?>> read)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var value = await read();
            if (value == Concern.AtRisk) return value;
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException(
            "The job was still ON_TRACK 45 seconds after the schedule was created — the sweep "
            + "did not run. Temporal shows whether the "
            + "workflow started; a workflow that starts and does nothing is the task-queue "
            + "disagreement TemporalWorkerHost.TaskQueueFor exists to prevent.");
    }
}
