using HotelOS.Contracts.Context.V1;
using HotelOS.GuestOps.Application.Abstractions;
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
    public static IServiceCollection AddGuestOpsPlatformAdapters(
        this IServiceCollection services, IConfiguration configuration)
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
        services.AddGrpcClient<ContextService.ContextServiceClient>(client =>
            client.Address = new Uri(
                configuration["Context:Endpoint"] ?? "https://127.0.0.1:20054"));

        services.AddScoped<IBusinessDay, ContextBusinessDay>();

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
