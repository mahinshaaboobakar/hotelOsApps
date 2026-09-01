namespace HotelOS.Workforce.Domain;

/// <summary>
/// How long a span is, when it may cross midnight and may come in two pieces.
/// </summary>
/// <remarks>
/// <para>
/// A calculation rather than a stored number, and the reason is the one this
/// application has now given four times: the answer depends on the hours in
/// force on a date, and a stored total goes stale the moment a shift is
/// rescheduled. Planned hours are computed when asked.
/// </para>
/// <para>
/// It is its own file because it is a <i>rule about time</i> rather than a
/// property of any one aggregate — the rota uses it for planned hours, and
/// attendance will use the same arithmetic for worked ones (slice 5).
/// </para>
/// </remarks>
public static class WorkedHours
{
    /// <summary>The length of one span, in hours.</summary>
    /// <remarks>
    /// <b>An end earlier than the start crosses midnight</b> — <i>Night</i> is
    /// 23:00 → 07:00, which is eight hours and not minus sixteen. That is the
    /// single most likely arithmetic mistake in a rota, and it is made once
    /// here rather than at every call site.
    /// </remarks>
    /// <param name="starts">When it begins.</param>
    /// <param name="ends">When it ends.</param>
    /// <returns>Its length in hours.</returns>
    public static decimal Of(TimeOnly starts, TimeOnly ends)
    {
        var minutes = (ends.ToTimeSpan() - starts.ToTimeSpan()).TotalMinutes;

        if (minutes <= 0)
        {
            minutes += TimeSpan.FromDays(1).TotalMinutes;
        }

        return (decimal)minutes / 60m;
    }

    /// <summary>What one day's assignment is planned to be worth.</summary>
    /// <remarks>
    /// <para>
    /// An override replaces the catalogue's hours entirely — it is a different
    /// fact for that day, not an adjustment to them — so when one is present the
    /// catalogue's spans are not counted at all.
    /// </para>
    /// <para>
    /// An <b>off</b> shift is zero, and that is not a special case: it has no
    /// times, so there is nothing to add.
    /// </para>
    /// </remarks>
    /// <param name="assignment">The rota cell.</param>
    /// <param name="hours">The catalogue hours in force on that date, if any.</param>
    /// <returns>Planned hours for the day.</returns>
    public static decimal Planned(ShiftAssignment assignment, ShiftHours? hours)
    {
        if (assignment is { OverrideStartsAt: { } start, OverrideEndsAt: { } end })
        {
            return Of(start, end);
        }

        if (hours is null)
        {
            return 0m;
        }

        var total = 0m;

        if (hours is { StartsAt: { } first, EndsAt: { } firstEnd })
        {
            total += Of(first, firstEnd);
        }

        if (hours is { SecondStartsAt: { } second, SecondEndsAt: { } secondEnd })
        {
            total += Of(second, secondEnd);
        }

        return total;
    }
}
