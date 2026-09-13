using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Tasks;

/// <summary>A task's transitions, its history row and its announcement — one call, never three that could drift.</summary>
public sealed class TaskWriter(RoomCareDbContext db, IEventAppender events, TimeProvider clock)
{
    /// <summary>The instant every write in this call is stamped with.</summary>
    public DateTimeOffset Now => clock.GetUtcNow();

    /// <summary>A task at this property, checked against the version the caller saw.</summary>
    public async Task<RoomTask> RequireAsync(
        RequestScope scope, Guid taskId, long? expectedVersion, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("room_task", taskId);

        if (expectedVersion is { } expected && task.Version != expected)
        {
            throw new ConcurrencyException("room_task", taskId, expected);
        }

        return task;
    }

    /// <summary>Move the task, write the history row, and announce the event named.</summary>
    public void Move(RequestScope scope, RoomTask task, string toStatus, string eventType, TaskNote note)
    {
        var from = task.Status;
        task.Status = toStatus;
        Record(scope, task, HistoryKind.Transition, note.Reason, from, toStatus);
        Announce(scope, task, eventType, note);
    }

    /// <summary>A history row that is not a transition — a reduction, a decision, extra time.</summary>
    public void Record(RequestScope scope, RoomTask task, string kind, string? reason, string? from = null, string? to = null)
    {
        var actor = Actor.Of(scope);
        task.Touch(Now);
        db.History.Add(new TaskHistory
        {
            Id = Guid.CreateVersion7(),
            TaskId = task.Id,
            PropertyId = task.PropertyId,
            At = Now,
            ByKind = actor.Kind,
            ById = actor.Id,
            Via = actor.Via,
            Kind = kind,
            FromStatus = from,
            ToStatus = to,
            Reason = reason,
        });
    }

    /// <summary>Announce a task event carrying the task as it now stands.</summary>
    /// <remarks>
    /// Every announcement moves the version first: <c>event_store.events</c> is
    /// unique on <c>(aggregate_type, aggregate_id, entity_version)</c>, so two
    /// events on one task in one commit must carry two numbers — and the number
    /// is the row's, never a second counter (§7.10).
    /// </remarks>
    public void Announce(RequestScope scope, RoomTask task, string eventType, TaskNote note)
    {
        var actor = Actor.Of(scope);
        task.Touch(Now);
        events.Append(scope, eventType, EventTypes.TaskAggregate, task.Id, task.Version, new TaskAnnouncement
        {
            TaskId = task.Id,
            PropertyId = task.PropertyId,
            DepartmentId = note.DepartmentId,
            RoomId = task.RoomId,
            LocationId = task.LocationId,
            OperatingDay = task.OperatingDay.ToString("yyyy-MM-dd"),
            Window = task.Window,
            Service = task.Service,
            Priority = task.Priority,
            Status = task.Status,
            Outcome = task.Outcome,
            Found = note.Found,
            PartialDone = note.PartialDone,
            UserId = note.UserId ?? task.AssignedToUserId,
            What = note.What,
            Reason = note.Reason,
            ByKind = actor.Kind,
            ById = actor.Id,
            Via = actor.Via,
            OccurredAt = Now,
        });
    }

    /// <summary>Append another kind of event on the task's aggregate, moving its version first.</summary>
    public void AppendOn<TPayload>(RequestScope scope, RoomTask task, string eventType, TPayload payload)
    {
        task.Touch(Now);
        events.Append(scope, eventType, EventTypes.TaskAggregate, task.Id, task.Version, payload);
    }

    /// <summary>The attendant's running stretch on a task, if one is running.</summary>
    public Task<TaskWorkSession?> RunningAsync(Guid taskId, CancellationToken cancellationToken) =>
        db.WorkSessions.FirstOrDefaultAsync(s => s.TaskId == taskId && s.EndedAt == null, cancellationToken);

    /// <summary>The task's current assignment row, proposed or accepted.</summary>
    public Task<TaskAssignment?> CurrentAssignmentAsync(Guid taskId, CancellationToken cancellationToken) =>
        db.Assignments.FirstOrDefaultAsync(a => a.TaskId == taskId && a.EndedAt == null, cancellationToken);
}

/// <summary>What an announcement and its history row say beyond the task itself.</summary>
public sealed record TaskNote
{
    public static readonly TaskNote Empty = new();

    public string? Reason { get; init; }

    public string? What { get; init; }

    public string? Found { get; init; }

    public IReadOnlyList<string>? PartialDone { get; init; }

    public Guid? UserId { get; init; }

    public Guid? DepartmentId { get; init; }
}
