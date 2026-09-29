using HotelOS.Contracts.Integration.V1;
using PmsOracle.Normalisation;
using static PmsOracle.Tests.TestExponent;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// Reading a source amount: three things, or nothing.
/// </summary>
public sealed class AmountReadingTests
{
    /// <summary>
    /// The exponent is actually used — proved by a currency whose exponent is
    /// not two, ADR 0217.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every other test in this file uses INR, whose exponent is two — the
    /// same value the parameter used to default to.</b> So each of them passes
    /// whether the exponent is threaded or silently defaulted, and none can
    /// tell the two apart. A fixture whose values make two competing rules
    /// agree proves neither.
    /// </para>
    /// <para>
    /// Kuwaiti dinar has three, so the same source amount must scale by a
    /// thousand rather than a hundred. This is the test that fails if
    /// <c>minorUnitDigits</c> ever regains a default and callers stop supplying
    /// it.
    /// </para>
    /// </remarks>
    [Fact]
    public void a_currency_with_a_different_exponent_scales_differently()
    {
        var rupees = AmountReading.Read("184.00", "INR", TaxBasis.Net, MinorUnits);
        var dinar = AmountReading.Read("184.00", "KWD", TaxBasis.Net, ThreeMinorUnits);

        Assert.NotNull(rupees);
        Assert.NotNull(dinar);

        Assert.Equal(18_400, rupees.MinorUnits);
        Assert.Equal(184_000, dinar.MinorUnits);
    }

    [Fact]
    public void an_amount_carries_its_value_currency_and_basis()
    {
        var money = AmountReading.Read("18400.00", "INR", TaxBasis.Net, MinorUnits);

        Assert.NotNull(money);
        Assert.Equal(1_840_000, money.MinorUnits);
        Assert.Equal("INR", money.Currency);
        Assert.Equal(TaxBasis.Net, money.TaxBasis);
    }

    /// <summary>
    /// The corruption this exists to prevent: the same number means a different
    /// thing depending on the source, and the basis is the only thing that says
    /// which. Oracle sends net; another vendor surveyed for this round sends
    /// gross.
    /// </summary>
    [Fact]
    public void the_same_number_read_under_two_bases_is_two_different_amounts()
    {
        var net = AmountReading.Read("18400.00", "INR", TaxBasis.Net, MinorUnits)!;
        var gross = AmountReading.Read("18400.00", "INR", TaxBasis.Gross, MinorUnits)!;

        Assert.Equal(net.MinorUnits, gross.MinorUnits);
        Assert.NotEqual(net.TaxBasis, gross.TaxBasis);
        Assert.NotEqual(net, gross);
    }

    /// <summary>
    /// An unspecified basis is refused rather than passed through — the
    /// contract cannot express it, and this is where that is enforced.
    /// </summary>
    [Fact]
    public void an_unspecified_basis_yields_no_amount()
    {
        Assert.Null(AmountReading.Read("18400.00", "INR", TaxBasis.Unspecified, MinorUnits));
    }

    [Fact]
    public void an_absent_currency_yields_no_amount()
    {
        Assert.Null(AmountReading.Read("18400.00", "", TaxBasis.Net, MinorUnits));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a number")]
    public void an_unreadable_value_yields_no_amount(string? sourceValue)
    {
        Assert.Null(AmountReading.Read(sourceValue, "INR", TaxBasis.Net, MinorUnits));
    }

    /// <summary>
    /// Invariant culture. A decimal read under a comma-separator culture is a
    /// value silently multiplied or divided by a thousand.
    /// </summary>
    [Fact]
    public void the_decimal_point_is_read_the_same_way_everywhere()
    {
        var money = AmountReading.Read("1234.56", "INR", TaxBasis.Net, MinorUnits);

        Assert.NotNull(money);
        Assert.Equal(123_456, money.MinorUnits);
    }

    /// <summary>
    /// Minor units are kept. One surveyed vendor truncated its amounts to
    /// <c>int</c> on the way through, discarding what it had been given.
    /// </summary>
    [Fact]
    public void the_minor_units_survive()
    {
        var money = AmountReading.Read("0.99", "INR", TaxBasis.Net, MinorUnits);

        Assert.NotNull(money);
        Assert.Equal(99, money.MinorUnits);
    }

    [Fact]
    public void a_half_minor_unit_rounds_away_from_zero()
    {
        var money = AmountReading.Read("10.005", "INR", TaxBasis.Net, MinorUnits);

        Assert.NotNull(money);
        Assert.Equal(1001, money.MinorUnits);
    }

    /// <summary>
    /// The recorded source-contract defect survives — ADR 0335.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This asserts the RECORD rather than the violation, and that is the
    /// only instrument available.</b> The defect has no runtime symptom: if
    /// OHIP does send major units — very likely, and not established — every
    /// value is correct, every suite is green, and nothing anywhere fails.
    /// Where nothing can fail there is no behaviour to aim a test at, and the
    /// defence is what the file says about itself.
    /// </para>
    /// <para>
    /// <b>What it buys is that the undo is noisy.</b> Converting this to a
    /// stated, cited convention is the right ending and must be a decision
    /// somebody makes, rather than a tidy-up that deletes an awkward paragraph.
    /// Anyone removing the record has to remove this too and say why.
    /// </para>
    /// <para>
    /// Keyed on the ADR number and on the label at the multiply, because the
    /// prose around them will legitimately be reworded and those two carry the
    /// claim. Read from the file as it ships rather than from a constant, so it
    /// cannot pass by agreeing with itself.
    /// </para>
    /// </remarks>
    [Fact]
    public void the_recorded_source_contract_defect_survives_in_the_source()
    {
        var source = File.ReadAllText(
            Path.Combine(
                Path.GetDirectoryName(TestFile())!,
                "..", "backend", "Normalisation", "AmountReading.cs"));

        Assert.Contains("ADR 0335", source, StringComparison.Ordinal);
        Assert.Contains("RECORDED SOURCE-CONTRACT DEFECT", source, StringComparison.Ordinal);
        Assert.Contains(
            "THE MULTIPLY IS THE UNESTABLISHED ASSUMPTION", source, StringComparison.Ordinal);
    }

    private static string TestFile(
        [System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
