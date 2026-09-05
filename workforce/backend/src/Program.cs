using HotelOS.Contracts.MasterData.V1;
using HotelOS.Platform;
using HotelOS.Platform.Transport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Assignment;
using HotelOS.Workforce.Application.Attendance;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Application.Duties;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Periods;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Swaps;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Application.Summaries;
using HotelOS.Workforce.Application.Teams;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Grpc;
using HotelOS.Workforce.Infrastructure;
using HotelOS.Workforce.Module;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Workforce — who is posted where, as an installable application.
//
// # It is installed, not deployed
//
// ADR 0122: this is a package. It reaches a property as a signed `.hopkg`,
// Software Center installs it, and the Kernel starts and stops it with the
// application's lifecycle. There is no bootstrap surface here and no
// unenrolled mode — unlike a platform service, an application is installed
// *into* a property that already exists, so its certificate exists before it
// does.

// The connection name the platform hands **every** package — a convention, not
// a manifest field, exactly as `dotnet <assembly> migrate` is (ADR 0092 §Q11).
// Install steps 6 and 8 set `ConnectionStrings__HotelOS` in the child's
// environment and ASP.NET maps it here.
//
// This file asked for `Workforce`, which nothing anywhere sets — the same
// defect as the certificate directory above, one layer down: the application
// would have launched and then answered every request with a null connection
// string. `packages/process.rs` holds the name as a constant; hello-hotel reads
// it; GuestOps reads it. This is the third reader, not a fourth spelling.
const string PlatformConnection = "HotelOS";

var builder = WebApplication.CreateBuilder(args);

// `dotnet HotelOS.Workforce.dll migrate` — ADR 0039, and install step 6.
//
// The package manifest names this command, so the assembly name and this verb
// must agree with it; a drift fails the install with a file-not-found that
// reads like a broken package rather than a mismatch.
//
// Before the host is built, so a migration never starts a listener or needs
// this application's mTLS identity.
if (args is ["migrate", ..])
{
    return await SchemaMigration.RunAsync(
        builder.Configuration,
        connectionName: PlatformConnection,
        schema: WorkforceDbContext.Schema,
        create: connection => new WorkforceDbContext(
            new DbContextOptionsBuilder<WorkforceDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(
                    connection,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__migrations", WorkforceDbContext.Schema))
                .Options),
        args);
}

// # What the platform told this application, when it started it — `PKG-Q46` (1)
//
// Three facts arrive in the environment, written by `packages/process.rs` at
// install step 8, and **not one of them is configuration this package chose**:
//
//   HOTELOS_CERTIFICATE_DIR   install step 4 wrote client.crt/.key and ca.crt
//   HOTELOS_KERNEL_ENDPOINT   the one peer an application is told
//   HOTELOS_PROPERTY_ID       which property this installation serves
//
// This file read `Service:CertificateDirectory` and `Kernel:Endpoint` instead —
// the **platform-service** convention, which is right for a service the
// installer configures and wrong for a package the Kernel launches. Nothing
// sets those keys for an application, and there is no `appsettings.json` here
// to carry a default, so the throw below fired on every real launch: *this
// application had never once started under a Kernel*. `AUTHZ-Q21` is why an
// application ships no endpoint configuration at all — peers come from
// discovery, and the Kernel's own address is the one thing discovery cannot
// answer, so it is handed over.
//
// Read once, through the SDK, because a second copy of a contract is the copy
// that stops matching on the day somebody renames a variable in `process.rs`.
var platform = PlatformEnvironment.Read()
    ?? throw new InvalidOperationException(
        "HOTELOS_CERTIFICATE_DIR, HOTELOS_KERNEL_ENDPOINT and HOTELOS_PROPERTY_ID are "
        + "all required: the Kernel sets them when it starts an installed application. "
        + "Every surface here authenticates its caller by client certificate, so there "
        + "is no honest half-configured mode — run this through an install rather than "
        + "from a checkout. `migrate` above needs none of them and runs either way.");

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();

    // One place where a domain failure becomes a status code. A try/catch per
    // RPC would eventually return Internal for a caller's typo.
    options.Interceptors.Add<DomainExceptionInterceptor>();

    // Who is calling, established once per request before any handler runs —
    // ADR 0014. Every handler retrieves that caller rather than constructing
    // one, which is what makes the invariant hold across the whole surface.
    options.Interceptors.Add<AuthenticationInterceptor>();
});

builder.Services.AddDbContext<WorkforceDbContext>(options =>
    options
        .UseSnakeCaseNamingConvention()
        .UseNpgsql(
            builder.Configuration.GetConnectionString(PlatformConnection),
            npgsql =>
            {
                // This application's own schema, and its migrations history with
                // it — never `public`, where another package's would sit beside
                // it.
                npgsql.MigrationsHistoryTable("__migrations", WorkforceDbContext.Schema);

                // Edge-first: PostgreSQL is on the same machine, so a transient
                // failure is a restart rather than a network blip. Retrying
                // briefly rides out the restart instead of failing a rota save.
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, TimeSpan.FromSeconds(2), null);
            }));

// Chapter 13: every component exposes health. The Kernel probes this, and the
// desktop greys out what depends on the application when it fails — so a user
// sees why a screen is unavailable rather than clicking into an error.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WorkforceDbContext>("postgresql");

