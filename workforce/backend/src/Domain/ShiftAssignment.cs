namespace HotelOS.Workforce.Domain;

/// <summary>
/// One person, one day, one shift.
/// </summary>
/// <remarks>
/// <para>
/// The rota cell. It references the <b>catalogue entry</b> and never a set of
/// hours — that indirection is what makes <c>WF-Q15</c> true by construction:
/// resolving what was worked reads the revision in force on
/// <see cref="Date"/>, so an edit made in November changes nothing about last
/// March.
/// </para>
/// <para>
/// <b>One shift per person per day</b>, enforced by a unique index. A split
/// shift is <i>one</i> catalogue entry with two spans — <i>Split — Banquet</i>
/// is 10–14 and 18–22 — so two assignments on one day would be two shifts, which
/// is a different thing and not one the rota offers.
/// </para>
/// </remarks>
public class ShiftAssignment
{
    /// <summary>This cell's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Master Data's person.</summary>
    public Guid StaffId { get; set; }

    /// <summary>The day, in the property's own calendar.</summary>
    /// <remarks>
    /// A date, not an instant: a rota is drawn in days, and a night shift
    /// belongs to the day it starts. What that day <i>means</i> when hours are
    /// counted is the business day — consumed from the platform, never computed
    /// here (ADR 0128 §6).
    /// </remarks>
    public DateOnly Date { get; set; }

    /// <summary>Which shift from the property's catalogue.</summary>
    public Guid CatalogueEntryId { get; set; }

    /// <summary>Which department this day is worked for.</summary>
    /// <remarks>
    /// Defaults from the person's primary posting and is stored, because a
    /// person with two postings can be rostered to either on a given day and the
    /// rota has to say which. It is the canon code — ADR 0119.
    /// </remarks>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>A one-off start for this day only, or null.</summary>
    /// <remarks>
    /// <para>
    /// <b>Not a copy of the catalogue's hours</b>, and the distinction is the
    /// whole reason this is allowed to exist. A copy would be a derived
    /// projection a client writes — a denormalised value permitted to disagree
    /// with its source. An override is a <i>different fact</i>: this person, this
    /// day, deliberately outside the catalogue entry's hours.
    /// </para>
    /// <para>
    /// Null is the ordinary case, and the resolved hours then come from the
    /// catalogue. Both times are stated or neither is.
    /// </para>
    /// </remarks>
    public TimeOnly? OverrideStartsAt { get; set; }

    /// <summary>The one-off end.</summary>
    public TimeOnly? OverrideEndsAt { get; set; }

    /// <summary>When the cell was filled.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }

    /// <summary>Does this day carry a one-off span?</summary>
    public bool IsOverridden => OverrideStartsAt is not null && OverrideEndsAt is not null;
}
