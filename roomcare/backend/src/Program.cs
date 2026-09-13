using HotelOS.Platform;
using HotelOS.RoomCare.Application;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Tick;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Room Care — are the rooms ready, as an installable application (ADR 0122).
// Installed into a property that already exists; no bootstrap surface and no
// unenrolled mode. Jobs' Program.cs is the template, and its comments carry the
// reasons for each line that is the same here.

var builder = WebApplication.CreateBuilder(args);

// The connection name the platform hands every package (ADR 0092 §Q11).
const string PlatformConnection = "HotelOS";

// Assigned when the host is built; read only from inside a running activity.
WebApplication? started = null;

// `dotnet HotelOS.RoomCare.dll migrate` — install step 6; before the host is built.
if (args is ["migrate", ..])
{
    return await SchemaMigration.RunAsync(
        builder.Configuration,
        connectionName: PlatformConnection,
        schema: RoomCareDbContext.Schema,
        create: connection => new RoomCareDbContext(
            new DbContextOptionsBuilder<RoomCareDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable("__migrations", RoomCareDbContext.Schema))
                .Options),
        args);
}

// A console sink first: a package ships no appsettings.json, and a process that
// cannot say why it stopped is one nobody can fix from outside.
builder.Host.UseSerilog((context, configuration) => configuration
    .WriteTo.Console()
    .ReadFrom.Configuration(context.Configuration));

builder.Services.AddDbContext<RoomCareDbContext>(options => options
    .UseSnakeCaseNamingConvention()
    .UseNpgsql(builder.Configuration.GetConnectionString(PlatformConnection), npgsql =>
    {
        npgsql.MigrationsHistoryTable("__migrations", RoomCareDbContext.Schema);
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, TimeSpan.FromSeconds(2), null);
    }));

builder.Services.AddHealthChecks().AddCheck<DatabaseReachable>("postgresql");

var platform = PlatformEnvironment.Read()
    ?? throw new InvalidOperationException(
        "Room Care was not started by a Kernel. HOTELOS_PACKAGE_ID, HOTELOS_CERTIFICATE_DIR, HOTELOS_KERNEL_ENDPOINT, "
        + "HOTELOS_PROPERTY_ID, HOTELOS_GRPC_URL and HOTELOS_NATS_URL are how the platform tells an installed application "
        + "where its identity is; without them it can open a port but authenticate nobody. "
        + "`dotnet HotelOS.RoomCare.dll migrate` needs none of them and runs before this line.");
builder.Services.AddSingleton(platform);

// The Kernel client, the authorizer, the module surface's authentication and the
// appender bound to this context — one call; its receipt admits the consumer.
var admission = builder.Services.AddHotelOsApplication<RoomCareDbContext>(platform);

// The manifest's `subscribes` — one durable consumer, ack after commit, idempotent on the row.
builder.Services.AddApplicationEventConsumer(
    natsUrl: platform.NatsUrl,
    admission: admission,
    declare: events => events
        .Consume<RoomStateObserved, RoomStateObservedHandler>(EventTypes.RoomStateObserved)
        .Consume<StayMoved, StayArrivedHandler>(EventTypes.StayArrived)
        .Consume<StayMoved, StayDepartedHandler>(EventTypes.StayDeparted)
        .Consume<StayRoomChanged, StayRoomChangedHandler>(EventTypes.StayRoomChanged)
        .Consume<StayCorrected, StayCorrectedHandler>(EventTypes.StayCorrected)
        .Consume<PostingAnnounced, UserPostedHandler>(EventTypes.UserPosted)
        .Consume<PostingAnnounced, UserPostingEndedHandler>(EventTypes.UserPostingEnded)
        .Consume<ShiftBoundary, ShiftBoundaryHandler>(EventTypes.ShiftStarted)
        .Consume<ShiftBoundary, ShiftBoundaryHandler>(EventTypes.ShiftEnded)
        .Consume<JobAnnounced, JobCreatedHandler>(EventTypes.JobCreated)
        .Consume<JobAnnounced, JobClosedHandler>(EventTypes.JobClosed)
        .Consume<StaffExited, StaffExitedHandler>(EventTypes.StaffExited));

// Master Data through the install grant (ADR 0092 §4) — never over the wire.
builder.Services.AddScoped<IHouse, MasterDataHouseReader>();
builder.Services.AddRoomCareApplication();
builder.Services.AddRoomCareModule();
builder.Services.AddModuleRefusals();

// The tick — TEMPORAL-Q1: one Schedule, every sixty seconds, overlap SKIP (§6.1).
var tick = new TickActivities(() => started!.Services);
builder.Services.AddSingleton(tick);
builder.Services.AddTemporal(temporal => temporal
    .Workflow<TickWorkflow>()
    .Activities(tick)
    .Schedule(TickWorkflow.ScheduleId, TickWorkflow.Cadence, nameof(TickWorkflow)));

builder.Host.UseApplicationListeners(platform);

var app = builder.Build();
started = app;

app.UseModuleRefusals();
app.MapRoomCareModule();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "text/plain";
        var lines = report.Entries.Select(entry =>
            $"{entry.Key}: {entry.Value.Status}" + (entry.Value.Description is { } said ? $" — {said}" : string.Empty));
        await context.Response.WriteAsync($"{report.Status}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}");
    },
});

app.Run();

return 0;
