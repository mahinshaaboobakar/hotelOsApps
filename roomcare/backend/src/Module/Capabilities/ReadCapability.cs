using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Module.Projections;
using Microsoft.Extensions.DependencyInjection;

using static HotelOS.Platform.ModuleEnvelope;

namespace HotelOS.RoomCare.Module.Capabilities;

/// <summary><c>roomcare.read</c> — every screen's data and the five widgets, asked on the property (RC-Q5).</summary>
/// <remarks>
/// Setup's reads are gated inside their projection on <c>roomcare.configure</c>:
/// the capability admits the call, the permission decides the person.
/// </remarks>
public static class ReadCapability
{
    public static async Task<object?> HandleAsync(IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var scope = request.Scope;
        await services.GetRequiredService<Gate>().PropertyAsync(scope, Permissions.Read, cancellationToken);

        return request.Method switch
        {
            "me" => await MeAsync(services, scope, cancellationToken),
            "board" => await services.GetRequiredService<BoardProjection>().BoardAsync(scope, cancellationToken),
            "room" => await services.GetRequiredService<RoomProjection>().RoomAsync(scope, body.Id("roomId"), cancellationToken),
            "prepare" => await services.GetRequiredService<PrepareProjection>().PrepareAsync(scope, body.Number("page"), cancellationToken),
            "attendants" => await services.GetRequiredService<PrepareProjection>().AttendantsAsync(scope, cancellationToken),
            "states" => await services.GetRequiredService<StatesProjection>().StatesAsync(scope, cancellationToken),
            "supervision" => await services.GetRequiredService<LaneProjection>().LaneAsync(scope, body.Number("page"), cancellationToken),
            "deepCleans" => await services.GetRequiredService<DeepCleanProjection>().PageAsync(scope, body.Number("page"), cancellationToken),
            "myRooms" => await services.GetRequiredService<WorkProjection>().MyRoomsAsync(scope, body.Number("page"), cancellationToken),
            "door" => await services.GetRequiredService<WorkProjection>().DoorAsync(scope, body.Id("taskId"), cancellationToken),
            "setup" => await services.GetRequiredService<SetupProjection>().SetupAsync(scope, cancellationToken),
            "services" => await services.GetRequiredService<SetupProjection>().ServicesAsync(scope, body.OptionalId("roomTypeId"), cancellationToken),
            "zones" => await services.GetRequiredService<SetupProjection>().ZonesAsync(scope, cancellationToken),
            "areas" => await services.GetRequiredService<SetupProjection>().AreasAsync(scope, body.Number("page"), body.Flag("withoutRoutine"), cancellationToken),
            "deepCleanPlan" => await services.GetRequiredService<SetupProjection>().PlanAsync(scope, cancellationToken),
            "grants" => await services.GetRequiredService<SetupProjection>().GrantsAsync(scope, services.GetRequiredService<ManagerGrants>(), cancellationToken),
            "widgetRoomsReady" => await services.GetRequiredService<WidgetProjection>().RoomsReadyAsync(scope, cancellationToken),
            "widgetArrivals" => await services.GetRequiredService<WidgetProjection>().ArrivalsAsync(scope, cancellationToken),
            "widgetAttention" => await services.GetRequiredService<WidgetProjection>().AttentionAsync(scope, cancellationToken),
            "widgetAttendants" => await services.GetRequiredService<WidgetProjection>().AttendantsAsync(scope, cancellationToken),
            "widgetPending" => await services.GetRequiredService<WidgetProjection>().PendingAsync(scope, cancellationToken),
            _ => throw new InvalidRequestException(ModuleParameters.NotOffered),
        };
    }

    /// <summary>The bar's identity clause — name · department · property (page 64 §3).</summary>
    private static async Task<object> MeAsync(IServiceProvider services, RequestScope scope, CancellationToken cancellationToken)
    {
        var house = services.GetRequiredService<IHouse>();
        var property = await house.DaySettingsAsync(scope.PropertyId, cancellationToken);
        var name = scope.UserId is { } user ? (await house.NamesAsync([user], cancellationToken)).GetValueOrDefault(user) : null;
        return new { name, department = "Housekeeping", property = string.IsNullOrWhiteSpace(property?.Name) ? property?.Code.ToUpperInvariant() : property.Name };
    }
}
