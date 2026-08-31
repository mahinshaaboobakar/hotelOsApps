using Grpc.Core;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Grpc;

/// <summary>
/// The stay's life — two collaborators, and the split R8 requires.
/// </summary>
/// <remarks>
/// Assignment is its own service because a room change is its own fact, and
/// keeping the two apart here is what stops a move being published as an
/// amendment by an author who only meant to save a form.
/// </remarks>
public partial class GuestOpsGrpcService
{
    public override async Task<Contracts.V1.RoomStay> CheckIn(
        CheckInRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        return ToProto(await lifecycle.CheckInAsync(
            scope,
            ParseRequired(request.StayId, "stay_id"),
            request.Version,
            context.CancellationToken));
    }

    public override async Task<Contracts.V1.RoomStay> CheckOut(
        CheckOutRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        return ToProto(await lifecycle.CheckOutAsync(
            scope,
            ParseRequired(request.StayId, "stay_id"),
            request.Version,
            context.CancellationToken));
    }

    public override async Task<Contracts.V1.RoomStay> CancelStay(
        CancelStayRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        return ToProto(await lifecycle.CancelAsync(
            scope,
            ParseRequired(request.StayId, "stay_id"),
            request.Reason,
            request.Version,
            context.CancellationToken));
    }

    public override async Task<Contracts.V1.RoomStay> RecordNoShow(
        RecordNoShowRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        return ToProto(await lifecycle.RecordNoShowAsync(
            scope,
            ParseRequired(request.StayId, "stay_id"),
            request.Version,
            context.CancellationToken));
    }

    public override async Task<Contracts.V1.RoomStay> CorrectStay(
        CorrectStayRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        return ToProto(await lifecycle.CorrectAsync(
            scope,
            ParseRequired(request.StayId, "stay_id"),
            (Domain.StayLifecycle)(int)request.To,
            request.Reason,
            request.Version,
            context.CancellationToken));
    }

    public override async Task<Contracts.V1.RoomStay> AssignRoom(
        AssignRoomRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        return ToProto(await assignment.AssignAsync(
            scope,
            ParseRequired(request.StayId, "stay_id"),
            ParseRequired(request.RoomId, "room_id"),
            ParseReason(request.Reason),
            request.AcceptConflict,
            request.Version,
            context.CancellationToken));
    }

    /// <summary>The reason, or a refusal to guess one.</summary>
    /// <remarks>
    /// An unrecognised reason is rejected rather than defaulted to
    /// <see cref="AssignmentReason.Move"/>: the reason is what distinguishes an
    /// upgrade from a correction on a room that changed, and a wrong one is a
    /// story about the stay that nobody can correct later.
    /// </remarks>
    private static AssignmentReason ParseReason(string reason)
        => reason switch
        {
            "" or "initial" => AssignmentReason.Initial,
            "move" => AssignmentReason.Move,
            "upgrade" => AssignmentReason.Upgrade,
            "correction" => AssignmentReason.Correction,
            _ => throw new InvalidRequestException(
                $"reason '{reason}' is not one of initial, move, upgrade, correction"),
        };
}
