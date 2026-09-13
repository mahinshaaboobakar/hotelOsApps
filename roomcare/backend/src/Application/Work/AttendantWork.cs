using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Work;

/// <summary>The attendant at the door — start, pause, resume, and what they found (S5 c1).</summary>
/// <remarks>
/// Every act here rides the assignment: the caller must be the task's current
/// assignee, and nothing else is asked (the registry header; Jobs' work-session
/// verbs are the precedent). The time accumulates across pauses as separate
/// stretches (survey F24).
/// </remarks>
public sealed class AttendantWork(RoomCareDbContext db, TaskWriter writer, TaskEnding ending)
{
    public async Task<RoomTask> StartAsync(RequestScope scope, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await MineAsync(scope, taskId, cancellationToken);
        if (await writer.RunningAsync(taskId, cancellationToken) is not null)
        {
            return task;
        }

        if (task.Status is not (RoomTaskStatus.Assigned or RoomTaskStatus.InProgress))
        {
            throw new InvalidRequestException("only an assigned room can be started");
        }

        if (task.EarliestAt is { } earliest && writer.Now < earliest)
        {
            throw new InvalidRequestException($"the guest asked for this room not before {earliest:HH:mm} UTC");
        }

        db.WorkSessions.Add(new TaskWorkSession
        {
            Id = Guid.CreateVersion7(),
            TaskId = task.Id,
            PropertyId = task.PropertyId,
            UserId = task.AssignedToUserId!.Value,
            StartedAt = writer.Now,
        });

        var phase = await db.Phases.Where(p => p.TaskId == taskId && p.Status == PhaseStatus.Pending)
            .OrderBy(p => p.Sequence).FirstOrDefaultAsync(cancellationToken);
        if (phase is not null && !await db.Phases.AnyAsync(p => p.TaskId == taskId && p.Status == PhaseStatus.Active, cancellationToken))
        {
            phase.Status = PhaseStatus.Active;
            phase.StartedAt = writer.Now;
            phase.ByUserId = task.AssignedToUserId;
        }

        if (task.Status == RoomTaskStatus.InProgress)
        {
            writer.Record(scope, task, HistoryKind.Transition, "resumed");
        }
        else
        {
            writer.Move(scope, task, RoomTaskStatus.InProgress, EventTypes.TaskStarted, TaskNote.Empty);
        }

        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<RoomTask> PauseAsync(RequestScope scope, Guid taskId, string? reason, CancellationToken cancellationToken)
    {
        var task = await MineAsync(scope, taskId, cancellationToken);
        var running = await writer.RunningAsync(taskId, cancellationToken)
            ?? throw new InvalidRequestException("this room is not being worked right now");
        running.Stop(writer.Now, SessionEnd.Pause);
        writer.Record(scope, task, HistoryKind.Transition, reason is null ? "paused" : $"paused — {reason}");
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    /// <summary>What the attendant found — done, partial, declined, or the DND board.</summary>
    public async Task<RoomTask> AttemptAsync(RequestScope scope, AttemptCommand command, CancellationToken cancellationToken)
    {
        var task = await MineAsync(scope, command.TaskId, cancellationToken);
        var person = task.AssignedToUserId!.Value;
        await ending.RecordAttemptAsync(scope, task, person, command, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    /// <summary>More time than the standard allows — recorded, never silent.</summary>
    public async Task<RoomTask> AskExtraTimeAsync(RequestScope scope, Guid taskId, int minutes, string? reason, CancellationToken cancellationToken)
    {
        if (minutes is < 1 or > 240)
        {
            throw new InvalidRequestException("extra time is between 1 and 240 minutes");
        }

        var task = await MineAsync(scope, taskId, cancellationToken);
        task.ExtraMinutes += minutes;
        writer.Record(scope, task, HistoryKind.ExtraTime, $"+{minutes} min{(reason is null ? string.Empty : " — " + reason)}");
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    private async Task<RoomTask> MineAsync(RequestScope scope, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await writer.RequireAsync(scope, taskId, null, cancellationToken);
        Gate.Assignee(scope, task);
        if (!task.IsOpen)
        {
            throw new InvalidRequestException("this room's service has already ended");
        }

        return task;
    }
}

/// <summary>An attempt at the door: what was found, what a partial service did, and a note.</summary>
public sealed record AttemptCommand(Guid TaskId, string Found)
{
    public IReadOnlyList<string> PartialDone { get; init; } = [];

    public string? Note { get; init; }

    /// <summary>Whether a daily service changed the linen — resets the room's linen date (S5 c11).</summary>
    public bool LinenChanged { get; init; }
}
