using HotelOS.Contracts.MasterData.V1;
using HotelOS.Platform;
using HotelOS.Platform.Transport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Attendance;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Application.Duties;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Swaps;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Grpc;
using HotelOS.Workforce.Infrastructure;
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
        connectionName: "Workforce",
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
            builder.Configuration.GetConnectionString("Workforce"),
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
builder.Services.AddHotelOsPlatform<WorkforceDbContext>(
    serviceName: "workforce",
    kernelEndpoint: new Uri(
        builder.Configuration["Kernel:Endpoint"] ?? "https://127.0.0.1:50051"),
    certificateDirectory: builder.Configuration["Service:CertificateDirectory"]);

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

// The listener resolves this application's identity eagerly and refuses to
// start without one. That is the property worth having: an installed package
// with no certificate must not open a port at all, rather than open one and
// authenticate nobody.
builder.Host.UsePlatformListener(
    builder.Configuration.GetValue("Service:Port", 50061),
    builder.Configuration["Service:CertificateDirectory"]
        ?? throw new InvalidOperationException(
            "Service:CertificateDirectory is required; an installed application "
            + "is enrolled before it starts"));

var app = builder.Build();

app.MapGrpcService<WorkforceGrpcService>();
app.MapHealthChecks("/health");

app.Run();

// Top-level statements return `int` because `migrate` above does. Reached only
// when the host stops.
return 0;
