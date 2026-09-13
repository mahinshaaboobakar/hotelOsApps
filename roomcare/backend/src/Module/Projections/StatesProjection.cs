using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>The Room states tab's data set — every room's four facts with the version an edit is based on (frames 4c–4e).</summary>
/// <remarks>
/// The whole house, grouped by zone, no pages — like the wall. A room blocked
/// for a deep clean is out of order by its owner's fact and is listed as not
/// editable here.
/// </remarks>
public sealed class StatesProjection(RoomCareDbContext db, IHouse house, PropertyClock clock, DayFacts facts)
{
    /// <summary>The stay words the tab edits in, from the stay statuses the room holds.</summary>
    public static string StayWord(IReadOnlyList<string> statuses) =>
        statuses.Contains(StayStatus.CheckedOut) ? "DEPARTED"
        : statuses.Contains(StayStatus.CheckedIn) || statuses.Contains(StayStatus.DueOut) ? "IN_HOUSE"
        : "NONE";

    public async Task<RoomStatesPageView> StatesAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var snapshot = await LoadAsync(scope.PropertyId, house, clock, db, cancellationToken);
        var day = await facts.LoadAsync(snapshot, scope.PropertyId, cancellationToken);
        var dayEnds = WindowTimes.DayEnds(day.Date, day.Now);

        var rows = snapshot.Rooms.Select(room =>
        {
            day.States.TryGetValue(room.Id, out var state);
            return (room, row: new StateRowView(
                room.Id.ToString(),
                room.Number,
                state?.Condition ?? Condition.Dirty,
                state?.Occupancy ?? Occupancy.Unknown,
                At(state?.NextSoldAt),
                StayWord(state?.StayStatuses ?? []),
                state?.ConditionSource ?? ConditionSource.System,
                day.Name(state?.ConditionSetById),
                (state?.LastObservedAt is { } seen && seen > (state?.ConditionSetAt ?? seen) ? seen : state?.ConditionSetAt ?? day.Now.Instant).ToString("o"),
                state?.Version ?? 0,
                day.IsBlocked(room.Id)));
        }).ToList();

        var zones = rows.GroupBy(x => snapshot.ZoneOf(x.room.Id))
            .OrderBy(g => g.Key.Id is null ? 1 : 0).ThenBy(g => g.Key.Name)
            .Select(g => new StatesZoneView(g.Key.Id?.ToString(), g.Key.Name, g.Select(x => x.row).ToList()))
            .ToList();

        return new RoomStatesPageView(
            rows.Count,
            rows.Count(x => x.row.Condition == Condition.Dirty),
            rows.Count(x => x.row.Occupancy == Occupancy.Occupied),
            rows.Count(x => day.States.TryGetValue(x.room.Id, out var s) && s.NextSoldAt is { } sold && sold < dayEnds),
            At(day.SilentSince),
            day.Policy.StatesDefaultView,
            zones);
    }
}
