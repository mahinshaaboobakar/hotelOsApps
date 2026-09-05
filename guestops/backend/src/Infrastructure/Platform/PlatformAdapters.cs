using HotelOS.Contracts.Context.V1;
using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.Platform;
using HotelOS.Platform.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.GuestOps.Infrastructure.Platform;

/// <summary>
/// The two platform-facing adapters, and where each one's configuration comes
/// from.
/// </summary>
/// <remarks>
/// Both are seams onto things an installed application does not yet receive at
/// install — a channel it is authenticated on, and the platform's PII key
/// material. Registered honestly: they are constructed from configuration, and
/// where the configuration is absent the failure is loud and immediate rather
/// than a fallback that produces plausible wrong answers.
/// </remarks>
public static class PlatformAdapters
{
    /// <param name="services">The container this registers into.</param>
    /// <param name="configuration">The application's configuration.</param>
    /// <param name="platform">
    /// What the Kernel told this application at start-up, or null when it was
    /// not started by one. It carries the certificate directory, which is the
    /// only way the Context channel can authenticate as this application.
    /// </param>
    public static IServiceCollection AddGuestOpsPlatformAdapters(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformEnvironment? platform)
    {
        // The Context Service, by address — the **sanctioned interim** of
        // `AUTHZ-Q21`, which ruled the final state is `Kernel.DiscoverService`
        // like every other service.
        //
        // Not discovered here because an installed application has no identity
        // to discover with, and inventing a discovery path is what that round
        // exists to answer. **This line is removed in the same change that
        // wires the app's Kernel channel** — it is an interim with an expiry
        // rather than a configuration option.
        // **20054, the DEVELOPMENT port.** This said 15053, which is a number
        // this platform defines nowhere — not `INSTALLED_PORTS`, not
        // `dev_settings`, nothing — so the Context Service has never been
        // reachable from this application in development, and every call
        // through `IBusinessDay` has been failing to a fallback since it was
        // written. ADR 0104: development never shares a port with the installed
        // product, and it does not invent a third number either.
        // **It presents this application's certificate** — and did not, until
        // 2026-09-05. This client was registered with an address and nothing
        // else: no `ConfigurePrimaryHttpMessageHandler`, so no client
        // certificate, so a listener demanding mTLS refuses the handshake and
        // every Context call fails. It was the only outbound client across the
        // three shipped applications without one — Jobs and Workforce both use
        // `PlatformTransport.Handler` for Master Data.
        //
        // The comment that stood here said an installed package "has no service
        // certificate — nothing enrols one at install", and **that was wrong**:
        // install issues one, `ApplicationDoors` already resolves it to serve
        // this application's own listener, and the same directory is what this
        // presents. A comment asserting an outcome, believed for a round.
        var endpoint = PlatformEndpoint.For(
            "context",
            new Uri(configuration["Context:Endpoint"] ?? "https://127.0.0.1:20054"));

        // Built from the directory rather than resolved from the container:
        // `ServiceCertificate.Source` is registered only by
        // `AddPlatformAuthentication`, which Program.cs calls **conditionally**,
        // so `GetRequiredService` here would throw on the first Context call of
        // a process started without a platform environment — a missing
        // registration that escapes DI validation and every start-up check, and
        // surfaces as one screen failing.
        //
        // A null directory yields a source that resolves no certificate, which
        // `PlatformTransport` already tolerates: the call then fails at the
        // handshake, which is the honest outcome for a process nobody enrolled.
        var certificates = new ServiceCertificate.Source(
            platform?.CertificateDirectory ?? string.Empty);

        services
            .AddGrpcClient<ContextService.ContextServiceClient>(
                client => client.Address = endpoint.Uri)
            .ConfigurePrimaryHttpMessageHandler(
                () => PlatformTransport.Handler(endpoint, certificates));

        services.AddScoped<IBusinessDay, ContextBusinessDay>();
        services.AddScoped<INeighbours, ContextNeighbours>();

        services.AddSingleton<IContactProtector>(_ => new ContactProtector(
            RequiredKey(configuration, "Pii:FieldKey"),
            RequiredKey(configuration, "Pii:IndexKey")));

        return services;
    }

    /// <summary>A configured key, or a refusal to start.</summary>
    /// <remarks>
    /// <para>
    /// <b>Never generated.</b> A random key at startup would encrypt today's
    /// contacts under something yesterday's rows cannot be read with, and the
    /// blind index would stop matching every guest already stored — a silent,
    /// total loss of the lookup this application exists to serve, discovered
    /// the first time somebody rings.
    /// </para>
    /// <para>
    /// So an absent key fails the start, naming what was missing — ADR 0053's
    /// rule for a dependency, applied to the one dependency whose absence would
    /// otherwise look like success.
    /// </para>
    /// <para>
    /// <b>Where the key comes from for a packaged application is
    /// <c>AUTHZ-Q22</c>, open.</b> A platform service reads its material from
    /// the secret store it was provisioned with; nothing provisions one for a
    /// <c>.hopkg</c>. This configuration entry is the seam, not the answer —
    /// and refusing to start without one is the behaviour that row cites as
    /// correct meanwhile.
    /// </para>
    /// </remarks>
    private static byte[] RequiredKey(IConfiguration configuration, string path)
    {
        var value = configuration[path];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{path} is not configured, and guest contact details cannot be stored or "
                + "found without it. A generated key would silently orphan every contact "
                + "already stored.");
        }

        return Convert.FromBase64String(value);
    }
}
