using Grpc.Core;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Grpc;

/// <summary>Recording that a guest filing was made — S19b.</summary>
/// <remarks>
/// <b>Nothing here submits anything.</b> Sending guest data to an authority is
/// an integration, and every integration on this platform is a connector. These
/// two RPCs record that a person filed and what the authority gave back.
/// </remarks>
public partial class GuestOpsGrpcService
{
    /// <summary>Record that this stay was filed, with its receipt.</summary>
    /// <param name="request">The stay, the authority and the reference.</param>
    /// <param name="context">The call.</param>
    /// <returns>The updated obligation.</returns>
    public override async Task<Contracts.V1.StayReporting> RecordFiling(
        RecordFilingRequest request, ServerCallContext context)
    {
        var filed = await reporting.RecordFilingAsync(
            request.Context.ToScope(CallerContext.Get(context)),
            ParseRequired(request.StayId, "stay_id"),
            request.Authority,
            request.Reference,
            context.CancellationToken);

        return ToProto(filed);
    }

    /// <summary>What this property still owes an authority.</summary>
    /// <param name="request">The day to judge overdue against.</param>
    /// <param name="context">The call.</param>
    /// <returns>Outstanding obligations, the oldest deadline first.</returns>
    public override async Task<ListOutstandingFilingsResponse> ListOutstandingFilings(
        ListOutstandingFilingsRequest request, ServerCallContext context)
    {
        var filings = await reporting.OutstandingAsync(
            request.Context.ToScope(CallerContext.Get(context)),
            ParseDate(request.AsOf, "as_of"),
            context.CancellationToken);

        var response = new ListOutstandingFilingsResponse();
        response.Filings.AddRange(filings.Select(ToProto));
        return response;
    }

    /// <summary>The obligation on the wire.</summary>
    /// <remarks>
    /// The state is the enum's snake_case name rather than its number: this is
    /// read by an operator on a filing screen, and a wire that said <c>2</c>
    /// would need a second table to be legible.
    /// </remarks>
    private static Contracts.V1.StayReporting ToProto(Domain.StayReporting row)
        => new()
        {
            StayId = row.StayId.ToString(),
            RequiredBy = ToIso(row.RequiredBy),
            State = row.State switch
            {
                Domain.ReportingState.Needed => "needed",
                Domain.ReportingState.Filed => "filed",
                _ => "not_required",
            },
            FiledAt = ToIso(row.FiledAt),
            FiledBy = Or(row.FiledBy),
            Authority = row.Authority ?? string.Empty,
            Reference = row.Reference ?? string.Empty,
        };
}
