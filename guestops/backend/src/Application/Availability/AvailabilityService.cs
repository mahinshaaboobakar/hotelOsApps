using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Availability;

/// <summary>What is sellable, of one room type, on one date.</summary>
/// <param name="RoomTypeId">Master Data's type.</param>
/// <param name="Date">The business day asked about.</param>
/// <param name="TotalRooms">Rooms of the type — Master Data's count.</param>
/// <param name="HeldByStays">Stays holding it, by <see cref="Lifecycle.HoldsInventory"/>.</param>
/// <param name="OutOfOrder">EngineeringOps's, heard as an event.</param>
/// <param name="StopSold">This property's own commercial decision.</param>
public sealed record TypeAvailability(
    Guid RoomTypeId,
    DateOnly Date,
    int TotalRooms,
    int HeldByStays,
    int OutOfOrder,
    int StopSold)
{
    /// <summary>What is left, never below zero.</summary>
    /// <remarks>
    /// Clamped because the inputs come from three owners and can briefly
    /// disagree — a room both out of order and held by a stay that has not yet
    /// been moved is real for a few seconds. A negative number on a desk screen
    /// is a bug report; withholding the room is the conservative answer.
    /// </remarks>
    public int Free => Math.Max(0, TotalRooms - HeldByStays - OutOfOrder - StopSold);
}

/// <summary>
/// Availability — an answer computed, never a table someone feeds.
/// </summary>
/// <remarks>
/// <para>
/// GUEST-Q7's shape is the design constraint, not just its scope. Four inputs
/// and three owners: the rooms are <b>Master Data's</b> (read), out-of-order is
/// <b>EngineeringOps's</b> (consumed by event into a local read model, never
/// their table and never authoritative), and the stays and stop-sell are
/// <b>ours</b>.
/// </para>
/// <para>
/// <b>A lagging projection makes the answer conservative and no number
/// wrong.</b> That is the line between an event-derived read model and
/// duplicated master data, and it is why this needed no new inventory owner. A
/// stored availability table would need all four inputs writing into it — four
/// ways to drift, and a second owner of the truth about rooms.
/// </para>
/// <para>
/// <b>No pricing, no minimum stay, no closed-to-arrival, no allotments.</b>
/// Those are revenue-management concepts and the platform has named no owner
/// for them.
/// </para>
/// </remarks>
public sealed class AvailabilityService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    IRoomInventory rooms)
{
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="from">Inclusive.</param>
    /// <param name="to">Inclusive.</param>
    /// <param name="roomTypeIds">Empty means every type the property has.</param>
    /// <param name="cancellationToken">The call's token.</param>
    public async Task<IReadOnlyList<TypeAvailability>> GetAsync(
        RequestScope scope,
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid> roomTypeIds,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ReservationRead, ResourceTypes.Property, scope.PropertyId,
            cancellationToken);

        if (to < from)
        {
            throw new InvalidRequestException("the last date is before the first");
        }

        var counts = await rooms.CountByTypeAsync(scope.PropertyId, roomTypeIds, cancellationToken);
        var types = counts.Keys.ToList();

        // One read of each contributing set for the whole range, rather than
        // one per day: a fortnight's board would otherwise be forty-two queries
        // for a question whose inputs do not change between them.
        var stays = await db.Stays
            .Where(s => s.PropertyId == scope.PropertyId && types.Contains(s.RoomTypeId))
            .Select(s => new StayHold(
                s.RoomTypeId, s.Lifecycle, s.ArrivalAt.At, s.DepartureAt.At,
                s.Terms != null ? s.Terms.ReservesInventory : (bool?)null))
            .ToListAsync(cancellationToken);

        var outOfOrder = await db.RoomsOutOfOrder
            .Where(r => r.PropertyId == scope.PropertyId)
            .ToListAsync(cancellationToken);

        var stopSells = await db.StopSells
            .Where(s => s.PropertyId == scope.PropertyId && types.Contains(s.RoomTypeId))
            .ToListAsync(cancellationToken);

        var answer = new List<TypeAvailability>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var (typeId, total) in counts)
            {
                answer.Add(new TypeAvailability(
                    typeId,
                    date,
                    total,
                    stays.Count(s => s.RoomTypeId == typeId && s.HoldsOn(date)),

                    // Attributed to the type, never the property. One broken
                    // room reduces one type's availability; subtracting it from
                    // every type would make a hotel with a single fault look
                    // full across the board.
                    outOfOrder.Count(r => r.RoomTypeId == typeId
                                          && Covers(r.FromDate, r.ToDate, date)),
                    stopSells.Count(s => s.RoomTypeId == typeId && Covers(s.FromDate, s.ToDate, date))));
            }
        }

        return answer;
    }

    private static bool Covers(DateOnly from, DateOnly? to, DateOnly date)
        => date >= from && (to is null || date <= to);

    /// <summary>One stay's claim on a type, over its nights.</summary>
    private sealed record StayHold(
        Guid RoomTypeId,
        StayLifecycle State,
        DateTimeOffset? Arrival,
        DateTimeOffset? Departure,
        bool? ReservesInventory)
    {
        /// <summary>Whether this stay is holding a room on <paramref name="date"/>.</summary>
        /// <remarks>
        /// <para>
        /// <b>The terms answer before the state does.</b> R18's guarantee
        /// carries <c>reserves_inventory</c> — precisely <i>"does this booking
        /// hold a room"</i>, asked of the system that knows — so a stay whose
        /// source stated it uses that. Only where nothing was stated does the
        /// state's default apply.
        /// </para>
        /// <para>
        /// The nights held are arrival-inclusive and departure-exclusive: a
        /// guest leaving on the 4th does not hold the room on the night of the
        /// 4th, which is the night somebody else can be sold.
        /// </para>
        /// </remarks>
        public bool HoldsOn(DateOnly date)
        {
            if (!(ReservesInventory ?? Lifecycle.HoldsInventory(State)))
            {
                return false;
            }

            if (Arrival is not { } arrival || Departure is not { } departure)
            {
                return false;
            }

            var first = DateOnly.FromDateTime(arrival.DateTime);
            var last = DateOnly.FromDateTime(departure.DateTime);

            return date >= first && date < last;
        }
    }
}
