using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Normalisation;

/// <summary>
/// What one configured integration needs to know before it can normalise
/// anything, beyond the message itself.
/// </summary>
/// <remarks>
/// <b>The identity is NOT here</b> — ADR 0324 and ADR 0330. It was a field on
/// this record so the normalisers could stamp <c>ExternalRef</c>; the Hub
/// stamps that now, and the adapters' own identity arrives at bootstrap in
/// <c>HOTELOS_CONNECTOR_INTEGRATION_ID</c>. Configuration is what a property
/// sets; identity is what the Supervisor states, and a record of the first is
/// the wrong home for the second.
/// </remarks>
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
/// <param name="GuaranteeMaximumFreshness">
/// How long a fetched guarantee policy may be trusted as current enrichment
/// input — ADR 0150's third rule, declared for this property.
/// <c>null</c> means nobody has declared one.
/// </param>
/// <remarks>
/// <para>
/// <b><see cref="GuaranteeMaximumFreshness"/> is nullable, and the null is a
/// value rather than a default.</b> ADR 0150 puts the maximum on the
/// integration precisely because a central number would be a claim about every
/// source made by something that has read none — and it says the <i>absence</i>
/// of a declared contract is a gap to report, never a default to choose. So
/// there is no fallback here: a property that has not declared one produces a
/// guarantee fact the Hub cannot evaluate, and the connector says so.
/// </para>
/// <para>
/// <b>Named for the guarantee rather than for source facts in general</b>,
/// because the guarantee is the only fact this connector supplies that retires
/// — room-stays and room-states are observations superseded by the next
/// message. A general name would state a policy for facts this package does not
/// produce.
/// </para>
/// <para>
/// <b>Not a number chosen here.</b> How long a property's cancellation policy
/// stays true is a fact about that property's operations, not about OHIP; the
/// reference's one-hour cache had no stated basis and the study cites its
/// <i>key</i> as the defect, so the hour is not evidence for a value.
/// </para>
/// <para>
/// <b><see cref="AmountTaxBasis"/> HAS a home, and these two paragraphs said it
/// did not.</b> They read <i>"has no home yet, and this is the flag"</i> and
/// <i>"the Hub does not exist"</i>, written when both were true. It is
/// per-integration configuration — whether a source means net or gross is a
/// fact about that source, and Oracle's flavours differ from other vendors' —
/// and it now lives where <c>CONN-Q9</c>(b) said it would: the Integration
/// Hub's per-integration settings map, offered by <c>ui/configuration.ts</c>
/// under <see cref="TaxBasisSetting"/> and read back by
/// <see cref="ReadTaxBasis"/>.
/// </para>
/// <para>
/// <b>It is also a declared invocation prerequisite</b> — ADR 0321, per
/// integration by ADR 0322, gating every normal dispatch by ADR 0327 — so a
/// property that has not configured it does not reach this connector at all.
/// The old sentence is worth keeping because a reader meeting it would conclude
/// the gate cannot be relied upon, which is the opposite of what is true.
/// </para>
/// <para>
/// The manifest still carries <c>configuration: []</c>, and that is unchanged
/// and deliberate: ADR 0092's flat <c>key / type / default / scope</c> list
/// cannot express a setting that is per integration rather than per package, so
/// the vocabulary lives on the connector's two halves rather than in the
/// package manifest (ADR 0128 §7).
/// </para>
/// <para>
/// <b><see cref="GuaranteeMaximumFreshness"/> does NOT share that state any
/// more, and the difference matters.</b> It shares the home — the same settings
/// map — and it is declared as a prerequisite by nothing, so its absence is
/// still an absence this connector must report rather than one the Hub gates.
/// </para>
/// </remarks>
public sealed record IntegrationSettings(
    string PropertyId,
    string PropertyCode,
    PropertyClock Clock,
    string Currency,
    TaxBasis AmountTaxBasis,
    TimeSpan? GuaranteeMaximumFreshness)
{
    /// <summary>The setting the tax basis arrives in — ADR 0266.</summary>
    /// <remarks>
    /// <b>Declared in <c>ui/configuration.ts</c> under the same name</b>, which
    /// is the duplication this package accepts deliberately: the Hub's settings
    /// map is opaque to it, so the vocabulary lives on both halves of the
    /// connector rather than coupling the platform to it (ADR 0128 §7).
    /// </remarks>
    public const string TaxBasisSetting = "amountTaxBasis";

    /// <summary>What the property configured, or <c>Unspecified</c>.</summary>
    /// <param name="settings">This instance's configuration, as the Hub holds it.</param>
    /// <returns>The basis, or <see cref="TaxBasis.Unspecified"/>.</returns>
    /// <remarks>
    /// <para>
    /// <b>Unrecognised reads as unspecified rather than as a guess.</b> An
    /// amount's basis decides whether a total already includes tax, so reading
    /// <c>"nett"</c> as net would be a guess about money — and
    /// <c>AmountReading</c> refuses an unspecified basis rather than passing it
    /// through, which is where that is enforced.
    /// </para>
    /// <para>
    /// <b>Case-insensitive and ordinal.</b> This is a keyword an operator
    /// types, not a number or a date, so `NUM-Q4`'s culture question does not
    /// arise — but a culture-aware comparison would still be wrong here, since
    /// two properties must read one stored word the same way.
    /// </para>
    /// </remarks>
    public static TaxBasis ReadTaxBasis(IReadOnlyDictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.TryGetValue(TaxBasisSetting, out var raw) || raw is null)
        {
            return TaxBasis.Unspecified;
        }

        var stated = raw.Trim();

        if (string.Equals(stated, "net", StringComparison.OrdinalIgnoreCase))
        {
            return TaxBasis.Net;
        }

        return string.Equals(stated, "gross", StringComparison.OrdinalIgnoreCase)
            ? TaxBasis.Gross
            : TaxBasis.Unspecified;
    }
}
