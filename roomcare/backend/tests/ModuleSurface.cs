using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using HotelOS.Contracts.Kernel.V1;
using HotelOS.Platform;
using HotelOS.RoomCare.Application;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace HotelOS.RoomCare.Tests;

/// <summary>Room Care's module surface on a real socket — the envelope's token check, the capability guard, and the JSON the screens read.</summary>
/// <remarks>
/// Jobs' <c>ModuleHarness</c>, reduced to what Room Care needs: the platform's
/// own authentication with this suite's key, a stand-in Kernel answering
/// capability and permission checks, and the application's services exactly as
/// the host wires them.
/// </remarks>
public sealed class ModuleSurface : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Bundle = new(JsonSerializerDefaults.Web);
    private readonly WebApplication _app;
    private readonly HttpClient _client;
    private readonly RsaSecurityKey _key;

    private ModuleSurface(WebApplication app, RsaSecurityKey key, RoomCareHarness data, Guid person)
    {
        _app = app;
        _key = key;
        _client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        Data = data;
        Person = person;
    }

    public RoomCareHarness Data { get; }

    public Guid Person { get; }

    public static async Task<ModuleSurface> StartAsync(RoomCareFixture fixture)
    {
        var data = new RoomCareHarness(fixture);
        var key = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "roomcare-module-test" };
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Error);
        builder.Services.Configure<KestrelServerOptions>(k => k.Listen(System.Net.IPAddress.Loopback, 0, l => l.Protocols = HttpProtocols.Http1));
        builder.Services.AddDbContext<RoomCareDbContext>(o => o.UseSnakeCaseNamingConvention().UseNpgsql(fixture.ApplicationConnection));
        builder.Services.AddSingleton<IJwksProvider>(new Keys(key));
        builder.Services.AddSingleton<IRevocationCache, NothingRevoked>();
        builder.Services.AddSingleton(new TokenValidationPolicy());
        builder.Services.AddSingleton<JwtCallerAuthenticator>();
        builder.Services.AddSingleton(new ServiceIdentity("roomcare"));
        builder.Services.AddSingleton<KernelService.KernelServiceClient>(new StandInKernel(_ => true));
        builder.Services.AddSingleton<KernelAuthorizer>();
        builder.Services.AddSingleton<IKernelAuthorizer>(p => p.GetRequiredService<KernelAuthorizer>());
        builder.Services.AddRoomCareApplication();
        builder.Services.AddSingleton<TimeProvider>(data.Clock);
        builder.Services.AddSingleton<IHouse>(data.House);
        builder.Services.AddScoped<IEventAppender>(p => new EventAppender(
            p.GetRequiredService<RoomCareDbContext>(), p.GetRequiredService<TimeProvider>(), p.GetRequiredService<ServiceIdentity>()));
        builder.Services.AddRoomCareModule();
        builder.Services.AddModuleRefusals();
        var app = builder.Build();
        app.UseModuleRefusals();
        app.MapRoomCareModule();
        await app.StartAsync();
        return new ModuleSurface(app, key, data, Guid.CreateVersion7());
    }

    /// <summary>Call a capability's method as the Shell forwards it — camelCase JSON, a bearer, the property header.</summary>
    public Task<(int Status, JsonElement? Body)> CallAsync(string capability, string method, object? parameters = null, bool withToken = true) =>
        CallAsAsync(Person, capability, method, parameters, withToken);

    /// <summary>The same call, signed in as someone else — an attendant reading their own rooms.</summary>
    public async Task<(int Status, JsonElement? Body)> CallAsAsync(Guid person, string capability, string method, object? parameters = null, bool withToken = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/module/{capability}/{method}")
        {
            Content = new StringContent(JsonSerializer.Serialize(parameters, Bundle), System.Text.Encoding.UTF8, "application/json"),
        };
        if (withToken)
        {
            request.Headers.Add("Authorization", $"Bearer {Token(person)}");
        }

        request.Headers.Add(ModuleEnvelope.PropertyHeader, Data.PropertyId.ToString());
        var response = await _client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return ((int)response.StatusCode, text.Length == 0 ? null : JsonDocument.Parse(text).RootElement.Clone());
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
            Subject = new ClaimsIdentity([new Claim("sub", person.ToString()),new Claim("sid", Guid.CreateVersion7().ToString())]),
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
    }

    private sealed class Keys(RsaSecurityKey key) : IJwksProvider
    {
        public SecurityKey? GetKey(string keyId) => keyId == key.KeyId ? key : null;

        public Task RefreshIfNeededAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> RefreshOnUnknownKidAsync(string keyId, CancellationToken cancellationToken) => Task.FromResult(keyId == key.KeyId);
    }

    private sealed class NothingRevoked : IRevocationCache
    {
        public bool IsRevoked(Guid sessionId) => false;

        public bool IsAuthoritative => true;
    }
}
