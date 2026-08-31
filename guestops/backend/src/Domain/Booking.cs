namespace HotelOS.GuestOps.Domain;

/// <summary>How a record came to exist here.</summary>
public enum RecordOrigin
{
    /// <summary>Somebody in the property entered it.</summary>
    Staff = 1,

    /// <summary>It arrived from a PMS through the Integration Hub.</summary>
    Pms = 2,
}

/// <summary>
/// The group — what the guest thinks they made.
/// </summary>
/// <remarks>
/// <para>
/// GUEST-Q2: a <i>booking</i> is the group and the <b>room-stay is the
/// anchor</b>. Every operation happens to a stay, never to the group — so this
/// carries identity, the expectation, and nothing operational. There is no
/// group check-in and no group cancellation; cancelling a booking is <i>n</i>
/// stay cancellations, said out loud.
/// </para>
/// <para>
/// <b>A group that spans properties is not modelled here</b> (S4, S32). This
/// installation holds its own legs; the source identifiers are what make the
/// onward ones <i>sayable</i> without being queryable, and no
/// cross-installation read exists.
/// </para>
/// </remarks>
public class Booking
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>How many stays the source says this booking has — R9.</summary>
    /// <remarks>
    /// Nullable because a source may not say, and *"three expected, one known"*
    /// and *"one known, expectation unstated"* are different states. Collapsing
    /// them loses S30's whole point: a group page that cannot say what it is
    /// waiting for.
    /// </remarks>
    public int? ExpectedStayCount { get; set; }

    /// <summary>Whether the source says it has sent the whole group.</summary>
    /// <remarks>
    /// <b>Carried, not computed</b> — GUEST-Q9's M5. A source that says *"this
    /// group is complete"* knows something we cannot: our own
    /// <see cref="ExpectedStayCount"/> against the stays we hold answers a
    /// different question — how much of what was promised has arrived. Both are
    /// kept because the group page needs both sentences.
    /// </remarks>
    public bool? IsComplete { get; set; }

    public RecordOrigin Origin { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public long Version { get; set; }

    public ICollection<RoomStay> Stays { get; set; } = [];

    public ICollection<BookingExternalRef> ExternalRefs { get; set; } = [];
}

/// <summary>One identifier as a source knows this booking.</summary>
/// <remarks>
/// <para>
/// Several, not one — R10. An OHIP reservation arrives with a
/// <c>reservationIdList[]</c> of <c>{id, type}</c> pairs: <i>there is not "the
/// reservation id"</i>, and a single column would silently pick a winner.
/// </para>
/// <para>
/// <b>Minted with the booking, in one transaction</b> — GUEST-Q8. The id and
/// its references are born together, so there is never a moment needing a
/// pre-existing canonical id to map to. A crash between the two would leave a
/// booking nothing could ever match again, and the next inbound fact would
/// create a duplicate.
/// </para>
/// </remarks>
public class BookingExternalRef
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    /// <summary>The registered connector — <c>oracle-cloud</c>, never <c>oracle</c>.</summary>
    public string IntegrationId { get; set; } = string.Empty;

    /// <summary>What kind of identifier this is, in the source's own terms.</summary>
    /// <remarks>
    /// Connector-declared and deliberately a string — <c>CONN-Q8</c>. A closed
    /// enum would be HotelOS inventing a vocabulary for systems it does not own.
    /// </remarks>
    public string IdentifierKind { get; set; } = string.Empty;

    public string ExternalId { get; set; } = string.Empty;

    public Booking? Booking { get; set; }
}
