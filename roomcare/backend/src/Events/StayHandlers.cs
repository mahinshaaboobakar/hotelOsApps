using System.Text.Json.Serialization;
using HotelOS.Platform;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;

namespace HotelOS.RoomCare.Events;

/// <summary><c>stay.arrived</c> — the room is occupied, as GuestOps saw it.</summary>
public sealed class StayArrivedHandler(RoomCareDbContext db, ObservationService observations, TimeProvider clock)
    : IEventHandler<StayMoved>
{
    public async Task HandleAsync(RequestScope scope, StayMoved payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        if (payload.RoomId is not { } room)
        {
            return;
        }

        await observations.ObserveAsync(scope, new ObservedFact(room, ObservationSource.GuestOps, payload.ArrivalAt ?? envelope.OccurredAt, clock.GetUtcNow())
        {
            EventId = envelope.EventId,
            OperatingDay = Day(payload.BusinessDate),
            Occupancy = Occupancy.Occupied,
            StayStatuses = [StayStatus.CheckedIn],
        }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static DateOnly? Day(string? text) => DateOnly.TryParse(text, out var day) ? day : null;
}

/// <summary><c>stay.departed</c> — vacant, and the property's on-departure condition (S4: "a line on the setup, not a constant").</summary>
public sealed class StayDepartedHandler(RoomCareDbContext db, ObservationService observations, StandardReader standard, TimeProvider clock)
    : IEventHandler<StayMoved>
{
    public async Task HandleAsync(RequestScope scope, StayMoved payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        if (payload.RoomId is not { } room)
        {
            return;
        }

        var policy = await standard.PolicyAsync(scope.PropertyId, cancellationToken);
        await observations.ObserveAsync(scope, new ObservedFact(room, ObservationSource.GuestOps, payload.DepartureAt ?? envelope.OccurredAt, clock.GetUtcNow())
        {
            EventId = envelope.EventId,
            OperatingDay = StayArrivedHandler.Day(payload.BusinessDate),
            Occupancy = Occupancy.Vacant,
            StayStatuses = [StayStatus.CheckedOut],
            Condition = policy.OnDepartureCondition,
        }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary><c>stay.room_changed</c> — the old room is a departure, the new one occupied (S5 c11: the wish travels with the stay).</summary>
public sealed class StayRoomChangedHandler(RoomCareDbContext db, ObservationService observations, StandardReader standard, TimeProvider clock)
    : IEventHandler<StayRoomChanged>
{
    public async Task HandleAsync(RequestScope scope, StayRoomChanged payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var policy = await standard.PolicyAsync(scope.PropertyId, cancellationToken);
        if (payload.FromRoomId is { } from)
        {
            await observations.ObserveAsync(scope, new ObservedFact(from, ObservationSource.GuestOps, envelope.OccurredAt, now)
            {
                EventId = envelope.EventId,
                Occupancy = Occupancy.Vacant,
                StayStatuses = [StayStatus.CheckedOut],
                Condition = policy.OnDepartureCondition,
            }, cancellationToken);
        }

        if (payload.ToRoomId is { } to)
        {
            await observations.ObserveAsync(scope, new ObservedFact(to, ObservationSource.GuestOps, envelope.OccurredAt, now)
            {
                EventId = envelope.EventId,
                Occupancy = Occupancy.Occupied,
                StayStatuses = [StayStatus.CheckedIn],
            }, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary><c>stay.corrected</c> — heard and not applied: it names no room, and a room's facts arrive with the Hub's next observation.</summary>
/// <remarks>
/// Declared because a correction is the desk overriding a lifecycle the PMS
/// sent, and a room-carrying follow-up is what Room Care acts on. Handled as an
/// acknowledgement so the durable consumer does not redeliver it forever.
/// </remarks>
public sealed class StayCorrectedHandler : IEventHandler<StayCorrected>
{
    public Task HandleAsync(RequestScope scope, StayCorrected payload, EventEnvelope envelope, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

/// <summary>The body of <c>stay.arrived</c> and <c>stay.departed</c> as GuestOps writes them.</summary>
public sealed record StayMoved(
    [property: JsonPropertyName("stay_id")] Guid StayId,
    [property: JsonPropertyName("room_id")] Guid? RoomId,
    [property: JsonPropertyName("arrival_at")] DateTimeOffset? ArrivalAt,
    [property: JsonPropertyName("departure_at")] DateTimeOffset? DepartureAt,
    [property: JsonPropertyName("business_date")] string? BusinessDate);

/// <summary>The body of <c>stay.room_changed</c>.</summary>
public sealed record StayRoomChanged(
    [property: JsonPropertyName("stay_id")] Guid StayId,
    [property: JsonPropertyName("from_room_id")] Guid? FromRoomId,
    [property: JsonPropertyName("to_room_id")] Guid? ToRoomId);

/// <summary>The body of <c>stay.corrected</c>.</summary>
public sealed record StayCorrected(
    [property: JsonPropertyName("stay_id")] Guid StayId,
    [property: JsonPropertyName("from")] string? From,
    [property: JsonPropertyName("to")] string? To);
