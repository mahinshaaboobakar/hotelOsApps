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

    /// <summary>A calendar day, as the wire carries it.</summary>
    /// <param name="on">The day.</param>
    /// <returns><c>yyyy-MM-dd</c>, culture-invariant.</returns>
    /// <remarks>
    /// The parts are numeric and the separators are literals, so this is
    /// invariant with or without the culture - which is exactly why it is
    /// stated: the next reader should not have to work that out, and the
    /// neighbouring <c>Clock</c> is NOT invariant without it.
    /// </remarks>
    public static string Day(DateOnly on)
        => on.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Two ends of a span, carried rather than joined.</summary>
    /// <param name="from">The start.</param>
    /// <param name="to">The end.</param>
    /// <returns>The pair, or null when either end is absent.</returns>
    /// <remarks>
    /// <para>
    /// <b>The separator is the reader's.</b> Four views joined these with an
    /// en-dash - one of them with spaces around it and three without - so the
    /// character, the spacing and the order were decided in a service, in one
    /// culture, and a screen could not have said it any other way.
    /// </para>
    /// <para>
    /// Null when either end is missing rather than a half-span: a range with one
    /// end is not a shorter range, and a surface handed one would have to invent
    /// what the other end means.
    /// </para>
    /// </remarks>
    public static object? Span(TimeOnly? from, TimeOnly? to)
        => from is { } start && to is { } end
            ? new { from = Clock(start), to = Clock(end) }
            : null;

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
