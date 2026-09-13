using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Assignment;

/// <summary>The supervisor's hand on the day — accept the proposal, assign, reassign, take a room off (roomcare.assign).</summary>
/// <remarks>
/// Assignment is a row insert under the task's version, and at most one
/// current row per task is a unique index: two supervisors assigning one room
/// in the same second produce one committed row and one refusal (§7.8). There
/// is no lock to get wrong.
/// </remarks>
public sealed class AssignmentService(RoomCareDbContext db, Gate gate, TaskWriter writer)
{
    /// <summary>Accept every proposed row of a window — the supervisor's one click.</summary>
    public async Task<int> AcceptAllAsync(RequestScope scope, DateOnly day, string window, CancellationToken cancellationToken)
    {
        var proposed = await db.Assignments
            .Where(a => a.PropertyId == scope.PropertyId && a.EndedAt == null && a.Mode == AssignmentMode.Proposed)
            .Join(db.Tasks.Where(t => t.OperatingDay == day && t.Window == window), a => a.TaskId, t => t.Id, (a, t) => new { a, t })
            .ToListAsync(cancellationToken);

        foreach (var pair in proposed)
        {
            await gate.TaskAsync(scope, Permissions.Assign, pair.t.Id, cancellationToken);
            Accept(scope, pair.t, pair.a);
        }

        await db.SaveChangesAsync(cancellationToken);
        return proposed.Count;
    }

    /// <summary>Assign a task to a person — a first assignment, a reassignment, or accepting one proposal as it stands.</summary>
    public Task<RoomTask> AssignAsync(
        RequestScope scope, Guid taskId, long expectedVersion, Guid userId, CancellationToken cancellationToken) =>
        db.AtomicallyAsync(() => AssignInsideAsync(scope, taskId, expectedVersion, userId, cancellationToken), cancellationToken);

    private async Task<RoomTask> AssignInsideAsync(
        RequestScope scope, Guid taskId, long expectedVersion, Guid userId, CancellationToken cancellationToken)
    {
        var task = await writer.RequireAsync(scope, taskId, expectedVersion, cancellationToken);
        await gate.TaskAsync(scope, Permissions.Assign, taskId, cancellationToken);
        if (!task.IsOpen)
        {
            throw new InvalidRequestException("this room's service has ended and cannot be assigned");
        }

        var current = await writer.CurrentAssignmentAsync(taskId, cancellationToken);
        if (current is { Mode: AssignmentMode.Proposed } && current.UserId == userId)
        {
            Accept(scope, task, current);
            await db.SaveChangesAsync(cancellationToken);
            return task;
        }

        if (current is not null)
        {
            await EndAsync(task, current, AssignmentEnd.Reassigned, cancellationToken);
        }

        var actor = Actor.Of(scope);
        db.Assignments.Add(new TaskAssignment
        {
            Id = Guid.CreateVersion7(),
            TaskId = task.Id,
            PropertyId = task.PropertyId,
            UserId = userId,
            AssignedByKind = actor.Kind,
            AssignedById = actor.Id,
            Via = actor.Via,
            AssignedAt = writer.Now,
            Mode = AssignmentMode.Manual,
        });
        task.AssignedToUserId = userId;
        writer.Move(scope, task, RoomTaskStatus.Assigned, EventTypes.TaskAssigned, new TaskNote { UserId = userId, Reason = current is null ? "assigned" : "reassigned" });
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    /// <summary>Take a room off whoever has it; the task returns to planned.</summary>
    public Task<RoomTask> UnassignAsync(RequestScope scope, Guid taskId, long expectedVersion, CancellationToken cancellationToken) =>
        db.AtomicallyAsync(() => UnassignInsideAsync(scope, taskId, expectedVersion, cancellationToken), cancellationToken);

    private async Task<RoomTask> UnassignInsideAsync(RequestScope scope, Guid taskId, long expectedVersion, CancellationToken cancellationToken)
    {
        var task = await writer.RequireAsync(scope, taskId, expectedVersion, cancellationToken);
        await gate.TaskAsync(scope, Permissions.Assign, taskId, cancellationToken);
        var current = await writer.CurrentAssignmentAsync(taskId, cancellationToken)
            ?? throw new InvalidRequestException("this room is not assigned to anyone");

        await EndAsync(task, current, AssignmentEnd.Ended, cancellationToken);
        task.AssignedToUserId = null;
        writer.Move(scope, task, RoomTaskStatus.Planned, EventTypes.TaskAssigned, new TaskNote { Reason = "taken off" });
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    /// <summary>End every current assignment a person holds — they left, or their shift ended (staff.exited).</summary>
    public Task<int> ReleaseAsync(RequestScope scope, Guid userId, string reason, CancellationToken cancellationToken) =>
        db.AtomicallyAsync(() => ReleaseInsideAsync(scope, userId, reason, cancellationToken), cancellationToken);

    private async Task<int> ReleaseInsideAsync(RequestScope scope, Guid userId, string reason, CancellationToken cancellationToken)
    {
        var rows = await db.Assignments
            .Where(a => a.PropertyId == scope.PropertyId && a.UserId == userId && a.EndedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            var task = await db.Tasks.FirstAsync(t => t.Id == row.TaskId, cancellationToken);
            await EndAsync(task, row, reason, cancellationToken);
            if (task.IsOpen)
            {
                task.AssignedToUserId = null;
                writer.Move(scope, task, RoomTaskStatus.Planned, EventTypes.TaskAssigned, new TaskNote { Reason = reason.ToLowerInvariant() });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private void Accept(RequestScope scope, RoomTask task, TaskAssignment proposed)
    {
        var actor = Actor.Of(scope);
        proposed.Mode = AssignmentMode.Accepted;
        proposed.AssignedByKind = actor.Kind;
        proposed.AssignedById = actor.Id;
        proposed.Via = actor.Via;
        proposed.AssignedAt = writer.Now;
        task.AssignedToUserId = proposed.UserId;
        writer.Move(scope, task, RoomTaskStatus.Assigned, EventTypes.TaskAssigned, new TaskNote { UserId = proposed.UserId, Reason = "proposal accepted" });
    }

    private async Task EndAsync(RoomTask task, TaskAssignment row, string reason, CancellationToken cancellationToken)
    {
        row.EndedAt = writer.Now;
        row.EndReason = reason;
        if (await writer.RunningAsync(task.Id, cancellationToken) is { } running)
        {
            running.Stop(writer.Now, SessionEnd.Reassigned);
        }

        // The end must reach the database before a new current row is inserted,
        // or the one-current-row index refuses the hand-over itself — inside the
        // caller's transaction, so the two rows commit together or not at all.
        await db.SaveChangesAsync(cancellationToken);
    }
}
