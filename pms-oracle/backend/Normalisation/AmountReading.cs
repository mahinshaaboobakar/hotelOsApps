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
/// </remarks>
public static class AmountReading
{
    /// <summary>Read a source amount.</summary>
    /// <param name="sourceValue">The amount as the source sent it, e.g. <c>"18400.00"</c>.</param>
    /// <param name="currency">The property's ISO 4217 currency.</param>
    /// <param name="basis">What this integration's source means by the number.</param>
    /// <param name="minorUnitDigits">Digits after the point in <paramref name="currency"/> — 2 for most.</param>
    /// <returns>The money, or <c>null</c> when the value or its configuration cannot support one.</returns>
    /// <remarks>
    /// Invariant culture, deliberately: the wire is not a person's locale, and
    /// a decimal read under a comma-separator culture is a value silently
    /// multiplied or divided by a thousand.
    /// </remarks>
    public static Money? Read(
        string? sourceValue,
        string currency,
        TaxBasis basis,
        int minorUnitDigits = 2)
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

        var scale = (decimal)Math.Pow(10, minorUnitDigits);

        // Rounded half away from zero — the rule a hotel invoice uses. The
        // reference truncated its amounts to `int` on one flavour, discarding
        // the minor units it had been given.
        var minorUnits = decimal.Round(value * scale, 0, MidpointRounding.AwayFromZero);

        return new Money
        {
            MinorUnits = (long)minorUnits,
            Currency = currency,
            TaxBasis = basis,
        };
    }
}
