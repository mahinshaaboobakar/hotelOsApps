namespace HotelOS.GuestOps.Domain;

/// <summary>
/// The property's calendar and wall clock — the only clock a hotel's days mean anything in.
/// </summary>
/// <remarks>
/// <para>
/// <b>ADR 0174: every application works in the property's zone.</b> Two
/// conversions, and each has exactly one right form, kept here so no caller
/// writes its own:
/// </para>
/// <list type="bullet">
/// <item><b>An instant to the property's day.</b> Never the instant's own
/// offset: PostgreSQL returns a <c>timestamptz</c> at offset zero, so a day read
/// straight off a stored instant is UTC's — a Kolkata arrival after midnight the
/// day before, a Guatemala arrival after 18:00 the day after.</item>
/// <item><b>The property's wall-clock time to an instant</b> — a 04:00 roll, an
/// 18:00 cancellation drop. Never at <c>TimeSpan.Zero</c>: 18:00 at the property
/// is not 18:00 UTC.</item>
/// </list>
/// <para>
/// A missing zone is never replaced by UTC. The callers answer <i>unknown</i>
/// instead, because a guessed day looks exactly like a right one.
/// </para>
/// <para>
/// <b>Calendar days, not the operating day.</b> Whether an application's day is
/// the calendar day or the hotel's operating day is WF-Q21, with the planner
/// (2026-09-19). These are calendar conversions; the operating day stays
/// Context's (ADR 0128 §6), and the places that use it are labelled.
/// </para>
/// </remarks>
public static class PropertyClock
{
    /// <summary>The property's calendar day an instant falls on.</summary>
    /// <param name="at">The instant.</param>
    /// <param name="zone">The property's zone.</param>
    /// <returns>The day at the property.</returns>
    public static DateOnly Day(DateTimeOffset at, TimeZoneInfo zone)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(at, zone).DateTime);

    /// <summary>The instant a wall-clock time on a day at the property is.</summary>
    /// <param name="zone">The property's zone.</param>
    /// <param name="date">The day at the property.</param>
    /// <param name="time">The wall-clock time there.</param>
    /// <returns>The instant, carrying the property's offset on that day.</returns>
    public static DateTimeOffset Instant(TimeZoneInfo zone, DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}
