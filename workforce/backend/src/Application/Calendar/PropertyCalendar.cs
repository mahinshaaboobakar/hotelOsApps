using HotelOS.Workforce.Application.Abstractions;

namespace HotelOS.Workforce.Application.Calendar;

/// <summary>
/// Calendar days at the property — the day an instant falls on, and the instant
/// a day or a time of day begins — in the property's own time zone.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every day this application names is a day at the property.</b> The
/// schedule, the rota's ribbon and the duty register turned instants into days
/// with <c>DateOnly.FromDateTime(instant.UtcDateTime)</c>, so at +05:30 a 02:00
/// duty sat on the day before and at a negative offset an evening duty sat on
/// the day after. The zone is Master Data's <c>Property.Timezone</c>, read
/// through <see cref="IStaffDirectory"/>.
/// </para>
/// <para>
/// <b>A zone this server cannot resolve is refused by name, never replaced by
/// UTC.</b> UTC would be a zone nobody chose for this property, and every day
/// computed in it would be a claim about the property nobody made — the read
/// fails instead, saying which zone it could not find.
/// </para>
/// </remarks>
public sealed class PropertyCalendar
{
    private readonly TimeZoneInfo _zone;

    private PropertyCalendar(TimeZoneInfo zone) => _zone = zone;

    /// <summary>The calendar for one property, in its own zone.</summary>
    /// <exception cref="InvalidOperationException">
    /// Master Data has no such property, or its zone is not one this server knows.
    /// </exception>
    public static async Task<PropertyCalendar> ForAsync(
        IStaffDirectory directory, Guid propertyId, CancellationToken cancellationToken)
    {
        var id = await directory.FindPropertyZoneAsync(propertyId, cancellationToken)
            ?? throw new InvalidOperationException(
                "the property has no record in Master Data, so its time zone is unknown");

        return TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)
            ? new PropertyCalendar(zone)
            : throw new InvalidOperationException(
                $"the property's time zone \"{id}\" is not one this server knows");
    }

    /// <summary>The day at the property that this instant falls on.</summary>
    public DateOnly DayOf(DateTimeOffset instant)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _zone).DateTime);

    /// <summary>The instant a time of day happens at the property, on that day — in UTC.</summary>
    /// <remarks>
    /// The offset is the zone's for that local moment, so a day either side of a
    /// daylight-saving change carries its own. **Returned in UTC**: the instant is
    /// the same either way, and Npgsql writes a <c>timestamptz</c> parameter only
    /// at offset zero — the first version returned +05:30 and every query using
    /// it was refused.
    /// </remarks>
    public DateTimeOffset At(DateOnly day, TimeOnly time)
    {
        var local = day.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, _zone.GetUtcOffset(local)).ToUniversalTime();
    }

    /// <summary>The instant the property's day begins — its local midnight.</summary>
    public DateTimeOffset StartOf(DateOnly day) => At(day, TimeOnly.MinValue);
}
