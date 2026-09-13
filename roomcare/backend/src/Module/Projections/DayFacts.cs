using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>Everything the board, the widgets and a room's line read about today, loaded in a handful of queries.</summary>
/// <remarks>
/// One loader so the map, the wall and the five widgets cannot count a room two
/// ways: a room is ready, dirty or in progress by one rule, written once here.
/// </remarks>
public sealed class DayFacts(RoomCareDbContext db, IHouse house, StandardReader standard)
{
    /// <summary>How long without a PMS fact before the strip says so — during a window, where facts are expected.</summary>
    public static readonly TimeSpan SilenceAfter = TimeSpan.FromMinutes(30);

    public async Task<Day> LoadAsync(HouseSnapshot snapshot, Guid propertyId, CancellationToken cancellationToken)
    {
        var now = snapshot.Now;
        var windows = await standard.WindowsAsync(propertyId, cancellationToken);
        var open = WindowTimes.Open(windows, now);
        var day = open?.Day ?? now.Day;
        var states = await db.RoomStates.Where(r => r.PropertyId == propertyId).ToDictionaryAsync(r => r.RoomId, cancellationToken);
        var tasks = await db.Tasks.Where(t => t.PropertyId == propertyId && t.OperatingDay == day).ToListAsync(cancellationToken);
        var ids = tasks.Select(t => t.Id).ToList();
        var attempts = await db.Attempts.Where(a => ids.Contains(a.TaskId)).ToListAsync(cancellationToken);
        var assignments = await db.Assignments.Where(a => ids.Contains(a.TaskId) && a.EndedAt == null).ToListAsync(cancellationToken);
        var sessions = await db.WorkSessions.Where(s => ids.Contains(s.TaskId)).ToListAsync(cancellationToken);
        var inspections = await db.Phases.Where(p => ids.Contains(p.TaskId) && p.Phase == Phase.Inspect).ToListAsync(cancellationToken);
        var lane = await db.Supervision.Where(s => s.PropertyId == propertyId && s.OperatingDay == day).ToListAsync(cancellationToken);
        var blocked = await db.DeepCleans
            .Where(d => d.PropertyId == propertyId && (d.Status == DeepCleanStatus.Blocked || d.Status == DeepCleanStatus.InProgress))
            .ToListAsync(cancellationToken);
        var lastRun = await db.PrepareRuns.Where(r => r.PropertyId == propertyId && r.OperatingDay == day)
            .OrderByDescending(r => r.At).FirstOrDefaultAsync(cancellationToken);
        var lastPms = await db.Observations.Where(o => o.PropertyId == propertyId && o.Source == ObservationSource.Pms)
            .MaxAsync(o => (DateTimeOffset?)o.RecordedAt, cancellationToken);
        var changedSince = lastRun is null
            ? []
            : await db.Observations.Where(o => o.PropertyId == propertyId && o.RecordedAt > lastRun.At).Select(o => o.RoomId).Distinct().ToListAsync(cancellationToken);
        var people = assignments.Select(a => a.UserId).Concat(sessions.Select(s => s.UserId)).Distinct().ToList();
        var names = await house.NamesAsync(people, cancellationToken);
        var policy = await standard.PolicyAsync(propertyId, cancellationToken);

        return new Day(now, day, open?.Window, states, tasks, attempts, assignments, sessions, inspections, lane, blocked, lastRun,
            lastPms, changedSince.ToHashSet(), names, policy);
    }

    /// <summary>Today, loaded.</summary>
    public sealed record Day(
        Application.Days.PropertyNow Now,
        DateOnly Date,
        ServiceWindow? Window,
        IReadOnlyDictionary<Guid, RoomState> States,
        IReadOnlyList<RoomTask> Tasks,
        IReadOnlyList<TaskAttempt> Attempts,
        IReadOnlyList<TaskAssignment> Assignments,
        IReadOnlyList<TaskWorkSession> Sessions,
        IReadOnlyList<TaskPhase> Inspections,
        IReadOnlyList<RoomSupervision> Lane,
        IReadOnlyList<DeepClean> Blocked,
        PrepareRun? LastRun,
        DateTimeOffset? LastPmsFact,
        IReadOnlySet<Guid> ChangedSinceRun,
        IReadOnlyDictionary<Guid, string> Names,
        PropertyPolicy Policy)
    {
        /// <summary>The room's task that matters now: the open window's, else the day's latest.</summary>
        public RoomTask? TaskOf(Guid roomId) =>
            Tasks.Where(t => t.RoomId == roomId)
                .OrderByDescending(t => Window is { } w && t.Window == w.Window)
                .ThenByDescending(t => t.CreatedAt)
                .FirstOrDefault();

        public string? Name(Guid? user) => user is { } id && Names.TryGetValue(id, out var name) ? name : null;

        public bool IsBlocked(Guid roomId) => Blocked.Any(b => b.RoomId == roomId);

        public bool InSupervision(Guid roomId) =>
            Lane.Any(s => s.RoomId == roomId && s.IsOpen) || (States.TryGetValue(roomId, out var r) && r.HasDisagreement);

        /// <summary>Ready means clean or inspected with no service still open on it.</summary>
        public bool IsReady(Guid roomId, RoomTask? task) =>
            States.TryGetValue(roomId, out var state) && state.Condition != Condition.Dirty && task?.IsOpen != true;

        public bool IsRunning(RoomTask? task) => task is not null && Sessions.Any(s => s.TaskId == task.Id && s.IsRunning);

        public TaskAttempt? LastAttempt(RoomTask? task) =>
            task is null ? null : Attempts.Where(a => a.TaskId == task.Id).OrderByDescending(a => a.At).FirstOrDefault();

        /// <summary>The priority as the ladder's position, 1 first.</summary>
        public int PriorityOf(RoomTask task) => DayDecision.Rank(task.Priority, Policy) + 1;

        /// <summary>When the PMS went quiet, if it has during a window at a property that expects it.</summary>
        public DateTimeOffset? SilentSince =>
            Policy.StaySource == StaySource.Pms && Window is not null && (LastPmsFact is null || Now.Instant - LastPmsFact > SilenceAfter)
                ? LastPmsFact ?? LastRun?.At
                : null;
    }
}
