namespace HotelOS.GuestOps.Domain;

/// <summary>Why a room was assigned.</summary>
public enum AssignmentReason
{
    /// <summary>The first room this stay was given.</summary>
    Initial = 1,

    /// <summary>The guest moved mid-stay — S14.</summary>
    Move = 2,

    /// <summary>A better room, on the same terms — GUEST-Q8 (b).</summary>
    /// <remarks>
    /// Still an <i>assignment</i>. An upgrade becomes an amendment only when the
    /// booked type or the terms themselves change: the test is what changed, not
    /// what the guest got. The rate, the group's expected types and every
    /// availability calculation read the <b>booked</b> type, so treating a
    /// courtesy upgrade as an amendment would quietly rewrite what was sold.
    /// </remarks>
    Upgrade = 3,

    /// <summary>The wrong room was recorded.</summary>
    Correction = 4,
}

/// <summary>The room a stay occupies, over time.</summary>
/// <remarks>
/// <para>
/// <b>A row, not a value</b> — R8. A room change is its own fact and must be
/// distinguishable from an update to the stay; S14 needs <i>both</i> rooms at
/// the moment of the move, because Room Care flips two axes and Jobs has work
/// open against one of them. A single <c>room_id</c> column on the stay would
/// answer <i>where are they</i> and destroy <i>where were they</i>.
/// </para>
/// <para>
/// The system this replaces had four downstream verbs — check-in, check-out,
/// change, update — and the branch distinguishing a change from an update was
/// commented out. Everything then arrived as an update, and housekeeping could
/// not tell a vacated room from a corrected phone number. The answer here is
/// not more verbs: the room lives in its own row, so a move is
/// <b>structurally</b> a different operation and cannot accidentally be
/// published as an amendment.
/// </para>
/// </remarks>
public class Assignment
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }

    /// <summary>Master Data's room. Referenced, never copied.</summary>
    public Guid RoomId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }

    public Guid? AssignedBy { get; set; }

    /// <summary>Null while this is the stay's current room.</summary>
    public DateTimeOffset? ReleasedAt { get; set; }

    public AssignmentReason Reason { get; set; }

    public RoomStay? Stay { get; set; }
}
