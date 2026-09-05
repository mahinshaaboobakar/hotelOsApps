using HotelOS.Platform;

namespace HotelOS.GuestOps.Application.Abstractions;

/// <summary>
/// Which sibling applications answer for this property.
/// </summary>
/// <remarks>
/// <para>
/// Two of the stay's tabs change with it: <b>Requests</b> is renamed when Jobs
/// is absent and <b>Servicing</b> is dimmed when Room Care is absent. Neither
/// is emptied — the owner's ruling of 2026-08-31 is that <i>an application's own
/// flow is never gated on another application being installed; an absent
/// dependency loses its capability, never the flow.</i>
/// </para>
/// <para>
/// <b>Context establishes this, and nothing else can.</b> A
/// <c>Resolution</c> lists the domains that answered, and its own contract says
/// <i>"a domain not installed on this property is simply not listed"</i>. That
/// is the only place in the platform where the fact exists: the desktop's host
/// API exposes a module's own granted capabilities and says nothing about its
/// neighbours, and GuestOps's own tables can only show whether a neighbour has
/// ever <i>done</i> anything — which is a different fact. A property with Jobs
/// installed and nothing raised yet would be reported as not having Jobs.
/// </para>
/// <para>
/// <b>It answers unknown for everything today</b>, and that is measured rather
/// than assumed: Context's <c>Domains</c> class has one member and every
/// resolver records only <c>masterdata</c>, so no reply can name a neighbour.
/// <c>ContextNeighbours</c> carries the finding. The port is right; the
/// authority behind it is not yet able to speak.
/// </para>
/// <para>
/// <b>Three-valued, and the third value is the point.</b> Unknown means nobody
/// established it, and every caller must then draw the <i>installed</i> variant.
/// Collapsing unknown into absent would take a capability away from a property
/// that has the application, which is the more damaging of the two mistakes:
/// an extra button that refuses is a nuisance, and a missing button is a desk
/// that cannot raise a job.
/// </para>
/// </remarks>
public interface INeighbours
{
    /// <summary>Whether a domain answers for this property.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="domain">A domain name as Context spells it — <c>job</c>, <c>roomcare</c>.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>True, false, or null when nothing established it.</returns>
    Task<bool?> InstalledAsync(
        RequestScope scope, string domain, CancellationToken cancellationToken);
}

/// <summary>The domain names Context uses, so no caller spells one.</summary>
/// <remarks>
/// Constants rather than literals for the reason the permission names are:
/// a typo is not a compile error, and the symptom would be a tab permanently
/// drawn as though its application were missing.
/// </remarks>
public static class Neighbours
{
    /// <summary>Jobs — what needs doing.</summary>
    public const string Jobs = "job";

    /// <summary>Room Care — what happened in the room.</summary>
    public const string RoomCare = "roomcare";
}
