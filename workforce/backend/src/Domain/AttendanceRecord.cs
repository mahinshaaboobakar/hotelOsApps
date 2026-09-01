namespace HotelOS.Workforce.Domain;

/// <summary>Where an attendance record came from.</summary>
/// <remarks>
/// <para>
/// <b>The record is source-agnostic and its provenance is mandatory.</b> v1 is
/// manual — <c>WF-Q13</c>: attendance is entered by a supervisor, and the
/// biometric and card devices hotels already own arrive later through the
/// Integration Hub. The shape must not have to change when they do.
/// </para>
/// <para>
/// Which is why this is a column rather than an assumption. A record with no
/// source is one nobody can audit: <i>"was this typed, or did a turnstile say
/// so"</i> is the first question anybody asks about a disputed hour.
/// </para>
/// </remarks>
public enum AttendanceSource
{
    /// <summary>A supervisor typed it. v1's only writer.</summary>
    Manual = 0,

    /// <summary>A device reported it — biometric, card, turnstile.</summary>
    Device = 1,

    /// <summary>A bulk import, from a system being replaced.</summary>
    Import = 2,
}

/// <summary>
/// What one person actually did on one business day.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart to a rota cell: the rota says what was <i>planned</i>, this
/// says what <i>happened</i>. Neither derives from the other, and the difference
/// between them is the day's story — which is why they are two records and not
/// one row with planned and actual columns.
/// </para>
/// <para>
/// <b>Times on a business date, not instants.</b> A supervisor enters <i>"in
/// 07:05, out 15:10"</i>, and a night shift's <i>out</i> at 07:00 belongs to the
/// next calendar morning and the <b>same</b> business day. Storing a time
/// against the business date says exactly that, and lets
/// <see cref="WorkedHours"/> do the arithmetic it already does for the rota —
/// including <c>WF-Q17</c>'s rule that <b>an identical in and out is zero
/// worked</b>, which this aggregate is the reason for.
/// </para>
/// <para>
/// <b>The business date is the platform's</b> — ADR 0128 §6 — consumed, never
/// computed here. A hotel whose day rolls at 03:00 has an attendance record for
/// the day the shift belonged to, not the day the clock happened to show.
/// </para>
/// </remarks>
public class AttendanceRecord
{
    /// <summary>This record's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Whose day.</summary>
    public Guid StaffId { get; set; }

    /// <summary>The business day this belongs to.</summary>
    public DateOnly BusinessDate { get; set; }

    /// <summary>When they arrived, or null when nobody has said.</summary>
    /// <remarks>
    /// Null is a real and common state: a record may exist because somebody was
    /// marked absent, and an <i>in</i> with no <i>out</i> is the ordinary shape
    /// of a shift still being worked.
    /// </remarks>
    public TimeOnly? InAt { get; set; }

    /// <summary>When they left. Earlier than the arrival means the shift crossed midnight.</summary>
    public TimeOnly? OutAt { get; set; }

    /// <summary>Where the record came from.</summary>
    public AttendanceSource Source { get; set; }

    /// <summary>Which account entered it, for a manual record.</summary>
    /// <remarks>
    /// Required when <see cref="Source"/> is <see cref="AttendanceSource.Manual"/>.
    /// The provenance obligation's fourth surface, after the posting, the leave
    /// request and the swap proposal — and the one where it matters most, because
    /// this record is what a wage is eventually computed from.
    /// </remarks>
    public Guid? RecordedByUserId { get; set; }

    /// <summary>What the device or import called this event.</summary>
    /// <remarks>
    /// Required when the source is not manual, and the reason the shape does not
    /// change when devices arrive: a reading that cannot be traced to the machine
    /// that produced it cannot be reconciled when two machines disagree.
    /// </remarks>
    public string? ExternalReference { get; set; }

    /// <summary>Anything the supervisor wants recorded.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>When the record was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }

    /// <summary>Did this person turn up at all?</summary>
    /// <remarks>
    /// Derived from the arrival, never stored. A <c>was_present</c> flag beside a
    /// null arrival is two places for one fact, and one of them would eventually
    /// be wrong.
    /// </remarks>
    public bool Attended => InAt is not null;

    /// <summary>Is the shift still open — arrived, not yet left?</summary>
    public bool StillIn => InAt is not null && OutAt is null;

    /// <summary>How long they worked, or null while the shift is open.</summary>
    /// <remarks>
    /// <b>Computed</b>, through the same arithmetic the rota plans with — so a
    /// night shift is eight hours rather than minus sixteen, and an identical in
    /// and out is zero rather than a full day behind a typo.
    /// </remarks>
    public decimal? Worked =>
        InAt is { } arrived && OutAt is { } left ? WorkedHours.Of(arrived, left) : null;
}
