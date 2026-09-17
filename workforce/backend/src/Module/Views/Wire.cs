using System.Globalization;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// How a domain value crosses the wire.
/// </summary>
/// <remarks>
/// <para>
/// The mirror of <see cref="Wording"/>, and deliberately not part of it:
/// <c>Wording</c> is how a value is <i>said on a screen</i>, and everything here
/// is how a value is carried so that a screen can decide how to say it. One
/// file whose summary needed "and" joining those two would be two files
/// (ADR 0038).
/// </para>
/// <para>
/// <b>ADR 0175.</b> Four views were formatting clock times themselves, which
/// put a 24-hour cycle and one culture's separator into every property's
/// screens. They send the value now; the surface renders it through
/// <c>formatClock</c>.
/// </para>
/// </remarks>
internal static class Wire
{
    /// <summary>A wall-clock time, as the wire carries it.</summary>
    /// <param name="at">The time.</param>
    /// <returns><c>HH:mm</c>, culture-invariant.</returns>
    /// <remarks>
    /// <para>
    /// <b>Invariant, and that is the half a reviewer misses.</b> <c>HH</c> and
    /// <c>mm</c> are culture-neutral numerics, but the <c>:</c> between them is
    /// <c>CurrentCulture</c>'s time separator — a period in several locales — so
    /// an uninvariant <c>"HH:mm"</c> emits a value the reader cannot parse back.
    /// </para>
    /// <para>
    /// <b>A clock time, not an instant.</b> These have no date and no zone: a
    /// Morning shift starts at 07:00 wherever the property is, and rendering one
    /// through an instant formatter would attach a timezone to something that
    /// never had one. That is why <c>formatClock</c> exists beside
    /// <c>formatInstant</c> rather than the surface reusing the latter.
    /// </para>
    /// </remarks>
    public static string Clock(TimeOnly at)
        => at.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>The same, where the value may be absent.</summary>
    /// <param name="at">The time, or null.</param>
    /// <returns>The clock string, or null — never a placeholder.</returns>
    /// <remarks>
    /// Null rather than an em-dash or an empty string: what absence looks like
    /// is the reader's, and a service that chose a character here would be
    /// deciding it for every property at once.
    /// </remarks>
    public static string? Clock(TimeOnly? at)
        => at is { } value ? Clock(value) : null;
}
