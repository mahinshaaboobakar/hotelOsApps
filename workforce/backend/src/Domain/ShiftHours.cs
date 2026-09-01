namespace HotelOS.Workforce.Domain;

/// <summary>
/// The hours one shift has had, over one stretch of time.
/// </summary>
/// <remarks>
/// <para>
/// <c>WF-Q15</c>, owner 2026-08-31: <b>an edited definition never rewrites
/// history — effective-forward from a manager-chosen date.</b> A property that
/// moves <i>Morning</i> from 07:00 to 06:30 in November must not turn last
/// March into a rota of 06:30 starts.
/// </para>
/// <para>
/// So a catalogue entry owns a <b>series</b> of these, each with its own window,
/// and an assignment resolves the row in force on its own date. That makes the
/// ruling true by construction rather than by a rule somebody has to remember:
/// there is no code path that could rewrite a past rota, because a past date
/// resolves to the hours that were in force then.
/// </para>
/// <para>
/// <b>Only the hours are versioned, and that is a judgment worth stating.</b>
/// The name, short code and colour change <i>in place</i> on
/// <see cref="ShiftCatalogueEntry"/>. Renaming <i>Morning</i> to <i>AM</i> does
/// not corrupt a past rota — and versioning the name would make one shift appear
/// under two names in one week's history, which is worse than the problem it
/// would solve. What <c>WF-Q15</c> protects is <i>what was worked</i>, and that
/// is the times.
/// </para>
/// </remarks>
public class ShiftHours
{
    /// <summary>This revision's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>The catalogue entry these hours belong to.</summary>
    public Guid CatalogueEntryId { get; set; }

    /// <summary>When the shift starts, or null when it is a non-working entry.</summary>
    /// <remarks>
    /// <c>WF-Q12</c>: <i>Week-off</i> is a rota marker, not a leave type — an
    /// <b>off</b> entry in the catalogue with no request and no balance. An off
    /// shift has no times and counts no hours, and both facts are the same fact:
    /// the times are absent, so <see cref="IsWorking"/> is derived rather than
    /// stored beside them where it could disagree.
    /// </remarks>
    public TimeOnly? StartsAt { get; set; }

    /// <summary>When it ends. Earlier than the start means it crosses midnight.</summary>
    /// <remarks>
    /// <i>Night</i> is 23:00 → 07:00, and that is one span rather than two. The
    /// same reasoning the MOD duty needed, one aggregate over.
    /// </remarks>
    public TimeOnly? EndsAt { get; set; }

    /// <summary>A second span, for a split shift.</summary>
    /// <remarks>
    /// <i>Split — Banquet</i> is 10–14 and 18–22: one shift, two spans, one
    /// person. Two nullable pairs rather than a child table, because a shift has
    /// at most two spans in every hotel this design was walked against, and a
    /// table would invite a third that nobody asked for.
    /// </remarks>
    public TimeOnly? SecondStartsAt { get; set; }

    /// <summary>The second span's end.</summary>
    public TimeOnly? SecondEndsAt { get; set; }

    /// <summary>The first day these hours apply.</summary>
    /// <remarks>The date the manager chose — <c>WF-Q15</c>'s own words.</remarks>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>The last day, or null while these are the current hours.</summary>
    /// <remarks>
    /// Closed by the next revision rather than by a person: editing a shift
    /// creates the successor and ends this one the day before it starts, so the
    /// series can never have a gap or an overlap.
    /// </remarks>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>When this revision was recorded.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Whether these hours are worked at all.</summary>
    /// <remarks>
    /// Derived from the times, never stored. An <i>off</i> entry has none, and a
    /// stored flag beside absent times is a second place for the same fact to be
    /// wrong — the same reason there is no <c>kind</c> column on a capability and
    /// no <c>is_current_mod</c> flag on a duty.
    /// </remarks>
    public bool IsWorking => StartsAt is not null && EndsAt is not null;

    /// <summary>Are these the hours in force on <paramref name="on"/>?</summary>
    /// <param name="on">The day to resolve for.</param>
    /// <returns>Whether this revision covers it.</returns>
    public bool InForceOn(DateOnly on) =>
        on >= EffectiveFrom && (EffectiveTo is null || on <= EffectiveTo);
}
