using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Rooms;

/// <summary>The one writer of a room's condition: the row, the version and the event, in the caller's transaction.</summary>
/// <remarks>
/// <para>
/// §7.1 and diagram 42: <i>update, bump version, insert <c>room.cleaned</c>,
/// COMMIT</i>. Nothing here saves — the caller's <c>SaveChangesAsync</c> is the
/// commit that carries the row and the event together, so there is no gap in
/// which one exists without the other.
/// </para>
/// <para>
/// <b>Nothing waits for the PMS.</b> An attendant's done is this call; the PMS
/// is an observation (§2.2).
/// </para>
/// </remarks>
public sealed class ConditionWriter(RoomCareDbContext db, IEventAppender events)
{
    /// <summary>The room's state row, created the first time Room Care hears of the room.</summary>
    /// <remarks>
    /// A room nobody has reported starts <c>DIRTY</c>, set by the system: Room
    /// Care never presents as ready a room nobody has cleaned.
    /// </remarks>
    public async Task<RoomState> RoomAsync(Guid propertyId, Guid roomId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var room = db.RoomStates.Local.FirstOrDefault(r => r.RoomId == roomId)
            ?? await db.RoomStates.FirstOrDefaultAsync(r => r.RoomId == roomId, cancellationToken);

        if (room is not null)
        {
            return room.PropertyId == propertyId
                ? room
                : throw new NotFoundException("room", roomId);
        }

        room = new RoomState
        {
            RoomId = roomId,
            PropertyId = propertyId,
            ConditionSetAt = at,
            CreatedAt = at,
            UpdatedAt = at,
            Version = 0,
        };
        db.RoomStates.Add(room);
        return room;
    }

    /// <summary>Set the condition and announce it; an unchanged condition moves nothing and says nothing.</summary>
    public bool Set(RequestScope scope, RoomState room, ConditionChange change)
    {
        if (!Condition.All.Contains(change.To))
        {
            throw new InvalidRequestException("that is not a condition Room Care sets");
        }

        var from = room.Condition;
        if (from == change.To && room.ConditionSource == change.Source)
        {
            return false;
        }

        room.Condition = change.To;
        room.ConditionSource = change.Source;
        room.ConditionSetAt = change.At;
        room.ConditionSetByKind = change.Actor.Kind;
        room.ConditionSetById = change.Actor.Id;
        room.Touch(change.At);

        if (from == change.To)
        {
            return true;
        }

        var type = change.To switch
        {
            Condition.Clean => EventTypes.RoomCleaned,
            Condition.Inspected => EventTypes.RoomInspected,
            _ => EventTypes.RoomConditionChanged,
        };

        events.Append(scope, type, EventTypes.RoomAggregate, room.RoomId, room.Version, new RoomConditionAnnouncement
        {
            RoomId = room.RoomId,
            PropertyId = room.PropertyId,
            From = from,
            To = change.To,
            Source = change.Source,
            ByKind = change.Actor.Kind,
            ById = change.Actor.Id,
            Via = change.Actor.Via,
            TaskId = change.TaskId,
            OperatingDay = change.Day?.ToString("yyyy-MM-dd"),
            InspectionRef = change.InspectionRef,
            Reason = change.Reason,
            OccurredAt = change.At,
        });
        return true;
    }
}

/// <summary>One change of condition, with everything its announcement carries.</summary>
public sealed record ConditionChange(string To, string Source, Actor Actor, DateTimeOffset At)
{
    public Guid? TaskId { get; init; }

    public DateOnly? Day { get; init; }

    public string? InspectionRef { get; init; }

    public string? Reason { get; init; }
}
