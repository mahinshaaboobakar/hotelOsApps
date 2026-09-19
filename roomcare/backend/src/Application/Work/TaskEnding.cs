using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Application.DeepCleans;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Application.Supervision;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Work;

/// <summary>How a task ends at the door — the attempt row, the outcome, and a done room's condition, in one commit.</summary>
/// <remarks>
/// <para>
/// <b>Done is the condition write</b> (§7.1): the task's end, <c>room.cleaned</c>
/// and the room's version move together, and there is no path that ends a task
/// as done without them. When the task's rule asks for inspection the INSPECT
/// phase opens and <c>roomcare.inspection.requested</c> leaves with a
/// correlation id — the inspection application answers on it (RC-Q1(6)).
/// </para>
/// <para>
/// <b>DND does not end the task</b> — the room stays on the list and is
/// re-checked through the window; at the window's close the last DND stands
/// (S5 c1 way 4). On a room past the threshold, a DND opens the supervisor's
/// lane: the attendant never takes that call again (S5 c9).
/// </para>
/// </remarks>
public sealed class TaskEnding(
    RoomCareDbContext db, TaskWriter writer, ConditionWriter condition, SupervisionLane lane, DeepCleanService deepCleans)
{
    public async Task RecordAttemptAsync(
        RequestScope scope, RoomTask task, Guid person, AttemptCommand command, CancellationToken cancellationToken)
    {
        if (!AttemptFound.All.Contains(command.Found))
        {
            throw new InvalidRequestException("an attempt ends done, partial, declined by the guest, or DND");
        }

        if (command.Found == AttemptFound.Partial && command.PartialDone.Count == 0)
        {
            throw new InvalidRequestException("a partial service says what was done — bathroom, towels, rubbish or bed");
        }

        if (command.PartialDone.FirstOrDefault(p => !PartialPart.All.Contains(p)) is { } bad)
        {
            throw new InvalidRequestException("that is not part of a service");
        }

        db.Attempts.Add(new TaskAttempt
        {
            Id = Guid.CreateVersion7(),
            TaskId = task.Id,
            PropertyId = task.PropertyId,
            At = writer.Now,
            ByUserId = person,
            Found = command.Found,
            PartialDone = command.PartialDone.ToList(),
            Note = command.Note,
        });

        var note = new TaskNote { Found = command.Found, PartialDone = command.PartialDone, Reason = command.Note };
        if (command.Found == AttemptFound.Dnd)
        {
            await StopRunningAsync(task, SessionEnd.Pause, cancellationToken);
            writer.Announce(scope, task, EventTypes.TaskAttempted, note);
            if (task.RoomId is { } room && (await db.RoomStates.FirstOrDefaultAsync(r => r.RoomId == room, cancellationToken))?.SupervisedSince is not null)
            {
                await lane.OpenAsync(scope, room, task.OperatingDay, SupervisionReason.DaysWithoutService, cancellationToken);
            }

            return;
        }

        writer.Announce(scope, task, EventTypes.TaskAttempted, note);
        await EndAsync(scope, task, command, cancellationToken);
    }

    private async Task EndAsync(RequestScope scope, RoomTask task, AttemptCommand command, CancellationToken cancellationToken)
    {
        await StopRunningAsync(task, SessionEnd.End, cancellationToken);
        task.PartialDone = command.PartialDone.ToList();
        task.Outcome = command.Found switch
        {
            AttemptFound.Done when await SupervisorChoseCleanAsync(task, cancellationToken) => TaskOutcome.SupervisorCleaned,
            AttemptFound.Done => TaskOutcome.Done,
            AttemptFound.Partial => TaskOutcome.Partial,
            _ => TaskOutcome.Declined,
        };

        if (command.Found == AttemptFound.Done)
        {
            await PhasesDoneAsync(task, cancellationToken);
            if (task.RoomId is { } roomId)
            {
                await CleanAsync(scope, task, roomId, command.LinenChanged, cancellationToken);
            }
        }

        writer.Move(scope, task, RoomTaskStatus.Ended, EventTypes.TaskEnded, new TaskNote { Found = command.Found, Reason = command.Note });
    }

    private async Task CleanAsync(RequestScope scope, RoomTask task, Guid roomId, bool linenChanged, CancellationToken cancellationToken)
    {
        var room = await condition.RoomAsync(task.PropertyId, roomId, writer.Now, cancellationToken);
        condition.Set(scope, room, new ConditionChange(Condition.Clean, ConditionSource.Attendant, Actor.Of(scope), writer.Now)
        {
            TaskId = task.Id,
            Day = task.OperatingDay,
        });

        if (task.Service == Service.DepartureClean || linenChanged)
        {
            room.LinenLastChangedOn = task.OperatingDay;
        }

        room.DaysWithoutService = 0;
        await deepCleans.RoomReadyAsync(scope, roomId, task.OperatingDay, cancellationToken);
        if (task.InspectionRule != InspectionRule.None
            && await db.Phases.FirstOrDefaultAsync(p => p.TaskId == task.Id && p.Phase == Phase.Inspect, cancellationToken) is { } inspect)
        {
            inspect.Status = PhaseStatus.Active;
            inspect.StartedAt = writer.Now;
            writer.AppendOn(scope, task, EventTypes.InspectionRequested,
                new InspectionRequestedAnnouncement
                {
                    TaskId = task.Id,
                    RoomId = roomId,
                    PropertyId = task.PropertyId,
                    Service = task.Service,
                    ChecklistRef = task.ChecklistRef,
                    CorrelationId = $"inspection:{task.Id}",
                    OccurredAt = writer.Now,
                });
        }
    }

    private async Task PhasesDoneAsync(RoomTask task, CancellationToken cancellationToken)
    {
        var open = await db.Phases
            .Where(p => p.TaskId == task.Id && p.Phase != Phase.Inspect && (p.Status == PhaseStatus.Pending || p.Status == PhaseStatus.Active))
            .ToListAsync(cancellationToken);
        foreach (var phase in open)
        {
            phase.Status = PhaseStatus.Done;
            phase.StartedAt ??= writer.Now;
            phase.EndedAt = writer.Now;
            phase.ByUserId = task.AssignedToUserId;
        }
    }

    private async Task StopRunningAsync(RoomTask task, string reason, CancellationToken cancellationToken)
    {
        if (await writer.RunningAsync(task.Id, cancellationToken) is { } running)
        {
            running.Stop(writer.Now, reason);
        }
    }

    private Task<bool> SupervisorChoseCleanAsync(RoomTask task, CancellationToken cancellationToken) =>
        db.Supervision.AnyAsync(
            s => s.PropertyId == task.PropertyId && s.RoomId == task.RoomId && s.OperatingDay == task.OperatingDay
                && s.Decision == SupervisionDecision.Clean,
            cancellationToken);
}
