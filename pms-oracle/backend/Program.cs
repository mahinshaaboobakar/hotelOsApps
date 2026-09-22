using HotelOS.Connector;
using PmsOracle.Hosting;

// The pms-oracle connector process — composition only; the lifecycle is
// `Hosting/ConnectorEntry.cs`, where it can be tested without a Runtime.
//
// The transport is the platform's — the SDK's `NamedPipeConnectorTransport`,
// ADR 0196 and ADR 0208 — and this package only passes it. Nothing here chooses
// or creates the channel, which `CONN-Q27` refused by name; the adapter opens
// the handle the Supervisor created and this process inherited.
//
// On anything but Windows there is no platform transport, so it is `null` and
// ConnectorEntry says so (`NoTransport`) rather than attaching to nothing.
//
// *Until the platform's 84a74011 this passed `transport: null` everywhere,
// because nothing implemented `IConnectorTransport`; the comment called that
// "the whole of what this process cannot do yet".*
//
// `CancellationToken.None` because stopping is the channel closing: the session
// returns when the Runtime closes it, and no signal is guessed at.
// The HttpClient the OHIP token attempt dials with. One for the process: it
// pools connections, and a client per invocation is the socket-exhaustion
// defect that looks like a slow PMS.
using var http = new HttpClient();

return await ConnectorEntry.RunAsync(
    ConnectorBootstrap.FromEnvironment(),
    OperatingSystem.IsWindows() ? new NamedPipeConnectorTransport() : null,
    new InvocationDispatch(http).HandleAsync,
    Console.Error,
    CancellationToken.None);
