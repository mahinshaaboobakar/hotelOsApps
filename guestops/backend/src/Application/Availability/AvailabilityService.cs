using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
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

/// <summary>One room nobody is holding, over the dates that were asked about.</summary>
/// <param name="Id">Master Data's room. What a check-in is assigned.</param>
/// <param name="Number">What the desk calls it — <c>308</c>, <c>PH-2</c>.</param>
/// <remarks>
/// <b>Two fields, and no state.</b> Whether the room is clean is Room Care's
/// and this application does not assert it; whether it is free is established
/// by the room being in this list at all. A third field saying <i>vacant</i>
/// would be the list repeating its own definition back to the reader.
/// </remarks>
public sealed record FreeRoom(Guid Id, string Number);

/// <summary>The free rooms of one type, and how many that type has at all.</summary>
/// <param name="Free">Rooms nobody is holding over the dates asked about.</param>
/// <param name="Total">Rooms of this type the property has, free or not.</param>
/// <remarks>
/// <b><c>Total</c> is carried so that an empty list is not one answer to two
/// questions.</b> *This property has no rooms of that type* and *every room of
/// that type is taken* are different facts with opposite remedies — configure
/// the property, or choose another type — and a bare empty list reports them
/// alike, which is a measurement nobody took.
/// </remarks>
public sealed record RoomsOfType(IReadOnlyList<FreeRoom> Free, int Total);

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
    IRoomInventory rooms,
    IBusinessDay businessDay)
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
                s.Terms != null ? s.Terms.ReservesInventory : (bool?)null,

                // No room: this question is asked per TYPE, and which room a
                // stay happens to be in cannot change a count of the type.
                null))
            .ToListAsync(cancellationToken);

        var outOfOrder = await db.RoomsOutOfOrder
            .Where(r => r.PropertyId == scope.PropertyId)
            .ToListAsync(cancellationToken);

        var stopSells = await db.StopSells
            .Where(s => s.PropertyId == scope.PropertyId && types.Contains(s.RoomTypeId))
            .ToListAsync(cancellationToken);

        // Which nights a stay holds is read on the property's calendar
        // (ADR 0174). A stay whose days cannot be established holds none.
        var zone = await businessDay.ZoneAsync(scope, cancellationToken);

        var answer = new List<TypeAvailability>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var (typeId, total) in counts)
            {
                answer.Add(new TypeAvailability(
                    typeId,
                    date,
                    total,
                    stays.Count(s => s.RoomTypeId == typeId && s.HoldsOn(date, zone)),

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

    /// <summary>Which rooms of one type nobody is holding over these dates.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="roomTypeId">The type the desk has chosen.</param>
    /// <param name="from">Inclusive.</param>
    /// <param name="to">Inclusive.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The free rooms, by number, in the order a desk reads them.</returns>
    /// <exception cref="InvalidRequestException">The last date is before the first.</exception>
    /// <remarks>
    /// <para>
    /// <b>Here rather than in a view, because this is the same question
    /// <see cref="GetAsync"/> answers at a coarser grain.</b> A view that worked
    /// out for itself which rooms are taken would be a second place availability
    /// is decided, and the two would disagree the first time either was
    /// corrected — this one reuses <c>HoldsOn</c>, including its
    /// arrival-inclusive, departure-exclusive nights and its deference to a
    /// source's <c>reserves_inventory</c>.
    /// </para>
    /// <para>
    /// <b>Check-in needs a room (S8), and nothing else in this application could
    /// name one.</b> <c>WalkInCommand</c> has required a <c>roomId</c> since it
    /// was written and no read listed rooms, so the walk-in sheet could not be
    /// completed by any caller — an absence no comparison of screens can find,
    /// because an absent control has no node to compare.
    /// </para>
    /// <para>
    /// <b>It says nothing about whether a room is clean.</b> That is Room Care's
    /// fact, and an absent neighbour loses its capability and never this flow
    /// (APPS-Q2). A free room here is one nobody is holding and that is not out
    /// of order; a property with no Room Care still takes walk-ins.
    /// </para>
    /// </remarks>
    public async Task<RoomsOfType> FreeRoomsAsync(
        RequestScope scope,
        Guid roomTypeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ReservationRead, ResourceTypes.Property, scope.PropertyId,
            cancellationToken);

        if (to < from)
        {
            throw new InvalidRequestException("the last date is before the first");
        }

        var rooms = await db.Set<MasterDataRoom>()
            .Where(r => r.PropertyId == scope.PropertyId
                && r.RoomTypeId == roomTypeId
                && r.Active
                && r.DeletedAt == null)
            .Select(r => new { r.Id, r.RoomNumber })
            .ToListAsync(cancellationToken);

        if (rooms.Count == 0)
        {
            return new RoomsOfType([], 0);
        }

        // Only stays that have a room. One without is holding the TYPE's
        // inventory — which `GetAsync` counts — and is holding no particular
        // room, so it cannot make one unavailable here.
        var held = await db.Stays
            .Where(s => s.PropertyId == scope.PropertyId
                && s.RoomTypeId == roomTypeId
                && s.CurrentRoomId != null)
            .Select(s => new StayHold(
                s.RoomTypeId, s.Lifecycle, s.ArrivalAt.At, s.DepartureAt.At,
                s.Terms != null ? s.Terms.ReservesInventory : (bool?)null,
                s.CurrentRoomId))
            .ToListAsync(cancellationToken);

        var outOfOrder = await db.RoomsOutOfOrder
            .Where(r => r.PropertyId == scope.PropertyId && r.RoomTypeId == roomTypeId)
            .ToListAsync(cancellationToken);

        var zone = await businessDay.ZoneAsync(scope, cancellationToken);

        var taken = new HashSet<Guid>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var stay in held.Where(s => s.HoldsOn(date, zone)))
            {
                taken.Add(stay.RoomId!.Value);
            }

            foreach (var room in outOfOrder.Where(r => Covers(r.FromDate, r.ToDate, date)))
            {
                taken.Add(room.RoomId);
            }
        }

        // Ordered by what the desk says out loud. `308` before `1204` would be
        // wrong to a person reading a list, and so would `PH-2` sorted among
        // digits — so it is the number's own text, which is what is printed on
        // the door and on the key card.
        return new RoomsOfType(
            [
                .. rooms
                    .Where(r => !taken.Contains(r.Id))
                    .OrderBy(r => r.RoomNumber, StringComparer.OrdinalIgnoreCase)
                    .Select(r => new FreeRoom(r.Id, r.RoomNumber)),
            ],
            rooms.Count);
    }

    /// <summary>The rooms free for a stay's own type, over its own nights.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="stayId">The stay being given a room.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The free rooms, and how many that type has at all.</returns>
    /// <exception cref="NotFoundException">No such stay at this property.</exception>
    /// <remarks>
    /// <para>
    /// <b>The assignment sheet knows a stay, not a date range.</b> Asking it to
    /// carry the type and the nights would put the stay's own facts in a
    /// client's hands to send back — and a screen that sent the wrong range
    /// would be offered rooms that are free for dates nobody is staying.
    /// </para>
    /// <para>
    /// <b>The zone is applied here, once.</b> A stay's nights are instants, and
    /// which day they fall on is the property's (ADR 0174) — a caller deriving
    /// days from the instants would be reading them in the browser's zone.
    /// </para>
    /// <para>
    /// <b>A stay whose nights cannot be established has no free rooms rather
    /// than every room.</b> Without a business day nothing here can say which
    /// nights are held, and answering *all of them* would offer an occupied
    /// room as free.
    /// </para>
    /// </remarks>
    public async Task<RoomsOfType> FreeRoomsForStayAsync(
        RequestScope scope,
        Guid stayId,
        CancellationToken cancellationToken)
    {
        var stay = await db.Stays
            .Where(s => s.Id == stayId && s.PropertyId == scope.PropertyId)
            .Select(s => new { s.RoomTypeId, Arrival = s.ArrivalAt, Departure = s.DepartureAt })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        var zone = await businessDay.ZoneAsync(scope, cancellationToken);

        if (stay.Arrival.DateIn(zone) is not { } from || stay.Departure.DateIn(zone) is not { } to)
        {
            return new RoomsOfType([], 0);
        }

        return await FreeRoomsAsync(scope, stay.RoomTypeId, from, to, cancellationToken);
    }

    private static bool Covers(DateOnly from, DateOnly? to, DateOnly date)
        => date >= from && (to is null || date <= to);

    /// <summary>One stay's claim on a type, over its nights.</summary>
    /// <remarks>
    /// <b><c>RoomId</c> is carried for <see cref="FreeRoomsAsync"/> and ignored
    /// by <see cref="GetAsync"/>.</b> It is one field on one projection rather
    /// than a second record, because <see cref="HoldsOn"/> is the rule for
    /// *whether a stay is holding a room on a night* and two copies of that
    /// would answer differently the first time either was corrected — and the
    /// two questions here differ only in whether the answer is counted per type
    /// or attributed to a room.
    /// </remarks>
    private sealed record StayHold(
        Guid RoomTypeId,
        StayLifecycle State,
        DateTimeOffset? Arrival,
        DateTimeOffset? Departure,
        bool? ReservesInventory,
        Guid? RoomId)
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
        public bool HoldsOn(DateOnly date, TimeZoneInfo? zone)
        {
            if (!(ReservesInventory ?? Lifecycle.HoldsInventory(State)))
            {
                return false;
            }

            if (Arrival is not { } arrival || Departure is not { } departure || zone is null)
            {
                return false;
            }

            // The instant's own date was UTC's until 2026-09-19 (ADR 0174).
            var first = PropertyClock.Day(arrival, zone);
            var last = PropertyClock.Day(departure, zone);

            return date >= first && date < last;
        }
    }
}
