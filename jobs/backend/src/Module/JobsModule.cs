using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.Jobs.Module;

/// <summary>
/// The surface this application's own UI reaches — design page 63 §3, one
/// route per capability the screens call.
/// </summary>
/// <remarks>
/// <para>
/// A packaged UI runs with no network of its own: its <c>host.call</c> crosses
/// a MessagePort to the Shell, which forwards it here. <b>The token check and
/// the capability guard are inside <c>MapModuleCapability</c></b>, not here —
/// an application that had to remember them would work perfectly on the desk it
/// was written at and fail nowhere visible on a property.
/// </para>
/// <para>
/// <b>Eight capabilities, and the same eight the manifest declares.</b> A route
/// for a capability the manifest never requested would be a door with no lock
/// behind it: the guard checks what an administrator granted, and nothing is
/// granted that was not asked for.
/// </para>
/// </remarks>
public static class JobsModule
{
    /// <summary>What the projections need — registered with the application's own services.</summary>
    public static IServiceCollection AddJobsModule(this IServiceCollection services)
    {
        services.AddScoped<Naming>();
        services.AddScoped<BoardProjection>();
        services.AddScoped<JobProjection>();
        services.AddScoped<SettingsProjection>();
        services.AddScoped<LiveProjection>();
        return services;
    }

    /// <summary>Serve the module's eight capabilities.</summary>
    public static void MapJobsModule(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, Permissions.Read, ReadCapability.HandleAsync);
        Map(endpoints, Permissions.Create, WriteCapabilities.CreateAsync);
        Map(endpoints, Permissions.Assign, WriteCapabilities.AssignAsync);
        Map(endpoints, Permissions.Complete, WriteCapabilities.CompleteAsync);
        Map(endpoints, Permissions.Cancel, WriteCapabilities.CancelAsync);
        Map(endpoints, Permissions.Amend, WriteCapabilities.AmendAsync);
        Map(endpoints, Permissions.Configure, ConfigureCapabilities.ConfigureAsync);
        Map(endpoints, Permissions.Curate, ConfigureCapabilities.CurateAsync);
    }

    /// <summary>
    /// One capability, served by one handler, resolved per call.
    /// </summary>
    /// <remarks>
    /// <b>A scope per call, opened here and disposed after.</b> The handler is
    /// given a provider rather than capturing one, because a captured scoped
    /// service would be one <c>DbContext</c> for the life of the process — the
    /// defect that looks like a database which has stopped seeing new rows. The
    /// envelope hands over what it authenticated and no <c>HttpContext</c>, by
    /// design, so the scope is this file's to open.
    /// </remarks>
    private static void Map(
        IEndpointRouteBuilder endpoints,
        string capability,
        Func<IServiceProvider, ModuleEnvelope.ModuleRequest, CancellationToken, Task<object?>> handler) =>
        endpoints.MapModuleCapability(
            capability,
            async (request, cancellationToken) =>
            {
                using var call = endpoints.ServiceProvider.CreateScope();
                return await handler(call.ServiceProvider, request, cancellationToken);
            });
}
