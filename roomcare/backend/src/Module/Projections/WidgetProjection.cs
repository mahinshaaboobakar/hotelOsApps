using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>The five widgets — Rooms Ready · Arrivals Waiting · Attention · Attendants Now · Pending; one question each (page 56).</summary>
/// <remarks>
/// Every number is counted by <see cref="DayFacts"/>'s rules, the same the board
/// uses, so a widget and the board it opens can never disagree. An uncomputable
/// number is absent, never approximate: "on shift" is Workforce's count as it
/// last announced it, and null when Workforce never has.
/// </remarks>
public sealed class WidgetProjection(RoomCareDbContext db, IHouse house, PropertyClock clock, DayFacts facts)
{
    private const int Rows = 3;

    public async Task<RoomsReadyView> RoomsReadyAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var (snapshot, day) = await LoadDayAsync(scope, cancellationToken);
        var departures = day.Tasks.Where(t => t.Service == Service.DepartureClean && t.RoomId != null).GroupBy(t => t.RoomId!.Value).Select(g => g.Key).ToList();
        var ready = departures.Count(r => day.IsReady(r, day.TaskOf(r)));
        var running = departures.Count(r => day.IsRunning(day.TaskOf(r)) || day.TaskOf(r)?.Status == RoomTaskStatus.InProgress);
        return new RoomsReadyView(departures.Count, ready, running, departures.Count - ready - running, snapshot.Now.Instant.ToString("o"));
    }

    public async Task<ArrivalsWaitingView> ArrivalsAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var (snapshot, day) = await LoadDayAsync(scope, cancellationToken);
        var ends = WindowTimes.DayEnds(day.Date, day.Now);
        var waiting = day.States.Values
            .Where(s => s.NextSoldAt is { } sold && sold < ends && s.Condition == Condition.Dirty && snapshot.ById.ContainsKey(s.RoomId))
            .OrderBy(s => s.NextSoldAt)
            .ToList();
        return new ArrivalsWaitingView(
            waiting.Count,
            waiting.Take(Rows).Select(s => Room(snapshot, s.RoomId, State(day, s.RoomId), At(s.NextSoldAt), "warn")).ToList(),
            snapshot.Now.Instant.ToString("o"));
    }

    public async Task<AttentionView> AttentionAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var (snapshot, day) = await LoadDayAsync(scope, cancellationToken);
        var rows = day.Lane.Where(l => l.IsOpen)
            .Select(l => Room(snapshot, l.RoomId, l.Reason, l.OpenedAt.ToString("o"), "bad", DaysOf(day, l.RoomId, l.Reason)))
            .Concat(day.States.Values.Where(s => s.HasDisagreement)
                .Select(s => Room(snapshot, s.RoomId, SupervisionReason.Disagreement, At(s.DisagreementObservedAt), "warn", s.DisagreementObservedCondition)))
            .ToList();
        return new AttentionView(rows.Count, rows.Take(Rows).ToList(), snapshot.Now.Instant.ToString("o"));
    }

    public async Task<AttendantsNowView> AttendantsAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var (snapshot, day) = await LoadDayAsync(scope, cancellationToken);
        var running = day.Sessions.Where(s => s.IsRunning).OrderBy(s => s.StartedAt).ToList();
        var presence = await db.Presence.FirstOrDefaultAsync(
            p => p.PropertyId == scope.PropertyId && p.DepartmentCode == day.Policy.DepartmentCode, cancellationToken);
        return new AttendantsNowView(
            presence?.OnNow,
            running.Count,
            running.Take(Rows).Select(s => new WidgetPersonView(s.UserId.ToString(), day.Name(s.UserId) ?? "—",
                snapshot.Number(day.Tasks.FirstOrDefault(t => t.Id == s.TaskId)?.RoomId), s.StartedAt.ToString("o"))).ToList(),
            snapshot.Now.Instant.ToString("o"));
    }

    public async Task<PendingView> PendingAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var (snapshot, day) = await LoadDayAsync(scope, cancellationToken);
        var pending = day.Tasks.Where(t => t.Status == RoomTaskStatus.PendingPolicy && t.RoomId != null).OrderBy(t => t.PriorityRank).ToList();
        return new PendingView(
            pending.Count,
            pending.Take(Rows + 1).Select(t => t.DecisionInputs.Reason == DayDecision.UnsoldMayWait
                ? Room(snapshot, t.RoomId!.Value, PendingWord.UnsoldDeparture, null, "warn")
                : Room(snapshot, t.RoomId!.Value, PendingWord.SeeRoom, null, "warn", t.DecisionInputs.Reason)).ToList(),
            snapshot.Now.Instant.ToString("o"));
    }

    private async Task<(HouseSnapshot, DayFacts.Day)> LoadDayAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var snapshot = await LoadAsync(scope.PropertyId, house, clock, db, cancellationToken);
        return (snapshot, await facts.LoadAsync(snapshot, scope.PropertyId, cancellationToken));
    }

    private static string State(DayFacts.Day day, Guid roomId)
    {
        var task = day.TaskOf(roomId);
        if (day.IsRunning(task) || task?.Status == RoomTaskStatus.InProgress)
        {
            return "IN_PROGRESS";
        }

        var assigned = task is not null && day.Assignments.Any(a => a.TaskId == task.Id && a.Mode != AssignmentMode.Proposed);
        return assigned ? "NOT_STARTED" : "NOBODY_AVAILABLE";
    }

    /// <summary>The day this is without service — the supervision lane's own count (today included).</summary>
    private static string? DaysOf(DayFacts.Day day, Guid roomId, string reason) =>
        reason == SupervisionReason.DaysWithoutService && day.States.TryGetValue(roomId, out var state)
            ? (state.DaysWithoutService + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null;

    private static WidgetRoomView Room(HouseSnapshot snapshot, Guid roomId, string what, string? at, string tone, string? detail = null) =>
        new(roomId.ToString(), snapshot.Number(roomId), what, at, tone, detail);

    /// <summary>Why a room waits on a click, as the Pending widget words it.</summary>
    private static class PendingWord
    {
        public const string UnsoldDeparture = "UNSOLD_DEPARTURE";
        public const string SeeRoom = "SEE_ROOM";
    }
}
