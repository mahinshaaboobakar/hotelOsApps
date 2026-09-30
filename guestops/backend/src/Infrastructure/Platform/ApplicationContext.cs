using HotelOS.Contracts.Common.V1;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Infrastructure.Platform;

/// <summary>
/// The envelope for a call this application makes onward — carrying no user.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every GuestOps operation failed on this.</b> The Context Service refused
/// with <i>"a Application connection may not supply a user identity; present an
/// access token instead"</i>, and <c>IBusinessDay</c> sits behind
/// <c>AvailabilityService</c>, <c>BookingReadService</c>, <c>BookingService</c>,
/// <c>StayMatcher</c>, <c>RegistrationService</c>, <c>StayAssignmentService</c>
/// and <c>StayListService</c> — so one refusal reached all of them, and the stay
/// list, availability and booking all stopped together.
/// </para>
/// <para>
/// <b><c>ToRequestContext(scope)</c> is correct and was the wrong call here.</b>
/// It branches on <c>CallerKind</c>, deliberately: a platform service carrying
/// Alice must emit a user envelope or she is lost at that hop (ADR 0014). A desk
/// user really is behind this call, so <c>Caller</c> is <c>User</c> and the
/// factory emitted a <c>UserContext</c> — which <c>RequestScopeExtensions</c>
/// refuses, because the transport principal on GuestOps' <i>outbound</i>
/// connection is an application rather than a platform service. The factory reads
/// one axis and the refusal reads the other, and both are right about their own.
/// </para>
/// <para>
/// <b>AUTHZ-Q18 is why there is no second option.</b> <c>CallerKind</c> has no
/// <c>Application</c> member on purpose, and the reason is written at the enum:
/// <i>"an installed application is performed as nobody — it carries no session
/// and propagates no user, and the user it asks about is an argument to the
/// authorization call rather than an identity it holds."</i> So presenting an
/// access token is not the alternative — an application has no user session to
/// present, ever. The register's words for the same ruling: <i>packages are
/// services, not a third kind.</i>
/// </para>
/// <para>
/// <b>Accepted because the envelope carries no <c>User</c> at all.</b>
/// <c>RequestScopeExtensions.Propagate</c> opens with
/// <c>if (context.User is not { } propagated)</c> and returns the caller on that
/// arm; the refusal lives inside the branch where a user IS present. A
/// service-context envelope therefore passes, and this is not a workaround
/// against a guard but the path the guard leaves open.
/// </para>
/// <para>
/// <b>Public rather than internal, by ADR 0025's own order.</b> Its test
/// reaches it directly, no application in this repository uses
/// <c>InternalsVisibleTo</c>, and inventing one to keep a keyword would be a
/// new mechanism where widening is what the rule asks for. Its neighbours
/// here — <c>ContextBusinessDay</c>, <c>ContextNeighbours</c> — are public
/// already, so this is the surface it belongs to rather than an exception.
/// </para>
/// <para>
/// <b>Its own file because two callers need it.</b> <c>ContextBusinessDay</c> and
/// <c>ContextNeighbours</c> both ask Context, and a private copy in each would be
/// two things that must agree — the first draft of this fix did exactly that, and
/// two copies of an authorization envelope is the shape that drifts silently.
/// </para>
/// </remarks>
public static class ApplicationContext
{
    /// <summary>The manifest's declared id — <c>guestops/manifest.yaml:14</c>.</summary>
    /// <remarks>
    /// Informational. <c>ForService</c> records that the Kernel reads the
    /// caller's identity from the verified peer certificate rather than from this
    /// field, so naming something else here would achieve nothing — which is also
    /// why it must not be mistaken for a provenance this process asserts.
    /// </remarks>
    private const string Application = "guestops";

    /// <summary>The envelope for work this application does as itself.</summary>
    /// <param name="scope">The inbound scope — read for property and organization only.</param>
    /// <returns>A request context naming this application and no user.</returns>
    /// <remarks>
    /// The property and organization travel because the operation runs against
    /// them; the user does not, because this application cannot assert one. The
    /// operating day and a room's neighbours are property facts and need no user
    /// to answer.
    /// </remarks>
    public static RequestContext AsItself(RequestScope scope)
        => RequestContextFactory.ForService(Application, scope.PropertyId, scope.OrganizationId);
}
