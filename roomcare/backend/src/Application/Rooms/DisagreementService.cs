using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;

namespace HotelOS.RoomCare.Application.Rooms;

/// <summary>A supervisor clears a disagreement — keep ours or take theirs, recorded (S4).</summary>
public sealed class DisagreementService(
    RoomCareDbContext db, Gate gate, ConditionWriter writer, IEventAppender events, TimeProvider clock)
{
    public async Task<RoomState> ClearAsync(
        RequestScope scope, Guid roomId, long expectedVersion, string kept, CancellationToken cancellationToken)
    {
        if (!DisagreementKept.All.Contains(kept))
        {
            throw new InvalidRequestException("a disagreement is cleared by keeping OURS or taking THEIRS");
        }

        await gate.RoomAsync(scope, Permissions.Amend, roomId, cancellationToken);
        var person = Actor.PersonOf(scope, "clearing a disagreement");
        var now = clock.GetUtcNow();
        var room = await writer.RoomAsync(scope.PropertyId, roomId, now, cancellationToken);

        if (room.Version != expectedVersion)
        {
            throw new ConcurrencyException("room", roomId, expectedVersion);
        }

        if (!room.HasDisagreement)
        {
            throw new InvalidRequestException("this room has no disagreement standing");
        }

        var ours = room.Condition;
        var theirs = room.DisagreementObservedCondition!;
        if (kept == DisagreementKept.Theirs)
        {
            writer.Set(scope, room, new ConditionChange(theirs, ConditionSource.Supervisor, Actor.Of(scope), now)
            {
                Reason = $"disagreement cleared — took {room.DisagreementSource?.ToLowerInvariant()}'s word",
            });
        }

        room.DisagreementClearedAt = now;
        room.DisagreementClearedBy = person;
        room.DisagreementClearedKept = kept;
        room.Touch(now);

        events.Append(scope, EventTypes.DisagreementCleared, EventTypes.RoomAggregate, room.RoomId, room.Version,
            new DisagreementAnnouncement
            {
                RoomId = room.RoomId,
                PropertyId = room.PropertyId,
                Ours = ours,
                Theirs = theirs,
                Source = room.DisagreementSource ?? ObservationSource.Pms,
                Kept = kept,
                ById = person,
                OccurredAt = now,
            });

        await db.SaveChangesAsync(cancellationToken);
        return room;
    }
}
