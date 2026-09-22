using System.Net;
using System.Threading.Channels;
using Google.Protobuf;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Adapters;
using PmsOracle.Authentication;
using PmsOracle.Hosting;
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
    public async Task A_drain_is_understood_and_refused_with_both_missing_halves()
    {
        await using var hub = new Hub(HttpStatusCode.OK);

        var fault = await hub.FaultAsync(
            ConnectorProtocolKinds.Drain, new DrainInvocation().ToByteArray());

        // Understood, not unrecognised — and the refusal names the transport
        // and the settings reader separately, because they are different work.
        Assert.Contains("understands 'drain'", fault);
        Assert.Contains(nameof(IOhipQueue), fault);
        Assert.Contains(nameof(PmsOracle.Normalisation.IntegrationSettings), fault);
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

    private static ReadOnlyMemory<byte> Test(IReadOnlyDictionary<string, string> settings)
    {
        var invocation = new TestInvocation();

        foreach (var (key, value) in settings)
        {
            invocation.Settings[key] = value;
        }

        return invocation.ToByteArray();
    }

    /// <summary>The Hub's end: dispatches one invocation and answers credential requests.</summary>
    private sealed class Hub : IAsyncDisposable
    {
        private readonly Channel<ConnectorFrame> _toConnector = Channel.CreateUnbounded<ConnectorFrame>();
        private readonly Channel<ConnectorFrame> _fromConnector = Channel.CreateUnbounded<ConnectorFrame>();
        private readonly Answers _answers;
        private readonly string? _denies;
        private readonly Task _serving;
        private readonly HttpClient _http;

        public Hub(HttpStatusCode? status, string? denies = null)
        {
            _answers = new Answers(status);
            _denies = denies;
            _http = new HttpClient(_answers) { Timeout = TimeSpan.FromSeconds(5) };

            _serving = new ConnectorSession(new Pair(_toConnector, _fromConnector))
                .RunAsync(new InvocationDispatch(_http).HandleAsync, CancellationToken.None);
        }

        /// <summary>Credential names the connector asked for, in order.</summary>
        public List<string> Asked { get; } = [];

        /// <summary>How many times OHIP was actually dialled.</summary>
        public int Dialled => _answers.Sent;

        public async Task<T> InvokeAsync<T>(string kind, ReadOnlyMemory<byte> payload, MessageParser<T> parser)
            where T : IMessage<T>
        {
            var reply = await ExchangeAsync(kind, payload);

            Assert.Equal(ConnectorFrameRole.Reply, reply.Role);

            return parser.ParseFrom(reply.Payload.Span);
        }

        public async Task<string> FaultAsync(string kind, ReadOnlyMemory<byte> payload)
        {
            var reply = await ExchangeAsync(kind, payload);

            Assert.Equal(ConnectorFrameRole.Fault, reply.Role);

            return System.Text.Encoding.UTF8.GetString(reply.Payload.Span);
        }

        private async Task<ConnectorFrame> ExchangeAsync(string kind, ReadOnlyMemory<byte> payload)
        {
            var asked = Guid.NewGuid();

            await _toConnector.Writer.WriteAsync(
                new ConnectorFrame(asked, ConnectorFrameRole.Request, kind, payload, Within: null));

            while (true)
            {
                var frame = await _fromConnector.Reader.ReadAsync().AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(10));

                // A credential request belongs to the invocation it was made
                // in, and the session says so: `within` is stamped by the SDK,
                // never by the connector.
                if (frame.Role is ConnectorFrameRole.Request)
                {
                    Assert.Equal(asked, frame.Within);
                    await AnswerCredentialAsync(frame);
                    continue;
                }

                return frame;
            }
        }

        private async Task AnswerCredentialAsync(ConnectorFrame request)
        {
            Assert.Equal(ConnectorProtocolKinds.CredentialRequest, request.Kind);

            var asked = CredentialRequest.Parser.ParseFrom(request.Payload.Span);

            Asked.Add(asked.CredentialName);

            var response = string.Equals(asked.CredentialName, _denies, StringComparison.Ordinal)
                ? new CredentialResponse
                {
                    Denied = new CredentialDenied
                    {
                        Reason = CredentialDenialReason.NotConfigured,
                    },
                }
                : new CredentialResponse
                {
                    Granted = new CredentialGranted
                    {
                        Material = ByteString.CopyFromUtf8($"{asked.CredentialName}-value"),
                    },
                };

            await _toConnector.Writer.WriteAsync(
                new ConnectorFrame(
                    request.CorrelationId,
                    ConnectorFrameRole.Reply,
                    ConnectorProtocolKinds.CredentialResponse,
                    response.ToByteArray(),
                    Within: null));
        }

        public async ValueTask DisposeAsync()
        {
            _toConnector.Writer.TryComplete();
            await _serving;
            _http.Dispose();
        }

        /// <summary>The connector's side of the in-memory channel.</summary>
        private sealed class Pair(
            Channel<ConnectorFrame> inbound, Channel<ConnectorFrame> outbound) : IConnectorChannel
        {
            public ValueTask SendAsync(ConnectorFrame frame, CancellationToken cancellationToken) =>
                outbound.Writer.WriteAsync(frame, cancellationToken);

            public async ValueTask<ConnectorFrame?> ReceiveAsync(CancellationToken cancellationToken) =>
                await inbound.Reader.WaitToReadAsync(cancellationToken)
                && inbound.Reader.TryRead(out var frame)
                    ? frame
                    : null;

            public ValueTask DisposeAsync()
            {
                outbound.Writer.TryComplete();
                return ValueTask.CompletedTask;
            }
        }

        /// <summary>OHIP, as a status code or a refusal to connect at all.</summary>
        private sealed class Answers(HttpStatusCode? status) : HttpMessageHandler
        {
            public int Sent { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Sent++;

                return status is { } answered
                    ? Task.FromResult(new HttpResponseMessage(answered)
                {
                    // OHIP answers a granted password grant with a token and its
                    // lifetime — `providers/oracle/cloud/dto/jpa/OracleCloudAuthToken.java:20-25`.
                    // An empty 200 is a shape the source never sends, and a double
                    // that sent one would be testing a different contract.
                    Content = new StringContent(
                        """{"access_token":"ohip-token","expires_in":3600}""",
                        System.Text.Encoding.UTF8,
                        "application/json"),
                })
                    : throw new HttpRequestException("no route to host");
            }
        }
    }
}
