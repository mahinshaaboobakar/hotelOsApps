namespace HotelOS.Workforce.Domain;

/// <summary>Which end of a shift a boundary is.</summary>
public enum ShiftBoundaryKind
{
    /// <summary>People came on.</summary>
    Started,

    /// <summary>People went off.</summary>
    Ended,
}

/// <summary>
/// A shift boundary this application has announced — the record that makes a
/// scheduled event exactly-once.
/// </summary>
/// <remarks>
/// <para>
/// <b>The announcement is the fact</b> — ratified 2026-09-04 as the pattern for
/// every scheduled announcement on this platform. The platform's rule is that an
/// event is appended in the transaction of whatever caused it, so a crash cannot
/// keep the change and lose the announcement. <b>A boundary has no such
/// change.</b> Nothing in Workforce happens at 07:00; the rota row was written
/// last week, and the event announces the passage of time.
/// </para>
/// <para>
/// So this row is the change. One transaction inserts it <b>and</b> appends the
/// event. A trigger that fires twice — a retry, a restart, two schedulers —
/// violates the unique index and the whole transaction rolls back, so the second
/// attempt announces nothing. <b>Exactly once, by construction, from an
/// at-least-once trigger</b>, which is what lets the sweep be ordinary rather
/// than exactly-once itself.
/// </para>
/// <para>
/// It also gives the event a well-formed aggregate. <c>AUTHZ-Q20</c>'s rule is
/// <i>announce against what you own</i>, and this is the thing Workforce owns
/// here: unique by construction, version 1 always, so no two announcements can
/// collide on <c>(aggregate, version)</c> — which they would if the aggregate
/// were the catalogue entry, whose version never moves when a shift starts.
/// </para>
/// <para>
/// And it answers a question nothing else can: <i>did we announce Housekeeping's
/// 07:00 boundary, and when?</i>
/// </para>
/// </remarks>
public class ShiftBoundary
{
    /// <summary>This announcement's id — the event's aggregate id.</summary>
    public Guid Id { get; set; }

    /// <summary>The property.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Whose department came on or went off.</summary>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>Which shift — the catalogue entry.</summary>
    public Guid CatalogueEntryId { get; set; }

    /// <summary>
    /// The rota date the cells belong to.
    /// </summary>
    /// <remarks>
    /// <b>Not the calendar date of <see cref="At"/>.</b> A night shift belongs
    /// to the day it starts on, so its end at 07:00 carries yesterday's business
    /// date — which is what makes the key unique for the two boundaries of one
    /// crossing shift rather than colliding with the next morning's.
    /// </remarks>
    public DateOnly BusinessDate { get; set; }

    /// <summary>Which end of the shift.</summary>
    public ShiftBoundaryKind Kind { get; set; }

    /// <summary>The boundary instant itself.</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>
    /// How many people were covered in that department immediately after it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The field that removes a class of bug rather than mitigating it</b>,
    /// and the reason it is here rather than left to the consumer. A department
    /// running Morning 07:00–15:00 and Afternoon 15:00–23:00 emits
    /// <c>shift.ended</c> and <c>shift.started</c> at one instant; a consumer
    /// setting <i>staffed</i> from the verb lands on whichever arrived last, and
    /// the wrong order reads unstaffed all afternoon with nothing looking
    /// broken. Both events carry the same count, so the boolean is right either
    /// way.
    /// </para>
    /// <para>
    /// Only Workforce can compute it: a shift may cross midnight and may be
    /// split, which is <see cref="ShiftCoverage"/>. Publishing raw boundaries
    /// would put that logic in every consumer.
    /// </para>
    /// </remarks>
    public int OnNowAfter { get; set; }

    /// <summary>When the announcement was written.</summary>
    public DateTimeOffset AnnouncedAt { get; set; }
}
