using Temporalio.Common;
using Temporalio.Workflows;

namespace HotelOS.Jobs.Application.Concerns;

/// <summary>What the minute Schedule starts — <c>TEMPORAL-Q1</c>, page 62a.</summary>
/// <remarks>
/// <para>
/// It does one thing: call the activity. <b>Workflow code is replayed</b>, so it
/// may not touch a database, a clock or a random number; all of that is in the
/// activity, which is ordinary code.
/// </para>
/// <para>
/// <b>No retry.</b> The sweep is idempotent and runs again in sixty seconds, so
/// the next tick is the retry; a retry policy here would stack attempts against
/// a property that is already failing.
/// </para>
/// </remarks>
[Workflow]
public sealed class ConcernSweepWorkflow
{
    /// <summary>The schedule that starts it — one id, named once.</summary>
    /// <remarks>
    /// Here rather than at the wiring so the test that fires a schedule live and
    /// the line that declares the real one cannot come to disagree about which
    /// workflow the property is running.
    /// </remarks>
    public const string ScheduleId = "jobs-concern-sweep";

    /// <summary>The ceiling on one tick — deliberately the cadence itself.</summary>
    /// <remarks>
    /// <para>
    /// The timeout unsticks a hung tick; it does not police the sweep's speed.
    /// The frame-beside-capture audit measured the sweep in <b>milliseconds at a
    /// thousand jobs</b>, so a minute is three orders of magnitude of headroom
    /// and nothing healthy will ever reach it.
    /// </para>
    /// <para>
    /// A minute rather than page 62a's suggested five, because overlap SKIP
    /// means a running tick suppresses the ones behind it: the timeout is the
    /// ceiling on how long escalation can be <i>silently</i> absent. At five
    /// minutes a hang hides four missed sweeps behind one workflow that still
    /// looks alive; at one it fails loudly every minute in Temporal's history,
    /// which is the visibility this migration was for. A tick that has not
    /// finished by the time the next is due has already missed its slot.
    /// </para>
    /// </remarks>
    public static readonly TimeSpan Ceiling = TimeSpan.FromMinutes(1);

    /// <summary>Run one tick.</summary>
    [WorkflowRun]
    public Task RunAsync() =>
        Workflow.ExecuteActivityAsync(
            (ConcernActivities a) => a.SweepAsync(),
            new ActivityOptions
            {
                StartToCloseTimeout = Ceiling,
                RetryPolicy = new RetryPolicy { MaximumAttempts = 1 },
            });
}
