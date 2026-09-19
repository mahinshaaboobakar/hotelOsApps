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

    /// <summary>Display names by login, read at answer time; a person with no staff row is absent.</summary>
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}

/// <summary>A property's code, zone and business-day boundary.</summary>
public sealed record PropertyDaySettings(string Code, string Name, string Timezone, TimeOnly Boundary)
{
    /// <summary>
    /// The business day an instant fell on at this property — the same rule as the property's own "today", so a count
    /// of days between the two is a count of the property's days. Never the instant's UTC date, which is a day early or
    /// late for part of every day anywhere but UTC (owner instruction, 2026-09-19; Workforce's b5c5ffc).
    /// <para>
    /// <b>PENDING WF-Q21</b> (with the planner): whether an application's day is the calendar day or the hotel's
    /// operating day. Room Care's chapters make it the operating day (01 §6.1, R12: "the day rolls on the property's
    /// operating day"), and "today" (<c>PropertyNow.Day</c>) and this method both come from
    /// <see cref="Domain.OperatingDay.At"/>. So both sides of every count follow one rule, and a ruling for the
    /// calendar day changes that one function, not this method.
    /// </para>
    /// </summary>
    public DateOnly DayOf(DateTimeOffset instant) => Domain.OperatingDay.At(instant, Timezone, Boundary).Date;
}

/// <summary>A room as a screen and the decision need it — identity only.</summary>
public sealed record HouseRoom(Guid Id, string Number, Guid RoomTypeId, int SortOrder);

public sealed record HouseRoomType(Guid Id, string Code, string Name);

public sealed record HouseZone(Guid Id, string Code, string Name);

public sealed record HouseArea(Guid Id, string Name, string LocationType);
