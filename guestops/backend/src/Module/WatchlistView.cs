using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The watchlist widget — what is late, and what has no room.
/// </summary>
/// <remarks>
/// Both lists are ordinary conditions of running a hotel rather than defects:
/// a guest who has not left by the check-out hour is the commonest thing at a
/// front desk, and an arrival without a room is the ordinary case the whole
/// design is built around (`GUEST-Q2`).
/// </remarks>
public sealed class WatchlistView(
    GuestOpsDbContext db,
    IBusinessDay businessDay)
{
    /// <summary>The counters, and the first few of each list.</summary>
    public async Task<object?> AnswerAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var date = await businessDay.CurrentAsync(scope, cancellationToken);
        var bounds = date is { } day
            ? await businessDay.BoundsAsync(scope, day, cancellationToken)
            : null;

        var mine = db.Stays.Where(s => s.PropertyId == scope.PropertyId);

        // Overdue means still in house with a departure already past. Without
        // the day's bounds there is no honest "past" to compare against — the
        // same refusal `StayListService` makes for the departures list, for the
        // same reason: a guessed window is wrong by a day near midnight.
        var overdue = bounds is { } window
            ? await mine
                .Where(s => s.Lifecycle == StayLifecycle.InHouse
                    && s.DepartureAt.At != null
                    && s.DepartureAt.At < window.End)
                .OrderBy(s => s.DepartureAt.At)
                .Take(5)
                .ToListAsync(cancellationToken)
            : [];

        var unassigned = await mine
            .Where(s => s.CurrentRoomId == null
                && s.Lifecycle != StayLifecycle.Cancelled
                && s.Lifecycle != StayLifecycle.NoShow
                && s.Lifecycle != StayLifecycle.Departed)
            .OrderBy(s => s.ArrivalAt.At)
            .Take(5)
            .ToListAsync(cancellationToken);

        var names = await NamesAsync([.. overdue, .. unassigned], cancellationToken);
        var rooms = await RoomsAsync(scope, overdue, cancellationToken);
        var types = await TypesAsync(unassigned, cancellationToken);

        return new
        {
            // Null rather than zero when the boundary is unknown: "nothing is
            // overdue" and "we cannot tell what is overdue" are different
            // answers, and the widget contract types this nullable so the
            // second one can be shown as itself.
            overdueOut = bounds is null ? (int?)null : overdue.Count,

            noRoom = unassigned.Count,

            notCheckedOut = await mine.CountAsync(
                s => s.Lifecycle == StayLifecycle.Booked
                    && s.BusinessDate != null && s.BusinessDate < date,
                cancellationToken),

            overdue = overdue.Select(stay => new
            {
                room = stay.CurrentRoomId is { } id && rooms.TryGetValue(id, out var number)
                    ? number
                    : null,
                guest = Named(names, stay.Id),
                due = stay.DepartureAt.At?.ToString("HH:mm"),
                late = (string?)null,
                stay = stay.Id.ToString(),
            }).ToArray(),

            unassigned = unassigned.Select(stay => new
            {
                guest = Named(names, stay.Id),
                type = types.TryGetValue(stay.RoomTypeId, out var type) ? type : null,
                at = stay.ArrivalAt.At?.ToString("HH:mm"),
                stay = stay.Id.ToString(),
            }).ToArray(),
        };
    }

    /// <summary>The design's own words for a party nobody has named yet.</summary>
    private static string Named(IReadOnlyDictionary<Guid, string> names, Guid stayId)
        => names.TryGetValue(stayId, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : "Not yet named";

    private async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        IReadOnlyList<RoomStay> stays, CancellationToken cancellationToken)
    {
        var ids = stays.Select(s => s.Id).Distinct().ToArray();

        var party = await db.Party
            .Where(member => ids.Contains(member.StayId))
            .Join(db.Guests, member => member.GuestId, guest => guest.Id,
                (member, guest) => new { member.StayId, guest.NameAsGiven })
            .ToListAsync(cancellationToken);

        return party
            .GroupBy(member => member.StayId)
            .ToDictionary(group => group.Key, group => group.First().NameAsGiven);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> RoomsAsync(
        RequestScope scope, IReadOnlyList<RoomStay> stays, CancellationToken cancellationToken)
    {
        var ids = stays.Where(s => s.CurrentRoomId is not null)
            .Select(s => s.CurrentRoomId!.Value).Distinct().ToArray();

        return await db.Set<MasterDataRoomName>()
            .Where(r => r.PropertyId == scope.PropertyId && ids.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.RoomNumber, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> TypesAsync(
        IReadOnlyList<RoomStay> stays, CancellationToken cancellationToken)
    {
        var ids = stays.Select(s => s.RoomTypeId).Distinct().ToArray();

        return await db.Set<MasterDataRoomTypeName>()
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);
    }
}
