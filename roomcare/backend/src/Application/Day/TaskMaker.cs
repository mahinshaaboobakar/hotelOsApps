using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;

namespace HotelOS.RoomCare.Application.Day;

/// <summary>Turns a decision into a task with its phases, or brings an unstarted task up to date.</summary>
public sealed class TaskMaker(RoomCareDbContext db, IHouse house, StandardReader standard, TaskWriter writer)
{
    /// <summary>What a run did to one room's task.</summary>
    public enum Change
    {
        Created,
        Updated,
        Unchanged,
    }

    /// <summary>A new task for a room — the standard copied onto it, so a later edit never rewrites this day.</summary>
    public async Task<Change> CreateAsync(
        RequestScope scope,
        HouseRoom room,
        RoomState? state,
        Decided decided,
        DecisionInputs inputs,
        DateOnly day,
        string window,
        PrepareRun run,
        PropertyPolicy policy,
        CancellationToken cancellationToken)
    {
        var rule = await standard.StandardAsync(scope.PropertyId, room.RoomTypeId, decided.Service, cancellationToken);
        var now = writer.Now;
        var task = new RoomTask
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            LocationId = room.Id,
            RoomId = room.Id,
            OperatingDay = day,
            Window = window,
            Service = decided.Service,
            Priority = decided.Band,
            PriorityRank = Rank(decided, room, policy),
            LinenDue = DayDecision.Linen(decided.Service, state?.LinenLastChangedOn, day, policy),
            MinutesExpected = rule.Minutes,
            Credits = rule.Credits,
            InspectionRule = rule.InspectionRule,
            ChecklistRef = rule.ChecklistRef,
            Status = decided.Pending ? RoomTaskStatus.PendingPolicy : RoomTaskStatus.Planned,
            DecidedBy = run.ByKind == ActorKind.System ? DecidedBy.Automatic : DecidedBy.Prepare,
            DecisionRunId = run.Id,
            DecisionInputs = inputs,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 0,
        };
        db.Tasks.Add(task);
        AddPhases(task, rule.Phases);

        var department = await house.DepartmentIdAsync(scope.PropertyId, policy.DepartmentCode, cancellationToken);
        writer.Record(scope, task, HistoryKind.Transition, decided.Reason, null, task.Status);
        writer.Announce(scope, task, EventTypes.TaskCreated, new TaskNote { Reason = decided.Reason, DepartmentId = department });
        return Change.Created;
    }

    /// <summary>A task for an area's routine at a scheduled time (S3).</summary>
    public RoomTask CreateArea(RequestScope scope, AreaSchedule schedule, DateOnly day, string window, DateTimeOffset dueAt, Guid? department)
    {
        var now = writer.Now;
        var task = new RoomTask
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            LocationId = schedule.LocationId,
            OperatingDay = day,
            Window = window,
            Service = Service.AreaClean,
            Priority = PriorityBand.Daily,
            DueAt = dueAt,
            MinutesExpected = schedule.Minutes,
            DecidedBy = DecidedBy.System,
            DecisionInputs = new DecisionInputs { Window = window, Reason = "the area's schedule" },
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Tasks.Add(task);
        AddPhases(task, [Phase.Clean, Phase.Done]);
        writer.Record(scope, task, HistoryKind.Transition, "the area's schedule", null, task.Status);
        writer.Announce(scope, task, EventTypes.TaskCreated, new TaskNote { Reason = "the area's schedule", DepartmentId = department });
        return task;
    }

    /// <summary>Bring an unstarted task's facts up to date; a started or ended task is left as it is.</summary>
    public Change Reconcile(
        RequestScope scope, RoomTask task, RoomState? state, Decided decided, DecisionInputs inputs, PrepareRun run, PropertyPolicy policy)
    {
        if (!RoomTaskStatus.Unstarted.Contains(task.Status))
        {
            return Change.Unchanged;
        }

        var linen = DayDecision.Linen(task.Service, state?.LinenLastChangedOn, task.OperatingDay, policy);
        var band = decided.Band;
        var pending = decided.Pending && task.Status == RoomTaskStatus.PendingPolicy;
        var status = task.Status == RoomTaskStatus.PendingPolicy && !decided.Pending ? RoomTaskStatus.Planned : task.Status;

        if (task.Priority == band && task.LinenDue == linen && task.Status == status && task.Service == decided.Service)
        {
            return Change.Unchanged;
        }

        var from = task.Status;
        task.Priority = band;
        task.PriorityRank = (DayDecision.Rank(band, policy) * 1000) + (task.PriorityRank % 1000);
        task.LinenDue = linen;
        task.Status = pending ? RoomTaskStatus.PendingPolicy : status;
        if (task.Status is RoomTaskStatus.PendingPolicy or RoomTaskStatus.Planned)
        {
            task.Service = decided.Service;
        }

        task.DecisionRunId = run.Id;
        task.DecisionInputs = inputs;
        writer.Record(scope, task, HistoryKind.Reprioritised, $"reconciled — {decided.Reason}", from, task.Status);
        return Change.Updated;
    }

    private void AddPhases(RoomTask task, IReadOnlyList<string> phases)
    {
        var sequence = 0;
        foreach (var phase in phases.Where(p => p != Phase.Inspect))
        {
            db.Phases.Add(new TaskPhase
            {
                Id = Guid.CreateVersion7(),
                TaskId = task.Id,
                PropertyId = task.PropertyId,
                Phase = phase,
                Sequence = ++sequence,
            });
        }

        if (task.InspectionRule != InspectionRule.None)
        {
            db.Phases.Add(new TaskPhase
            {
                Id = Guid.CreateVersion7(), TaskId = task.Id, PropertyId = task.PropertyId, Phase = Phase.Inspect, Sequence = ++sequence,
            });
        }
    }

    /// <summary>The band's place on the ladder, then the house's own room order; sold-tonight rooms sort by arrival when read.</summary>
    private static int Rank(Decided decided, HouseRoom room, PropertyPolicy policy) =>
        (DayDecision.Rank(decided.Band, policy) * 1000) + Math.Clamp(room.SortOrder, 0, 998);
}
