using System.Globalization;

namespace HotelOS.GuestOps.Application.Abstractions;

/// <summary>
/// Reading a day or a time off the wire, in the one form the wire has.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every wire value this application reads is ISO-8601, and until 2026-09-23
/// nothing said so in code.</b> Thirteen call sites used
/// <c>DateOnly.TryParse</c> and <c>TimeOnly.TryParse</c>, which parse under
/// <see cref="CultureInfo.CurrentCulture"/> — so what a value meant depended on
/// the locale of the machine the service happened to be running on.
/// </para>
/// <para>
/// <b>It is not leniency; it is a silent reinterpretation.</b> <c>03/04/2026</c>
/// is the third of April on one server and the fourth of March on another, and
/// nothing downstream can tell which it was. A registration card's date of
/// birth, a PMS feed's business date and a cancellation's drop time all came
/// through those calls. The test that found it sent <c>14/03/86</c> to a card
/// expecting it to be refused, and the service stored 14 March 1986 — a date
/// nobody typed, from a format the contract does not have.
/// </para>
/// <para>
/// <b>Exact, and invariant.</b> The format is the one
/// <c>RegistrationRule.ValueOf</c> emits and the one every proto field is
/// documented as; anything else is not a date this application can read, and
/// the caller is told so rather than given a guess. That is ADR 0227's rule —
/// <i>decode where the format is known, refuse where it is not, never default</i>
/// — applied to the wire instead of to a spreadsheet.
/// </para>
/// <para>
/// <b>Null, not an exception.</b> Some callers refuse an unreadable value and
/// some record its absence, and which of the two is right is the caller's
/// decision every time. Throwing here would take it away from them.
/// </para>
/// </remarks>
public static class Iso
{
    /// <summary>The day the wire sent, or null where it did not send one.</summary>
    /// <param name="value">An ISO day — <c>2026-09-23</c>.</param>
    /// <returns>The day, or null for anything else.</returns>
    public static DateOnly? Day(string? value)
        => DateOnly.TryParseExact(
            value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? day
            : null;

    /// <summary>The time of day the wire sent, or null where it did not send one.</summary>
    /// <param name="value">An ISO time — <c>18:00</c> or <c>18:00:00</c>.</param>
    /// <returns>The time, or null for anything else.</returns>
    /// <remarks>
    /// <b>Two forms, because the platform sends both.</b> A property's roll
    /// boundary is configured to the minute and a vendor's drop time often
    /// carries seconds; neither is ambiguous, and both are exact.
    /// </remarks>
    public static TimeOnly? Time(string? value)
        => TimeOnly.TryParseExact(
            value,
            ["HH:mm", "HH:mm:ss"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var time)
            ? time
            : null;
}
