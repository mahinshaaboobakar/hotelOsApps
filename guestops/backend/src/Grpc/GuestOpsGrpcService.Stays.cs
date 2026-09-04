using Grpc.Core;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.GuestOps.Application.Stays;
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
    /// <summary>One of the four lists, paged — <c>CORE-Q13</c>.</summary>
    /// <remarks>
    /// <para>
    /// The RPC was declared with no override, so it answered
    /// <c>UNIMPLEMENTED</c>. That fails loudly, unlike the never-populated
    /// fields CORE-Q13 removed — but a numbered pager drawn over it would have
    /// been a promise the wire refuses.
    /// </para>
    /// <para>
    /// <c>STAY_VIEW_UNSPECIFIED</c> is refused by name rather than defaulted to
    /// arrivals: a caller that forgot the field would otherwise get a plausible
    /// list for a question it never asked.
    /// </para>
    /// </remarks>
    public override async Task<ListStaysResponse> ListStays(
        ListStaysRequest request, ServerCallContext context)
    {
        var window = Paging.Of(request.Page);

        var found = await stays.ListAsync(
            request.Context.ToScope(CallerContext.Get(context)),
            new StayQuery(FromProto(request.View), ParseDay(request.BusinessDate), window),
            context.CancellationToken);

        var response = new ListStaysResponse
        {
            Meta = Meta(request.Context),
            Page = Paging.Respond(window, found.Total),
        };

        response.Stays.AddRange(found.Rows.Select(ToProto));
        return response;
    }

    /// <summary>The wire's view, or a refusal naming the field.</summary>
    private static Application.Stays.StayView FromProto(Contracts.V1.StayView view) => view switch
    {
        Contracts.V1.StayView.Arrivals => Application.Stays.StayView.Arrivals,
        Contracts.V1.StayView.InHouse => Application.Stays.StayView.InHouse,
        Contracts.V1.StayView.Departures => Application.Stays.StayView.Departures,
        Contracts.V1.StayView.Attention => Application.Stays.StayView.Attention,
        _ => throw new InvalidRequestException("view is required"),
    };

    /// <summary>The business day asked for, or none.</summary>
    /// <remarks>
    /// Empty means the property's current day, which the service asks the
    /// Context Service for. An unparseable date is refused rather than quietly
    /// becoming today — a client sending <c>31/08/2026</c> would otherwise get a
    /// correct-looking list for the wrong question.
    /// </remarks>
    private static DateOnly? ParseDay(string value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.TryParse(value, out var parsed)
                ? parsed
                : throw new InvalidRequestException("business_date must be an ISO-8601 date");

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
