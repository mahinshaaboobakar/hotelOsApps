using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>The attendant's screens — my rooms, and one room at the door (frames 3, 3b).</summary>
/// <remarks>
/// Only what the supervisor accepted appears: a proposed room is not the
/// attendant's until then (S0). The DND re-check is read from the last attempt
/// and the property's spacing — no timer anywhere.
/// </remarks>
public sealed class WorkProjection(RoomCareDbContext db, IHouse house, PropertyClock clock, DayFacts facts)
{
    public const int PageSize = 24;

    public async Task<MyRoomsView> MyRoomsAsync(RequestScope scope, int page, CancellationToken cancellationToken)
    {
        var person = Actor.PersonOf(scope, "seeing your rooms");
        var snapshot = await LoadAsync(scope.PropertyId, house, clock, db, cancellationToken);
        var day = await facts.LoadAsync(snapshot, scope.PropertyId, cancellationToken);
        var mine = day.Tasks.Where(t => t.AssignedToUserId == person && t.RoomId != null)
            .OrderBy(t => t.IsOpen ? 0 : 1).ThenBy(t => t.PriorityRank).ThenBy(t => day.States.GetValueOrDefault(t.RoomId!.Value)?.NextSoldAt)
            .ToList();
        var rows = new List<MyRoomView>();
        foreach (var task in mine)
        {
            rows.Add(await RowAsync(snapshot, day, task, cancellationToken));
        }

        return new MyRoomsView(
            mine.Count,
            mine.Count(t => t.Outcome is TaskOutcome.Done or TaskOutcome.SupervisorCleaned),
            mine.Count(t => t.Status == RoomTaskStatus.InProgress),
            mine.Sum(t => t.MinutesExpected + t.ExtraMinutes),
            day.Now.Instant.ToString("o"),
            rows.Skip(Math.Max(0, page) * PageSize).Take(PageSize).ToList(),
            new Views.Paging(Math.Max(0, page), PageSize, rows.Count));
    }

    public async Task<DoorView> DoorAsync(RequestScope scope, Guid taskId, CancellationToken cancellationToken)
    {
        var person = Actor.PersonOf(scope, "working a room");
        var snapshot = await LoadAsync(scope.PropertyId, house, clock, db, cancellationToken);
        var day = await facts.LoadAsync(snapshot, scope.PropertyId, cancellationToken);
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.PropertyId == scope.PropertyId && t.AssignedToUserId == person, cancellationToken)
            ?? throw new NotFoundException("room_task", taskId);
        var sessions = await db.WorkSessions.Where(s => s.TaskId == taskId).ToListAsync(cancellationToken);
        var phases = await db.Phases.Where(p => p.TaskId == taskId).OrderBy(p => p.Sequence).ToListAsync(cancellationToken);
        var running = sessions.FirstOrDefault(s => s.IsRunning);

        return new DoorView(
            await RowAsync(snapshot, day, task, cancellationToken),
            At(sessions.Min(s => (DateTimeOffset?)s.StartedAt)),
            task.MinutesExpected,
            task.ExtraMinutes,
            sessions.Sum(s => s.Minutes) + (running is null ? 0 : (int)(day.Now.Instant - running.StartedAt).TotalMinutes),
            running is not null,
            task.InspectionRule,
            phases.Select(p => new PhaseView(p.Sequence, p.Phase, p.Status)).ToList(),
            PartialPart.All);
    }

    private async Task<MyRoomView> RowAsync(HouseSnapshot snapshot, DayFacts.Day day, RoomTask task, CancellationToken cancellationToken)
    {
        var room = task.RoomId!.Value;
        day.States.TryGetValue(room, out var state);
        var assignment = day.Assignments.FirstOrDefault(a => a.TaskId == task.Id);
        var line = BoardProjection.Row(snapshot, day, room);
        var last = day.LastAttempt(task);
        var declinedDay = state is { DaysWithoutService: > 0 } ? state.DaysWithoutService + 1 : (int?)null;
        await Task.CompletedTask;
        return new MyRoomView(
            task.Id.ToString(),
            task.Version,
            room.ToString(),
            snapshot.Number(room),
            task.Service,
            task.Reduction,
            At(state?.NextSoldAt),
            day.PriorityOf(task),
            At(task.EarliestAt),
            task.Service == Service.DepartureClean ? "STRIP" : task.LinenDue,
            declinedDay,
            RoomOutcome.Of(day, state, task, assignment, line.Marks.Blocked, line.Marks),
            last?.Found == AttemptFound.Dnd && task.IsOpen ? At(last.At.AddMinutes(day.Policy.DndRecheckMinutes)) : null);
    }
}
