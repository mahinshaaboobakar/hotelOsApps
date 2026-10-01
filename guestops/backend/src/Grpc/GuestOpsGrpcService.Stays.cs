using Grpc.Core;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.GuestOps.Domain;
using HotelOS.Formats;
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

    /// <summary>The wire's lifecycle, or a refusal naming the field.</summary>
    /// <remarks>
    /// <para>
    /// <b>This was <c>(Domain.StayLifecycle)(int)request.To</c>, a bare double
    /// cast</b>, so a caller sending <c>STAY_LIFECYCLE_UNSPECIFIED = 0</c> — or any
    /// number at all — reached <c>CorrectAsync</c>, which validates the reason and
    /// not the target. The stay was then stored with a lifecycle <b>no name in the
    /// domain describes</b>, and <c>stay.corrected</c> announced a <c>to</c> with no
    /// name. <c>Domain.StayLifecycle</c> starts at <c>Waitlisted = 1</c> on purpose;
    /// the proto's zero exists only because proto3 requires one, which makes it a
    /// wire artefact rather than a state.
    /// </para>
    /// <para>
    /// <b>The reason <see cref="FromProto(Contracts.V1.StayView)"/> gives applies
    /// here word for word</b> — <i>refused by name rather than defaulted, because a
    /// caller that forgot the field would otherwise get a plausible answer for a
    /// question it never asked.</i> That door refused its zero and this one cast it;
    /// the same file now does the same thing twice.
    /// </para>
    /// <para>
    /// <b>DERIVED rather than enumerated, which is the difference from the view
    /// above.</b> <c>Enum.IsDefined</c> asks the domain what it declares, so a
    /// member added to <c>StayLifecycle</c> is accepted the day it is declared, and
    /// an unknown number from a newer client stays refused forever. A typed
    /// seven-arm switch would be a hand-kept list of the thing it is converting.
    /// <i>What it assumes — that the two enums agree member for member — is asserted
    /// by a test rather than trusted</i>, because a renumbering would make this
    /// silently map one state onto another.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <b>Public rather than private, by ADR 0025's own order</b> — the same
    /// reasoning <c>SettingsService.MintCardNumber</c> settled in this application
    /// today: its test reaches it, no application in this repository uses
    /// <c>InternalsVisibleTo</c>, and inventing one to keep a keyword would be a new
    /// mechanism where widening is what the rule asks for. The refusal is the
    /// deliverable, and a guard asserted by nothing is the defect this change exists
    /// to close.
    /// </remarks>
    public static Domain.StayLifecycle FromProto(Contracts.V1.StayLifecycle to)
    {
        var mapped = (Domain.StayLifecycle)(int)to;

        return Enum.IsDefined(mapped)
            ? mapped
            : throw new InvalidRequestException(
                $"to is required and must name a lifecycle; '{to}' names none");
    }

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
            : Iso8601.Day(value)
                ?? throw new InvalidRequestException("business_date must be an ISO-8601 date");

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
            FromProto(request.To),
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
