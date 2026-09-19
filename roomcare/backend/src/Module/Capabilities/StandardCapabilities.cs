using HotelOS.Platform;
using HotelOS.RoomCare.Application.DeepCleans;
using HotelOS.RoomCare.Application.Standard;
using Microsoft.Extensions.DependencyInjection;

using static HotelOS.Platform.ModuleEnvelope;

namespace HotelOS.RoomCare.Module.Capabilities;

/// <summary><c>roomcare.configure</c> and <c>roomcare.plan</c> — Setup's saves, the general manager's grant, the deep-clean plan.</summary>
public static class StandardCapabilities
{
    public static async Task<object?> ConfigureAsync(IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var scope = request.Scope;
        var standard = services.GetRequiredService<StandardService>();
        var house = services.GetRequiredService<HouseSetupService>();
        var grants = services.GetRequiredService<ManagerGrants>();
        switch (request.Method)
        {
            case "savePolicy":
                var policy = await standard.SavePolicyAsync(scope, Policy(body), body.Version(), cancellationToken);
                return new { version = policy.Version };
            case "saveWindow":
                var window = await standard.SaveWindowAsync(scope, body.Text("window"), body.Time("starts"), body.Time("ends"),
                    body.Flag("enabled", true), body.Flag("allowAssignmentOutside"), body.Version(), cancellationToken);
                return new { version = window.Version };
            case "saveService":
                var service = await standard.SaveServiceAsync(scope, new ServiceEdit(body.OptionalId("roomTypeId"), body.Text("service"),
                    body.Number("minutes"), body.Decimal("credits"), body.Text("inspectionRule"), body.Texts("phases")), body.Version(), cancellationToken);
                return new { version = service.Version };
            case "assignZone":
                return new { rooms = await house.AssignZoneAsync(scope, body.Ids("roomIds"), body.Id("zoneId"), cancellationToken) };
            case "saveArea":
                var area = await house.SaveAreaAsync(scope, body.Id("locationId"), body.Times("times"), body.Number("minutes", 20),
                    body.Flag("enabled", true), body.Version(), cancellationToken);
                return new { version = area.Version };
            case "saveDeepCleanPlan":
                var plan = await house.SaveDeepCleanPlanAsync(scope, body.Id("roomTypeId"), body.Number("everyMonths"), body.Version(), cancellationToken);
                return new { version = plan.Version };
            case "grantManager":
                var made = await grants.GrantAsync(scope, body.Id("userId"), cancellationToken);
                return new { userId = made.UserId.ToString(), grantedAt = made.GrantedAt };
            case "revokeManager":
                var taken = await grants.RevokeAsync(scope, body.Id("userId"), cancellationToken);
                return new { revoked = taken is not null, revokedAt = taken?.RevokedAt };
            default:
                throw new InvalidRequestException(ModuleParameters.NotOffered);
        }
    }

    public static async Task<object?> PlanAsync(IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var deepCleans = services.GetRequiredService<DeepCleanService>();
        var project = request.Method switch
        {
            "planDeepClean" => await deepCleans.PlanAsync(request.Scope, body.Id("roomId"), body.Date("from"), body.Date("to"), cancellationToken),
            "cancelDeepClean" => await deepCleans.CancelAsync(request.Scope, body.Id("deepCleanId"), body.Version(), cancellationToken),
            _ => throw new InvalidRequestException(ModuleParameters.NotOffered),
        };
        return new { id = project.Id.ToString(), version = project.Version, status = project.Status };
    }

    private static PolicyEdit Policy(System.Text.Json.JsonElement? body) => new()
    {
        TriggerMode = body.OptionalText("triggerMode"),
        WhoLeads = body.OptionalText("whoLeads"),
        StaySource = body.OptionalText("staySource"),
        BoardDefaultView = body.OptionalText("boardDefaultView"),
        StatesDefaultView = body.OptionalText("statesDefaultView"),
        LinenRuleKind = body.OptionalText("linenRuleKind"),
        LinenEveryDays = body.OptionalNumber("linenEveryDays"),
        Towels = body.OptionalText("towels"),
        TurndownEnabled = body.OptionalFlag("turndownEnabled"),
        RefreshAfterDays = body.OptionalNumber("refreshAfterDays"),
        DndRecheckMinutes = body.OptionalNumber("dndRecheckMinutes"),
        SupervisorAfterDays = body.OptionalNumber("supervisorAfterDays"),
        PriorityLadder = body.Texts("priorityLadder") is { Count: > 0 } ladder ? ladder : null,
        AssignmentStrategy = body.OptionalText("assignmentStrategy"),
        UnsoldDeparture = body.OptionalText("unsoldDeparture"),
    };
}
