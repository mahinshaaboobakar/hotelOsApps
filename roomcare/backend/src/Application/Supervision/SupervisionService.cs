using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Supervision;

/// <summary>The supervisor's decision on a room in the lane — final, and theirs on the record (S5 c9).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>DND_APPROVED</c> — no cleaning needed: the day's open tasks on the room end <c>SUPERVISOR_DND_APPROVED</c>.</item>
/// <item><c>CLEAN</c> — the room is to be cleaned: its open task stays open, and ends <c>SUPERVISOR_CLEANED</c> when done.</item>
/// <item><c>OTHER</c> — the supervisor's own call, with a note: the day's open tasks end <c>SUPERVISOR_DECIDED</c>.</item>
/// </list>
/// </remarks>
public sealed class SupervisionService(RoomCareDbContext db, Gate gate, TaskWriter writer, IEventAppender events)
{
    public async Task<RoomSupervision> DecideAsync(
        RequestScope scope, Guid supervisionId, string decision, string? note, CancellationToken cancellationToken)
    {
        if (!SupervisionDecision.All.Contains(decision))
        {
            throw new InvalidRequestException("a supervisor decides DND_APPROVED, CLEAN or OTHER");
        }

        if (decision == SupervisionDecision.Other && string.IsNullOrWhiteSpace(note))
        {
            throw new InvalidRequestException("a decision other than DND approved or clean needs a note saying what was decided");
        }

        var lane = await db.Supervision.FirstOrDefaultAsync(s => s.Id == supervisionId && s.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("room_supervision", supervisionId);
        if (!lane.IsOpen)
        {
            throw new InvalidRequestException("this room's decision was already made, and a supervisor's decision is final");
        }

        await gate.RoomAsync(scope, Permissions.Amend, lane.RoomId, cancellationToken);
        var person = Actor.PersonOf(scope, "a supervisor's decision");
        lane.Decision = decision;
        lane.DecidedByUserId = person;
        lane.DecidedAt = writer.Now;
        lane.Note = note;

        var open = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId && t.RoomId == lane.RoomId && t.OperatingDay == lane.OperatingDay)
            .Where(t => RoomTaskStatus.Open.Contains(t.Status))
            .ToListAsync(cancellationToken);
        foreach (var task in open)
        {
            writer.Record(scope, task, HistoryKind.SupervisorDecision, $"{decision.ToLowerInvariant()}{(note is null ? string.Empty : " — " + note)}");
            if (decision == SupervisionDecision.Clean)
            {
                continue;
            }

            task.Outcome = decision == SupervisionDecision.DndApproved ? TaskOutcome.SupervisorDndApproved : TaskOutcome.SupervisorDecided;
            if (await writer.RunningAsync(task.Id, cancellationToken) is { } running)
            {
                running.Stop(writer.Now, SessionEnd.End);
            }

            writer.Move(scope, task, RoomTaskStatus.Ended, EventTypes.TaskEnded, new TaskNote { Reason = note ?? decision });
        }

        events.Append(scope, EventTypes.SupervisionDecided, EventTypes.SupervisionAggregate, lane.Id, 2, new SupervisionAnnouncement
        {
            SupervisionId = lane.Id,
            RoomId = lane.RoomId,
            PropertyId = lane.PropertyId,
            OperatingDay = lane.OperatingDay.ToString("yyyy-MM-dd"),
            Reason = lane.Reason,
            Decision = decision,
            ById = person,
            OccurredAt = writer.Now,
        });

        await db.SaveChangesAsync(cancellationToken);
        return lane;
    }
}
