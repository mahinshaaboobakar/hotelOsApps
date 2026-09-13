using System.Text.Json;
using System.Text.Json.Serialization;
using HotelOS.Platform;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;

namespace HotelOS.RoomCare.Events;

/// <summary><c>room.state_observed</c> — the Hub's observation becomes a row, and the ordering clause applies it (HUB-Q4, S4).</summary>
/// <remarks>
/// <para>
/// The Hub appends a <c>RoomStateFact</c> — the generated protobuf message —
/// through the SDK's appender, so the wire is that message serialised with
/// snake_case names: <c>header</c> and <c>state</c>, timestamps as
/// <c>{seconds, nanos}</c>, enums as their numbers. A test serialises a real
/// <c>RoomStateFact</c> the same way and reads it back through this record.
/// </para>
/// <para>
/// Enum fields are read from either a number or the proto's name, so a Hub that
/// later writes names does not turn every observation into "unspecified".
/// Out of order and out of service are the block's, not a condition Room Care
/// sets; such a fact is recorded with its occupancy and stays, and no condition.
/// </para>
/// </remarks>
public sealed class RoomStateObservedHandler(RoomCareDbContext db, ObservationService observations, TimeProvider clock)
    : IEventHandler<RoomStateObserved>
{
    public async Task HandleAsync(RequestScope scope, RoomStateObserved payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        if (payload.State is not { } state || !Guid.TryParse(state.RoomId, out var roomId))
        {
            return;
        }

        var occurred = payload.Header?.OccurredAt?.At?.ToInstant() ?? envelope.OccurredAt;
        var condition = Wire.Condition(state.Condition) ?? Wire.Condition(state.RoomCareStatus);
        await observations.ObserveAsync(scope, new ObservedFact(roomId, ObservationSource.Pms, occurred, clock.GetUtcNow())
        {
            EventId = envelope.EventId,
            OperatingDay = DateOnly.TryParse(payload.Header?.BusinessDate, out var day) ? day : null,
            Occupancy = Wire.Occupancy(state.Occupancy),
            Condition = condition,
            StayStatuses = state.StayStatuses?.Select(Wire.Stay).OfType<string>().ToList() ?? [],
            SaysNextSold = true,
            NextSoldAt = state.NextSoldAt?.ToInstant(),
            IsPseudoRoom = state.IsPseudoRoom,
        }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>The body of <c>room.state_observed</c> — a <c>RoomStateFact</c> as the appender writes it.</summary>
public sealed record RoomStateObserved(
    [property: JsonPropertyName("header")] FactHeaderWire? Header,
    [property: JsonPropertyName("state")] RoomStateWire? State);

public sealed record FactHeaderWire(
    [property: JsonPropertyName("property_id")] string? PropertyId,
    [property: JsonPropertyName("occurred_at")] FactTimeWire? OccurredAt,
    [property: JsonPropertyName("business_date")] string? BusinessDate);

public sealed record FactTimeWire([property: JsonPropertyName("at")] TimestampWire? At);

public sealed record TimestampWire(
    [property: JsonPropertyName("seconds")] long Seconds,
    [property: JsonPropertyName("nanos")] int Nanos)
{
    public DateTimeOffset ToInstant() => DateTimeOffset.FromUnixTimeSeconds(Seconds).AddTicks(Nanos / 100);
}

public sealed record RoomStateWire(
    [property: JsonPropertyName("room_id")] string? RoomId,
    [property: JsonPropertyName("occupancy")] JsonElement Occupancy,
    [property: JsonPropertyName("condition")] JsonElement Condition,
    [property: JsonPropertyName("room_care_status")] JsonElement RoomCareStatus,
    [property: JsonPropertyName("stay_statuses")] IReadOnlyList<JsonElement>? StayStatuses,
    [property: JsonPropertyName("next_sold_at")] TimestampWire? NextSoldAt,
    [property: JsonPropertyName("is_pseudo_room")] bool? IsPseudoRoom);

/// <summary>The proto enums' numbers and names, mapped onto Room Care's words.</summary>
internal static class Wire
{
    public static string? Condition(JsonElement value) => Number(value, "ROOM_CONDITION_") switch
    {
        1 => Domain.Condition.Dirty,
        2 => Domain.Condition.Clean,
        3 => Domain.Condition.Inspected,
        _ => null,
    };

    public static string? Occupancy(JsonElement value) => Number(value, "OCCUPANCY_") switch
    {
        1 => Domain.Occupancy.Vacant,
        2 => Domain.Occupancy.Occupied,
        _ => null,
    };

    public static string? Stay(JsonElement value) => Number(value, "STAY_LIFECYCLE_") switch
    {
        1 => StayStatus.Booked,
        2 => StayStatus.CheckedIn,
        3 => StayStatus.CheckedOut,
        4 => StayStatus.Cancelled,
        5 => StayStatus.NoShow,
        6 => StayStatus.DueOut,
        7 => StayStatus.Waitlisted,
        8 => StayStatus.Pending,
        _ => null,
    };

    private static readonly Dictionary<string, int> Names = new()
    {
        ["ROOM_CONDITION_DIRTY"] = 1, ["ROOM_CONDITION_CLEAN"] = 2, ["ROOM_CONDITION_INSPECTED"] = 3,
        ["ROOM_CONDITION_OUT_OF_ORDER"] = 4, ["ROOM_CONDITION_OUT_OF_SERVICE"] = 5,
        ["OCCUPANCY_VACANT"] = 1, ["OCCUPANCY_OCCUPIED"] = 2,
        ["STAY_LIFECYCLE_BOOKED"] = 1, ["STAY_LIFECYCLE_CHECKED_IN"] = 2, ["STAY_LIFECYCLE_CHECKED_OUT"] = 3,
        ["STAY_LIFECYCLE_CANCELLED"] = 4, ["STAY_LIFECYCLE_NO_SHOW"] = 5, ["STAY_LIFECYCLE_DUE_OUT"] = 6,
        ["STAY_LIFECYCLE_WAITLISTED"] = 7, ["STAY_LIFECYCLE_PENDING"] = 8,
    };

    private static int Number(JsonElement value, string prefix) => value.ValueKind switch
    {
        JsonValueKind.Number when value.TryGetInt32(out var n) => n,
        JsonValueKind.String when value.GetString() is { } s && Names.TryGetValue(s.StartsWith(prefix) ? s : prefix + s, out var n) => n,
        _ => 0,
    };
}
