namespace HotelOS.Workforce.Events;

/// <summary>
/// The event types this application announces.
/// </summary>
/// <remarks>
/// <para>
/// <b>No service prefix</b> — ADR 0006 removed them. The subject is
/// <c>domain.action</c>; the Kernel builds
/// <c>property.&lt;id&gt;.&lt;domain&gt;.&lt;action&gt;.v&lt;n&gt;</c> around it, and no
/// package ever knows a NATS subject.
/// </para>
/// <para>
/// <b>Routing exists.</b> Verified in the platform tree, 2026-08-31:
/// <c>streams.rs</c> routes <c>shift.&gt;</c>, <c>leave.&gt;</c>,
/// <c>duty.&gt;</c> and <c>attendance.&gt;</c> into OPERATIONAL, and
/// <c>user.&gt;</c> has been routed since Master Data grew staff. An unrouted
/// subject is acked, matches nothing, and dead-letters silently — which looks
/// exactly like working, so it was confirmed before these names were written.
/// </para>
/// </remarks>
public static class EventTypes
{
    /// <summary>The aggregate a posting announcement is made against.</summary>
    /// <remarks>
    /// <para>
    /// <b>The posting</b> — <c>AUTHZ-Q20</c>, ruled 2026-08-31 on
    /// <c>HUB-Q4</c>'s announce-against-what-you-own. A service announces
    /// against the aggregate it owns, and Workforce owns the posting: neither
    /// the user nor the department is its row to speak for.
    /// </para>
    /// <para>
    /// It is also the only shape that versions. The event store's
    /// <c>uq_events__aggregate_version</c> is
    /// <c>UNIQUE (aggregate_type, aggregate_id, entity_version)</c>, and a
    /// posting carries its own counter — so a person holding two postings
    /// announces twice without collision, and no foreign row is incremented to
    /// make room. Announcing against the user had no version this application
    /// could legally supply.
    /// </para>
    /// </remarks>
    public const string PostingAggregate = "posting";

    /// <summary>This user now works in this department.</summary>
    public const string UserPosted = "user.posted";

    /// <summary>This user no longer works in this department.</summary>
    /// <remarks>
    /// Announced when a posting's window closes. The row survives — the rota it
    /// covered was worked under it — so this is what withdraws the
    /// authorization, never a delete.
    /// <para>
    /// Both directions exist deliberately. ADR 0087's second addendum records
    /// what happens when only one does: <i>"a posting revoked left its tuple
    /// standing, so somebody removed from a property stayed reachable there"</i>
    /// — the direction ADR 0061's invariant forbids.
    /// </para>
    /// </remarks>
    public const string UserPostingEnded = "user.posting_ended";
}
