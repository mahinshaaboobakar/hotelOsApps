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

/// <summary>
/// What decides when this integration's source facts stop being current —
/// ADR 0150.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is no Hub-wide TTL, and this is what replaces it.</b> ADR 0150:
/// <i>"the Hub does not know what a fact means and cannot invent its lifetime;
/// a number chosen centrally is a claim about every source made by something
/// that has read none of them."</i> So the connector declares which rule
/// governs, in the ADR's own order — source expiry, then source window, then
/// the integration's own maximum.
/// </para>
/// <para>
/// <b><see cref="DedupePromise"/>'s shape, one question over.</b> The connector
/// knows what its source promises; the Hub implements the retirement. Declaring
/// it is how the Hub retires a fact without a per-connector branch, and it is
/// why this is a declaration rather than a number.
/// </para>
/// </remarks>
public enum SourceFreshness
{
    /// <summary>This integration supplies no source fact that retires.</summary>
    /// <remarks>
    /// The two on-site flavours. Their facts are room-stays and room-states —
    /// observations of a moment, superseded by the next message rather than
    /// expiring — and neither sends a guarantee policy at all. <b>Stated rather
    /// than left unset</b>: <i>no such fact</i> and <i>nobody has said</i> are
    /// different answers with different remedies, and an integration with
    /// nothing to retire must not read as one whose contract is missing.
    /// </remarks>
    NoRetirableFact,

    /// <summary>The source states when the fact expires — ADR 0150's first rule.</summary>
    /// <remarks>
    /// <b>Named and unused.</b> No source surveyed for this round supplies one,
    /// and the member exists so that a source which gains one is not forced
    /// into <see cref="IntegrationDeclaresMaximum"/> — which would substitute a
    /// number this side chose for a statement the source made.
    /// </remarks>
    SourceStatesExpiry,

    /// <summary>The source states a validity window — ADR 0150's second rule.</summary>
    /// <remarks>
    /// Named and unused, for the same reason as
    /// <see cref="SourceStatesExpiry"/>. OHIP's guarantee is fetched <i>for</i>
    /// an arrival date, which is the fact's key and not a window: it says what
    /// the policy is about, never when the answer stops being true.
    /// </remarks>
    SourceStatesWindow,

    /// <summary>
    /// The source states neither, so this integration's declared maximum
    /// governs — ADR 0150's third rule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// OHIP's. The guarantee object carries a code, three flags, two offsets
    /// and a penalty basis
    /// (<c>cloud/models/OracleCloudReservationGuarantees.java:13-27</c>,
    /// <c>:50-52</c>, <c>:75-79</c>, <c>:85-92</c>) and <b>no expiry and no
    /// validity window anywhere in it</b>.
    /// </para>
    /// <para>
    /// <b>The maximum is not a number this enum carries</b>, because it is not
    /// a fact about OHIP: how long a property's cancellation policy stays true
    /// is a fact about that property's operations. It arrives as configuration
    /// on <see cref="Normalisation.IntegrationSettings"/>, and its absence is
    /// reported as a gap rather than defaulted — ADR 0150 says so in as many
    /// words.
    /// </para>
    /// <para>
    /// The reference cached the policy for an hour with no stated basis, under
    /// a key that omitted the arrival date it had queried by
    /// (<c>OracleCloudGuaranteeServiceImpl.java:36-39</c>, <c>:52-55</c>,
    /// <c>:63</c>). The study cites the <i>key</i> as the defect and is
    /// deliberately silent on the duration, so the hour is not evidence for a
    /// number here.
    /// </para>
    /// </remarks>
    IntegrationDeclaresMaximum,
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
/// <param name="Freshness">
/// What retires this integration's source facts — ADR 0150.
/// </param>
public sealed record IntegrationCapability(
    string IntegrationId,
    ChangeDelivery Delivery,
    DedupePromise Dedupe,
    IReadOnlyList<FactKind> Produces,
    IReadOnlyCollection<string> StatusVocabulary,
    IReadOnlyCollection<string> IdentifierKinds,
    SourceFreshness Freshness);
