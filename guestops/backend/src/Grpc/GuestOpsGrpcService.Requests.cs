using Grpc.Core;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Grpc;

/// <summary>Guest requests and stay notes — S18, S19.</summary>
/// <remarks>
/// <b>GuestOps owns the request; Jobs owns the work.</b> Handing off records the
/// request and announces it; no job id, assignee or job state is set by this
/// surface. The job identifier that appears on a request is learned from Jobs'
/// own reply (EVT-Q3), which is why no RPC here writes it.
/// </remarks>
public partial class GuestOpsGrpcService
{
    /// <summary>Log something the guest asked for.</summary>
    /// <param name="request">The stay, the text, and whether it is work.</param>
    /// <param name="context">The call.</param>
    /// <returns>The stored request.</returns>
    public override async Task<Contracts.V1.StayRequest> LogRequest(
        LogRequestRequest request, ServerCallContext context)
    {
        var logged = await requests.LogAsync(
            request.Context.ToScope(CallerContext.Get(context)),
            ParseRequired(request.StayId, "stay_id"),
            request.Text,
            request.HandOff,
            context.CancellationToken);

        return new Contracts.V1.StayRequest
        {
            Id = logged.Id.ToString(),
            StayId = logged.StayId.ToString(),
            Text = logged.Text,
            LoggedBy = Or(logged.LoggedBy),
            LoggedAt = ToIso(logged.LoggedAt),
            HandedOff = logged.HandedOff,

            // Empty until Jobs replies — and that is also what an uninstalled
            // Jobs looks like, deliberately (APPS-Q2).
            JobId = Or(logged.JobId),
        };
    }

    /// <summary>Add a remark about this stay.</summary>
    /// <param name="request">The stay and the remark.</param>
    /// <param name="context">The call.</param>
    /// <returns>The stored note.</returns>
    /// <remarks>
    /// A note is about these nights; a preference should be true next time and
    /// belongs to the guest identity. Writing a preference here would lose it at
    /// check-out.
    /// </remarks>
    public override async Task<Contracts.V1.StayNote> AddNote(
        AddNoteRequest request, ServerCallContext context)
    {
        var note = await requests.AddNoteAsync(
            request.Context.ToScope(CallerContext.Get(context)),
            ParseRequired(request.StayId, "stay_id"),
            request.Text,
            context.CancellationToken);

        return new Contracts.V1.StayNote
        {
            Id = note.Id.ToString(),
            StayId = note.StayId.ToString(),
            Text = note.Text,
            Author = Or(note.Author),
            At = ToIso(note.At),
        };
    }
}
