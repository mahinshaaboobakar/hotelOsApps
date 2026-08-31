namespace HotelOS.GuestOps.Domain;

/// <summary>
/// The anchor — one property, one room type, one date range, its own party.
/// </summary>
/// <remarks>
/// <para>
/// GUEST-Q2 and its addendum. The stay's anchor is the <b>room type</b>; the
/// room number is an <see cref="Assignment"/>, absent at booking, changeable
/// through the stay, and required at check-in. A booking three weeks out has a
/// type and no room, which is the ordinary case rather than the exception —
/// Oracle's own reject vocabulary contains <c>BLANK ROOM NO</c>.
/// </para>
/// <para>
/// <b>Every operation happens here</b>, never to the group: check-in,
/// check-out, cancellation, the move, the folio. A group has no single arrival
/// state, and any summary of one is a count.
/// </para>
/// </remarks>
public class RoomStay
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>The room type sold — Master Data's id, and the anchor.</summary>
    public Guid RoomTypeId { get; set; }

    /// <summary>The room currently assigned, or none.</summary>
    /// <remarks>
    /// <para>
    /// A <b>derived projection</b> of the open <see cref="Assignment"/> row: the
    /// service resolves it, and the create and move messages have nowhere to put
    /// it — which is stronger than validating and rejecting, because a client
    /// cannot express the mistake (CLAUDE.md §"Clients never write a derived
    /// projection").
    /// </para>
    /// <para>
    /// It is stored rather than computed on read because the query pattern is
    /// constant — every board, every list, every Context answer wants it — and
    /// because it changes only when its own assignment row changes, in the same
    /// transaction. That is the projection case, distinct from a
    /// <i>clock-dependent</i> value like availability or the current business
    /// date, which are never stored.
    /// </para>
    /// </remarks>
    public Guid? CurrentRoomId { get; set; }

    public StayLifecycle Lifecycle { get; set; }

    /// <summary>When the guest arrived, and how that is known.</summary>
    public StayTime ArrivalAt { get; set; } = StayTime.None;

    /// <summary>When the guest left, and how that is known.</summary>
    public StayTime DepartureAt { get; set; } = StayTime.None;

    /// <summary>The business day this stay's arrival belongs to.</summary>
    /// <remarks>
    /// <para>
    /// <b>Attached, never computed here</b> — ADR 0128 §6. The Hub stamps it on
    /// a normalised fact from <c>operating_day(occurred_at, boundary)</c>; a
    /// staff-created stay asks the Context Service for the same derivation at
    /// the moment of creation.
    /// </para>
    /// <para>
    /// A <b>stamped historical fact</b>, not the rolling current date. The day
    /// an arrival belonged to does not change afterwards, which is what makes
    /// storing it different from storing <i>today's</i> business date — the
    /// thing ADR 0051 removed and ADR 0128 §6 left derived.
    /// </para>
    /// </remarks>
    public DateOnly? BusinessDate { get; set; }

    /// <summary>How the guest arrived — S13.</summary>
    /// <remarks>
    /// Distinct from <see cref="PmsUnknown"/>, and the two are separate columns
    /// because they are separate facts: a walk-in entered in the PMS is not
    /// PMS-unknown, and a phone booking taken here during an upgrade is
    /// PMS-unknown and not a walk-in. One flag would lose the walk-in ratio,
    /// which every hotel reports on and which cannot be reconstructed later.
    /// </remarks>
    public bool WalkIn { get; set; }

    /// <summary>Whether the PMS knows this stay exists — GUEST-Q5.</summary>
    /// <remarks>
    /// A <b>permanent, valid state</b> rather than a pending one. Write-back is
    /// deferred (<c>CONN-Q5</c>), so a stay created here never reaches the PMS
    /// and some never will be known to it. The day a matching fact arrives, a
    /// <see cref="StayLinkCandidate"/> proposes the join and a person decides.
    /// </remarks>
    public bool PmsUnknown { get; set; }

    public RecordOrigin Origin { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    /// <summary>Optimistic concurrency, and the event's <c>entity_version</c>.</summary>
    public long Version { get; set; }

    public Booking? Booking { get; set; }

    public ICollection<Assignment> Assignments { get; set; } = [];

    public ICollection<StayGuest> Party { get; set; } = [];

    public ICollection<StayAbsence> Absences { get; set; } = [];

    public ICollection<StayExternalRef> ExternalRefs { get; set; } = [];

    public CommercialTerms? Terms { get; set; }

    public StaySource? Source { get; set; }
}

/// <summary>One identifier as a source knows this stay — R10, CONN-Q8.</summary>
/// <remarks>
/// Minted with the stay in one transaction, for GUEST-Q8's reason: the id and
/// its references are born together, so mapping is never a lookup performed
/// before the entity exists.
/// </remarks>
public class StayExternalRef
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }

    public string IntegrationId { get; set; } = string.Empty;

    public string IdentifierKind { get; set; } = string.Empty;

    public string ExternalId { get; set; } = string.Empty;

    public RoomStay? Stay { get; set; }
}
