using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using HotelOS.Contracts.Kernel.V1;
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
using HotelOS.Jobs.Application.Settings;
using HotelOS.Jobs.Application.Work;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Jobs.Module;
using HotelOS.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// The module surface, hosted as the Kernel hosts it and called as the Shell
/// calls it — <c>POST /module/{capability}/{method}</c> over a real socket.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is real here.</b> The application's own <c>Program</c> registrations
/// (every service, the module's projections, Entity Framework against a real
/// PostgreSQL on a scratch database), the platform SDK's own
/// <c>MapModuleCapability</c> — envelope, status vocabulary and both guards,
/// unmodified — its own <c>JwtCallerAuthenticator</c> doing the five checks
/// ADR 0013 §3 requires, and its own <c>KernelAuthorizer</c> asking a Kernel
/// over gRPC.
/// </para>
/// <para>
/// <b>What stands in, and it is two endpoints rather than two behaviours.</b>
/// Identity is not running, so this suite mints the signing key and serves it
/// through <see cref="TestKeys"/>: the token is verified by the platform's real
/// validation, against a key this suite owns. The Kernel is not running, so
/// <see cref="StandInKernel"/> answers <c>Authorize</c> from a set of
/// permissions each test states — the authorizer, the wire and the failure
/// path are the platform's. Nothing else is substituted: every figure a row
/// asserts came back from the database through the handler.
/// </para>
/// </remarks>
public sealed class ModuleHarness : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly HttpClient client;
    private readonly RsaSecurityKey key;

    private ModuleHarness(WebApplication app, HttpClient client, RsaSecurityKey key, JobsHarness data, StandInKernel kernel)
    {
        this.app = app;
        this.client = client;
        this.key = key;
        Data = data;
        Kernel = kernel;
    }

    /// <summary>What the authorizer asked the Kernel — a test reads it back.</summary>
    public StandInKernel Kernel { get; }

    /// <summary>
    /// What the surface logged as a failure, so a 500 says what it was.
    /// </summary>
    /// <remarks>
    /// The envelope answers an unexpected failure with no body, on purpose: its
    /// message could name a table or a connection string and the person reading
    /// it is on a shift. That is right in a property and useless in a round, so
    /// the harness keeps what was logged.
    /// </remarks>
    public static List<string> Failures { get; } = [];

    /// <summary>The property, the catalogue and the services a test seeds through.</summary>
    public JobsHarness Data { get; }

    /// <summary>The person the Shell says is signed in.</summary>
    public Guid Caller => Person;

    private static readonly Guid Person = Guid.CreateVersion7();

    /// <summary>The eight this application requests, all held unless a test takes one away.</summary>
    private static readonly string[] Everything =
    [
        Permissions.Read, Permissions.Create, Permissions.Assign, Permissions.Complete,
        Permissions.Cancel, Permissions.Amend, Permissions.Configure, Permissions.Curate,
    ];

    /// <summary>What the Kernel allows — a test removes one to prove a refusal.</summary>
    public HashSet<string> Granted { get; private init; } = [];

    /// <summary>Stand the surface up over a real socket.</summary>
    public static async Task<ModuleHarness> StartAsync(JobsFixture fixture)
    {
        var data = new JobsHarness(fixture);
        var key = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "jobs-module-test" };
        var granted = new HashSet<string>(Everything);
        var kernel = new StandInKernel(granted.Contains);

        var app = Build(fixture, data, key, kernel);
        await app.StartAsync();

        return new ModuleHarness(
            app,
            new HttpClient { BaseAddress = new Uri(Address(app)) },
            key,
            data,
            kernel)
        {
            Granted = granted,
        };
    }

    /// <summary>Call one capability's method, as the Shell forwards it.</summary>
    public async Task<ModuleAnswer> CallAsync(string capability, string method, object? parameters = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/module/{capability}/{method}")
        {
            // A serialised string with a length, which is what the Shell
            // forwards — camelCase, because a bundle's parameters are a
            // JavaScript object. JsonContent would send it chunked, and the
            // envelope reads Content-Length to decide whether a body was sent
            // at all (reported to the platform, 2026-09-05).
            Content = new StringContent(
                JsonSerializer.Serialize(parameters, Bundle),
                System.Text.Encoding.UTF8,
                "application/json"),
        };
        request.Headers.Add("Authorization", $"Bearer {Token()}");
        request.Headers.Add(ModuleEnvelope.PropertyHeader, Data.PropertyId.ToString());

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode == 500 && Failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"the surface failed on {capability}/{method}: {Failures[^1]}");
        }

        return new ModuleAnswer(
            (int)response.StatusCode,
            body.Length == 0 ? null : JsonDocument.Parse(body).RootElement.Clone());
    }

    /// <summary>What the surface answers a call with no token at all.</summary>
    public Task<int> StatusWithoutTokenAsync(string capability, string method) =>
        StatusAsync(capability, method, token: null, property: Data.PropertyId);

    /// <summary>What it answers a token signed by somebody else's key.</summary>
    /// <remarks>
    /// A second key with the same <c>kid</c>: the token parses, names a key the
    /// provider knows about, and fails on the signature — which is the check
    /// that would be missing if an application rolled its own validation.
    /// </remarks>
    public Task<int> StatusWithForeignTokenAsync(string capability, string method)
    {
        var forged = new RsaSecurityKey(RSA.Create(2048)) { KeyId = key.KeyId };
        return StatusAsync(capability, method, Token(forged), Data.PropertyId);
    }

    /// <summary>What it answers a call that names no property.</summary>
    public Task<int> StatusWithoutPropertyAsync(string capability, string method) =>
        StatusAsync(capability, method, Token(key), property: null);

    private async Task<int> StatusAsync(string capability, string method, string? token, Guid? property)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/module/{capability}/{method}");
        if (token is not null) request.Headers.Add("Authorization", $"Bearer {token}");
        if (property is { } named) request.Headers.Add(ModuleEnvelope.PropertyHeader, named.ToString());
        var response = await client.SendAsync(request);
        return (int)response.StatusCode;
    }

    /// <summary>How a bundle's parameters look on the wire.</summary>
    private static readonly JsonSerializerOptions Bundle = new(JsonSerializerDefaults.Web);

    /// <summary>A token the platform's own validation accepts, signed with this suite's key.</summary>
    private string Token() => Token(key);

    private static string Token(RsaSecurityKey signing)
    {
        var handler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://identity.hotelos.local",
            Audience = "hotelos-platform",
            Expires = DateTime.UtcNow.AddMinutes(10),
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            IssuedAt = DateTime.UtcNow,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", Person.ToString()),
                new Claim("sid", Guid.CreateVersion7().ToString()),
            ]),
            SigningCredentials = new SigningCredentials(signing, SecurityAlgorithms.RsaSha256),
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static WebApplication Build(JobsFixture fixture, JobsHarness data, RsaSecurityKey key, StandInKernel kernel)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddProvider(new Recording());

        // A real socket on a port the operating system picks, HTTP/1.1 — which
        // is what the Kernel starts an application on and what the Shell's
        // forwarder speaks. Loopback by address rather than by name: localhost
        // resolves to ::1 first, and the listener would be somewhere else.
        builder.Services.Configure<KestrelServerOptions>(kestrel => kestrel.Listen(
            System.Net.IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http1));

        builder.Services.AddDbContext<JobsDbContext>(options => options
            .UseNpgsql(fixture.ApplicationConnection)
            .UseSnakeCaseNamingConvention());

        // The platform's own authentication and authorization, given this
        // suite's key and this suite's Kernel endpoint.
        builder.Services.AddSingleton<IJwksProvider>(new TestKeys(key));
        builder.Services.AddSingleton<IRevocationCache, NothingRevoked>();
        builder.Services.AddSingleton(new TokenValidationPolicy());
        builder.Services.AddSingleton<JwtCallerAuthenticator>();
        builder.Services.AddSingleton(new ServiceIdentity("jobs"));
        builder.Services.AddSingleton<KernelService.KernelServiceClient>(kernel);
        builder.Services.AddSingleton<KernelAuthorizer>();
        builder.Services.AddSingleton<IKernelAuthorizer>(p => p.GetRequiredService<KernelAuthorizer>());

        // The seeding harness's clock, not the machine's: the rows are written
        // against it, so a host reading the wall clock would compute a running
        // session that had been going for days. The same reason the timer is
        // the service's figure and never the desktop's.
        builder.Services.AddSingleton<TimeProvider>(data.Clock);
        builder.Services.AddSingleton<IPropertyDirectory>(data.Directory);
        // Bound to this application's own context, exactly as
        // AddHotelOsPlatform binds it: the event must be appended in the
        // transaction that caused the change, and resolving DbContext by its
        // base type would find nothing.
        builder.Services.AddScoped<IEventAppender>(provider => new EventAppender(
            provider.GetRequiredService<JobsDbContext>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ServiceIdentity>()));
        builder.Services.AddScoped<JobRecords>();
        builder.Services.AddScoped<JobAnnouncer>();
        builder.Services.AddScoped<JobNumbering>();
        builder.Services.AddScoped<JobPolicyResolver>();
        builder.Services.AddScoped<AssignmentService>();
        builder.Services.AddScoped<JobService>();
        builder.Services.AddScoped<WorkSessionService>();
        builder.Services.AddScoped<CompletionService>();
        builder.Services.AddScoped<CancellationService>();
        builder.Services.AddScoped<CourseService>();
        builder.Services.AddScoped<NoteService>();
        builder.Services.AddScoped<JobQueries>();
        builder.Services.AddScoped<CatalogueService>();
        builder.Services.AddScoped<PropertyCatalogueService>();
        builder.Services.AddScoped<ConcernPolicyService>();
        builder.Services.AddScoped<PresenceService>();
        builder.Services.AddScoped<ClosingHoldService>();
        builder.Services.AddJobsModule();
        builder.Services.AddModuleRefusals();

        var app = builder.Build();
        app.UseModuleRefusals();
        app.MapJobsModule();
        return app;
    }

    private static string Address(WebApplication app) =>
        app.Urls.FirstOrDefault() ?? throw new InvalidOperationException("the module surface bound no address");

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
    }

    /// <summary>What a module call answered — the status the bundle sees, and the JSON.</summary>
    public sealed record ModuleAnswer(int Status, JsonElement? Body)
    {
        /// <summary>The value at this path, or a failure naming what was there instead.</summary>
        public JsonElement At(params string[] path)
        {
            var value = Body ?? throw new InvalidOperationException($"the call answered {Status} with no body");
            foreach (var step in path)
            {
                value = value.ValueKind switch
                {
                    JsonValueKind.Object when value.TryGetProperty(step, out var next) => next,
                    JsonValueKind.Array when int.TryParse(step, out var index) && index < value.GetArrayLength() =>
                        value[index],
                    _ => throw new InvalidOperationException($"no '{step}' in {value}"),
                };
            }

            return value;
        }

        public string Text(params string[] path) => At(path).GetString() ?? string.Empty;

        public int Count(params string[] path) => At(path).GetArrayLength();

        public int Number(params string[] path) => At(path).GetInt32();
    }

    /// <summary>Keeps what the surface logged, so a failure can be read.</summary>
    private sealed class Recording : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Kept();

        public void Dispose()
        {
        }

        private sealed class Kept : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                Failures.Add($"{formatter(state, exception)} {exception}");
            }
        }
    }

    /// <summary>Identity's key set, as this suite owns it.</summary>
    private sealed class TestKeys(RsaSecurityKey key) : IJwksProvider
    {
        public SecurityKey? GetKey(string keyId) => keyId == key.KeyId ? key : null;

        public Task RefreshIfNeededAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> RefreshOnUnknownKidAsync(string keyId, CancellationToken cancellationToken) =>
            Task.FromResult(keyId == key.KeyId);
    }

    /// <summary>No session in this suite has been ended.</summary>
    private sealed class NothingRevoked : IRevocationCache
    {
        public bool IsRevoked(Guid sessionId) => false;

        public bool IsAuthoritative => true;
    }
}
