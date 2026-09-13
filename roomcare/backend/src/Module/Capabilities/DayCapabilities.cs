using HotelOS.Platform;
using HotelOS.RoomCare.Application.Assignment;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Application.Supervision;
using HotelOS.RoomCare.Application.Work;
using HotelOS.RoomCare.Domain;
using Microsoft.Extensions.DependencyInjection;

using static HotelOS.Platform.ModuleEnvelope;

namespace HotelOS.RoomCare.Module.Capabilities;

/// <summary><c>roomcare.assign</c> and <c>roomcare.amend</c> — the supervisor's hand on the day.</summary>
public static class DayCapabilities
{
    public static async Task<object?> AssignAsync(IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var assignment = services.GetRequiredService<AssignmentService>();
        return request.Method switch
        {
            "prepare" => Run(await services.GetRequiredService<PrepareService>().PressAsync(request.Scope, body.OptionalText("window"), cancellationToken)),
            "acceptProposal" => new { accepted = await assignment.AcceptAllAsync(request.Scope, body.Date("day"), body.Text("window"), cancellationToken) },
            "assign" => Task(await assignment.AssignAsync(request.Scope, body.Id("taskId"), body.Version(), body.Id("userId"), cancellationToken)),
            "unassign" => Task(await assignment.UnassignAsync(request.Scope, body.Id("taskId"), body.Version(), cancellationToken)),
            _ => throw new InvalidRequestException($"roomcare.assign has no method '{request.Method}'"),
        };
    }

    public static async Task<object?> AmendAsync(IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var scope = request.Scope;
        var amend = services.GetRequiredService<AmendService>();
        return request.Method switch
        {
            "skip" => Task(await amend.SkipAsync(scope, body.Id("taskId"), body.Version(), body.Text("reason"), cancellationToken)),
            "defer" => Task(await amend.DeferAsync(scope, body.Id("taskId"), body.Version(), body.Instant("notBefore"), body.OptionalText("reason"), cancellationToken)),
            "reduce" => Task(await amend.ReduceAsync(scope, body.Id("taskId"), body.Version(), body.Text("what"), body.OptionalText("reason"), cancellationToken)),
            "reprioritise" => Task(await amend.ReprioritiseAsync(scope, body.Id("taskId"), body.Version(), body.Text("priority"), body.Text("reason"), cancellationToken)),
            "recordOnBehalf" => Task(await amend.RecordOnBehalfAsync(scope, Attempt(body), body.Version(), cancellationToken)),
            "clearDisagreement" => Room(await services.GetRequiredService<DisagreementService>()
                .ClearAsync(scope, body.Id("roomId"), body.Version(), body.Text("kept"), cancellationToken)),
            "decide" => Lane(await services.GetRequiredService<SupervisionService>()
                .DecideAsync(scope, body.Id("supervisionId"), body.Text("decision"), body.OptionalText("note"), cancellationToken)),
            "saveStates" => await services.GetRequiredService<RoomStatesService>().SaveAsync(scope, Edits(body), cancellationToken),
            _ => throw new InvalidRequestException($"roomcare.amend has no method '{request.Method}'"),
        };
    }

    internal static AttemptCommand Attempt(System.Text.Json.JsonElement? body) => new(body.Id("taskId"), body.Text("found"))
    {
        PartialDone = body.Texts("partialDone"),
        Note = body.OptionalText("note"),
        LinenChanged = body.Flag("linenChanged"),
    };

    internal static object Task(RoomTask task) => new { id = task.Id.ToString(), version = task.Version, status = task.Status, outcome = task.Outcome };

    private static object Room(RoomState room) => new { roomId = room.RoomId.ToString(), version = room.Version, condition = room.Condition };

    private static object Lane(RoomSupervision lane) => new { id = lane.Id.ToString(), decision = lane.Decision, decidedAt = lane.DecidedAt };

    private static object Run(PrepareRun run) => new
    {
        id = run.Id.ToString(), kind = run.Kind, window = run.Window, created = run.TasksCreated, updated = run.TasksUpdated,
        pending = run.Pending, unassignable = run.Unassignable,
    };

    private static IReadOnlyList<RoomStateEdit> Edits(System.Text.Json.JsonElement? body) =>
        body.Objects("rooms").Select(room => new RoomStateEdit(room.Id("roomId"), room.Version())
        {
            Condition = room.OptionalText("condition"),
            Occupancy = room.OptionalText("occupancy"),
            Stay = room.OptionalText("stay"),
            SoldAt = room.OptionalTime("soldAt"),
            ClearSold = room.Flag("clearSold"),
        }).ToList();
}
