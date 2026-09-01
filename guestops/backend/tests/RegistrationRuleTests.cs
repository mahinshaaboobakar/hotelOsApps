using HotelOS.GuestOps.Domain;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Who counts as a visitor, and what their card must carry.
/// </summary>
/// <remarks>
/// Pure, and the reason the rule is a separate type: a property may be asked
/// years later why a card demanded a passport, and the answer is decided here
/// over values rather than inside a service that needs a database to run.
/// </remarks>
public class RegistrationRuleTests
{
    /// <summary>
    /// The same nationality is a visitor at one property and not at another.
    /// </summary>
    /// <remarks>
    /// <b>The test that guards the market rule.</b> Nothing in the product may
    /// name a country: a hotel in Kochi and a hotel in Dubai run the same build
    /// and each treats the other's nationals as guests from outside. Two
    /// properties, one nationality, opposite answers.
    /// </remarks>
    [Theory]
    [InlineData("IN", "IN", false)]
    [InlineData("IN", "AE", true)]
    [InlineData("AE", "AE", false)]
    [InlineData("AE", "IN", true)]
    public void A_visitor_is_decided_by_the_propertys_home_country(
        string nationality, string homeCountry, bool expected)
        => Assert.Equal(expected, RegistrationRule.IsVisitor(nationality, homeCountry));

    /// <summary>Case never decides whether a passport is demanded.</summary>
    [Theory]
    [InlineData("in")]
    [InlineData("In")]
    [InlineData("IN")]
    public void Nationality_is_compared_without_case(string nationality)
        => Assert.False(RegistrationRule.IsVisitor(nationality, "IN"));

    /// <summary>
    /// An uncaptured nationality is not a visitor — a blank field is not a fact.
    /// </summary>
    /// <remarks>
    /// Answering "visitor" would demand a passport of every guest whose card is
    /// merely incomplete, turning a data-entry gap into a refusal at the desk.
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_unknown_nationality_is_not_a_visitor(string? nationality)
        => Assert.False(RegistrationRule.IsVisitor(nationality, "IN"));

    /// <summary>The two required sets are chosen, never merged.</summary>
    /// <remarks>
    /// A property that asks a passport of everyone and one that asks it of
    /// visitors only are both ordinary. A single set with an "and also, if
    /// foreign" modifier could not express the first without the second.
    /// </remarks>
    [Fact]
    public void The_required_set_follows_the_guest_not_the_property()
    {
        var settings = Settings();

        Assert.Equal(["name_as_on_id"], RegistrationRule.RequiredFor(settings, "IN"));
        Assert.Equal(
            ["name_as_on_id", "passport_number"],
            RegistrationRule.RequiredFor(settings, "GB"));
    }

    /// <summary>What is missing is reported, and a filled field is not.</summary>
    [Fact]
    public void Missing_lists_only_the_required_fields_without_a_value()
    {
        var card = new Registration { Nationality = "GB", NameAsOnId = "Joseph Mathew" };

        Assert.Equal(["passport_number"], RegistrationRule.Missing(Settings(), card));
    }

    /// <summary>A complete card is missing nothing.</summary>
    [Fact]
    public void A_complete_card_reports_nothing_missing()
    {
        var card = new Registration
        {
            Nationality = "GB",
            NameAsOnId = "Joseph Mathew",
            PassportNumber = "Z1234567",
        };

        Assert.Empty(RegistrationRule.Missing(Settings(), card));
    }

    /// <summary>Whitespace is not a value.</summary>
    /// <remarks>
    /// A space typed into a required field would otherwise satisfy it, which is
    /// how a card passes its own check and fails an inspection.
    /// </remarks>
    [Fact]
    public void A_blank_string_does_not_satisfy_a_required_field()
    {
        var card = new Registration { Nationality = "IN", NameAsOnId = "   " };

        Assert.Equal(["name_as_on_id"], RegistrationRule.Missing(Settings(), card));
    }

    /// <summary>
    /// A field name the product does not know is reported missing, not ignored.
    /// </summary>
    /// <remarks>
    /// A property that configures <c>passport_numbr</c> gets told the card lacks
    /// it, which is visible. Treating an unrecognised name as satisfied would
    /// make a typo in the configuration look like compliance.
    /// </remarks>
    [Fact]
    public void An_unknown_configured_field_is_reported_rather_than_ignored()
    {
        var settings = Settings();
        settings.RequiredForHomeCountry = ["passport_numbr"];

        var card = new Registration { Nationality = "IN", PassportNumber = "Z1234567" };

        Assert.Equal(["passport_numbr"], RegistrationRule.Missing(settings, card));
    }

    /// <summary>A property requiring nothing is satisfied by an empty card.</summary>
    /// <remarks>
    /// The product proposes a shape and never a legal minimum — so "require
    /// nothing" has to be expressible, however unlikely a property is to choose
    /// it.
    /// </remarks>
    [Fact]
    public void A_property_that_requires_nothing_is_satisfied_by_an_empty_card()
    {
        var settings = Settings();
        settings.RequiredForHomeCountry = [];

        Assert.Empty(RegistrationRule.Missing(settings, new Registration { Nationality = "IN" }));
    }

    private static GuestOpsSettings Settings() => new()
    {
        HomeCountry = "IN",
        RequiredForHomeCountry = ["name_as_on_id"],
        RequiredForVisitors = ["name_as_on_id", "passport_number"],
    };
}
