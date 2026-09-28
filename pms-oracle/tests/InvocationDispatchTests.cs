using System.Net;
using System.Text;
using System.Threading.Channels;
using Google.Protobuf;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Adapters;
using PmsOracle.Authentication;
using PmsOracle.Hosting;
using PmsOracle.Normalisation;
using PmsOracle.Vocabularies;
using static PmsOracle.Tests.TestExponent;
using System.Reflection;
using PmsOracle.Integrations;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// What this connector answers to each invocation kind — ADR 0194's vocabulary,
/// served rather than refused.
/// </summary>
/// <remarks>
/// <para>
/// <b>Driven through a real <see cref="ConnectorSession"/>.</b> A
/// <see cref="ConnectorInvocation"/> cannot be constructed from outside the
/// SDK, and that is the right constraint: it carries the correlation the
/// session stamps. So these push frames into an in-memory channel and read what
/// comes back, which also exercises the credential round trip — the connector
/// asking, this test answering as the Hub would, and the reply being matched to
/// the invocation that asked.
/// </para>
/// <para>
/// <b>What these cannot show.</b> The process serves only a channel the
/// Connector Runtime provides, and no Supervisor exists (<c>CONN-Q32a</c>). So
/// this proves the dispatch answers each kind correctly when driven; that
/// anything ever drives it is a different proof, on a tier no test in this
/// package can reach.
/// </para>
/// </remarks>
public class InvocationDispatchTests
{
    private static readonly IReadOnlyDictionary<string, string> Configured =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OhipCredentials.EndpointSetting] = "https://ohip.example",
            [OhipCredentials.HotelCodeSetting] = "KOCHI01",
            [OhipCredentials.ExternalSystemCodeSetting] = "HOTELOS",
            [OhipCredentials.ClientIdSetting] = "client",
            [OhipCredentials.PmsUsernameSetting] = "supervisor",
        };

    [Fact]
    public async Task A_test_asks_for_every_declared_secret_and_reports_what_ohip_answered()
    {
        await using var hub = new Hub(HttpStatusCode.OK);

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Test, Test(Configured), TestResult.Parser);

        // The three the manifest declares, asked for by name and inside the
        // invocation — the connector holds none of them between calls.
        Assert.Equal(OhipCredentials.SecretNames, hub.Asked);
        Assert.Equal(ConnectionTestOutcome.Reached, result.Outcome);
    }

    [Fact]
    public async Task A_denied_credential_reads_as_a_configuration_a_person_can_fix()
    {
        await using var hub = new Hub(HttpStatusCode.OK, denies: OhipCredentials.PmsPasswordSecret);

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Test, Test(Configured), TestResult.Parser);

        // Not a fault, and not a credential failure at OHIP: nobody set it, so
        // the screen that can set it is told which one — and OHIP is never
        // dialled with a grant that cannot succeed.
        Assert.Equal(ConnectionTestOutcome.ConfigurationIncomplete, result.Outcome);
        Assert.Contains(OhipCredentials.PmsPasswordSecret, result.Missing);
        Assert.Equal(0, hub.Dialled);
    }

    [Fact]
    public async Task A_source_that_never_answers_is_unreachable_rather_than_refused()
    {
        await using var hub = new Hub(status: null);

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Test, Test(Configured), TestResult.Parser);

        // The two send an operator to different people: this one to whoever
        // owns the network, not to whoever issues credentials.
        Assert.Equal(ConnectionTestOutcome.Unreachable, result.Outcome);
    }

    [Fact]
    public async Task A_rejected_credential_is_refused_and_says_so()
    {
        await using var hub = new Hub(HttpStatusCode.Unauthorized);

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Test, Test(Configured), TestResult.Parser);

        Assert.Equal(ConnectionTestOutcome.Refused, result.Outcome);
    }

    [Fact]
    public async Task A_drain_acquires_a_token_and_answers_what_the_queue_held()
    {
        await using var hub = new Hub(HttpStatusCode.OK);

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Drain, Drain(Configured), DrainResult.Parser);

        // An empty queue is the ordinary answer, not a failure (ADR 0194) — and
        // getting here at all means the credentials were requested inside the
        // invocation and a token was acquired with them.
        Assert.Empty(result.Payloads);
        Assert.Equal(OhipCredentials.SecretNames, hub.Asked);
    }

    /// <summary>
    /// The key crosses the wire — ADR 0246, <c>CONN-Q51</c> A1.
    /// </summary>
    /// <remarks>
    /// The queue's own tests prove <c>DrainedEvent.DedupeKey</c>. Only this one
    /// proves the mapping onto <see cref="DrainedPayload"/>, which is a single
    /// line that nothing else would fail on: a drain that dropped the key would
    /// still answer payloads, and the Hub would store changes it can never
    /// recognise again.
    /// </remarks>
    [Fact]
    public async Task A_drained_payload_carries_the_key_the_connector_chose()
    {
        await using var hub = new Hub(
            HttpStatusCode.OK,
            holding: """{"businessEventData":[{"businessEventId":{"id":"evt-9"}}]}""");

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Drain, Drain(Configured), DrainResult.Parser);

        var payload = Assert.Single(result.Payloads);
        Assert.Equal("ohip-business-event:evt-9", payload.DedupeKey);
    }

    /// <summary>
    /// The Hub is told when to ask again — <c>CONN-Q51</c> A2 — where this end
    /// can say it without the property's zone.
    /// </summary>
    [Fact]
    public async Task A_drain_states_the_interval_when_no_tight_window_is_configured()
    {
        await using var hub = new Hub(HttpStatusCode.OK);

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Drain, Drain(Configured), DrainResult.Parser);

        // `Configured` sets no tight window, so no zone can change the answer
        // and three hours is exact rather than a guess. Absent, the Hub would
        // wait its recovery ceiling — a wait for a source that failed, applied
        // to one that is working.
        Assert.True(result.HasNextPollAfterSeconds);
        Assert.Equal(
            (uint)OhipPollingSchedule.DefaultNormal.TotalSeconds,
            result.NextPollAfterSeconds);
    }

    /// <summary>
    /// A half of a check-in is answered as a member, with the key both halves
    /// share — ADR 0272's <c>join</c>.
    /// </summary>
    /// <remarks>
    /// <b>The two halves differ only by the case of one status</b> — `"Checked
    /// In"` is the contact half and `"CHECKED IN"` the room half (R6). That is
    /// why <c>OnSiteJoinKey</c>'s vocabulary is compared ordinally, and it is
    /// the fixture that can tell a case-folding implementation from this one:
    /// under <c>OrdinalIgnoreCase</c> both messages would claim the same part
    /// and the pair would never complete.
    /// </remarks>
    [Theory]
    [InlineData("Checked In", "ContactHalf")]
    [InlineData("CHECKED IN", "RoomHalf")]
    public async Task A_half_of_a_check_in_is_a_member_carrying_the_key_and_which_half(
        string status, string part)
    {
        await using var hub = new Hub(HttpStatusCode.OK);

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Join,
            Join(OracleOnSiteAdapter.StayPayload, Push(status)),
            JoinResult.Parser);

        Assert.Equal(JoinResult.OutcomeOneofCase.Member, result.OutcomeCase);
        Assert.Equal("Menon|Asha|2026-09-28", result.Member.Key);
        Assert.Equal(part, result.Member.Part);

        // The window is the connector's to state — only this end knows what its
        // source does — which is the same reason the drain states its interval.
        Assert.Equal(
            (uint)PartJoinInvocation.HoldWindow.TotalSeconds, result.Member.HoldSeconds);
    }

    /// <summary>
    /// Everything that is not a recognised half is whole, and that is not a
    /// refusal.
    /// </summary>
    /// <remarks>
    /// A message declared whole goes to normalisation, which refuses it naming
    /// the field. Answering <c>member</c> on a doubtful one would hold a
    /// payload for a partner that never arrives, so a rejection an operator can
    /// act on would expire as <c>join_window_expired</c> instead.
    /// </remarks>
    [Theory]
    [InlineData(OracleOnSiteAdapter.RoomStatusPayload, """{"Status":"Checked In"}""")]
    [InlineData(OracleCloudAdapter.ReservationPayload, """{"Status":"Checked In"}""")]
    [InlineData(OracleOnSiteAdapter.StayPayload, """{"Status":"Due In"}""")]
    [InlineData(OracleOnSiteAdapter.StayPayload, """{"Status":"Nonsense"}""")]
    [InlineData(OracleOnSiteAdapter.StayPayload, """{"Status":"Checked In"}""")]
    [InlineData(OracleOnSiteAdapter.StayPayload, "not json at all")]
    public async Task Anything_that_is_not_a_recognised_half_is_whole(string kind, string body)
    {
        await using var hub = new Hub(HttpStatusCode.OK);

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Join, Join(kind, body), JoinResult.Parser);

        Assert.Equal(JoinResult.OutcomeOneofCase.Whole, result.OutcomeCase);
    }

    /// <summary>
    /// The protocol and the retired seam give one answer — they share the
    /// decision rather than each holding a copy.
    /// </summary>
    [Fact]
    public async Task The_adapter_and_the_invocation_agree_about_which_half_a_message_is()
    {
        await using var hub = new Hub(HttpStatusCode.OK);

        var overWire = await hub.InvokeAsync(
            ConnectorProtocolKinds.Join,
            Join(OracleOnSiteAdapter.StayPayload, Push("Checked In")),
            JoinResult.Parser);

        var inProcess = new OracleOnSiteAdapter(OnSiteSettings(), MinorUnits)
            .JoinFor(Encoding.UTF8.GetBytes(Push("Checked In")), OracleOnSiteAdapter.StayPayload);

        Assert.NotNull(inProcess);
        Assert.Equal(inProcess.Key, overWire.Member.Key);
        Assert.Equal(inProcess.Part, overWire.Member.Part);
    }


    /// <summary>One configured on-site integration, for the agreement test.</summary>
    private static IntegrationSettings OnSiteSettings() => new(
        IntegrationId: "oracle-onpremise",
        PropertyId: "prop-kochi",
        PropertyCode: "KOCHI01",
        Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
        Currency: "INR",
        AmountTaxBasis: TaxBasis.Net,
        GuaranteeMaximumFreshness: null);
    /// <summary>One on-site message, as the agent sends it.</summary>
    /// <remarks>
    /// <b>The arrival date carries a time because the agent's does</b> —
    /// <c>OnSiteDateReading.Format</c>. It read <c>2026-09-28</c> until
    /// 2026-09-28, a bare ISO day the normaliser REFUSES; these tests were
    /// green on a payload the connector cannot normalise, because the join key
    /// then parsed loosely and nothing compared the two readers. A fixture is
    /// a claim about what the source sends, and this one was not true.
    /// </remarks>
    private static string Push(string status) =>
        $$"""
        {"Status":"{{status}}","Surname":"Menon","FirstName":"Asha","ArrivalDate":"2026-09-28T00:00:00"}
        """;

    [Fact]
    public async Task A_pushed_payload_is_keyed_over_the_protocol()
    {
        // For a `delivery: push` integration this is the ONLY way a payload is
        // ever keyed: there is no drain to carry a key and the Hub never
        // computes one (ADR 0255 §2). Both on-site integrations declared
        // `dedupe_key` while nothing served it, so every pushed payload was
        // unkeyable and the declaration faulted with UnsupportedKind.
        await using var hub = new Hub(HttpStatusCode.OK);
        var body = Push("Due In");

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.DedupeKey,
            new DedupeKeyInvocation
            {
                PayloadKind = OracleOnSiteAdapter.StayPayload,
                Payload = ByteString.CopyFromUtf8(body),
            }.ToByteArray(),
            DedupeKeyResult.Parser);

        Assert.Equal(
            PayloadIdentity.For(
                OracleOnSiteAdapter.StayPayload, Encoding.UTF8.GetBytes(body)),
            result.DedupeKey);
    }

    [Fact]
    public async Task Every_kind_the_dispatch_serves_is_dispatched_and_every_other_is_refused()
    {
        // **The two-way walk that keeps `Served` honest.** It used to be typed
        // out in the refusal's message, which told an operator the connector
        // served `test` and `drain` for days after `join` was added. The
        // population is read off the protocol rather than listed here, so a
        // frame added to the SDK is covered the day it lands.
        var kinds = typeof(ConnectorProtocolKinds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(kinds);
        Assert.Superset(InvocationDispatch.ServedKinds.ToHashSet(), kinds.ToHashSet());

        // **Three buckets, not two.** `cancel` is a control frame the session
        // consumes, so it never reaches the dispatch as a request and is
        // neither served nor refused by it. Excluding it silently would leave a
        // hole in the walk, so it is asserted below in its own right — the
        // deliberate divergence stated where it diverges.
        var consumedBySession = new[] { ConnectorProtocolKinds.Cancel };

        await using var hub = new Hub(HttpStatusCode.OK);

        foreach (var kind in consumedBySession)
        {
            var control = await hub.ExchangeAsync(kind, ReadOnlyMemory<byte>.Empty);

            Assert.False(InvocationDispatch.ServedKinds.Contains(kind), kind);
            Assert.DoesNotContain("does not serve the invocation kind",
                Encoding.UTF8.GetString(control.Payload.Span), StringComparison.Ordinal);
        }

        foreach (var kind in kinds.Except(consumedBySession))
        {
            // The ROLE cannot be asserted either way: a served kind given an
            // empty payload may answer a Reply (`join` reads it as whole) or a
            // Fault (`drain` has no credentials). Only the refusal's own
            // sentence separates "not served" from "served and unhappy".
            var reply = await hub.ExchangeAsync(kind, ReadOnlyMemory<byte>.Empty);
            var refused = reply.Role == ConnectorFrameRole.Fault
                && Encoding.UTF8.GetString(reply.Payload.Span)
                    .Contains("does not serve the invocation kind", StringComparison.Ordinal);

            // The message names the kind: a walk that fails without saying
            // WHICH member disagreed sends the reader back to run it by hand.
            Assert.True(
                !InvocationDispatch.ServedKinds.Contains(kind) == refused,
                $"'{kind}' — served: {InvocationDispatch.ServedKinds.Contains(kind)}, "
                + $"refused: {refused}");
        }
    }

    /// <summary>A <c>join</c> invocation carrying one payload.</summary>
    private static ReadOnlyMemory<byte> Join(string payloadKind, string body) =>
        new JoinInvocation
        {
            PayloadKind = payloadKind,
            Payload = ByteString.CopyFromUtf8(body),
        }.ToByteArray();

    /// <summary>
    /// And says nothing where it would have to guess — ADR 0220's missing
    /// property context, stated as an absence rather than as a number.
    /// </summary>
    [Fact]
    public async Task A_drain_states_no_interval_when_the_answer_depends_on_the_propertys_zone()
    {
        await using var hub = new Hub(HttpStatusCode.OK);

        var settings = new Dictionary<string, string>(Configured, StringComparer.Ordinal)
        {
            [OhipPollingSchedule.TightFromSetting] = "14:00",
            [OhipPollingSchedule.TightUntilSetting] = "18:00",
        };

        var result = await hub.InvokeAsync(
            ConnectorProtocolKinds.Drain, Drain(settings), DrainResult.Parser);

        // Sending `Normal` here would state an interval this connector knows
        // to be wrong for four hours of every day, and would poll slowly
        // through exactly the window the property asked to be watched. The
        // field is optional so that absent can mean the connector did not say.
        Assert.False(result.HasNextPollAfterSeconds);
    }

    /// <summary>
    /// A drain has nowhere to say "your configuration is incomplete":
    /// DrainResult carries payloads only, so an empty reply would report a
    /// healthy poll of a queue that was never asked.
    /// </summary>
    [Fact]
    public async Task An_incomplete_configuration_faults_rather_than_draining_nothing()
    {
        await using var hub = new Hub(HttpStatusCode.OK, denies: OhipCredentials.PmsPasswordSecret);

        var fault = await hub.FaultAsync(ConnectorProtocolKinds.Drain, Drain(Configured));

        Assert.Contains(OhipCredentials.PmsPasswordSecret, fault, StringComparison.Ordinal);
        Assert.Contains("nothing was lost", fault, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_kind_is_refused_by_name_and_says_what_is_served()
    {
        await using var hub = new Hub(HttpStatusCode.OK);

        var fault = await hub.FaultAsync("reconcile", ReadOnlyMemory<byte>.Empty);

        // A newer Hub talking to an older connector: the operator needs to know
        // which of the two to upgrade, so the served kinds are named.
        Assert.Contains("'reconcile'", fault);
        Assert.Contains(ConnectorProtocolKinds.Test, fault);
        Assert.Contains(ConnectorProtocolKinds.Drain, fault);
    }

    private static ReadOnlyMemory<byte> Drain(IReadOnlyDictionary<string, string> settings)
    {
        var invocation = new DrainInvocation();

        foreach (var (key, value) in settings)
        {
            invocation.Settings[key] = value;
        }

        return invocation.ToByteArray();
    }

    private static ReadOnlyMemory<byte> Test(IReadOnlyDictionary<string, string> settings)
    {
        var invocation = new TestInvocation();

        foreach (var (key, value) in settings)
        {
            invocation.Settings[key] = value;
        }

        return invocation.ToByteArray();
    }

}
