using Grpc.Core;
using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Grpc;

/// <summary>
/// What is sellable — one collaborator, <see cref="AvailabilityService"/>.
/// </summary>
/// <remarks>
/// The response carries the <b>working</b> and not only the answer: total,
/// held, out of order, stop-sold, free. A desk told only *"0 free"* cannot tell
/// a sold-out hotel from four suites a manager closed for a wedding party, and
/// those are different conversations with the caller.
/// </remarks>
public partial class GuestOpsGrpcService
{
    public override async Task<GetAvailabilityResponse> GetAvailability(
        GetAvailabilityRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var answer = await availability.GetAsync(
            scope,
            ParseDate(request.FromDate, "from_date"),
            ParseDate(request.ToDate, "to_date"),
            [.. request.RoomTypeIds.Select(id => ParseRequired(id, "room_type_id"))],
            context.CancellationToken);

        var response = new GetAvailabilityResponse();

        foreach (var row in answer)
        {
            response.Availability.Add(new Contracts.V1.TypeAvailability
            {
                RoomTypeId = row.RoomTypeId.ToString(),
                BusinessDate = row.Date.ToString("yyyy-MM-dd"),
                TotalRooms = row.TotalRooms,
                HeldByStays = row.HeldByStays,
                OutOfOrder = row.OutOfOrder,
                StopSold = row.StopSold,
                Free = row.Free,
            });
        }

        return response;
    }
}
