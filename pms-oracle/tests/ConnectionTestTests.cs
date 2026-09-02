using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;

using PmsOracle.Adapters;
using PmsOracle.Authentication;
using PmsOracle.Normalisation;
using PmsOracle.Vocabularies;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// What <c>Test connection</c> answers — <c>CONN-Q12</c>, frame 3's button.
/// </summary>
/// <remarks>
/// The half that can be answered without a credential value, which is also the
/// half that catches the failure people actually hit: a form filled in most of
/// the way. The reachability half needs a Token Vault reader that does not
/// exist, and these tests assert that the connector says so rather than
/// implying success.
/// </remarks>
public sealed class ConnectionTestTests
{
    private static OracleCloudAdapter Adapter() =>
        new(
            new IntegrationSettings(
                IntegrationId: "oracle-cloud",
                PropertyId: "prop-kochi",
                PropertyCode: "KOCHI01",
                Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
                Currency: "INR",
                AmountTaxBasis: TaxBasis.Net),
            new NeverDrains());

    private static Dictionary<string, string> Complete() => new()
    {
        ["endpoint"] = "https://ohip.example.com",
        ["hotelCode"] = "KOCHI01",
        ["externalSystemCode"] = "HOTELOS",
        ["clientId"] = "hotelos_client",
        ["pmsUsername"] = "hotelos_kochi",
    };

    private static string[] AllSecrets() =>
        ["application-key", "pms-password", "client-secret"];

    [Fact]
    public async Task A_half_filled_configuration_names_what_is_missing()
    {
        var settings = Complete();
        settings.Remove("externalSystemCode");

        var found = await Adapter().TestAsync(settings, ["application-key"], default);

        Assert.Equal(ConnectionTestOutcome.ConfigurationIncomplete, found.Outcome);

        // Settings first, then secrets, in the order the form draws them — an
        // operator works down the screen rather than hunting.
        Assert.Equal(
            ["externalSystemCode", "pms-password", "client-secret"],
            found.Missing);
    }

    [Fact]
    public async Task The_sentence_names_them_too_so_a_person_need_not_read_a_list()
    {
        var found = await Adapter().TestAsync(Complete(), [], default);

        Assert.Contains("application-key", found.Detail);
        Assert.Contains("pms-password", found.Detail);
    }

    /// <summary>
    /// **The one that keeps this honest.** A complete configuration has been
    /// checked for completeness and not for reachability, and the outcome says
    /// so — a green light here would claim OHIP had answered when nothing
    /// dialled it.
    /// </summary>
    [Fact]
    public async Task A_complete_configuration_is_not_reported_as_reached()
    {
        var found = await Adapter().TestAsync(Complete(), AllSecrets(), default);

        Assert.NotEqual(ConnectionTestOutcome.Reached, found.Outcome);
        Assert.Equal(ConnectionTestOutcome.NotSupported, found.Outcome);
        Assert.Empty(found.Missing);
    }

    /// <summary>
    /// A credential is "set" by its name being stored, never by a value
    /// reaching this method.
    /// </summary>
    [Fact]
    public async Task Nothing_here_is_given_a_secret_value()
    {
        // The seam takes `IReadOnlyList<string>` — names. There is no parameter
        // a value could arrive in, which is bound 4 holding by shape rather
        // than by care, one layer below the wire that also cannot carry one.
        var found = await Adapter().TestAsync(Complete(), AllSecrets(), default);

        Assert.DoesNotContain("secret", found.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_missing_setting_and_a_missing_secret_are_reported_together()
    {
        var settings = Complete();
        settings["pmsUsername"] = "   ";

        var found = await Adapter().TestAsync(settings, ["application-key", "pms-password"], default);

        // Blank is absent — a whitespace username is not configured for a
        // password grant, and calling it present produces a 401 at poll time.
        Assert.Equal(["pmsUsername", "client-secret"], found.Missing);
    }

    /// <summary>A queue that is never drained: this suite dials nothing.</summary>
    private sealed class NeverDrains : IOhipQueue
    {
        public Task<IReadOnlyList<PolledPayload>> DrainAsync(
            IntegrationSettings settings, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "a connection test must not drain the queue: the queue is emptied "
                + "by reading, so testing by draining would discard a hotel's changes");
    }
}
