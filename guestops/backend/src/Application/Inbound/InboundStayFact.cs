using HotelOS.GuestOps.Domain;

namespace HotelOS.GuestOps.Application.Inbound;

/// <summary>One identifier a source knows an entity by.</summary>
public sealed record InboundRef(string IntegrationId, string IdentifierKind, string ExternalId);

/// <summary>A member of the party, as a source reported it.</summary>
public sealed record InboundGuest(
    string NameAsGiven,
    string? NameGiven,
    string? NameFamily,
    string? Phone,
    string? Email,
    bool? IsPrimary);

/// <summary>
/// A normalised room-stay fact, in this application's own terms.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not the wire type.</b> <c>hotelos.integration.v1.RoomStayFact</c> is the
/// Integration Hub's contract and DD's to change; mapping it here at the edge
/// keeps the rules below free of it, exactly as the gRPC layer maps a request
/// into a command rather than handing the generated message to a service.
/// </para>
/// <para>
/// <b>It carries what a decision needs and nothing else.</b> The provenance and
/// the raw payload stay in the Hub's inbox (ADR 0128 §5); what reaches the rule
/// is the fact.
/// </para>
/// </remarks>
/// <param name="IntegrationId">The connector that produced it — ADR 0020's closed set.</param>
/// <param name="PropertyId">Whose property this is about.</param>
/// <param name="StayRefs">The stay's identifiers, as the source knows them (R10).</param>
/// <param name="BookingRefs">The group's identifiers, where the source names one.</param>
/// <param name="ExpectedStayCount">R9's <c>noOfRooms</c>. Null when unstated.</param>
/// <param name="IsComplete">The source's claim about the group, never our arithmetic.</param>
/// <param name="Lifecycle">Where the source says the stay has reached.</param>
/// <param name="RoomTypeId">The anchor, resolved to Master Data by the Hub's Enrich.</param>
/// <param name="RoomId">The assignment, absent until one is made.</param>
/// <param name="Arrival">When, and how the source knows it.</param>
/// <param name="Departure">When, and how the source knows it.</param>
/// <param name="BusinessDate">Attached by the Hub — never computed here.</param>
/// <param name="WalkIn">How the guest arrived.</param>
/// <param name="Guests">The party, forwarded for this domain to resolve or create.</param>
/// <param name="Terms">What it was sold on, where the source sent terms.</param>
/// <param name="Absences">What the source did not supply, and why.</param>
public sealed record InboundStayFact(
    string IntegrationId,
    Guid PropertyId,
    IReadOnlyList<InboundRef> StayRefs,
    IReadOnlyList<InboundRef> BookingRefs,
    int? ExpectedStayCount,
    bool? IsComplete,
    StayLifecycle Lifecycle,
    Guid RoomTypeId,
    Guid? RoomId,
    StayTime Arrival,
    StayTime Departure,
    DateOnly? BusinessDate,
    bool WalkIn,
    IReadOnlyList<InboundGuest> Guests,
    CommercialTerms? Terms,
    IReadOnlyList<StayAbsence> Absences);

/// <summary>What applying an inbound fact did.</summary>
/// <remarks>
/// Named rather than boolean because the four are operationally different: one
/// is a normal day, one is silence, one is a person's work, and one is a stay
/// that now exists.
/// </remarks>
public enum InboundOutcome
{
    /// <summary>A stay was created from this fact.</summary>
    Created,

    /// <summary>An existing stay moved.</summary>
    Applied,

    /// <summary>Nothing changed and nothing was published.</summary>
    /// <remarks>
    /// Covers two silences that are both correct: a replayed fact already
    /// applied, and a fact that <b>matched a standing override</b> — GUEST-Q4's
    /// silent confirmation, which is what keeps a returning feed's fourteen
    /// facts from becoming fourteen rows of work.
    /// </remarks>
    Settled,

    /// <summary>The fact differs from a standing override — GUEST-Q3.</summary>
    /// <remarks>
    /// Recorded, not applied. The override remains the answer everywhere until
    /// a person decides.
    /// </remarks>
    Disagreed,

    /// <summary>The fact cannot move this stay, and is recorded — S26.</summary>
    Contradicted,

    /// <summary>It may be a stay this property created. Held — GUEST-Q5.</summary>
    Held,
}
