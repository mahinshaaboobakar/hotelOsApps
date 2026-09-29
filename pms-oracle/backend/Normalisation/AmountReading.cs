using System.Globalization;
using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Normalisation;

/// <summary>
/// Reads a source amount into the contract's <see cref="Money"/> — value,
/// currency and tax basis, or nothing.
/// </summary>
/// <remarks>
/// <para>
/// R19. An amount carries three things or it is not an amount, and the on-site
/// flavours supply exactly one of them: a bare decimal string in
/// <c>Amount</c>, with no currency and no indication of whether tax is
/// included. The other two therefore come from configuration, and each from a
/// different place:
/// </para>
/// <list type="bullet">
///   <item><b>Currency</b> is the property's — Core Administration holds it
///   (ADR 0052), and every integration at that property agrees about it.</item>
///   <item><b>Tax basis</b> is the <i>integration's</i>, because it is a fact
///   about the source system: Oracle sends net, and another vendor surveyed for
///   this round sends gross. The reference wrote both into one field and its
///   stored revenue means a different thing per connector with nothing
///   recording which.</item>
/// </list>
/// <para>
/// Neither is defaulted here. A basis that could be omitted would put the
/// silent net/gross corruption back one level below the wire, where the
/// contract's own <c>TAX_BASIS_UNSPECIFIED</c> was designed to keep it out.
/// </para>
/// <para>
/// <b>⚠ AND "A BARE DECIMAL STRING" IS SYNTAX, WHICH THIS FILE READ AS
/// SEMANTICS — ADR 0335 names that phrase, in this file, as the defect.</b> A
/// decimal says how the vendor serialised a number and nothing about the unit
/// it is in. <see cref="Read"/> multiplies by <c>10^minorUnitDigits</c>, which
/// <i>asserts the source value is in MAJOR units</i>, and no document
/// establishes that for OHIP. The measurement, the bound and what closes it are
/// in <c>Read</c>'s own remarks, where the multiply is.
/// </para>
/// </remarks>
public static class AmountReading
{
    /// <summary>Read a source amount.</summary>
    /// <param name="sourceValue">The amount as the source sent it, e.g. <c>"18400.00"</c>.</param>
    /// <param name="currency">The property's ISO 4217 currency.</param>
    /// <param name="basis">What this integration's source means by the number.</param>
    /// <param name="minorUnitDigits">
    /// Digits after the point in <paramref name="currency"/>, from the
    /// platform's currency authority — never chosen here.
    /// </param>
    /// <returns>The money, or <c>null</c> when the value or its configuration cannot support one.</returns>
    /// <remarks>
    /// <para>
    /// Invariant culture, deliberately: the wire is not a person's locale, and
    /// a decimal read under a comma-separator culture is a value silently
    /// multiplied or divided by a thousand.
    /// </para>
    /// <para>
    /// <b>No default, and the default is what this replaces.</b> The parameter
    /// read <c>= 2</c> with its own documentation saying <i>"2 for most"</i>,
    /// and both callers omitted it — so every amount in a currency with a
    /// different exponent was silently scaled by 100, in the direction nobody
    /// would notice until a reconciliation. ADR 0217 rules the exponent is
    /// Reference Data, <i>not inferred from the property's configured</i>
    /// anything, and a default is the strongest form of inferring it: a value
    /// somebody set, for every currency at once, invisibly.
    /// </para>
    /// <para>
    /// Required rather than validated, so a caller cannot express the omission
    /// and the compiler names every site instead of the author remembering
    /// them.
    /// </para>
    /// <para>
    /// <b>⚠ RECORDED SOURCE-CONTRACT DEFECT — ADR 0335, measured 2026-09-29.</b>
    /// The scaling below multiplies by <c>10^minorUnitDigits</c>, so it holds
    /// that <paramref name="sourceValue"/> is in the currency's MAJOR unit.
    /// <b>Nothing establishes that.</b> The contract's own rule is that
    /// <i>"an unknown source convention is a CONNECTOR defect against its own
    /// source contract — it is not `minor_unit_digits` being unavailable"</i>,
    /// so this is recorded here rather than reported as a missing exponent.
    /// </para>
    /// <para>
    /// <b>What was measured, so the next reader can check it rather than take
    /// it.</b> The Oracle reference's transcription of the OHIP guarantee block
    /// carries <i>no amount field at all</i>
    /// (<c>cloud/models/OracleCloudReservationGuarantees.java:85-92</c> —
    /// <c>basisType</c>, <c>nights</c>, <c>currencyCode</c>); its
    /// <c>int amountBeforeTax</c> (<c>cloud/dto/mongo/Reservation.java:102</c>)
    /// is the reference's own storage type, which this file already names as a
    /// truncation defect; and <b>no sample payload carrying that field exists
    /// anywhere in the reference</b> — searched <c>*.json</c>, <c>*.md</c>,
    /// <c>*.txt</c> and <c>*.log</c> across the whole tree, with the same
    /// literal matching in <c>*.java</c> as the control that the search works.
    /// </para>
    /// <para>
    /// <b>This is latent rather than live, and the bound is worth stating.</b>
    /// <c>CloudNormaliser</c> returns <c>Unresolved("minor_unit_digits")</c>
    /// before reaching here whenever the exponent is absent, and the Reference
    /// Data catalogue publishes nothing (<c>CONN-Q83</c>, open), so the multiply
    /// cannot execute against a real exponent today. Ruled, unreachable, and
    /// wrong the day it becomes reachable.
    /// </para>
    /// <para>
    /// <b>What closes it, and it is not a better comment.</b> An OHIP
    /// specification stating the unit convention, cited here, plus a test
    /// asserting the scaling against that stated convention rather than against
    /// a worked example. Until then nobody writes the sentence: inferring it
    /// from <c>"18400.00"</c> is precisely what ADR 0335 forbids, quoting that
    /// value.
    /// </para>
    /// </remarks>
    public static Money? Read(
        string? sourceValue,
        string currency,
        TaxBasis basis,
        int minorUnitDigits)
    {
        if (string.IsNullOrWhiteSpace(sourceValue) || string.IsNullOrWhiteSpace(currency))
        {
            return null;
        }

        // An unspecified basis is refused rather than passed through. The
        // contract cannot express it and this is where that is enforced.
        if (basis is TaxBasis.Unspecified)
        {
            return null;
        }

        if (!decimal.TryParse(
                sourceValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value))
        {
            return null;
        }

        // ⚠ THE MULTIPLY IS THE UNESTABLISHED ASSUMPTION — ADR 0335. Scaling UP
        // by the exponent holds that `value` is in major units. See the recorded
        // defect in this method's remarks; do not remove this label without a
        // cited OHIP convention, because removing it is what makes the
        // assumption invisible again.
        var scale = (decimal)Math.Pow(10, minorUnitDigits);

        // Rounded half away from zero — the rule a hotel invoice uses. The
        // reference truncated its amounts to `int` on one flavour.
        //
        // This sentence used to end "discarding the minor units it had been
        // given" — carried here from chapter 02's R19, where it is written about
        // APALEO. It is kept as a correction rather than deleted because it was
        // my own uncited claim about OHIP's convention, one layer up from the
        // defect recorded above: "the minor units it had been given" presumes
        // the source sent a major-unit decimal with fractional precision, which
        // is the very thing nothing establishes.
        var minorUnits = decimal.Round(value * scale, 0, MidpointRounding.AwayFromZero);

        return new Money
        {
            MinorUnits = (long)minorUnits,
            Currency = currency,
            TaxBasis = basis,
        };
    }
}
