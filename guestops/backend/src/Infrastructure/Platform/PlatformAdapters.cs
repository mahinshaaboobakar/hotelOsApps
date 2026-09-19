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
    /// <summary>
    /// The Context Service's registered name — what the Kernel's directory
    /// answers to, and what the peer's certificate must present.
    /// </summary>
    /// <remarks>
    /// The Kernel's <c>CONTEXT_PRINCIPAL</c> (<c>kernel-core/src/pki/mod.rs</c>).
    /// Written here because the SDK's <c>PlatformEndpoint</c> names only the
    /// Kernel, Identity and Master Data.
    /// </remarks>
    private const string ContextName = "context";

    /// <summary>
    /// The directory for a process no Kernel started: it refuses, by name.
    /// </summary>
    /// <remarks>
    /// A checkout run has no Kernel and so nothing to discover with. It gets
    /// this rather than a configured address, because an address that works
    /// from a checkout is an address an installed build can fall back to —
    /// which is exactly how the Context literal outlived its expiry.
    /// </remarks>
    private sealed class NoKernel : IPlatformDirectory
    {
        public ValueTask<PlatformEndpoint> LocateAsync(
            string serviceName, CancellationToken cancellationToken)
            => throw new InvalidOperationException(
                $"GuestOps was not started by a Kernel, so there is nothing to ask where "
                + $"{serviceName} answers. An installed application discovers its peers; "
                + "it configures none.");
    }

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
        // **The Context Service, DISCOVERED through the Kernel** — `AUTHZ-Q21`'s
        // final state, reached 2026-09-19 on the owner's first run of 0.3.1.
        //
        // **What this said until then, and why it went.** It dialled
        // `configuration["Context:Endpoint"] ?? "https://127.0.0.1:20054"` —
        // *"the sanctioned interim of AUTHZ-Q21 … Not discovered here because an
        // installed application has no identity to discover with … This line is
        // removed in the same change that wires the app's Kernel channel."*
        // That channel was wired (`SHELL-Q40` §3·3, `ed38380`) and the line was
        // not removed: the interim outlived its own expiry. So when development
        // Context moved to 25154, every Context call from the owner's GuestOps
        // was refused at 20054 and drawn as a service fault — while the same
        // log showed `DiscoverService` answering for the Kernel and Identity.
        //
        // The port is asked for at connect time (`PlatformTransport.Handler`'s
        // directory overload), so it is never written anywhere in this
        // application and a Context that moves is simply found where it is.
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

        // The NAME only — the request URI carries it for `Host:` and TLS, and the
        // port belongs to the socket, which the handler dials where the Kernel
        // says (`PlatformEndpoint.Uri`'s own remark).
        services
            .AddGrpcClient<ContextService.ContextServiceClient>(
                client => client.Address = new Uri($"https://{ContextName}"))
            .ConfigurePrimaryHttpMessageHandler(provider => PlatformTransport.Handler(
                ContextName,
                provider.GetService<IPlatformDirectory>() ?? new NoKernel(),
                certificates));

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
