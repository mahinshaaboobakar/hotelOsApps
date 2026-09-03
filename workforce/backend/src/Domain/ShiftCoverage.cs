namespace HotelOS.Workforce.Domain;

/// <summary>
/// When a rota cell is actually being worked — the question "who is on now" is
/// made of.
/// </summary>
/// <remarks>
/// <para>
/// <b>It is not <c>start ≤ now &lt; end</c>, and that is the whole reason this
/// exists.</b> Two shapes this application already supports break the naive
/// test, and both are ordinary rather than exotic:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>A shift may cross midnight.</b> Night is written <c>23:00 → 07:00</c> —
/// <see cref="ShiftHours.EndsAt"/> before <see cref="ShiftHours.StartsAt"/> is
/// how the catalogue spells it — so at 06:00 on Tuesday the person working is
/// on <i>Monday's</i> cell. A query over today's assignments alone answers
/// "nobody", every night, for eight hours.
/// </description></item>
/// <item><description>
/// <b>A shift may be split.</b> Split — Banquet is <c>10–14, 18–22</c>: two
/// spans with a gap between them, and somebody is not on shift at 16:00.
/// </description></item>
/// </list>
/// <para>
/// Both fall out of one idea: a span is an interval of <b>minutes from the
/// cell's own midnight</b>, and a crossing span simply ends past 1440. An
/// instant is converted into the same scale, and coverage is containment. No
/// branch anywhere says "if this is a night shift".
/// </para>
/// <para>
/// <b>An override replaces the catalogue's hours entirely</b> — it is a
/// different fact for that day rather than an adjustment to them — which is the
/// same rule <see cref="WorkedHours.Planned"/> applies, stated once in each
/// place it decides something.
/// </para>
/// </remarks>
public static class ShiftCoverage
{
    /// <summary>Minutes in a day, which is the scale everything here uses.</summary>
    private const int Day = 24 * 60;

    /// <summary>One worked interval, in minutes from its cell's midnight.</summary>
    /// <param name="Starts">When it begins.</param>
    /// <param name="Ends">When it finishes — past <c>1440</c> when it crosses midnight.</param>
    public readonly record struct Span(int Starts, int Ends);

    /// <summary>The intervals a cell is worked over, in its own day's minutes.</summary>
    /// <param name="assignment">The rota cell.</param>
    /// <param name="hours">The catalogue hours in force on that date, if any.</param>
    /// <returns>
    /// One span, two for a split shift, or none — an off shift has no times, so
    /// it has nothing to be covered by. That is not a special case; there is
    /// simply nothing to add.
    /// </returns>
    public static IReadOnlyList<Span> Spans(ShiftAssignment assignment, ShiftHours? hours)
    {
        if (assignment is { OverrideStartsAt: { } start, OverrideEndsAt: { } end })
        {
            return [Between(start, end)];
        }

        if (hours is null)
        {
            return [];
        }

        var spans = new List<Span>(2);

        if (hours is { StartsAt: { } first, EndsAt: { } firstEnd })
        {
            spans.Add(Between(first, firstEnd));
        }

        if (hours is { SecondStartsAt: { } second, SecondEndsAt: { } secondEnd })
        {
            spans.Add(Between(second, secondEnd));
        }

        return spans;
    }

    /// <summary>Is this cell being worked at this moment?</summary>
    /// <param name="assignment">The rota cell.</param>
    /// <param name="hours">The catalogue hours in force on the cell's date.</param>
    /// <param name="onDate">The date the question is asked about.</param>
    /// <param name="atTime">The time of day the question is asked about.</param>
    /// <returns>Whether somebody on this cell is on shift then.</returns>
    /// <remarks>
    /// Half-open — <c>[starts, ends)</c>. At exactly 15:00 the afternoon shift
    /// is on and the morning shift is off, which is what a changeover means and
    /// what stops one person being counted in both.
    /// </remarks>
    public static bool Covers(
        ShiftAssignment assignment, ShiftHours? hours, DateOnly onDate, TimeOnly atTime)
    {
        var minute = MinutesFrom(assignment.Date, onDate, atTime);

        return Spans(assignment, hours).Any(span => minute >= span.Starts && minute < span.Ends);
    }

    /// <summary>Every moment this cell starts or finishes somebody's work.</summary>
    /// <param name="assignment">The rota cell.</param>
    /// <param name="hours">The catalogue hours in force on the cell's date.</param>
    /// <returns>
    /// The absolute moments, so a span that crosses midnight reports its end on
    /// the following date rather than as a time that reads as earlier.
    /// </returns>
    public static IEnumerable<DateTime> Boundaries(ShiftAssignment assignment, ShiftHours? hours)
    {
        var midnight = assignment.Date.ToDateTime(TimeOnly.MinValue);

        foreach (var span in Spans(assignment, hours))
        {
            yield return midnight.AddMinutes(span.Starts);
            yield return midnight.AddMinutes(span.Ends);
        }
    }

    /// <summary>The dates whose cells can cover a given date.</summary>
    /// <param name="onDate">The date in question.</param>
    /// <returns>That date, and the one before it.</returns>
    /// <remarks>
    /// <b>The day before is not optional.</b> A night shift belongs to the date
    /// it starts on, so at 06:00 the people working are on yesterday's cells —
    /// a window of one day answers "nobody is on" every night. Stated as a
    /// function rather than left to each caller to remember, because the caller
    /// who forgets gets a plausible answer rather than an error.
    /// </remarks>
    public static IReadOnlyList<DateOnly> DatesCovering(DateOnly onDate) =>
        [onDate.AddDays(-1), onDate];

    /// <summary>A span in minutes from its own day's midnight.</summary>
    /// <remarks>
    /// An end at or before its start has crossed midnight, so it lands in the
    /// next day. Equality counts as crossing for the same reason
    /// <see cref="WorkedHours.Of"/> treats it as zero rather than as a full day:
    /// the catalogue refuses a zero-length shift, so this is the answer for the
    /// case that reaches it anyway — and a span of zero length covers no
    /// instant, which is the honest reading of "these two times are the same".
    /// </remarks>
    private static Span Between(TimeOnly starts, TimeOnly ends)
    {
        var from = (int)starts.ToTimeSpan().TotalMinutes;
        var to = (int)ends.ToTimeSpan().TotalMinutes;

        return new Span(from, to >= from ? to : to + Day);
    }

    /// <summary>An instant, in minutes from a cell's own midnight.</summary>
    private static long MinutesFrom(DateOnly cellDate, DateOnly onDate, TimeOnly atTime) =>
        ((long)onDate.DayNumber - cellDate.DayNumber) * Day
        + (long)atTime.ToTimeSpan().TotalMinutes;
}
