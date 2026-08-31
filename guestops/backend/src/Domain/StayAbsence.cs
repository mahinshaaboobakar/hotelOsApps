namespace HotelOS.GuestOps.Domain;

/// <summary>Why a field is missing.</summary>
/// <remarks>
/// Three sentences that a single "incomplete" flag collapses, and they differ
/// in whether anyone should be alerted, whether a connector needs fixing, and
/// whether replaying would help — R26's rejected-versus-superseded distinction
/// losing its neighbour.
/// </remarks>
public enum AbsenceReason
{
    /// <summary>The source sent nothing for it, and that is a fact about the stay.</summary>
    NotSupplied = 1,

    /// <summary>This integration cannot supply it at all — a capability difference.</summary>
    NotAvailableFromSource = 2,

    /// <summary>Present but unreadable. The value travels with the rejection.</summary>
    Unreadable = 3,
}

/// <summary>
/// A field this stay does not have, and why — R25, adopted from the wire.
/// </summary>
/// <remarks>
/// <para>
/// <b>An axis of its own, separate from <see cref="StayLifecycle"/>.</b> R1's
/// lesson applied to the stay: *"checked in, room not yet reported"* (R6, S11)
/// is not a lifecycle state, it is a complete lifecycle with an incomplete
/// record. Collapsing them produces a status vocabulary that grows one value per
/// kind of missing field, and the discarded axis cannot be recovered.
/// </para>
/// <para>
/// <b>Recording the absence is what makes both wrong answers unnecessary.</b>
/// The system this replaces met a stay with no contact detail and did both:
/// dropped it silently on one flavour, and on another fabricated an email from
/// the guest's first name so a mandatory downstream field would accept it.
/// </para>
/// <para>
/// <see cref="RawValue"/> is why the vocabulary can grow deliberately — an
/// unrecognised status names itself here instead of being guessed at years
/// later.
/// </para>
/// </remarks>
public class StayAbsence
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }

    /// <summary>The field, in this application's own terms — <c>assignment</c>, <c>guest.phone</c>.</summary>
    public string Field { get; set; } = string.Empty;

    public AbsenceReason Reason { get; set; }

    /// <summary>What arrived, when the reason is <see cref="AbsenceReason.Unreadable"/>.</summary>
    public string? RawValue { get; set; }

    public DateTimeOffset RecordedAt { get; set; }

    public RoomStay? Stay { get; set; }
}

/// <summary>The field names this application records absences against.</summary>
/// <remarks>
/// Constants rather than literals, because a typo in one is not a compile error
/// and the symptom is an Attention list that quietly misses a row.
/// </remarks>
public static class AbsentFields
{
    /// <summary>No room assigned — the ordinary state of a booking (S8).</summary>
    public const string Assignment = "assignment";

    /// <summary>Nobody named on the stay yet — valid, never a placeholder (S2).</summary>
    public const string Party = "party";

    /// <summary>Checked in or out with no time observed — R7's antecedent (S12).</summary>
    public const string ArrivalTime = "arrival_time";

    /// <summary>No way to reach the guest. A stay with none is valid (R25).</summary>
    public const string Contact = "contact";

    /// <summary>Commercial terms the source did not send.</summary>
    public const string Terms = "terms";
}
