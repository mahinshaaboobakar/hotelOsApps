namespace HotelOS.GuestOps.Domain;

/// <summary>
/// A room type this property has chosen not to sell, for a date range.
/// </summary>
/// <remarks>
/// <para>
/// GUEST-Q7. <b>The seller's control, not inventory ownership.</b> It says
/// <i>"we choose not to sell this type on these dates"</i> — a commercial
/// decision belonging to whoever runs the book. It does <b>not</b> say a room is
/// unusable: that is <b>out of order</b>, which is EngineeringOps's to declare
/// and this application's only to hear.
/// </para>
/// <para>
/// Two sentences, two owners, and availability subtracts both. Collapsing them
/// would make GuestOps a second inventory owner, which is what the ruling's
/// shape — <i>"an answer GuestOps computes, never a table someone else must
/// feed"</i> — exists to prevent.
/// </para>
/// </remarks>
public class StopSell
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>Master Data's room type. Referenced, never copied.</summary>
    public Guid RoomTypeId { get; set; }

    /// <summary>Inclusive.</summary>
    public DateOnly FromDate { get; set; }

    /// <summary>Inclusive.</summary>
    public DateOnly ToDate { get; set; }

    /// <summary>Why — "renovation", "block for the wedding party".</summary>
    /// <remarks>
    /// Free text on purpose. A closed list would be this application deciding
    /// what commercial reasons a hotel is allowed to have.
    /// </remarks>
    public string? Reason { get; set; }

    public Guid? SetBy { get; set; }

    public DateTimeOffset SetAt { get; set; }
}

/// <summary>
/// A room EngineeringOps has taken out of order, as this application heard it.
/// </summary>
/// <remarks>
/// <para>
/// <b>An event-derived read model, never authoritative.</b> EngineeringOps owns
/// out-of-order state (ADR 0056); this is a projection built from events this
/// application already receives, so that availability can subtract it without
/// reading another application's tables.
/// </para>
/// <para>
/// <b>A lagging projection makes the answer conservative and no number
/// wrong.</b> That is the line between an event-derived read model and
/// duplicated master data, and it is why this needs no new inventory owner: if
/// the projection is a few seconds behind, one room is withheld from sale for a
/// few seconds and nothing anywhere becomes untrue.
/// </para>
/// </remarks>
public class RoomOutOfOrder
{
    public Guid RoomId { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>The room's type, resolved when the event was consumed.</summary>
    /// <remarks>
    /// Carried rather than looked up at read time, and it is not optional: an
    /// out-of-order room reduces <b>one</b> type's availability, and subtracting
    /// it from every type would make a hotel with one broken room look full
    /// across the board. Master Data owns the room-to-type relation and this is
    /// the answer it gave when the fact arrived.
    /// </remarks>
    public Guid RoomTypeId { get; set; }

    public DateOnly FromDate { get; set; }

    /// <summary>Null while open-ended.</summary>
    public DateOnly? ToDate { get; set; }

    /// <summary>When the event that set this was published.</summary>
    /// <remarks>
    /// Kept so the projection's own staleness is visible: an operator asking why
    /// a room is being withheld can see how old the fact is, rather than
    /// guessing whether the feed is alive.
    /// </remarks>
    public DateTimeOffset ObservedAt { get; set; }
}
