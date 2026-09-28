// The Hub's end of the Connector Protocol, in memory.
//
// **Extracted along a boundary the file already drew.**
// `InvocationDispatchTests` reached 331 code lines against ADR 0036's 300 when
// the `join` cases landed, and it held two subjects: what this connector
// ANSWERS, and the other end that ASKS. The second was already a class of its
// own — this is that class, unchanged, and a size split would have cut the
// first subject in half instead.
//
// `internal` rather than nested and private: a second test class driving an
// invocation would otherwise need its own copy of the channel pair, and two
// harnesses drift in the half nobody reads — which kinds each one bothered to
// drive.

using System.Net;
using System.Text;
using System.Threading.Channels;
using Google.Protobuf;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Authentication;
using PmsOracle.Hosting;
using Xunit;

namespace PmsOracle.Tests;
/// <summary>The Hub's end: dispatches one invocation and answers credential requests.</summary>
internal sealed class Hub : IAsyncDisposable
{
    private readonly Channel<ConnectorFrame> _toConnector = Channel.CreateUnbounded<ConnectorFrame>();
    private readonly Channel<ConnectorFrame> _fromConnector = Channel.CreateUnbounded<ConnectorFrame>();
    private readonly Answers _answers;
    private readonly string? _denies;
    private readonly Task _serving;
    private readonly HttpClient _http;

    public Hub(HttpStatusCode? status, string? denies = null, string? holding = null)
    {
        _answers = new Answers(status, holding);
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
    /// <param name="status">What the token endpoint answers.</param>
    /// <param name="holding">
    /// What the queue is holding, in OHIP's own shape; <c>null</c> for an
    /// empty queue. Spelled here rather than built from a helper because a
    /// double that sent a shape of its own invention would prove the wire
    /// carries whatever this file imagines — which is the whole failure
    /// this connector already found once.
    /// </param>
    private sealed class Answers(HttpStatusCode? status, string? holding = null) : HttpMessageHandler
    {
        public int Sent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Sent++;

            // The queue is a different endpoint from the token, and OHIP
            // says "empty" with 204. A double that answered a token body
            // here would be testing a shape the source never sends.
            if (request.RequestUri!.AbsolutePath.EndsWith("businessEvents", StringComparison.Ordinal))
            {
                return Task.FromResult(holding is { } page
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            page, System.Text.Encoding.UTF8, "application/json"),
                    }
                    : new HttpResponseMessage(HttpStatusCode.NoContent));
            }

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
