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
    /// <b>The user, not the posting.</b> Chapter 01 §4 and AUTHZ-Q7's shape:
    /// what consumes this is the Kernel's authorization registration, and the
    /// tuple it materialises is <c>department:{id}#posted@user:{uid}</c>. An
    /// announcement on a <c>posting</c> aggregate would be about a record;
    /// this one is about a person gaining a place in the property, which is the
    /// fact the graph is interested in.
    /// </remarks>
    public const string UserAggregate = "user";

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
