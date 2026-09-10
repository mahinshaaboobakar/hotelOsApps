namespace PmsOracle.Integrations.Cloud;

/// <summary>
/// One fetched guarantee policy, identified — what this connector hands the Hub.
/// </summary>
/// <param name="IntegrationId">
/// Which of the three flavours fetched it. Part of the key because the same
/// property can be configured against more than one, and a cloud policy is not
/// an on-premise one (R28).
/// </param>
/// <param name="PropertyCode">The property the policy was queried for, in OHIP's terms.</param>
/// <param name="ArrivalDate">
/// The arrival date the policy was queried for, exactly as it was sent to OHIP.
/// </param>
/// <param name="ObservedAt">When this connector fetched it.</param>
/// <param name="Guarantee">OHIP's own object, nested verbatim.</param>
/// <remarks>
/// <para>
/// <b>This is the connector's envelope, not a transcription.</b> Everything
/// else in this directory is what OHIP sends, under OHIP's spellings.
/// <paramref name="Guarantee"/> still is — it is nested unchanged — and the
/// four fields around it are this connector identifying a fact the source does
/// not identify for itself.
/// </para>
/// <para>
/// <b>The identity has to be added here because OHIP does not echo it, and
/// that is exactly the defect the reference shipped.</b> The API is queried per
/// arrival date and the reference cached the answer under <c>hotelId</c> alone
/// (<c>OracleCloudGuaranteeServiceImpl.java:36-39</c>, <c>:52-55</c>,
/// <c>:63</c>), so the first arrival date's cancellation and deposit policy was
/// served to every reservation at that property for an hour. **The key is the
/// whole of it**: the domain fact underneath is sound, and a response that
/// arrives without the question it answered cannot be keyed by anything but the
/// question the caller remembers asking.
/// </para>
/// <para>
/// <b>ADR 0147 keys it at the granularity the source guarantees</b> — property
/// plus arrival date, plus the canonical integration identity — and that is
/// these three fields. Nothing here is a Hub-side store: the connector fetches
/// and identifies, and durability, deduplication and keyed lookup are the
/// Hub's, which is what keeps a connector from becoming a mini-database.
/// </para>
/// <para>
/// <b><paramref name="ObservedAt"/> is ADR 0150's retained metadata</b> and is
/// not by itself a freshness rule — the ADR rejects deriving freshness from
/// arrival time alone. It is what the declared maximum is measured <i>from</i>,
/// and it is recorded here because nothing downstream can reconstruct when a
/// fetch happened.
/// </para>
/// </remarks>
public sealed record GuaranteeRecord(
    string IntegrationId,
    string PropertyCode,
    string ArrivalDate,
    DateTimeOffset ObservedAt,
    OhipGuarantee Guarantee)
{
    /// <summary>
    /// The identity of the source fact, at the granularity the source
    /// guarantees it.
    /// </summary>
    /// <returns>The key, as a single string.</returns>
    /// <remarks>
    /// <para>
    /// <b>All three segments, and the arrival date is the one the reference
    /// dropped.</b> A key that omitted it would make every arrival date at a
    /// property one fact, which is that defect exactly — reproduced here, in a
    /// connector written to avoid it, if the segment were left out.
    /// </para>
    /// <para>
    /// Composed here rather than at the two call sites that need it — the
    /// dedupe key and whatever the Hub eventually looks up by — so that the
    /// question <i>what identifies a guarantee</i> has one answer. Two
    /// compositions of a key drift, and a lookup that disagrees with a dedupe
    /// serves one property's policy under another's identity.
    /// </para>
    /// </remarks>
    public string Key() => $"{IntegrationId}:{PropertyCode}:{ArrivalDate}";
}
