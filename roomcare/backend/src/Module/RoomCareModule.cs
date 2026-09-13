using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Module.Capabilities;
using HotelOS.RoomCare.Module.Projections;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.RoomCare.Module;

/// <summary>The one surface Room Care's own screens reach — seven capabilities, one per permission (design page 63 §3).</summary>
/// <remarks>
/// The token check and the capability guard are inside <c>MapModuleCapability</c>,
/// so they cannot be forgotten here — only removed on purpose. Each call runs in
/// its own scope, so a request's context and clock are its own.
/// </remarks>
public static class RoomCareModule
{
    public static IServiceCollection AddRoomCareModule(this IServiceCollection services)
    {
        services.AddScoped<DayFacts>();
        services.AddScoped<BoardProjection>();
        services.AddScoped<RoomProjection>();
        services.AddScoped<PrepareProjection>();
        services.AddScoped<StatesProjection>();
        services.AddScoped<LaneProjection>();
        services.AddScoped<DeepCleanProjection>();
        services.AddScoped<WorkProjection>();
        services.AddScoped<SetupProjection>();
        services.AddScoped<WidgetProjection>();
        return services;
    }

    public static void MapRoomCareModule(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, Permissions.Read, ReadCapability.HandleAsync);
        Map(endpoints, Permissions.Assign, DayCapabilities.AssignAsync);
        Map(endpoints, Permissions.Amend, DayCapabilities.AmendAsync);
        Map(endpoints, Permissions.Clean, RoomCapabilities.CleanAsync);
        Map(endpoints, Permissions.Inspect, RoomCapabilities.InspectAsync);
        Map(endpoints, Permissions.Configure, StandardCapabilities.ConfigureAsync);
        Map(endpoints, Permissions.Plan, StandardCapabilities.PlanAsync);
    }

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
