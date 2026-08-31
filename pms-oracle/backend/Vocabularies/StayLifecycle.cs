namespace PmsOracle.Vocabularies;

/// <summary>
/// What this connector can say about where a room-stay has reached — the
/// meanings it declares, independent of which Oracle deployment supplied them.
/// </summary>
/// <remarks>
/// <para>
/// Three source vocabularies land here: OHIP's five reservation statuses, the
/// on-site flavours' ten, and the room-level list OHIP sends per room. They
/// disagree about spelling, casing and granularity (requirement R5, and
/// <c>42c</c> §2 across five vendors), so the mapping is per integration and
/// this is the one place their meanings meet.
/// </para>
/// <para>
/// <b>This is the connector's declared vocabulary, not the platform's.</b> The
/// canonical event vocabulary belongs to the Integration Hub and does not exist
/// yet; when it does, these meanings bind to it at the Hub boundary. Declaring
/// them here first is what lets the parsers be written and tested now, and it
/// is the half ADR 0128 §5 makes the connector's own.
/// </para>
/// </remarks>
public enum StayLifecycle
{
    /// <summary>Booked and not yet arrived — OHIP <c>Reserved</c>, on-site <c>Due In</c>.</summary>
    Booked,

    /// <summary>The guest is in the room — OHIP <c>InHouse</c>, on-site <c>CHECKED IN</c>.</summary>
    CheckedIn,

    /// <summary>The guest has left — OHIP <c>CheckedOut</c>, on-site <c>CHECKED OUT</c>.</summary>
    CheckedOut,

    /// <summary>The booking was cancelled before arrival.</summary>
    Cancelled,

    /// <summary>The guest did not arrive and the stay lapsed.</summary>
    NoShow,

    /// <summary>
    /// In house and leaving today — on-site <c>DUE OUT</c>.
    /// </summary>
    /// <remarks>
    /// Kept as its own meaning rather than folded into <see cref="CheckedIn"/>,
    /// because housekeeping works from it: due out plus dirty plus not sold
    /// again tonight is the <c>STRIP_LINEN</c> decision (requirement R3).
    /// </remarks>
    DueOut,

    /// <summary>
    /// Held without a confirmed room — on-site <c>WAITLIST</c>.
    /// </summary>
    Waitlisted,

    /// <summary>
    /// Received and not yet resolved by the PMS — on-site <c>PENDING</c>.
    /// </summary>
    /// <remarks>
    /// The reference discarded this along with <c>DUE OUT</c> and
    /// <c>WAITLIST</c> (study §5.1). It is declared here because a stay the PMS
    /// is still deciding about is a fact, and dropping it is how a booking
    /// appears from nowhere later.
    /// </remarks>
    Pending,
}
