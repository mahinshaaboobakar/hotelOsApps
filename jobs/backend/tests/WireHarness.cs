using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Assignment;
using HotelOS.Jobs.Application.Cancellation;
using HotelOS.Jobs.Application.Catalogue;
using HotelOS.Jobs.Application.Completion;
using HotelOS.Jobs.Application.Course;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Application.Notes;
using HotelOS.Jobs.Application.Policies;
using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Application.Rating;
using HotelOS.Jobs.Application.Settings;
using HotelOS.Jobs.Application.Work;
using HotelOS.Jobs.Contracts.V1;
using HotelOS.Jobs.Grpc;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// The application's own gRPC surface, hosted and called over a real socket.
///
/// # What is real
///
/// The service, its interceptors, protobuf over HTTP/2, Entity Framework and a
/// real PostgreSQL on a scratch database provisioned the house way. A request
/// leaves a generated client, crosses a loopback connection, is parsed by the
/// generated server, runs the application's own services, commits — and the
/// reply is read back the same way. No fixture is handed to a handler: every
/// figure a test reads came over the wire from the database.
///
/// # What is substituted, and why
///
/// Three, each because the platform it belongs to is not running on this
/// machine and none of them is this application's to start:
///
/// * <b>Authentication</b> — Identity is down, so a caller is filed where the
///   platform's interceptor would file one.
/// * <b>Authorization</b> — the Kernel is down, so the recording authorizer
///   stands in and records what each RPC asked for.
/// * <b>Master Data</b> — down, so the directory double answers for the
///   property's code, its zone and whether a location exists.
///
/// The ledger names all three on every row that leans on one.
/// </summary>
public sealed class WireHarness : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly GrpcChannel _channel;

    private WireHarness(WebApplication app, GrpcChannel channel, JobsFixture fixture,
        DirectoryDouble directory, RecordingAuthorizer authorizer)
    {
        _app = app;
        _channel = channel;
        Fixture = fixture;
        Directory = directory;
        Authorizer = authorizer;
        Client = new JobsService.JobsServiceClient(channel);
    }

    public JobsService.JobsServiceClient Client { get; }

    public JobsFixture Fixture { get; }

    public DirectoryDouble Directory { get; }

    public RecordingAuthorizer Authorizer { get; }

    /// <summary>This run's property — one per harness, so two runs never share rows.</summary>
    public Guid PropertyId { get; } = Guid.CreateVersion7();

    public Guid OrganizationId { get; } = Guid.CreateVersion7();

    /// <summary>The request context every call carries.</summary>
    public HotelOS.Contracts.Common.V1.RequestContext Context() => new()
    {
        PropertyId = PropertyId.ToString(),
        OrganizationId = OrganizationId.ToString(),
    };

    /// <summary>A context for a different property, to prove one cannot read another's.</summary>
    public HotelOS.Contracts.Common.V1.RequestContext OtherProperty() => new()
    {
        PropertyId = Guid.CreateVersion7().ToString(),
        OrganizationId = OrganizationId.ToString(),
    };

    /// <summary>Open a context on the database, to check what the wire actually wrote.</summary>
    public JobsDbContext Db() => Fixture.Context();

    public static async Task<WireHarness> StartAsync(JobsFixture fixture)
    {
        var directory = new DirectoryDouble();
        var authorizer = new RecordingAuthorizer();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Error);
        // HTTP/2 cleartext on a free loopback port: the real protocol, without
        // the certificates the Kernel would hand a running installation.
        builder.WebHost.ConfigureKestrel((KestrelServerOptions kestrel) =>
            kestrel.Listen(System.Net.IPAddress.Loopback, 0,
                listen => listen.Protocols = HttpProtocols.Http2));

        builder.Services.AddDbContext<JobsDbContext>(options => options
            .UseSnakeCaseNamingConvention()
            .UseNpgsql(fixture.ApplicationConnection,
                npgsql => npgsql.MigrationsHistoryTable("__migrations", JobsDbContext.Schema)));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(new ServiceIdentity("jobs"));
        builder.Services.AddSingleton<DomainExceptionInterceptor>();
        builder.Services.AddSingleton<IKernelAuthorizer>(authorizer);
        builder.Services.AddSingleton<IPropertyDirectory>(directory);
        builder.Services.AddScoped<IEventAppender>(provider => new EventAppender(
            provider.GetRequiredService<JobsDbContext>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ServiceIdentity>()));

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

        builder.Services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.Interceptors.Add<DomainExceptionInterceptor>();
            options.Interceptors.Add<StandInCallerInterceptor>();
        });

        var app = builder.Build();
        app.MapGrpcService<JobsGrpcService>();
        await app.StartAsync();

        var address = app.Urls.First();
        var channel = GrpcChannel.ForAddress(address);
        return new WireHarness(app, channel, fixture, directory, authorizer);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}

/// <summary>
/// Files a caller where the platform's authentication interceptor would.
/// Identity is not running, and a handler reaching <c>CallerContext.Get</c>
/// with nothing filed refuses every call — which would prove nothing about
/// this application's own wire.
/// </summary>
public sealed class StandInCallerInterceptor : Interceptor
{
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        CallerContext.Set(context, WireCaller.Current);
        return continuation(request, context);
    }
}

/// <summary>Who the wire round calls as.</summary>
public static class WireCaller
{
    public static AuthenticatedCaller Current { get; set; } = AuthenticatedCaller.ForService("jobs");
}
