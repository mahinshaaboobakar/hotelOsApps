using HotelOS.Contracts.Integration.V1;
using PmsOracle.Normalisation;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The tax basis arrives in invocation settings — ADR 0266.
/// </summary>
/// <remarks>
/// <b>Nothing declared it until now, and nothing failed.</b> Every fixture in
/// the suite supplied <c>TaxBasis.Net</c> directly, so 277 tests passed over a
/// setting no form offered and no backend read — a default every test shares
/// is not a default any test is examining. Over the Connector Protocol the
/// value can only come off the wire, and an absent one made every amount
/// vanish into a fact that reads as a source which sent no figure.
/// </remarks>
public sealed class TaxBasisSettingTests
{
    private static Dictionary<string, string> With(string value) =>
        new() { [IntegrationSettings.TaxBasisSetting] = value };

    [Theory]
    [InlineData("net", TaxBasis.Net)]
    [InlineData("NET", TaxBasis.Net)]
    [InlineData("Net", TaxBasis.Net)]
    [InlineData("  net  ", TaxBasis.Net)]
    [InlineData("gross", TaxBasis.Gross)]
    [InlineData("GROSS", TaxBasis.Gross)]
    public void A_configured_basis_is_read_whatever_case_it_was_typed_in(
        string typed, TaxBasis expected)
    {
        // One stored word must read the same way at two properties, so the
        // comparison is ordinal and case-insensitive: a keyword an operator
        // types, not a number or a date.
        Assert.Equal(expected, IntegrationSettings.ReadTaxBasis(With(typed)));
    }

    [Theory]
    [InlineData("nett")]
    [InlineData("inclusive")]
    [InlineData("")]
    [InlineData("   ")]
    public void An_unrecognised_basis_is_unspecified_and_never_guessed(string typed)
    {
        // The basis decides whether a total already includes tax. Reading
        // "nett" as net would be a guess about money, and a guess that looks
        // exactly like a right answer.
        Assert.Equal(TaxBasis.Unspecified, IntegrationSettings.ReadTaxBasis(With(typed)));
    }

    [Fact]
    public void An_absent_setting_is_unspecified_rather_than_a_default()
    {
        Assert.Equal(
            TaxBasis.Unspecified,
            IntegrationSettings.ReadTaxBasis(new Dictionary<string, string>()));
    }

    [Fact]
    public void The_form_offers_the_name_the_backend_reads()
    {
        // A setting the backend reads and no form offers is unconfigurable —
        // the declared-but-unreachable shape, at a configuration field. The
        // two halves of this package carry the vocabulary separately by
        // design (ADR 0128 §7), so nothing but a test holds them together.
        var declared = File.ReadAllText(
            Path.Combine(
                Path.GetDirectoryName(TestFile())!, "..", "ui", "configuration.ts"));

        Assert.Contains(
            $"\"{IntegrationSettings.TaxBasisSetting}\"", declared, StringComparison.Ordinal);
    }

    private static string TestFile(
        [System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
