namespace HotelOS.RoomCare.Application.Abstractions;

/// <summary>What Room Care reads about the property from Master Data — rooms, types, zones, areas, names, the day.</summary>
/// <remarks>
/// A port, so the decision and the projections never name a table they do not
/// own. The adapter reads through the install grant (ADR 0092 §4); a test
/// supplies a double. Nothing read here is stored.
/// </remarks>
public interface IHouse
{
    /// <summary>The property's zone and day boundary; null when Master Data has no such property.</summary>
    Task<PropertyDaySettings?> DaySettingsAsync(Guid propertyId, CancellationToken cancellationToken);

    /// <summary>Every active room, in Master Data's order.</summary>
    Task<IReadOnlyList<HouseRoom>> RoomsAsync(Guid propertyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<HouseRoomType>> RoomTypesAsync(Guid propertyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<HouseZone>> ZonesAsync(Guid propertyId, CancellationToken cancellationToken);

    /// <summary>The public-area nodes of the location tree (S3).</summary>
    Task<IReadOnlyList<HouseArea>> AreasAsync(Guid propertyId, CancellationToken cancellationToken);

    Task<Guid?> DepartmentIdAsync(Guid propertyId, string departmentCode, CancellationToken cancellationToken);

    /// <summary>Display names by login, read at answer time; a person with no staff row is absent.</summary>
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}

/// <summary>A property's code, zone and business-day boundary.</summary>
public sealed record PropertyDaySettings(string Code, string Name, string Timezone, TimeOnly Boundary);

/// <summary>A room as a screen and the decision need it — identity only.</summary>
public sealed record HouseRoom(Guid Id, string Number, Guid RoomTypeId, int SortOrder);

public sealed record HouseRoomType(Guid Id, string Code, string Name);

public sealed record HouseZone(Guid Id, string Code, string Name);

public sealed record HouseArea(Guid Id, string Name, string LocationType);
