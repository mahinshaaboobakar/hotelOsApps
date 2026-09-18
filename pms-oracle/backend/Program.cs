using HotelOS.Connector;
using PmsOracle.Hosting;

// The pms-oracle connector process — composition only; the lifecycle is
// `Hosting/ConnectorEntry.cs`, where it can be tested without a Runtime.
//
// `transport: null` is deliberate and it is the whole of what this process
// cannot do yet. ADR 0188 keeps the transport out of the package — `Main`
// binds to a Runtime-provided bootstrap and never learns whether the platform
// chose pipes, sockets or gRPC — and nothing implements `IConnectorTransport`
// today. Picking one here would be this package defining the channel, which
// `CONN-Q27` refused by name. When the platform supplies a transport, it is
// passed here and nothing else changes.
//
// `CancellationToken.None` because stopping is the channel closing: the session
// returns when the Runtime closes it, and no signal is guessed at.
return await ConnectorEntry.RunAsync(
    ConnectorBootstrap.FromEnvironment(),
    transport: null,
    InvocationRefusal.HandleAsync,
    Console.Error,
    CancellationToken.None);
