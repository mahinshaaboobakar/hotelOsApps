namespace PmsOracle.Integrations.Cloud;

/// <summary>
/// One entry from OHIP's business-event queue: a notification that something
/// changed, and the key with which to go and read it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not the record.</b> A business event carries a module, an action,
/// a primary key and a timestamp — never the reservation. Reading it obliges a
/// second call, and that notify-then-fetch shape is not an Oracle quirk: HTNG
/// 2008B, a vendor-neutral standard, splits the same way, and both surveyed
/// webhook providers carry an entity id and nothing more.
/// </para>
/// <para>
/// <b>And the action does not say what happened.</b> OHIP emits
/// <c>UPDATE RESERVATION</c> for a check-in, a check-out and an ordinary edit
/// alike. The reference discovered this and worked around it by re-reading and
/// discarding anything whose status had moved on, with the reason written into
/// a comment. So <see cref="ActionType"/> is carried for provenance and for
/// support, and the <b>fetched state</b> decides the business fact — which is
/// also ADR 0016 Part 2's rule arriving from the other direction.
/// </para>
/// <para>
/// The queue is <b>consumed by reading it</b>: there is no re-fetch, so the
/// Hub stores these bytes before requesting the next page. That is why the type
/// carries nothing derived — what is stored has to be what arrived.
/// </para>
/// </remarks>
/// <param name="EventId">The queue entry's own id, and the deduplication key for this integration.</param>
/// <param name="ModuleName">Which OHIP module changed — only <c>Reservation</c> is in v1 scope.</param>
/// <param name="ActionType">What OHIP called the change. Provenance, not meaning.</param>
/// <param name="PrimaryKey">The key to read back — a reservation id, for the reservation module.</param>
/// <param name="HotelId">The OHIP hotel this concerns, checked against the configured integration.</param>
/// <param name="CreatedDateTime">When OHIP recorded the change, as OHIP formatted it.</param>
public sealed record BusinessEventNotification(
    string EventId,
    string ModuleName,
    string ActionType,
    string PrimaryKey,
    string HotelId,
    string CreatedDateTime)
{
    /// <summary>The one module this connector reads in v1.</summary>
    /// <remarks>
    /// Others are stored and not fetched. The reference dropped them without
    /// recording that it had, which is why nobody can say what else OHIP emits;
    /// storing first means the question stays answerable.
    /// </remarks>
    public const string ReservationModule = "Reservation";

    /// <summary>Whether this connector fetches a record for this notification.</summary>
    public bool IsInScope => ModuleName == ReservationModule;
}
