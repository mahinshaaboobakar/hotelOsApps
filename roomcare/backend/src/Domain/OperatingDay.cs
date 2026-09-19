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
/// <b>That reason is under review with the planner (WF-Q21, alongside AUTHZ-Q18b), 2026-09-19</b>: GuestOps now calls
/// <c>GetOperatingDay</c>, and ADR 0210 fixed that call, so "an installed application cannot call Context" may no
/// longer hold. Nothing changes until the ruling; do not read this copy as settled.
/// </para>
/// </remarks>
public static class OperatingDay
{
    /// <summary>The business date, and the property-local time it was taken at.</summary>
    /// <remarks>
    /// <b>PENDING WF-Q21</b> (with the planner): calendar day or operating day. This is the one place Room Care
    /// decides; every "today" and every day an instant fell on comes through here. Until the ruling it is the
    /// operating day, as Room Care's chapters specify (01 §6.1, R12). If the ruling is the calendar day, the change
    /// is here: a boundary of 00:00.
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
