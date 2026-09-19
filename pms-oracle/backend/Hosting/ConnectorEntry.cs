using HotelOS.Connector;

namespace PmsOracle.Hosting;

/// <summary>
/// The connector process's whole life: take the Runtime's bootstrap, attach,
/// serve until the channel closes — ADR 0188.
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything <c>Main</c> would decide is a parameter here</b>, so the
/// lifecycle is testable without a Runtime and <c>Program.cs</c> is only the
/// composition. In particular the <see cref="IConnectorTransport"/> arrives from
/// outside: ADR 0188 keeps the transport out of this package — <i>"Main never
/// learns whether the platform chose named pipes, Unix sockets or loopback
/// gRPC"</i> — and a transport chosen in here would be the entry point defining
/// the channel, which <c>CONN-Q27</c> refused by name.
/// </para>
/// <para>
/// <b>Two absences, two exit codes.</b> Started by hand, there is no bootstrap;
/// started by the Runtime into a build with no transport, there is a bootstrap
/// and nothing to attach it with. They have opposite remedies — launch it
/// properly, or ship a transport — so folding them into one code would hide the
/// second inside the first.
/// </para>
/// <para>
/// <b>The codes are this package's diagnostics, not a Runtime contract.</b>
/// Nothing rules what the Supervisor does with a connector's exit code, so
/// nothing should key behaviour on these values until something does; the
/// message on <c>diagnostics</c> is the part written for a person.
/// </para>
/// <para>
/// <b>Stopping is the channel closing.</b> <see cref="ConnectorSession.RunAsync"/>
/// returns when the Runtime closes the channel, cancelling in-flight invocations
/// and dropping any answer that arrives after — so no signal handling is written
/// here, and none is guessed at.
/// </para>
/// </remarks>
public static class ConnectorEntry
{
    /// <summary>The channel was served until the Runtime closed it.</summary>
    public const int Served = 0;

    /// <summary>No bootstrap: this process was not started by the Connector Runtime.</summary>
    public const int NotLaunchedByRuntime = 2;

    /// <summary>A bootstrap, but no transport to attach with.</summary>
    public const int NoTransport = 3;

    /// <summary>Run the connector process to completion.</summary>
    /// <param name="bootstrap">
    /// What the Runtime launched this process with —
    /// <see cref="ConnectorBootstrap.FromEnvironment"/> in production, and
    /// <c>null</c> when nothing did.
    /// </param>
    /// <param name="transport">
    /// The platform's transport, or <c>null</c> when this build has none.
    /// </param>
    /// <param name="handler">What serves each invocation the Hub sends.</param>
    /// <param name="diagnostics">
    /// Where a person-readable reason is written when the process cannot serve.
    /// <b>The channel string is never written to it</b> — the bootstrap is the
    /// Runtime's to describe, and echoing it into a log is how a value nobody
    /// classified ends up somewhere it was never meant to be.
    /// </param>
    /// <param name="cancellationToken">Cancellation of the whole process.</param>
    /// <returns>One of <see cref="Served"/>, <see cref="NotLaunchedByRuntime"/> or <see cref="NoTransport"/>.</returns>
    public static async Task<int> RunAsync(
        ConnectorBootstrap? bootstrap,
        IConnectorTransport? transport,
        ConnectorInvocationHandler handler,
        TextWriter diagnostics,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(diagnostics);

        if (bootstrap is null)
        {
            await diagnostics.WriteLineAsync(
                $"pms-oracle was not launched by the Connector Runtime: {ConnectorBootstrap.ChannelVariable} " +
                "is not set. It serves only a channel the Runtime provides, so it has nothing to do " +
                "and has done nothing.");
            return NotLaunchedByRuntime;
        }

        if (transport is null)
        {
            // *This said the platform's transport was "none … implemented yet"
            // until the SDK's Named Pipe adapter landed (platform 84a74011).
            // Now the only way here is a platform the adapter does not run on.*
            await diagnostics.WriteLineAsync(
                "pms-oracle was given a channel bootstrap but has no transport to attach it with. " +
                "The platform's transport is a Windows Named Pipe (ADR 0196), and this process is " +
                "not running on Windows, so nothing was attached and no invocation was served.");
            return NoTransport;
        }

        await using var channel = await transport.AttachAsync(bootstrap, cancellationToken);
        await new ConnectorSession(channel).RunAsync(handler, cancellationToken);
        return Served;
    }
}
