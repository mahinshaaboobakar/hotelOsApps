using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>The board — the map and the wall, one data set grouped by zone, no pages (frames 1a, 1b; redline 4).</summary>
/// <remarks>
/// A whole-house view, not a list to page through — the stated departure from
/// page 64 §6 the owner ruled on 2026-09-13. Every active room is on it, zoned
/// or not; filters in the screen dim, they never remove.
/// </remarks>
public sealed class BoardProjection(RoomCareDbContext db, IHouse house, PropertyClock clock, DayFacts facts)
{
    public async Task<BoardPageView> BoardAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var snapshot = await LoadAsync(scope.PropertyId, house, clock, db, cancellationToken);
        var day = await facts.LoadAsync(snapshot, scope.PropertyId, cancellationToken);

        var rooms = snapshot.Rooms.Select(r => Row(snapshot, day, r.Id)).ToList();
        var groups = snapshot.Rooms
            .Select((room, index) => (room, row: rooms[index], zone: snapshot.ZoneOf(room.Id)))
            .GroupBy(x => x.zone)
            .OrderBy(g => g.Key.Id is null ? 1 : 0).ThenBy(g => g.Key.Name)
            .Select(g => new ZoneGroupView(g.Key.Id?.ToString(), g.Key.Name, Counts(g.Select(x => x.row).ToList()), g.Select(x => x.row).ToList()))
            .ToList();

        var strip = new StripView(
            rooms.Count,
            rooms.Count(r => r.Condition == Condition.Dirty && !r.Marks.Blocked),
            rooms.Count(r => r.Marks.InProgress),
            rooms.Count(r => r.Condition != Condition.Dirty && !r.Marks.InProgress && !r.Marks.Blocked),
            rooms.Count(r => r.Marks.Pending),
            rooms.Count(r => r.Marks.Blocked),
            rooms.Count(r => r.Marks.Supervision),
            At(day.LastPmsFact),
            At(day.SilentSince),
            snapshot.Now.Instant.ToString("o"),
            day.Window?.Window);

        return new BoardPageView(strip, day.Policy.BoardDefaultView, groups);
    }

    /// <summary>One room's line — used by the board, and by the room page's header.</summary>
    public static BoardRoomView Row(HouseSnapshot snapshot, DayFacts.Day day, Guid roomId)
    {
        day.States.TryGetValue(roomId, out var state);
        var task = day.TaskOf(roomId);
        var assignment = task is null ? null : day.Assignments.FirstOrDefault(a => a.TaskId == task.Id);
        var attendant = assignment is { Mode: not AssignmentMode.Proposed } ? assignment.UserId : (Guid?)null;
        var blocked = day.IsBlocked(roomId);
        var supervision = day.InSupervision(roomId);
        var marks = new MarksView(
            SoldTonight: state?.NextSoldAt is { } sold && sold < Application.Day.WindowTimes.DayEnds(day.Date, day.Now),
            Dnd: day.LastAttempt(task)?.Found == AttemptFound.Dnd && task?.IsOpen == true,
            Disagreement: state?.HasDisagreement == true,
            Blocked: blocked,
            Supervision: supervision,
            Pending: task?.Status == RoomTaskStatus.PendingPolicy,
            InProgress: day.IsRunning(task) || task?.Status == RoomTaskStatus.InProgress,
            NewSince: task is null && day.ChangedSinceRun.Contains(roomId),
            Manual: state?.ConditionSource == ConditionSource.Manual);

        return new BoardRoomView(
            roomId.ToString(),
            snapshot.Number(roomId),
            state?.Condition ?? Condition.Dirty,
            state?.ConditionSource ?? ConditionSource.System,
            day.Name(state?.ConditionSetById),
            (state?.ConditionSetAt ?? day.Now.Instant).ToString("o"),
            state?.Occupancy ?? Occupancy.Unknown,
            state is { Occupancy: Occupancy.Vacant, Condition: not Condition.Dirty } ? day.Date.DayNumber - day.Now.Settings.DayOf(state.ConditionSetAt).DayNumber : null,
            At(state?.NextSoldAt),
            task?.Id.ToString(),
            task?.Version,
            task?.Service,
            task?.Reduction,
            At(task?.EarliestAt),
            task is null ? null : day.PriorityOf(task),
            attendant?.ToString(),
            day.Name(attendant),
            RoomOutcome.Of(day, state, task, assignment, blocked, marks),
            task?.LinenDue is LinenDue.Due or LinenDue.Must ? task.LinenDue : null,
            marks,
            state?.Version ?? 0);
    }

    private static ZoneCountsView Counts(IReadOnlyList<BoardRoomView> rows) => new(
        rows.Count,
        rows.Count(r => r.Condition == Condition.Dirty && !r.Marks.Blocked),
        rows.Count(r => r.Marks.InProgress),
        rows.Count(r => r.Condition != Condition.Dirty && !r.Marks.InProgress && !r.Marks.Blocked),
        rows.Count(r => r.Marks.Dnd),
        rows.Count(r => r.Marks.Blocked),
        rows.Count(r => r.Marks.Supervision),
        rows.Count(r => r.Marks.Pending));
}
