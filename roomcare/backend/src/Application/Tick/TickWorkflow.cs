using Temporalio.Common;
using Temporalio.Workflows;

namespace HotelOS.RoomCare.Application.Tick;

/// <summary>The one Schedule's workflow — every sixty seconds, one activity, overlap skipped (TEMPORAL-Q1, §6.1).</summary>
[Workflow]
public sealed class TickWorkflow
{
    /// <summary>The Schedule's id, one per installation.</summary>
    public const string ScheduleId = "roomcare-tick";

    public static readonly TimeSpan Cadence = TimeSpan.FromSeconds(60);

    /// <summary>A pass that has not finished inside a minute is abandoned; the next tick starts clean.</summary>
    public static readonly TimeSpan Ceiling = Cadence;

    [WorkflowRun]
    public Task RunAsync() =>
        Workflow.ExecuteActivityAsync(
            (TickActivities a) => a.SweepAsync(),
            new ActivityOptions
            {
                StartToCloseTimeout = Ceiling,
                RetryPolicy = new RetryPolicy { MaximumAttempts = 1 },
            });
}
