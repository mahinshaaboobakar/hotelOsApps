using HotelOS.Contracts.MasterData.V1;
using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Assignment;
using HotelOS.Jobs.Application.Cancellation;
using HotelOS.Jobs.Application.Catalogue;
using HotelOS.Jobs.Application.Completion;
using HotelOS.Jobs.Application.Concerns;
using HotelOS.Jobs.Application.Course;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Application.Notes;
using HotelOS.Jobs.Application.Policies;
using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Application.Rating;
using HotelOS.Jobs.Application.Settings;
using HotelOS.Jobs.Application.Work;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Grpc;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using HotelOS.Platform.Transport;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Jobs — repairs and tasks, as an installable application (ADR 0122). It is
// installed into a property that already exists, so its certificate exists
// before it does; there is no bootstrap surface and no unenrolled mode.

var builder = WebApplication.CreateBuilder(args);

// Assigned the moment the host is built, and read only from inside a running
// activity — the sweep is declared before there is a provider to sweep with.
WebApplication? started = null;

// `dotnet HotelOS.Jobs.dll migrate` — install step 6; before the host is built.
if (args is ["migrate", ..])
{
    return await SchemaMigration.RunAsync(
        builder.Configuration,
        connectionName: "Jobs",
        schema: JobsDbContext.Schema,
        create: connection => new JobsDbContext(
            new DbContextOptionsBuilder<JobsDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable("__migrations", JobsDbContext.Schema))
                .Options),
        args);
}

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.Interceptors.Add<DomainExceptionInterceptor>();
    options.Interceptors.Add<AuthenticationInterceptor>();
});

builder.Services.AddDbContext<JobsDbContext>(options => options
    .UseSnakeCaseNamingConvention()
    .UseNpgsql(builder.Configuration.GetConnectionString("Jobs"), npgsql =>
    {
        npgsql.MigrationsHistoryTable("__migrations", JobsDbContext.Schema);
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, TimeSpan.FromSeconds(2), null);
    }));

builder.Services.AddHealthChecks().AddDbContextCheck<JobsDbContext>("postgresql");

// What the Kernel told this installation when it started it — the certificate
// directory, where the Kernel answers, and which property this serves. Read in
// one place, from the SDK, because the three names are a contract with
// `packages/process.rs` and a second copy of a contract is the copy that stops
// matching (WF-Q11 (8)).
//
// This service used to read `Service:CertificateDirectory` and
// `Kernel:Endpoint` from configuration. The Kernel sets neither: it sets the
// three environment variables below. So the application threw on the line that
// demanded the certificate directory and never reached its first request —
// which read as the platform refusing it rather than as this service asking
// the wrong question.
//
// Null is a state and partial is a defect, and the SDK keeps them apart: null
// means nobody started this with a Kernel, which is legitimate for `migrate`
// above and for a checkout, and is not legitimate for serving. An application
// with no certificate authenticates nobody, so it says so and stops.
var platform = PlatformEnvironment.Read()
    ?? throw new InvalidOperationException(
        "Jobs was not started by a Kernel. HOTELOS_CERTIFICATE_DIR, "
        + "HOTELOS_KERNEL_ENDPOINT and HOTELOS_PROPERTY_ID are how the platform "
        + "tells an installed application where its identity is, where the Kernel "
        + "answers and which property it serves; without them it can open a port "
        + "but authenticate nobody. `dotnet HotelOS.Jobs.dll migrate` needs none "
        + "of them and runs before this line.");

builder.Services.AddSingleton(platform);

// The Kernel client, the authorizer, the event appender bound to this context.
builder.Services.AddHotelOsPlatform<JobsDbContext>(
    serviceName: "jobs",
    kernelEndpoint: platform.KernelEndpoint,
    certificateDirectory: platform.CertificateDirectory);

// Master Data, read-only, over the canonical transport (ADR 0040).
var masterData = PlatformEndpoint.For(
    "masterdata", new Uri(builder.Configuration["MasterData:Endpoint"] ?? "https://127.0.0.1:50053"));
builder.Services
    .AddGrpcClient<MasterDataService.MasterDataServiceClient>(options => options.Address = masterData.Uri)
    .ConfigurePrimaryHttpMessageHandler(provider =>
        PlatformTransport.Handler(masterData, provider.GetRequiredService<ServiceCertificate.Source>()));

// The events this application consumes — the manifest's `subscribes`, one
// durable consumer, ack after commit, idempotent on the row (EVT-Q4).
builder.Services.AddApplicationEventConsumer(
    natsUrl: builder.Configuration["Events:NatsUrl"] ?? "nats://127.0.0.1:4222",
    declare: events => events
        .Consume<PpmDue, PpmDueHandler>(EventTypes.PpmDue)
        .Consume<ShiftStarted, ShiftStartedHandler>(EventTypes.ShiftStarted)
        .Consume<ShiftEnded, ShiftEndedHandler>(EventTypes.ShiftEnded)
        .Consume<StayDeparted, StayDepartedHandler>(EventTypes.StayDeparted)
        .Consume<StaffExited, StaffExitedHandler>(EventTypes.StaffExited));

builder.Services.AddScoped<IPropertyDirectory, MasterDataPropertyDirectory>();
builder.Services.AddScoped<JobRecords>();
builder.Services.AddScoped<JobAnnouncer>();
builder.Services.AddScoped<JobNumbering>();
builder.Services.AddScoped<JobPolicyResolver>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<WorkSessionService>();
builder.Services.AddScoped<CompletionService>();
builder.Services.AddScoped<CancellationService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<RatingService>();
builder.Services.AddScoped<JobQueries>();
builder.Services.AddScoped<CatalogueService>();
builder.Services.AddScoped<PropertyCatalogueService>();
builder.Services.AddScoped<ConcernPolicyService>();
builder.Services.AddScoped<PresenceService>();
builder.Services.AddScoped<ClosingHoldService>();

// The sweep — S5 D1: every sixty seconds, overlap SKIP.
builder.Services.AddScoped<Nudger>();
builder.Services.AddScoped<ConcernSweep>();
builder.Services.AddScoped<DayStart>();
builder.Services.AddScoped<AutoClose>();

// The tick, twice over, deliberately — TEMPORAL-Q1, page 62a's order. The
// Schedule is the trigger from now on; the timer stays until this installation
// is confirmed firing it, because until INSTALL-Q69 closes a property may have
// no Temporal, where the reconciler correctly does nothing. Both run the same
// object, so they cannot come to mean different things.
var sweepActivities = new ConcernActivities(() => started!.Services);
builder.Services.AddSingleton(sweepActivities);
builder.Services.AddHostedService<ConcernSweepHost>();
builder.Services.AddTemporal(temporal => temporal
    .Workflow<ConcernSweepWorkflow>()
    .Activities(sweepActivities)
    .Schedule(ConcernSweepWorkflow.ScheduleId, ConcernSweepHost.Interval, nameof(ConcernSweepWorkflow)));

// The listener resolves this application's identity eagerly and refuses to
// start without one — the property worth having: an installed package with no
// certificate must not open a port and authenticate nobody.
builder.Host.UsePlatformListener(
    builder.Configuration.GetValue("Service:Port", 50064),
    platform.CertificateDirectory);

var app = builder.Build();
started = app;

app.MapGrpcService<JobsGrpcService>();
app.MapHealthChecks("/health");

app.Run();

return 0;
