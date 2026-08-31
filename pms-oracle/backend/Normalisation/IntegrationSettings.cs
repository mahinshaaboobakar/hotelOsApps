using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Normalisation;

/// <summary>
/// What one configured integration needs to know before it can normalise
/// anything, beyond the message itself.
/// </summary>
/// <param name="IntegrationId">
/// The registered connector identifier — <c>oracle-onpremise</c>, never
/// <c>oracle</c>. ADR 0020 validates it against the closed set the Hub
/// registers, so three flavours of one PMS stay three identities.
/// </param>
/// <param name="PropertyId">The property this integration is configured for.</param>
/// <param name="PropertyCode">
/// What the PMS calls that property. Incoming messages claim one, and it is
/// checked against this rather than believed.
/// </param>
/// <param name="Clock">The property's zone and its two clock times.</param>
/// <param name="Currency">
/// The property's ISO 4217 currency. Core Administration's — every integration
/// at a property agrees about it (ADR 0052's class of configuration).
/// </param>
/// <param name="AmountTaxBasis">
/// Whether this source's amounts include tax.
/// </param>
/// <remarks>
/// <para>
/// <b><see cref="AmountTaxBasis"/> has no home yet, and this is the flag.</b>
/// It is per-integration configuration — whether a source means net or gross is
/// a fact about that source, and Oracle's flavours differ from other vendors'.
/// It belongs with the Integration Hub's per-integration configuration, which
/// the connector's <c>ui.module</c> submits (<c>CONN-Q9</c>, ruled (b)).
/// </para>
/// <para>
/// That configuration surface is unbuilt: the Hub does not exist, and the
/// manifest deliberately carries <c>configuration: []</c> because ADR 0092's
/// flat <c>key / type / default / scope</c> list cannot express a setting that
/// is per integration rather than per package. So it arrives here as an input
/// and is named as pending, which is honest where inventing a home would not
/// be. Nothing else about this type changes when the home exists.
/// </para>
/// </remarks>
public sealed record IntegrationSettings(
    string IntegrationId,
    string PropertyId,
    string PropertyCode,
    PropertyClock Clock,
    string Currency,
    TaxBasis AmountTaxBasis);
