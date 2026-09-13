using HotelOS.Platform;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Application.Work;
using HotelOS.RoomCare.Domain;
using Microsoft.Extensions.DependencyInjection;

using static HotelOS.Platform.ModuleEnvelope;

namespace HotelOS.RoomCare.Module.Capabilities;

/// <summary><c>room.clean</c> and <c>room.inspect</c> — the attendant's acts on their own room, and applying an inspection.</summary>
public static class RoomCapabilities
{
    public static async Task<object?> CleanAsync(IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var scope = request.Scope;
        var work = services.GetRequiredService<AttendantWork>();
        var acts = services.GetRequiredService<RoomActs>();
        return request.Method switch
        {
            "start" => DayCapabilities.Task(await work.StartAsync(scope, body.Id("taskId"), cancellationToken)),
            "pause" => DayCapabilities.Task(await work.PauseAsync(scope, body.Id("taskId"), body.OptionalText("reason"), cancellationToken)),
            "attempt" => DayCapabilities.Task(await work.AttemptAsync(scope, DayCapabilities.Attempt(body), cancellationToken)),
            "extraTime" => DayCapabilities.Task(await work.AskExtraTimeAsync(scope, body.Id("taskId"), body.Number("minutes"), body.OptionalText("reason"), cancellationToken)),
            "restock" => Restocked(await acts.RestockAsync(scope, body.Id("taskId"), Items(body), body.OptionalId("stayId"), cancellationToken)),
            "issue" => Issue(await acts.IssueAsync(scope, body.Id("taskId"), body.Text("note"), body.OptionalText("itemHint"), body.OptionalId("mediaId"), cancellationToken)),
            _ => throw new InvalidRequestException($"room.clean has no method '{request.Method}'"),
        };
    }

    public static async Task<object?> InspectAsync(IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        if (request.Method != "applyInspection")
        {
            throw new InvalidRequestException($"room.inspect has no method '{request.Method}'");
        }

        var body = request.Body;
        var room = await services.GetRequiredService<InspectionOutcome>().ApplyAsync(
            request.Scope, body.Id("taskId"), body.Flag("passed"), body.OptionalText("reason"), body.OptionalText("inspectionRef"), cancellationToken);
        return new { roomId = room.RoomId.ToString(), condition = room.Condition, version = room.Version };
    }

    private static IReadOnlyList<RestockItem> Items(System.Text.Json.JsonElement? body) =>
        body.Objects("items").Select(i => new RestockItem(i.Text("itemId"), i.Number("quantity"))).ToList();

    private static object Restocked(Restock restock) => new { id = restock.Id.ToString(), at = restock.At };

    private static object Issue(TaskIssue issue) => new { id = issue.Id.ToString(), correlationId = issue.CorrelationId, at = issue.At };
}
