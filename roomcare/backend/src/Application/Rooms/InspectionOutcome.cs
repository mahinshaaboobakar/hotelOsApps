using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Rooms;

/// <summary>Applies an inspection's outcome — passed makes the room inspected, failed makes it dirty with the reason (RC-Q1(6)).</summary>
/// <remarks>
/// The inspection is the inspection application's; Room Care applies what it
/// says. Its outcome event has no name yet, so this is reached through
/// <c>room.inspect</c> — held by that application's inspector — and becomes a
/// consumer the day the subject is named.
/// </remarks>
public sealed class InspectionOutcome(RoomCareDbContext db, IKernelAuthorizer authorizer, TaskWriter writer, ConditionWriter condition)
{
    public async Task<RoomState> ApplyAsync(
        RequestScope scope, Guid taskId, bool passed, string? reason, string? inspectionRef, CancellationToken cancellationToken)
    {
        var task = await writer.RequireAsync(scope, taskId, null, cancellationToken);
        var roomId = task.RoomId ?? throw new InvalidRequestException("an area's routine is not inspected here");
        await authorizer.RequireAsync(scope, Permissions.Inspect, ObjectTypes.Room, roomId, cancellationToken);
        var phase = await db.Phases.FirstOrDefaultAsync(p => p.TaskId == taskId && p.Phase == Phase.Inspect && p.Status == PhaseStatus.Active, cancellationToken)
            ?? throw new InvalidRequestException("this room's service is not waiting for an inspection");

        if (!passed && string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidRequestException("a failed inspection says why, so the attendant knows what to put right");
        }

        phase.Status = passed ? PhaseStatus.Done : PhaseStatus.Failed;
        phase.EndedAt = writer.Now;
        phase.ByUserId = scope.UserId;
        phase.Note = reason;

        var room = await condition.RoomAsync(scope.PropertyId, roomId, writer.Now, cancellationToken);
        condition.Set(scope, room, new ConditionChange(passed ? Condition.Inspected : Condition.Dirty, ConditionSource.Inspection, Actor.Of(scope), writer.Now)
        {
            TaskId = task.Id,
            Day = task.OperatingDay,
            InspectionRef = inspectionRef,
            Reason = passed ? null : $"inspection failed — {reason}",
        });
        writer.Record(scope, task, HistoryKind.Transition, passed ? "inspection passed" : $"inspection failed — {reason}");
        await db.SaveChangesAsync(cancellationToken);
        return room;
    }
}
