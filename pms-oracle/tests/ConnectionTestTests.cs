using System.Net;

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
    private static OracleCloudAdapter Adapter(HttpStatusCode? answers = null) =>
        new(
            new IntegrationSettings(
                IntegrationId: "oracle-cloud",
                PropertyId: "prop-kochi",
                PropertyCode: "KOCHI01",
                Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
                Currency: "INR",
                AmountTaxBasis: TaxBasis.Net),
            new NeverDrains(),
            new HttpClient(new Answers(answers)) { Timeout = TimeSpan.FromSeconds(5) });

    private static Dictionary<string, string> Complete() => new()
    {
        ["endpoint"] = "https://ohip.example.com",
        ["hotelCode"] = "KOCHI01",
        ["externalSystemCode"] = "HOTELOS",
        ["clientId"] = "hotelos_client",
        ["pmsUsername"] = "hotelos_kochi",
    };

    private static Dictionary<string, string> AllSecrets() => new()
    {
        ["application-key"] = "the-app-key",
        ["pms-password"] = "the-password",
        ["client-secret"] = "the-client-secret",
    };

    private static Dictionary<string, string> Only(params string[] names) =>
        names.ToDictionary(name => name, _ => "a-value", StringComparer.Ordinal);

    [Fact]
    public async Task A_half_filled_configuration_names_what_is_missing()
    {
        var settings = Complete();
        settings.Remove("externalSystemCode");

        var found = await Adapter().TestAsync(settings, Only("application-key"), default);

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
        var found = await Adapter().TestAsync(Complete(), new Dictionary<string, string>(), default);

        Assert.Contains("application-key", found.Detail);
        Assert.Contains("pms-password", found.Detail);
    }

    /// <summary>A complete set that OHIP accepts is REACHED, and only then.</summary>
    /// <remarks>
    /// **This is what makes VALIDATED mean something.** The finding comes from
    /// a token request a tenancy answered, through the same credential path the
    /// poller will use — so a green result means what a successful poll would
    /// mean, rather than that a separate test path happened to work.
    /// </remarks>
    [Fact]
    public async Task A_tenancy_that_issues_a_token_is_reached()
    {
        var found = await Adapter(HttpStatusCode.OK).TestAsync(
            Complete(), AllSecrets(), default);

        Assert.Equal(ConnectionTestOutcome.Reached, found.Outcome);
        Assert.Empty(found.Missing);
    }

    /// <summary>Refused and unreachable are kept apart, and this is why.</summary>
    /// <remarks>
    /// They send an operator to different people: one re-issues a credential,
    /// the other opens a firewall. A 403 is deliberately a refusal with its own
    /// sentence — the user exists and lacks a grant, an OPERA permissions
    /// problem, and telling somebody to re-check their password there sends
    /// them the wrong way for an afternoon.
    /// </remarks>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ConnectionTestOutcome.Refused)]
    [InlineData(HttpStatusCode.Forbidden, ConnectionTestOutcome.Refused)]
    [InlineData(HttpStatusCode.NotFound, ConnectionTestOutcome.Unreachable)]
    [InlineData(HttpStatusCode.TooManyRequests, ConnectionTestOutcome.Unreachable)]
    [InlineData(HttpStatusCode.InternalServerError, ConnectionTestOutcome.Unreachable)]
    public void What_the_tenancy_answered_decides_which_finding_it_is(
        HttpStatusCode status, ConnectionTestOutcome expected)
    {
        Assert.Equal(expected, OhipTokenAttempt.Read(status).Outcome);
    }

    /// <summary>A host that does not answer is unreachable, never refused.</summary>
    [Fact]
    public async Task A_host_that_does_not_answer_is_unreachable()
    {
        // The handler throws HttpRequestException, which is what a closed port,
        // a bad name or a firewall produces.
        var found = await Adapter().TestAsync(Complete(), AllSecrets(), default);

        Assert.Equal(ConnectionTestOutcome.Unreachable, found.Outcome);

        // Not a credential problem. The sentence must not send somebody to
        // change a password over a network fault.
        Assert.DoesNotContain("credential", found.Detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>An incomplete set is answered without dialling anyone.</summary>
    /// <remarks>
    /// It could not authenticate anyway, and asking a vendor to refuse a
    /// request we already know is unfinished spends a rate limit to learn
    /// nothing.
    /// </remarks>
    [Fact]
    public async Task An_incomplete_set_never_reaches_the_network()
    {
        var handler = new Answers(HttpStatusCode.OK);

        var adapter = new OracleCloudAdapter(
            Settings(), new NeverDrains(), new HttpClient(handler));

        var found = await adapter.TestAsync(Complete(), Only("application-key"), default);

        Assert.Equal(ConnectionTestOutcome.ConfigurationIncomplete, found.Outcome);
        Assert.Equal(0, handler.Sent);
    }

    [Fact]
    public async Task A_missing_setting_and_a_missing_secret_are_reported_together()
    {
        var settings = Complete();
        settings["pmsUsername"] = "   ";

        var found = await Adapter().TestAsync(settings, Only("application-key", "pms-password"), default);

        // Blank is absent — a whitespace username is not configured for a
        // password grant, and calling it present produces a 401 at poll time.
        Assert.Equal(["pmsUsername", "client-secret"], found.Missing);
    }

    private static IntegrationSettings Settings() =>
        new(
            IntegrationId: "oracle-cloud",
            PropertyId: "prop-kochi",
            PropertyCode: "KOCHI01",
            Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
            Currency: "INR",
            AmountTaxBasis: TaxBasis.Net);

    /// <summary>A tenancy that answers what the test says, and counts asks.</summary>
    /// <remarks>
    /// **A fake handler rather than a live dial.** Nobody here has an OHIP
    /// tenancy, and one that existed would make this suite depend on a vendor's
    /// availability to prove our own status mapping. `null` throws, which is
    /// what a closed port produces.
    /// </remarks>
    private sealed class Answers(HttpStatusCode? status) : HttpMessageHandler
    {
        public int Sent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Sent++;

            return status is { } answered
                ? Task.FromResult(new HttpResponseMessage(answered))
                : throw new HttpRequestException("no route to host");
        }
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
