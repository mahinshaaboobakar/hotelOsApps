using HotelOS.Platform;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Supervision;

/// <summary>Puts a room in the supervisor's lane for a day — once per reason, announced (chapter 03 §2.5).</summary>
public sealed class SupervisionLane(RoomCareDbContext db, IEventAppender events, TimeProvider clock)
{
    /// <summary>Open the lane for a room; a room already in it for that reason and day is left as it is.</summary>
    public async Task<RoomSupervision> OpenAsync(
        RequestScope scope, Guid roomId, DateOnly day, string reason, CancellationToken cancellationToken, int? days = null)
    {
        var standing = db.Supervision.Local.FirstOrDefault(s =>
                s.PropertyId == scope.PropertyId && s.RoomId == roomId && s.OperatingDay == day && s.Reason == reason)
            ?? await db.Supervision.FirstOrDefaultAsync(
                s => s.PropertyId == scope.PropertyId && s.RoomId == roomId && s.OperatingDay == day && s.Reason == reason,
                cancellationToken);
        if (standing is not null)
        {
            return standing;
        }

        var now = clock.GetUtcNow();
        var opened = new RoomSupervision
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            RoomId = roomId,
            OperatingDay = day,
            Reason = reason,
            OpenedAt = now,
        };
        db.Supervision.Add(opened);
        events.Append(scope, EventTypes.SupervisionOpened, EventTypes.SupervisionAggregate, opened.Id, 1, new SupervisionAnnouncement
        {
            SupervisionId = opened.Id,
            RoomId = roomId,
            PropertyId = scope.PropertyId,
            OperatingDay = day.ToString("yyyy-MM-dd"),
            Reason = reason,
            Days = days,
            OccurredAt = now,
        });
        return opened;
    }

    /// <summary>Whether the room waits on a supervisor's decision that day — the window does not close on it (S5 c9).</summary>
    public Task<bool> AwaitingAsync(Guid propertyId, Guid roomId, DateOnly day, CancellationToken cancellationToken) =>
        db.Supervision.AnyAsync(
            s => s.PropertyId == propertyId && s.RoomId == roomId && s.OperatingDay == day
                && s.Reason == SupervisionReason.DaysWithoutService && s.Decision == null,
            cancellationToken);
}
