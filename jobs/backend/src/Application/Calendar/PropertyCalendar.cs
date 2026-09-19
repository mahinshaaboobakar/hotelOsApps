using HotelOS.Jobs.Application.Abstractions;

namespace HotelOS.Jobs.Application.Calendar;

/// <summary>
/// Calendar days and wall-clock times at the property — the day an instant falls
/// on, its time of day there, and the instant a day or a time of day begins — in
/// the property's own time zone.
/// </summary>
/// <remarks>
/// <para>
/// <b>GG's Workforce <c>PropertyCalendar</c> (<c>b5c5ffc</c>) is the reference,
/// and this follows it</b> (ADR 0174; the owner, 2026-09-19: every app works in
/// the property's timezone). Jobs took "today" as the UTC day for the AUTO pick,
/// read service hours on the UTC clock, started a scheduled job's promise at UTC
/// midnight, and counted "closed today" as the last 24 hours.
/// <c>ZoneSourceGuardTests</c> refuses the two call shapes that did it.
/// </para>
/// <para>
/// <b>A zone this server cannot resolve is refused by name, never replaced by
/// UTC</b>, as the reference does: UTC is a zone nobody chose for this property,
/// and a day computed in it is a claim about the property nobody made. Jobs had
/// two such fallbacks.
/// </para>
/// <para>
/// <b>"Today" is the calendar day, pending WF-Q21</b> — the planner is ruling
/// whether an application's day is the calendar day or the hotel's operating
/// day. Built to the calendar day, as directed.
/// </para>
/// </remarks>
public sealed class PropertyCalendar
{
    private readonly TimeZoneInfo _zone;

    private PropertyCalendar(TimeZoneInfo zone) => _zone = zone;

    /// <summary>The calendar for one property, in its own zone.</summary>
    /// <exception cref="InvalidOperationException">Master Data has no zone for it, or its zone is not one this server knows.</exception>
    public static async Task<PropertyCalendar> ForAsync(
        IPropertyDirectory directory, Guid propertyId, CancellationToken cancellationToken)
    {
        var id = await directory.FindTimezoneAsync(propertyId, cancellationToken)
            ?? throw new InvalidOperationException("the property has no time zone in Master Data, so its day is unknown");

        return Resolve(id) is { } zone
            ? new PropertyCalendar(zone)
            : throw new InvalidOperationException($"the property's time zone \"{id}\" is not one this server knows");
    }

    /// <summary>The day at the property that this instant falls on — "today", for now (calendar day, pending WF-Q21).</summary>
    public DateOnly DayOf(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _zone).DateTime);

    /// <summary>The time on the property's clocks at this instant — what service hours are written in.</summary>
    public TimeOnly TimeOf(DateTimeOffset instant) =>
        TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _zone).DateTime);

    /// <summary>The instant a time of day happens at the property, on that day — in UTC.</summary>
    /// <remarks>
    /// The offset is the zone's for that local moment, so each side of a
    /// daylight-saving change carries its own. Returned in UTC, because Npgsql
    /// writes a <c>timestamptz</c> parameter only at offset zero (the reference's
    /// own finding).
    /// </remarks>
    public DateTimeOffset At(DateOnly day, TimeOnly time)
    {
        var local = day.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, _zone.GetUtcOffset(local)).ToUniversalTime();
    }

    /// <summary>The instant the property's day begins — its local midnight.</summary>
    public DateTimeOffset StartOf(DateOnly day) => At(day, TimeOnly.MinValue);

    /// <summary>The instant today began at the property.</summary>
    public DateTimeOffset TodayStartedAt(DateTimeOffset now) => StartOf(DayOf(now));

    /// <summary>IANA or Windows, as Master Data may hold either on a Windows host.</summary>
    private static TimeZoneInfo? Resolve(string id)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
        return TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windows)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windows, out zone) ? zone : null;
    }
}
