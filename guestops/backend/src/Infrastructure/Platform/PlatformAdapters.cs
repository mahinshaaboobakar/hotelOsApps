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
        // The Context Service, by address. **Not discovered through the
        // Kernel**, which is how a platform service finds its peers — an
        // installed application has no identity to discover with, and inventing
        // a discovery path here is exactly what round 51 exists to answer. A
        // configured address is the smallest honest stand-in and it is visible
        // in `appsettings.json` rather than buried.
        services.AddGrpcClient<ContextService.ContextServiceClient>(client =>
            client.Address = new Uri(
                configuration["Context:Endpoint"] ?? "https://127.0.0.1:15053"));

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
    /// <b>Where the key comes from for a packaged application is round 51's.</b>
    /// A platform service reads its material from the secret store it was
    /// provisioned with; nothing provisions one for a <c>.hopkg</c>, and this
    /// configuration entry is the seam, not the answer.
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
