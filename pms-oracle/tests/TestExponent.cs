namespace PmsOracle.Tests;

/// <summary>
/// The minor-unit exponent these tests state, because no caller may omit one.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two is this fixture's currency, not a default.</b> Every fixture here
/// configures <c>Currency: "INR"</c>, whose exponent is two — so the value is a
/// fact about the fixture rather than a number standing in for one nobody
/// resolved. <c>AmountReading.Read</c> carried <c>= 2</c> until ADR 0217, and
/// the whole point of removing it was that an omitted exponent is a value
/// somebody set for every currency at once, invisibly.
/// </para>
/// <para>
/// <b>Named rather than written out at each of the twenty-eight sites.</b> A
/// literal <c>2</c> repeated across six files is the same claim made
/// twenty-eight times, and the day one of them needs a different currency
/// nobody can tell which twos were considered from which were copied.
/// </para>
/// <para>
/// <b>It is deliberately not a default anywhere.</b> Nothing here supplies it
/// implicitly: each site passes it, which is what makes
/// <c>a_currency_with_a_different_exponent_scales_differently</c> able to fail
/// if the parameter ever regains one.
/// </para>
/// </remarks>
internal static class TestExponent
{
    /// <summary>The exponent for <c>INR</c>, which every fixture here uses.</summary>
    internal const int MinorUnits = 2;

    /// <summary>A currency whose exponent is not two — Kuwaiti dinar, three.</summary>
    /// <remarks>
    /// Chosen so the two candidates disagree: a test that used only <c>INR</c>
    /// would pass whether the exponent were threaded or defaulted, and could
    /// not tell the two apart.
    /// </remarks>
    internal const int ThreeMinorUnits = 3;
}
