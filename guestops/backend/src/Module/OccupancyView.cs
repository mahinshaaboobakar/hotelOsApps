using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The occupancy widget — how full the hotel is, right now.
/// </summary>
/// <remarks>
/// <b>Every number here is countable, so none of them is null.</b> The widget
/// contract types them nullable because a widget must be able to say *"not
/// measured"* rather than draw a zero — and that is the correct shape even
/// where this projection can always answer, because the day one of these
/// becomes uncomputable the widget already knows how to show it.
/// </remarks>
public sealed class OccupancyView(
    GuestOpsDbContext db,
    IRoomInventory inventory)
{
    /// <summary>In house, occupied, free, and tonight's arrivals.</summary>
    public async Task<object?> AnswerAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var inHouse = await db.Stays
            .Where(s => s.PropertyId == scope.PropertyId && s.Lifecycle == StayLifecycle.InHouse)
            .ToListAsync(cancellationToken);

        // A room is occupied when an assignment is open on it — the assignment
        // row rather than the stay's projection, because a stay may hold a room
        // it has not arrived into and the two answer different questions.
        var occupied = await db.Assignments
            .Where(a => a.ReleasedAt == null)
            .Join(db.Stays.Where(s => s.PropertyId == scope.PropertyId),
                a => a.StayId, s => s.Id, (a, _) => a.RoomId)
            .Distinct()
            .CountAsync(cancellationToken);

        var rooms = await inventory.CountByTypeAsync(scope.PropertyId, [], cancellationToken);
        var total = rooms.Values.Sum();

        var sold = inHouse
            .GroupBy(s => s.RoomTypeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var names = await db.Set<MasterDataRoomTypeName>()
            .Where(t => rooms.Keys.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        return new
        {
            inHouse = inHouse.Count,
            occupied,
            free = total - occupied,

            // Arrivals still expected on the current business day. Counted from
            // the stays themselves rather than from a stat elsewhere, so the
            // widget cannot disagree with the screen it taps through to.
            tonight = await db.Stays.CountAsync(
                s => s.PropertyId == scope.PropertyId
                    && s.Lifecycle == StayLifecycle.Booked,
                cancellationToken),

            types = rooms
                .Select(entry => new
                {
                    name = names.TryGetValue(entry.Key, out var name) ? name : null,
                    rooms = entry.Value,
                    sold = sold.TryGetValue(entry.Key, out var count) ? count : 0,
                })
                .Where(row => row.name is not null)
                .OrderBy(row => row.name)
                .ToArray(),
        };
    }
}
