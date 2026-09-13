using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Module;

/// <summary>The house as one screen reads it — rooms, types, zones and memberships, loaded once per call, never kept.</summary>
public sealed class HouseSnapshot
{
    private HouseSnapshot(
        PropertyNow now,
        IReadOnlyList<HouseRoom> rooms,
        IReadOnlyDictionary<Guid, HouseRoomType> types,
        IReadOnlyDictionary<Guid, HouseZone> zones,
        IReadOnlyDictionary<Guid, Guid> membership)
    {
        Now = now;
        Rooms = rooms;
        Types = types;
        Zones = zones;
        Membership = membership;
        ById = rooms.ToDictionary(r => r.Id);
    }

    /// <summary>The words a room with no zone is grouped under — every room is shown, zoned or not.</summary>
    public const string NoZone = "No zone yet";

    public PropertyNow Now { get; }

    public IReadOnlyList<HouseRoom> Rooms { get; }

    public IReadOnlyDictionary<Guid, HouseRoom> ById { get; }

    public IReadOnlyDictionary<Guid, HouseRoomType> Types { get; }

    public IReadOnlyDictionary<Guid, HouseZone> Zones { get; }

    /// <summary>Room → zone, from Room Care's own assignment (ADR 0044).</summary>
    public IReadOnlyDictionary<Guid, Guid> Membership { get; }

    public static async Task<HouseSnapshot> LoadAsync(
        Guid propertyId, IHouse house, PropertyClock clock, RoomCareDbContext db, CancellationToken cancellationToken)
    {
        var now = await clock.AtAsync(propertyId, cancellationToken);
        var rooms = await house.RoomsAsync(propertyId, cancellationToken);
        var types = (await house.RoomTypesAsync(propertyId, cancellationToken)).ToDictionary(t => t.Id);
        var zones = (await house.ZonesAsync(propertyId, cancellationToken)).ToDictionary(z => z.Id);
        var membership = await db.ZoneAssignments
            .Where(z => z.PropertyId == propertyId && z.EffectiveUntil == null)
            .ToDictionaryAsync(z => z.RoomId, z => z.ZoneId, cancellationToken);
        return new HouseSnapshot(now, rooms, types, zones, membership);
    }

    public string Number(Guid? roomId) =>
        roomId is { } id && ById.TryGetValue(id, out var room) ? room.Number : "—";

    public string TypeName(Guid roomId) =>
        ById.TryGetValue(roomId, out var room) && Types.TryGetValue(room.RoomTypeId, out var type) ? type.Name : "—";

    public (Guid? Id, string Name) ZoneOf(Guid roomId) =>
        Membership.TryGetValue(roomId, out var zone) && Zones.TryGetValue(zone, out var found)
            ? (found.Id, found.Name)
            : (null, NoZone);

    /// <summary>An instant as ISO 8601, for the screen's own locale formatting; null stays null.</summary>
    public static string? At(DateTimeOffset? instant) => instant?.ToString("o");
}
