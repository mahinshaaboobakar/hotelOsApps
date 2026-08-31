namespace PmsOracle.Normalisation;

/// <summary>
/// The property's own time configuration: its zone, and the two clock times a
/// date has to be completed with.
/// </summary>
/// <remarks>
/// <para>
/// A PMS sends dates. A hotel operates on datetimes. Turning
/// <c>2026-08-31</c> into a moment needs the property's check-in or check-out
/// time and the property's zone — neither of which is in the payload, and both
/// of which are Core Administration's configuration (R12).
/// </para>
/// <para>
/// <b>The zone is an IANA identifier and an offset does not satisfy it.</b>
/// This type cannot be constructed from an offset, which is R16 made structural
/// rather than remembered. The reference had a three-argument conversion that
/// fell through to a two-argument overload with <c>Asia/Kolkata</c> hardcoded,
/// so a property with no configured zone produced timestamps wrong by the
/// offset — silently, and while still looking like correct data. One surveyed
/// vendor supplies only a UTC offset, which cannot express daylight saving and
/// is therefore wrong for half the year in any property that observes it.
/// </para>
/// </remarks>
public sealed class PropertyClock
{
    private PropertyClock(TimeZoneInfo zone, TimeOnly checkIn, TimeOnly checkOut)
    {
        Zone = zone;
        CheckIn = checkIn;
        CheckOut = checkOut;
    }

    /// <summary>The property's zone, as an IANA identifier.</summary>
    public TimeZoneInfo Zone { get; }

    /// <summary>The property's check-in time, e.g. 14:00.</summary>
    public TimeOnly CheckIn { get; }

    /// <summary>The property's check-out time, e.g. 12:00.</summary>
    public TimeOnly CheckOut { get; }

    /// <summary>Build a clock from a property's configuration.</summary>
    /// <param name="ianaZone">An IANA zone identifier, e.g. <c>Asia/Kolkata</c>.</param>
    /// <param name="checkIn">The property's check-in time.</param>
    /// <param name="checkOut">The property's check-out time.</param>
    /// <returns>The clock, or <c>null</c> when the zone is not a usable IANA identifier.</returns>
    /// <remarks>
    /// Returns <c>null</c> rather than falling back to UTC or to the machine's
    /// zone. A derivation without a zone must fail, because the alternative is
    /// a timestamp that is plausible and wrong — and nobody re-examines a
    /// plausible timestamp.
    /// </remarks>
    public static PropertyClock? For(string ianaZone, TimeOnly checkIn, TimeOnly checkOut)
    {
        if (string.IsNullOrWhiteSpace(ianaZone))
        {
            return null;
        }

        // An offset such as "+05:30" is not a zone. Rejected explicitly rather
        // than left to the lookup, so the reason is the requirement's reason.
        if (ianaZone[0] is '+' or '-')
        {
            return null;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(ianaZone, out var zone)
            ? new PropertyClock(zone, checkIn, checkOut)
            : null;
    }

    /// <summary>Complete an arrival date with the property's check-in time.</summary>
    /// <param name="date">The date the source supplied.</param>
    /// <returns>The moment, in the property's zone.</returns>
    public DateTimeOffset ArrivalOn(DateOnly date) => At(date, CheckIn);

    /// <summary>Complete a departure date with the property's check-out time.</summary>
    /// <param name="date">The date the source supplied.</param>
    /// <returns>The moment, in the property's zone.</returns>
    public DateTimeOffset DepartureOn(DateOnly date) => At(date, CheckOut);

    private DateTimeOffset At(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);

        // Through the zone's own rules, so the offset is the one in force on
        // that date rather than the one in force today — which is the whole
        // reason an IANA zone is required and an offset refused.
        var offset = Zone.GetUtcOffset(local);

        return new DateTimeOffset(local, offset);
    }
}
