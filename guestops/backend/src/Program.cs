using HotelOS.GuestOps.Application;
using HotelOS.GuestOps.Grpc;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Module;
using HotelOS.GuestOps.Infrastructure.Platform;
using Wire = HotelOS.Contracts.Integration.V1;
using HotelOS.GuestOps.Events;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

// GuestOps — the reservation book, as an installable application.
//
// Two modes, and the first is the one install depends on:
//
//   HotelOS.GuestOps.dll migrate    ADR 0039's command, run by install step 6
//   HotelOS.GuestOps.dll            the service, started by install step 8
//
// The connection name the platform hands every package — a convention, not a
// manifest field (ADR 0092 §Q11). Install step 8 sets `ConnectionStrings__HotelOS`
// in the child's environment; `appsettings.json` carries a development default
// so this also runs outside an install.
const string PlatformConnection = "HotelOS";

var builder = WebApplication.CreateBuilder(args);

// # `migrate` runs before the host exists
//
// A migration must never start a listener, register with the Kernel or need
// this application's identity: it runs during installation, when none of those
// exist. Through `SchemaMigration` rather than `dotnet ef database update`,
// because it connects as `hotelos_migrator` having assumed the schema's owner
// role — so every object is created by the role that owns it, and the grants
// the isolation model depends on actually apply.
if (args is ["migrate", ..])
{
    return await SchemaMigration.RunAsync(
        builder.Configuration,
        connectionName: PlatformConnection,
        schema: GuestOpsDbContext.Schema,
        create: connection => new GuestOpsDbContext(
            new DbContextOptionsBuilder<GuestOpsDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(
                    connection,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__migrations", GuestOpsDbContext.Schema))
                .Options),
        args);
}

// The running application connects as its **own** role, with DML on its schema,
// `SELECT` on `masterdata`, and no DDL — so it could not alter its own schema
// if its code tried.
builder.Services.AddDbContext<GuestOpsDbContext>(options => options
    .UseSnakeCaseNamingConvention()
    .UseNpgsql(builder.Configuration.GetConnectionString(PlatformConnection)));

// # The platform wiring, and the one part of it that is not this round's
//
// `AddHotelOsPlatform` is the shared mechanism: the Kernel channel, the
// authorizer, the service identity, the clock, the exception interceptor and
// the transactional event appender bound to this context. None of it is
// reimplemented here — two event appenders drift, and one of them stops writing
// the queue row.
//
// # What the Kernel told this application, read from the SDK
//
// Three facts arrive in the environment when the Kernel starts an installed
// package (`packages/process.rs`), and none of them is configuration this
// application chose:
//
//   HOTELOS_CERTIFICATE_DIR   install step 4 wrote client.crt/.key and ca.crt
//   HOTELOS_KERNEL_ENDPOINT   the one peer an application is told
//   HOTELOS_PROPERTY_ID       which property this installation serves
//
// `PlatformEnvironment.Read()` is where those names live — one place, because
// they are a contract with the Kernel and a second copy is the copy that stops
// matching. It returns `null` when this process was **not** started by a
// Kernel, which is how a developer runs it from a checkout; the configured
// development endpoint carries that case and nothing else does.
//
// **`Platform:CertificateDirectory` is gone, and it was a ghost.** It was read
// from `builder.Configuration` and no `appsettings.json` has ever defined it,
// so it was `null` on every path this application has ever taken — which made
// the plaintext fallback below look like a configurable choice when it was
// unconditional. Under the Kernel it now resolves to a real directory, and
// that is the difference between this application launching and failing inside
// the first TLS handshake.
//
// **`certificateDirectory` is the seam — `AUTHZ-Q16`.** For a platform service
// it is where `hotelos-kernel enroll <service>` wrote an identity at
// provisioning. An *installed* application arrives later, from a package, and
// when nothing has given it one the channel falls back to plaintext and every
// authorized RPC fails closed. The fallback is evidence for that row, not a
// workaround.
//
// That is the correct behaviour and it is **not** worked around here: the
// Kernel refuses any request naming an application identity outright, because
// an unauthenticated package claim must not be honoured either way round —
// trusting it would let a package assert any id, and ignoring it would let a
// package inherit its user's full authority.
var platform = PlatformEnvironment.Read();

builder.Services.AddHotelOsPlatform<GuestOpsDbContext>(
    serviceName: "guestops",
    kernelEndpoint: platform?.KernelEndpoint
        ?? new Uri(builder.Configuration["Kernel:Endpoint"] ?? "https://127.0.0.1:15051"),
    certificateDirectory: platform?.CertificateDirectory);

// **The authentication half, which serving a UI requires** — `SHELL-Q38`.
// `AddHotelOsPlatform` binds `IKernelAuthorizer`; it does NOT register
// `JwtCallerAuthenticator`, which lives only in `AddPlatformAuthentication` —
// so an application that wired the platform and stopped there could map a
// module capability and fail at boot. That refusal is the fix working: the
// envelope resolves its guards at map time precisely so a missing registration
// says so at start-up, naming the call to add, rather than throwing on the
// first button a person presses.
//
// Guarded on having an identity for the same reason the route below is. The
// validator authenticates its own JWKS fetch with this application's
// certificate, so without one there is nothing to fetch keys with.
if (platform is not null)
{
    builder.Services.AddPlatformAuthentication(
        serviceRoot: platform.CertificateDirectory,
        identityEndpoint: new Uri(
            builder.Configuration["Services:Identity:Endpoint"]
                ?? throw new InvalidOperationException(
                    "Services:Identity:Endpoint is required: a validator with no authority "
                    + "endpoint cannot refresh signing keys")),
        natsUrl: builder.Configuration["Events:NatsUrl"] ?? "nats://127.0.0.1:24222",
        configuration: builder.Configuration);
}

builder.Services.AddGuestOpsApplication();

// Receive what the manifest declared — `EVT-Q4`. The Kernel read
// `events.subscribes` at install and hands the admitted set to this process, so
// this names handlers rather than subjects: a handler for something the
// manifest does not declare fails at start-up, because it could never fire.
//
// Both declared subjects now have handlers. `reservation.fact` carries the
// Hub's own `RoomStayFact` — the contract is read at the edge by
// `RoomStayFactMapper` and never restated, so a field DD changes is a compile
// error in one file.
builder.Services.AddApplicationEventConsumer(
    natsUrl: builder.Configuration["Events:NatsUrl"] ?? "nats://127.0.0.1:4222",
    declare: events => events
        .Consume<Wire.RoomStayFact, ReservationFactHandler>("reservation.fact")
        .Consume<JobCreated, JobCreatedHandler>("job.created"));
builder.Services.AddGuestOpsPlatformAdapters(builder.Configuration);

builder.Services.AddGrpc(options =>
    options.Interceptors.Add<DomainExceptionInterceptor>());

var app = builder.Build();

app.MapGrpcService<GuestOpsGrpcService>();

// The one route this application's own bundles reach — `SHELL-Q37`. Their
// realm has `default-src 'none'`, so a screen's `host.call` travels over a
// MessagePort to the Shell and arrives here. The session token is validated
// and the capability checked inside `MapModuleCapability`, not beside it:
// an application that had to remember either would work perfectly on the desk
// it was written at, which is the failure with no error anywhere.
// **Mapped only when the platform gave this application an identity.** An
// installed one always has: install step 4 issues it and Kernel start-up heals
// a missing one (`AUTHZ-Q16`). Run from a checkout there is no certificate and
// no Kernel, and a surface that answered every call with a handshake failure
// would be worse than one honestly absent — hello-hotel's own reasoning, and
// it is the same surface.
if (platform is not null)
{
    app.MapGuestOpsModule();
}

// What install step 8 probes. Plain HTTP rather than gRPC health, because the
// probe runs before the Kernel has any reason to trust this process and a
// liveness check should need nothing but a socket.
app.MapGet("/health", () => Results.Ok("healthy"));

await app.RunAsync();

return 0;
