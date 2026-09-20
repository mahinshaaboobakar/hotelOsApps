namespace HotelOS.RoomCare.Domain;

/// <summary>A property's business date at an instant — Context's own rule, over the same two Master Data columns.</summary>
/// <remarks>
/// <para>
/// <b>Why it is computed here and not asked.</b> Chapter 03 §6.1 reads
/// <i>"Context.GetOperatingDay — never computed here"</i>. An installed
/// application cannot call Context: it presents a certificate and no access
/// token, and a platform service admits only its own kind (ADR 0093 §PKG-Q8;
/// Workforce and Jobs were refused on the wire). The ruled read path is the
/// install grant, ADR 0092 §4. So Room Care reads <c>masterdata.properties</c>'
/// <c>timezone</c> and <c>business_day_boundary</c> — exactly the two inputs
/// Context's <c>OperatingDay.From</c> reads — and applies the same rule. The
/// §7.3 guarantee holds unchanged: there is no UTC hour anywhere in Room Care,
/// only a property's own setting.
/// </para>
/// <para>
/// <b>That reason is now stale, and this derivation is owed for retirement — ADR 0211 (WF-Q21), 2026-09-19.</b> The
/// day is the property's operating day, as chapter 01 §6.1's R12 says; what changes is who derives it. ADR 0210
/// makes <c>GetOperatingDay</c> platform/service-scoped with an application as a legitimate caller, so PKG-Q8's
/// refusal no longer justifies a local copy, and Room Care is to consume Context's value instead of computing it.
/// </para>
/// <para>
/// <b>Kept for 0.1.4, retired in 0.1.5</b>, on the architect's sequencing: nothing is deleted until FF proves the
/// Context call live. A cut waiting on a live proof is worse than a cut that names the work as owed. When the call
/// lands it sends <b>no property id</b> — Context binds the property to the caller (AUTHZ-Q18b) — and a day it
/// cannot read is said as unread, never a fallback day, exactly as <see cref="Zone"/> refuses a property with no
/// zone today.
/// </para>
/// </remarks>
public static class OperatingDay
{
    /// <summary>The business date, and the property-local time it was taken at.</summary>
    /// <remarks>
    /// <b>The operating day — ADR 0211 (WF-Q21), ruled 2026-09-19.</b> This is the one place Room Care derives it;
    /// every "today" and every day an instant fell on comes through here. That is also what makes the retirement
    /// above one change: Context's value replaces this body in 0.1.5, and every caller already reads the day from
    /// here.
    /// </remarks>
    public static (DateOnly Date, DateTime Local) At(DateTimeOffset instant, string timezone, TimeOnly boundary)
    {
        var zone = Zone(timezone);
        var local = TimeZoneInfo.ConvertTime(instant, zone).DateTime;
        var date = DateOnly.FromDateTime(local);
        return (TimeOnly.FromDateTime(local) < boundary ? date.AddDays(-1) : date, local);
    }

    /// <summary>The instant a property-local date and time falls on.</summary>
    public static DateTimeOffset Instant(DateOnly date, TimeOnly time, string timezone)
    {
        var zone = Zone(timezone);
        var local = date.ToDateTime(time);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    /// <summary>The zone, by IANA name or the host's own id — a property with none has no business date.</summary>
    public static TimeZoneInfo Zone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
        {
            throw new InvalidOperationException(
                "the property has no time zone, and a business date cannot be derived without one");
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out var zone))
        {
            return zone;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timezone, out var windows)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windows, out zone))
        {
            return zone;
        }

        throw new InvalidOperationException(
            $"the property's time zone '{timezone}' is not a zone this host recognises");
    }
}
