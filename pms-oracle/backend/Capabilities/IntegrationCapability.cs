namespace PmsOracle.Capabilities;

/// <summary>How a connector learns that something changed.</summary>
/// <remarks>
/// The distinction the whole round turned on. A <see cref="PolledQueue"/> is
/// <b>consumed by reading it</b>: there is no re-fetch, so durability at
/// receipt is the entire guarantee. A <see cref="Push"/> can be re-sent by its
/// sender. The Hub needs to know which it is dealing with before it decides
/// what a failed batch means.
/// </remarks>
public enum ChangeDelivery
{
    /// <summary>We poll a queue that the read empties.</summary>
    PolledQueue,

    /// <summary>The source posts to the property ingress.</summary>
    Push,
}

/// <summary>What a source can promise about repeated delivery.</summary>
/// <remarks>
/// ADR 0128 §5's split: the connector knows what the PMS promises, the Hub
/// implements all three mechanisms. Declaring it is how the Hub picks one
/// without a per-connector branch.
/// </remarks>
public enum DedupePromise
{
    /// <summary>A stable event id — OHIP's business-event id.</summary>
    EventId,

    /// <summary>An entity id and a change timestamp.</summary>
    EntityAndTimestamp,

    /// <summary>Neither; a content digest over the identity fields is the key.</summary>
    ContentDigest,
}

/// <summary>A kind of fact an integration can produce.</summary>
public enum FactKind
{
    /// <summary>Reservations and stays.</summary>
    RoomStay,

    /// <summary>Room occupancy, condition and the stays touching a room.</summary>
    RoomState,
}

/// <summary>
/// What one of this package's integrations can do — declared, not discovered.
/// </summary>
/// <param name="IntegrationId">
/// The registered connector identifier. ADR 0020 validates it against a closed
/// set, so the three flavours stay three identities and no event's provenance
/// is ambiguous (R28).
/// </param>
/// <param name="Delivery">How this integration learns of changes.</param>
/// <param name="Dedupe">What it can promise about repeats.</param>
/// <param name="Produces">The fact kinds it emits.</param>
/// <param name="StatusVocabulary">
/// Every source status value it accepts. What the setup sheet shows a hotel's
/// operator, and what an unrecognised value is measured against.
/// </param>
/// <param name="IdentifierKinds">
/// The identifier kinds it declares — <c>CONN-Q8</c>. Where a source names its
/// own kinds, as OHIP does, this is the set observed so far rather than a
/// closed list: the connector passes the source's value through, and this
/// records what has been seen.
/// </param>
public sealed record IntegrationCapability(
    string IntegrationId,
    ChangeDelivery Delivery,
    DedupePromise Dedupe,
    IReadOnlyList<FactKind> Produces,
    IReadOnlyCollection<string> StatusVocabulary,
    IReadOnlyCollection<string> IdentifierKinds);
