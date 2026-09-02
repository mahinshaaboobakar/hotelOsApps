using HotelOS.GuestOps.Application;
using HotelOS.GuestOps.Grpc;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.Platform;
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
// **`certificateDirectory` is the seam — `AUTHZ-Q16`.** For a platform service
// it is where `hotelos-kernel enroll <service>` wrote an identity at
// provisioning. An *installed* application arrives later, from a package, and
// nothing enrolls it — so this reads a configured path and, when none holds a
// certificate, the channel falls back to plaintext and every authorized RPC
// fails closed. The fallback is evidence for that row, not a workaround.
//
// That is the correct behaviour and it is **not** worked around here: the
// Kernel refuses any request naming an application identity outright, because
// an unauthenticated package claim must not be honoured either way round —
// trusting it would let a package assert any id, and ignoring it would let a
// package inherit its user's full authority. `AUTHZ-Q16` answers identity at
// install; until it lands, this application can migrate, start, and serve
// nothing that requires a decision.
builder.Services.AddHotelOsPlatform<GuestOpsDbContext>(
    serviceName: "guestops",
    kernelEndpoint: new Uri(
        builder.Configuration["Kernel:Endpoint"] ?? "https://127.0.0.1:15051"),
    certificateDirectory: builder.Configuration["Platform:CertificateDirectory"]);

builder.Services.AddGuestOpsApplication();

// Receive what the manifest declared — `EVT-Q4`. The Kernel read
// `events.subscribes` at install and hands the admitted set to this process, so
// this names handlers rather than subjects: a handler for something the
// manifest does not declare fails at start-up, because it could never fire.
//
// `reservation.fact` is declared and has no handler yet — the Hub's contract
// needs mapping into `InboundStayFact` first, and that mapper does not exist.
// A declared subject with no handler is acknowledged and dropped, which is the
// correct interim: nothing is lost that was ever being applied.
builder.Services.AddApplicationEventConsumer(
    natsUrl: builder.Configuration["Events:NatsUrl"] ?? "nats://127.0.0.1:4222",
    declare: events => events.Consume<JobCreated, JobCreatedHandler>("job.created"));
builder.Services.AddGuestOpsPlatformAdapters(builder.Configuration);

builder.Services.AddGrpc(options =>
    options.Interceptors.Add<DomainExceptionInterceptor>());

var app = builder.Build();

app.MapGrpcService<GuestOpsGrpcService>();

// What install step 8 probes. Plain HTTP rather than gRPC health, because the
// probe runs before the Kernel has any reason to trust this process and a
// liveness check should need nothing but a socket.
app.MapGet("/health", () => Results.Ok("healthy"));

await app.RunAsync();

return 0;
