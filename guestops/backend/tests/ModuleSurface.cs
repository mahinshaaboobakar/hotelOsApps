using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HotelOS.GuestOps.Application;
using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Module;
using HotelOS.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// GuestOps' module surface on a real socket — the envelope's token check and the
/// JSON the screens read.
/// </summary>
/// <remarks>
/// <para>
/// <b>GuestOps was one of two applications with no tier at this level</b> —
/// measured 2026-10-01 on <c>CreateSlimBuilder</c> rather than on a filename:
/// Room Care has <c>ModuleSurface.cs</c>, Jobs has <c>ModuleHarness.cs</c> and
/// <c>WireHarness.cs</c>, GuestOps and Workforce had none. So 32 module methods
/// across 7 capability handlers had no end-to-end coverage, which is why three
/// faults were found by a person pressing tabs.
/// </para>
/// <para>
/// <b>Built on Room Care's shape, and the divergence from Jobs' is the reason.</b>
/// Room Care composes with one call — its application's own registration
/// extension — while Jobs hand-registers seventeen services. A hand-kept list
/// drifts from the host's real composition silently, and the whole point of this
/// tier is to compose as the host does. So this calls
/// <see cref="GuestOpsApplicationRegistration.AddGuestOpsApplication"/>, and a
/// service added to the application tomorrow is here tomorrow.
/// </para>
/// <para>
/// <b>No <c>StandInKernel</c>, and that is a deliberate difference with a cost.</b>
/// Room Care and Jobs each register the real <c>KernelAuthorizer</c> over a
/// stand-in gRPC client — and their two <c>StandInKernel.cs</c> files are
/// byte-identical but for the namespace line, 77 lines each. A third copy is the
/// duplication this repository catches, so this registers
/// <see cref="RecordingAuthorizer"/> as <c>IKernelAuthorizer</c> directly, which
/// GuestOps' suite already has.
/// </para>
/// <para>
/// <b>What that costs, stated rather than left:</b> this tier does NOT exercise
/// <c>KernelAuthorizer</c>'s translation of a Kernel answer into a refusal. It
/// covers the envelope's token check, the capability routing, the handler, and the
/// JSON a screen receives. The Kernel's own decision is Part B's column, and the
/// permission names each service requires are already asserted by the service
/// suites — so the gap is narrow and named.
/// </para>
/// <para>
/// <b>And no <c>ModuleRefusals</c>, because GuestOps correctly has none.</b> Jobs
/// and Room Care each carry a 99-line middleware — byte-identical but for the
/// namespace — catching <c>AuthenticationFailedException</c> outside the handler to
/// return 401. <c>ModuleEnvelope.cs:305</c> now maps that exception to
/// <c>Results.Unauthorized()</c> itself, so the middleware is redundant. This
/// composes as GuestOps' <c>Program.cs</c> does, not as Room Care's test does.
/// </para>
/// </remarks>
public sealed class ModuleSurface : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web);

    private readonly WebApplication _app;
    private readonly HttpClient _client;
    private readonly RsaSecurityKey _key;
    private readonly GuestOpsScratch _scratch;

    private ModuleSurface(
        WebApplication app, RsaSecurityKey key, GuestOpsScratch scratch, Guid person)
    {
        _app = app;
        _key = key;
        _scratch = scratch;
        _client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        Person = person;
    }

    /// <summary>The desk clerk every call is made as, unless one names another.</summary>
    public Guid Person { get; }

    /// <summary>The property every call is scoped to.</summary>
    public static Guid Property => DeskHarness.Property;

    /// <summary>Start the surface on a loopback port the OS chooses.</summary>
    /// <remarks>
    /// <b>Port 0.</b> Nothing for two streams to converge on, and no name for
    /// anyone to match — the server is held in-process so disposal owns the
    /// handle.
    /// </remarks>
    public static async Task<ModuleSurface> StartAsync()
    {
        var scratch = await GuestOpsScratch.CreateAsync();
        var key = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "guestops-module-test" };

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Error);
        builder.Services.Configure<KestrelServerOptions>(
            k => k.Listen(IPAddress.Loopback, 0, l => l.Protocols = HttpProtocols.Http1));

        builder.Services.AddDbContext<GuestOpsDbContext>(
            o => o.UseSnakeCaseNamingConvention().UseNpgsql(scratch.Connection));

        // The platform's OWN authentication, against this suite's key. The token
        // below is minted here and validated by the real `JwtCallerAuthenticator`,
        // so nothing in this file needs a credential and none is asked for.
        builder.Services.AddSingleton<IJwksProvider>(new Keys(key));
        builder.Services.AddSingleton<IRevocationCache, NothingRevoked>();
        builder.Services.AddSingleton(new TokenValidationPolicy());
        builder.Services.AddSingleton<JwtCallerAuthenticator>();
        builder.Services.AddSingleton(new ServiceIdentity("guestops"));

        // See the remarks: the recording authorizer rather than a third copy of a
        // stand-in Kernel, with what that does not cover named.
        builder.Services.AddSingleton<IKernelAuthorizer>(Authorizer);

        builder.Services.AddGuestOpsApplication();
        builder.Services.AddSingleton<TimeProvider>(Clock);
        builder.Services.AddSingleton<IBusinessDay>(new StubBusinessDay(new DateOnly(2026, 9, 1)));
        builder.Services.AddScoped<IEventAppender, RecordingAppender>();

        var app = builder.Build();
        app.MapGuestOpsModule();
        await app.StartAsync();

        return new ModuleSurface(app, key, scratch, Guid.CreateVersion7());
    }

    /// <summary>What every call authorized, so a test can assert the permission as well as the JSON.</summary>
    public static RecordingAuthorizer Authorizer { get; } = new();

    /// <summary>The clock the surface runs on — fixed, so a date in a response is reproducible.</summary>
    public static ManualClock Clock { get; } =
        new(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Call a capability's method as the Shell forwards it.</summary>
    /// <param name="capability">The permission the manifest declared.</param>
    /// <param name="method">The method within it.</param>
    /// <param name="parameters">The body, serialised camelCase as the Shell sends it.</param>
    /// <param name="withToken">False to omit the bearer — the refusal path.</param>
    /// <returns>The status and the parsed body, or null where the body is empty.</returns>
    public Task<(int Status, JsonElement? Body)> CallAsync(
        string capability, string method, object? parameters = null, bool withToken = true)
        => CallAsAsync(Person, capability, method, parameters, withToken);

    /// <summary>The same call, signed in as somebody else.</summary>
    public async Task<(int Status, JsonElement? Body)> CallAsAsync(
        Guid person, string capability, string method, object? parameters = null, bool withToken = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/module/{capability}/{method}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(parameters, Wire), Encoding.UTF8, "application/json"),
        };

        if (withToken)
        {
            request.Headers.Add("Authorization", $"Bearer {Token(person)}");
        }

        request.Headers.Add(ModuleEnvelope.PropertyHeader, Property.ToString());

        var response = await _client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        return ((int)response.StatusCode,
            text.Length == 0 ? null : JsonDocument.Parse(text).RootElement.Clone());
    }

    private string Token(Guid person)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://identity.hotelos.local",
            Audience = "hotelos-platform",
            Expires = DateTime.UtcNow.AddMinutes(10),
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            IssuedAt = DateTime.UtcNow,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", person.ToString()),
                new Claim("sid", Guid.CreateVersion7().ToString()),
            ]),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        };

        var handler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _scratch.DisposeAsync();
    }

    /// <summary>The one key this suite signs with, answered as the JWKS would.</summary>
    private sealed class Keys(RsaSecurityKey key) : IJwksProvider
    {
        public SecurityKey? GetKey(string keyId) => keyId == key.KeyId ? key : null;

        public Task RefreshIfNeededAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<bool> RefreshOnUnknownKidAsync(string keyId, CancellationToken cancellationToken)
            => Task.FromResult(keyId == key.KeyId);
    }

    /// <summary>
    /// Nothing is revoked, and it says so AUTHORITATIVELY.
    /// </summary>
    /// <remarks>
    /// <c>IsAuthoritative</c> is true rather than false on purpose: a cache that
    /// admits it does not know would let the envelope take a different path, and a
    /// test whose refusals came from an unsure cache would be measuring the cache.
    /// </remarks>
    private sealed class NothingRevoked : IRevocationCache
    {
        public bool IsRevoked(Guid sessionId) => false;

        public bool IsAuthoritative => true;
    }
}
