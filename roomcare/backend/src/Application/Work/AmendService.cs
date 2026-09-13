using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;

namespace HotelOS.RoomCare.Application.Work;

/// <summary>Changing what a room's day asks for on the guest's behalf — skip, defer, reduce, re-prioritise (roomcare.amend).</summary>
/// <remarks>
/// Until GuestOps publishes the cleaning wish as a stay fact, the desk's word
/// reaches Room Care here (chapter 03 §3.3). The guest may only reduce the
/// standard, never opt out of a stay (S5 c2): a skip is one task, one window,
/// recorded as <c>SKIPPED_BY_GUEST</c>. Every change keeps its reason.
/// </remarks>
public sealed class AmendService(RoomCareDbContext db, Gate gate, TaskWriter writer, TaskEnding ending)
{
    /// <summary>"No service today" said at the desk — this window's task ends skipped by the guest.</summary>
    public async Task<RoomTask> SkipAsync(RequestScope scope, Guid taskId, long expectedVersion, string reason, CancellationToken cancellationToken)
    {
        var task = await OpenAsync(scope, taskId, expectedVersion, cancellationToken);
        task.Outcome = TaskOutcome.SkippedByGuest;
        if (await writer.RunningAsync(taskId, cancellationToken) is { } running)
        {
            running.Stop(writer.Now, SessionEnd.End);
        }

        writer.Move(scope, task, RoomTaskStatus.Ended, EventTypes.TaskEnded, new TaskNote { Reason = Required(reason) });
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    /// <summary>"Not before 12:00" — the earliest start moves, the priority does not (S5 c3).</summary>
    public async Task<RoomTask> DeferAsync(
        RequestScope scope, Guid taskId, long expectedVersion, DateTimeOffset notBefore, string? reason, CancellationToken cancellationToken)
    {
        var task = await OpenAsync(scope, taskId, expectedVersion, cancellationToken);
        task.EarliestAt = notBefore;
        writer.Record(scope, task, HistoryKind.Reduction, $"not before {notBefore:HH:mm} UTC{Suffix(reason)}");
        writer.Announce(scope, task, EventTypes.TaskReduced, new TaskNote { What = "EARLIEST_AT", Reason = reason });
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    /// <summary>"Light — no bed change" — recorded; the standard is not lowered (S5 c4).</summary>
    public async Task<RoomTask> ReduceAsync(
        RequestScope scope, Guid taskId, long expectedVersion, string what, string? reason, CancellationToken cancellationToken)
    {
        var task = await OpenAsync(scope, taskId, expectedVersion, cancellationToken);
        task.Reduction = Required(what);
        if (task.LinenDue == LinenDue.Due)
        {
            task.LinenDue = LinenDue.NotDue;
        }

        writer.Record(scope, task, HistoryKind.Reduction, $"{what}{Suffix(reason)}");
        writer.Announce(scope, task, EventTypes.TaskReduced, new TaskNote { What = what, Reason = reason });
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    /// <summary>Move a room up or down the day — a supervisor's override, with its reason (S5 c9).</summary>
    public async Task<RoomTask> ReprioritiseAsync(
        RequestScope scope, Guid taskId, long expectedVersion, string band, string reason, CancellationToken cancellationToken)
    {
        if (!PriorityBand.All.Contains(band))
        {
            throw new InvalidRequestException($"'{band}' is not a priority");
        }

        var task = await OpenAsync(scope, taskId, expectedVersion, cancellationToken);
        var from = task.Priority;
        task.Priority = band;
        task.PriorityRank = (PriorityBand.All.ToList().IndexOf(band) * 1000) + (task.PriorityRank % 1000);
        if (task.Status == RoomTaskStatus.PendingPolicy)
        {
            task.Status = RoomTaskStatus.Planned;
        }

        writer.Record(scope, task, HistoryKind.Reprioritised, $"{from.ToLowerInvariant()} → {band.ToLowerInvariant()} — {Required(reason)}");
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    /// <summary>Record what was found at another's room — the supervisor at the door on the attendant's behalf.</summary>
    public async Task<RoomTask> RecordOnBehalfAsync(RequestScope scope, AttemptCommand command, long expectedVersion, CancellationToken cancellationToken)
    {
        var task = await OpenAsync(scope, command.TaskId, expectedVersion, cancellationToken);
        var person = Actor.PersonOf(scope, "recording an exception on a room");
        await ending.RecordAttemptAsync(scope, task, person, command, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    private async Task<RoomTask> OpenAsync(RequestScope scope, Guid taskId, long expectedVersion, CancellationToken cancellationToken)
    {
        var task = await writer.RequireAsync(scope, taskId, expectedVersion, cancellationToken);
        await gate.TaskAsync(scope, Permissions.Amend, taskId, cancellationToken);
        return task.IsOpen ? task : throw new InvalidRequestException("this room's service has already ended");
    }

    private static string Required(string? text) =>
        string.IsNullOrWhiteSpace(text) ? throw new InvalidRequestException("a reason is required, so the record explains itself") : text.Trim();

    private static string Suffix(string? reason) => string.IsNullOrWhiteSpace(reason) ? string.Empty : " — " + reason.Trim();
}
