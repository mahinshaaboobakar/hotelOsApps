using System.Text;
using System.Threading.Channels;
using HotelOS.Connector;
using PmsOracle.Hosting;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The connector process's lifecycle — ADR 0188 — driven through an in-memory
/// channel.
/// </summary>
/// <remarks>
/// <para>
/// <b>A test double choosing a transport is not this package choosing one.</b>
/// Production passes <c>transport: null</c> because nothing implements
/// <see cref="IConnectorTransport"/>; here a queue stands in, so the part that
/// is this package's — what happens with a bootstrap, without one, and to an
/// invocation — can be shown working before the platform supplies the part that
/// is not.
/// </para>
/// <para>
/// The two refusal paths were also run as the real executable, by hand and with
/// <c>HOTELOS_CONNECTOR_CHANNEL</c> set: exit 2 and exit 3, each with its own
/// sentence and neither echoing the channel value. That is evidence for one run;
/// these are the check.
/// </para>
/// </remarks>
public class ConnectorEntryTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task started_by_hand_it_does_nothing_and_says_why()
    {
        var transport = new InMemoryTransport(new InMemoryChannel());
        var diagnostics = new StringWriter();

        var code = await ConnectorEntry.RunAsync(
            bootstrap: null, transport, InvocationRefusal.HandleAsync, diagnostics, default);

        Assert.Equal(ConnectorEntry.NotLaunchedByRuntime, code);
        // A transport WAS available and was not touched: the bootstrap is checked
        // first, so a process nobody launched cannot attach to anything.
        Assert.Equal(0, transport.Attached);
        Assert.Contains(ConnectorBootstrap.ChannelVariable, diagnostics.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task a_bootstrap_with_no_transport_attaches_nothing_and_does_not_echo_the_channel()
    {
        const string channel = "a-value-this-process-must-not-repeat";
        var diagnostics = new StringWriter();

        var code = await ConnectorEntry.RunAsync(
            new ConnectorBootstrap(channel), transport: null, InvocationRefusal.HandleAsync, diagnostics, default);

        Assert.Equal(ConnectorEntry.NoTransport, code);
        Assert.DoesNotContain(channel, diagnostics.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void the_two_absences_are_two_codes_and_neither_reads_as_success()
    {
        // Opposite remedies — launch it properly, or ship a transport — so one
        // code for both would hide the second inside the first.
        Assert.NotEqual(ConnectorEntry.NotLaunchedByRuntime, ConnectorEntry.NoTransport);
        Assert.NotEqual(ConnectorEntry.Served, ConnectorEntry.NotLaunchedByRuntime);
        Assert.NotEqual(ConnectorEntry.Served, ConnectorEntry.NoTransport);
    }

    /// <summary>
    /// A kind this connector does not serve is refused by name — and the
    /// subject changed with the contract: this drove <c>"test"</c> until ADR
    /// 0194's vocabulary landed and <see cref="InvocationDispatch"/> began
    /// serving it, so asserting the old blanket refusal here would have pinned
    /// a contract the package no longer has.
    /// </summary>
    [Fact]
    public async Task an_unserved_kind_is_refused_by_name_rather_than_guessed_at()
    {
        var channel = new InMemoryChannel();
        var transport = new InMemoryTransport(channel);
        var bootstrap = new ConnectorBootstrap("the-runtime's-channel");

        var running = ConnectorEntry.RunAsync(
            bootstrap, transport, InvocationRefusal.HandleAsync, TextWriter.Null, default);

        await channel.Inbound.Writer.WriteAsync(Request("reconcile"));

        // Read the answer BEFORE closing: the session closes the channel ahead
        // of waiting for its handlers, so an answer racing the close is dropped
        // — correctly, for a closed channel, and fatally for a test that closed
        // first and then looked.
        var answer = await channel.Outbound.Reader.ReadAsync().AsTask().WaitAsync(Patience);

        channel.Inbound.Writer.Complete();
        Assert.Equal(ConnectorEntry.Served, await running.WaitAsync(Patience));

        Assert.Equal(ConnectorFrameRole.Fault, answer.Role);
        Assert.Equal("reconcile", answer.Kind);
        Assert.Contains(
            "does not serve the invocation kind 'reconcile'",
            Encoding.UTF8.GetString(answer.Payload.Span),
            StringComparison.Ordinal);
        Assert.Same(bootstrap, transport.AttachedWith);
    }

    [Fact]
    public async Task a_handler_that_answers_is_answered_through_the_same_path()
    {
        // The positive control for the test above. Without it, a session that
        // faulted EVERY invocation — broken plumbing rather than a refusal —
        // would pass that test identically.
        var channel = new InMemoryChannel();
        var reply = "served"u8.ToArray();

        var running = ConnectorEntry.RunAsync(
            new ConnectorBootstrap("the-runtime's-channel"),
            new InMemoryTransport(channel),
            (_, _) => ValueTask.FromResult<ReadOnlyMemory<byte>>(reply),
            TextWriter.Null,
            default);

        await channel.Inbound.Writer.WriteAsync(Request("anything"));
        var answer = await channel.Outbound.Reader.ReadAsync().AsTask().WaitAsync(Patience);

        channel.Inbound.Writer.Complete();
        await running.WaitAsync(Patience);

        Assert.Equal(ConnectorFrameRole.Reply, answer.Role);
        Assert.Equal(reply, answer.Payload.ToArray());
    }

    private static ConnectorFrame Request(string kind) =>
        new(Guid.NewGuid(), ConnectorFrameRole.Request, kind, ReadOnlyMemory<byte>.Empty, Within: null);

    /// <summary>Two queues standing in for whatever the platform picks.</summary>
    private sealed class InMemoryChannel : IConnectorChannel
    {
        public Channel<ConnectorFrame> Inbound { get; } = Channel.CreateUnbounded<ConnectorFrame>();

        public Channel<ConnectorFrame> Outbound { get; } = Channel.CreateUnbounded<ConnectorFrame>();

        public ValueTask SendAsync(ConnectorFrame frame, CancellationToken cancellationToken) =>
            Outbound.Writer.WriteAsync(frame, cancellationToken);

        public async ValueTask<ConnectorFrame?> ReceiveAsync(CancellationToken cancellationToken) =>
            await Inbound.Reader.WaitToReadAsync(cancellationToken) && Inbound.Reader.TryRead(out var frame)
                ? frame
                : null;

        public ValueTask DisposeAsync()
        {
            Outbound.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Hands out one channel and records what it was attached with.</summary>
    private sealed class InMemoryTransport(InMemoryChannel channel) : IConnectorTransport
    {
        public int Attached { get; private set; }

        public ConnectorBootstrap? AttachedWith { get; private set; }

        public ValueTask<IConnectorChannel> AttachAsync(
            ConnectorBootstrap bootstrap, CancellationToken cancellationToken)
        {
            Attached++;
            AttachedWith = bootstrap;
            return ValueTask.FromResult<IConnectorChannel>(channel);
        }
    }
}