// One call: the Kernel client, the authorizer, the event appender bound to this
// application's DbContext, the identity its events and certificate carry, and
// the interceptor that maps a domain failure to a status code. None of it is
// reimplemented here — CLAUDE.md §"No duplicated shared code" makes no
// exception for an installed application.
// **One call, from what the Kernel told this application** — SHELL-Q40 §3 and
// ADR 0133, whose closing invariant is the whole reason this file no longer
// names a port, an endpoint, an issuer or a bus:
//
//   *installed applications never define their own topology, listener ports, or
//   platform service addresses — the Kernel composes topology; the SDK
//   materializes it.*
//
// This file used to reach for `Services:Identity:Endpoint` and `Nats:Url` out of
// configuration nothing sets for a package, which is the boot GG measured and
// SHELL-Q40 closed. Identity is now DISCOVERED through the Kernel, the bus and
// both doors are TOLD in the environment, and issuer and audience are platform
// constants rather than anything an application carries.
//
// It also registers `platform` itself, which the shift fan-out's sweep needs:
// it acts under this application's own service identity and has no request to
// take a property from.
builder.Services.AddHotelOsApplication<WorkforceDbContext>(platform);

// Master Data, read-only — CLAUDE.md: *"applications may read master data"*.
//
// Through the canonical transport like every other outbound call (ADR 0040):
// the authority is the peer's **name**, verified in the handshake, and the
// connect callback decides where the bytes go. A hand-rolled handler here would
// get the server half wrong in the way the SDK's own comment records.
//
// EVT-Q3's boundary, stated at the registration: this is a platform-internal
// *question*, not a call to a neighbouring application, and not a command.
builder.Services
    .AddGrpcClient<MasterDataService.MasterDataServiceClient>(options =>
        options.Address = PlatformEndpoint.For(
            "masterdata",
            new Uri(builder.Configuration["MasterData:Endpoint"]
                    ?? "https://127.0.0.1:50053")).Uri)
    .ConfigurePrimaryHttpMessageHandler(provider => PlatformTransport.Handler(
        PlatformEndpoint.For(
            "masterdata",
            new Uri(builder.Configuration["MasterData:Endpoint"]
                    ?? "https://127.0.0.1:50053")),
        provider.GetRequiredService<ServiceCertificate.Source>()));

builder.Services.AddScoped<IStaffDirectory, MasterDataStaffDirectory>();
builder.Services.AddScoped<PostingService>();
builder.Services.AddScoped<CapabilityService>();
builder.Services.AddScoped<ShiftCatalogueService>();
builder.Services.AddScoped<DutyService>();
builder.Services.AddScoped<RotaService>();
builder.Services.AddScoped<OvertimeCheck>();
builder.Services.AddScoped<PolicyService>();
builder.Services.AddScoped<LeaveService>();
builder.Services.AddScoped<LeaveTypeService>();
builder.Services.AddScoped<SwapProposalService>();
builder.Services.AddScoped<ApproverResolver>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<DayComparison>();
builder.Services.AddScoped<PostingAnnouncer>();
builder.Services.AddScoped<StaffChangeConsumer>();
builder.Services.AddScoped<PeriodService>();
builder.Services.AddScoped<AssignmentAdvisor>();

// Teams — Jobs' `S3-D1`, ruled Workforce's whole on 2026-09-04. `PostingService`
// holds it, because ending a posting has to end the memberships it supported in
// the same transaction.
builder.Services.AddScoped<TeamService>();

// The shift fan-out — Jobs' `S5-D13`. This is the *looking*; the tick that calls
// it is the platform's sweep host, and the scope it acts under is the SDK's
// service-identity constructor. Registered now so both arrive to something that
// already exists.
builder.Services.AddScoped<ShiftBoundaryAnnouncer>();

// The five reads behind this application's dock widgets — `SHELL-Q35`. Each is
// a view over rows this application already owns, and each is registered
// separately rather than behind one facade: a widget asks one question, and a
// service that answered five would be five purposes in one file.
builder.Services.AddScoped<ShiftBoardSummary>();
builder.Services.AddScoped<AttendanceTodaySummary>();
builder.Services.AddScoped<PendingRequestsSummary>();
builder.Services.AddScoped<ComingUpSummary>();
builder.Services.AddScoped<OnLeaveSummary>();

// **Two doors, and the SDK binds both from the environment** — SHELL-Q40 §4,
// ADR 0133. It still resolves this application's identity eagerly and refuses
// to start without one, which is the property worth having: an installed
// package with no certificate must not open a port at all.
builder.Host.UseApplicationListeners(platform);

var app = builder.Build();

// **Which surface answers is decided by which door a connection arrived at**,
// not by its path — SHELL-Q40 §3's sharpening, and the reason this is a
// structural call rather than two `Map` lines. ASP.NET routes by path across
// every listener, so an unbranched mapping would publish the whole gRPC API on
// the plaintext loopback door. Neither pin is this file's to remember.
app.MapApplicationDoors(
    platform,
    platformApi: door => door.MapGrpcService<WorkforceGrpcService>(),
    packagedUi: door =>
    {
        // **The surface this application serves to its own packaged UI** —
        // design page 63 §3, one `MapModuleCapability` per capability the
        // screens call. The Shell forwards a bundle's `host.call` to
        // `/module/{capability}/{method}`; the SDK validates the person's token
        // and checks they hold the capability in this property before any
        // handler runs.
        door.MapWorkforceModule();

        // What install step 8 probes, on the door the Kernel actually dials.
        // Plain HTTP rather than gRPC health, because the probe runs before the
        // Kernel has reason to trust this process and a liveness check should
        // need nothing but a socket.
        door.MapHealthChecks("/health");
    });

app.Run();

// Top-level statements return `int` because `migrate` above does. Reached only
// when the host stops.
return 0;
