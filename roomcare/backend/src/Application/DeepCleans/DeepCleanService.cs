using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.DeepCleans;

/// <summary>The deep clean as a planned project — plan the window, request the block, raise the job, bring the room back (S0).</summary>
/// <remarks>
/// <para>
/// <b>Room Care requests; others act.</b> The block is requested by event and
/// applied by the owner of out-of-order state (<c>permissions.yaml:1404</c>,
/// ADR 0056); the work is a Jobs job raised by <c>roomcare.deep_clean.due</c>
/// with a correlation id, and <c>job.created</c> brings its id back (EVT-Q3).
/// A multi-day assignment with changing hands is Jobs' (<c>JOBS-Q2</c>) and is
/// not built here.
/// </para>
/// <para>
/// <b>Return:</b> the job closes → the room is dirty → the day's decision gives
/// it a departure clean → when it is clean again the release of the block is
/// requested and the project is done, its date the next plan counts from.
/// </para>
/// </remarks>
public sealed class DeepCleanService(
    RoomCareDbContext db, Gate gate, PropertyClock clock, ConditionWriter condition, IEventAppender events)
{
    public async Task<DeepClean> PlanAsync(
        RequestScope scope, Guid roomId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Plan, cancellationToken);
        if (to < from)
        {
            throw new InvalidRequestException("a deep clean's window ends on or after the day it starts");
        }

        var now = await clock.AtAsync(scope.PropertyId, cancellationToken);
        if (from < now.Day)
        {
            throw new InvalidRequestException("a deep clean is planned from today onwards");
        }

        if (await db.DeepCleans.AnyAsync(
                d => d.PropertyId == scope.PropertyId && d.RoomId == roomId && DeepCleanStatus.Open.Contains(d.Status) && d.WindowFrom != null,
                cancellationToken))
        {
            throw new InvalidRequestException("this room already has a deep clean planned; cancel it first");
        }

        var project = await db.DeepCleans.FirstOrDefaultAsync(
                d => d.PropertyId == scope.PropertyId && d.RoomId == roomId && d.Status == DeepCleanStatus.Planned && d.WindowFrom == null,
                cancellationToken)
            ?? Add(scope.PropertyId, roomId, now.Day, now.Instant);

        project.WindowFrom = from;
        project.WindowTo = to;
        project.PlannedBy = Actor.PersonOf(scope, "planning a deep clean");
        project.Status = DeepCleanStatus.BlockRequested;
        project.BlockCorrelationId = $"block:{project.Id}";
        project.BlockRequestedAt = now.Instant;
        project.JobCorrelationId = $"deep-clean:{project.Id}";
        Bump(project, now.Instant);
        Announce(scope with { CorrelationId = project.BlockCorrelationId }, project, EventTypes.BlockRequested, project.BlockCorrelationId, now.Instant);
        Bump(project, now.Instant);
        Announce(scope with { CorrelationId = project.JobCorrelationId }, project, EventTypes.DeepCleanDue, project.JobCorrelationId, now.Instant);

        await db.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<DeepClean> CancelAsync(RequestScope scope, Guid deepCleanId, long expectedVersion, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Plan, cancellationToken);
        var project = await RequireAsync(scope, deepCleanId, expectedVersion, cancellationToken);
        if (!DeepCleanStatus.Open.Contains(project.Status))
        {
            throw new InvalidRequestException("this deep clean has already finished");
        }

        var now = clock.Now;
        var wasRequested = project.BlockCorrelationId is not null;
        project.Status = DeepCleanStatus.Cancelled;
        Bump(project, now);
        if (wasRequested)
        {
            Announce(scope, project, EventTypes.BlockReleaseRequested, project.BlockCorrelationId!, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return project;
    }

    /// <summary>Jobs answered on the correlation id — the job is under way.</summary>
    public async Task JobCreatedAsync(Guid propertyId, string correlationId, Guid jobId, CancellationToken cancellationToken)
    {
        var project = await db.DeepCleans.FirstOrDefaultAsync(
            d => d.PropertyId == propertyId && d.JobCorrelationId == correlationId, cancellationToken);
        if (project is null || project.JobId == jobId)
        {
            return;
        }

        project.JobId = jobId;
        project.JobStatusSeen = "OPEN";
        project.Status = project.Status is DeepCleanStatus.BlockRequested or DeepCleanStatus.Blocked ? DeepCleanStatus.InProgress : project.Status;
        project.UpdatedAt = clock.Now;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The job closed — the room comes back dirty, for a departure clean and, if the rule says, inspection.</summary>
    public async Task JobClosedAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken)
    {
        var project = await db.DeepCleans.FirstOrDefaultAsync(
            d => d.PropertyId == scope.PropertyId && d.JobId == jobId && DeepCleanStatus.Open.Contains(d.Status), cancellationToken);
        if (project is null)
        {
            return;
        }

        var now = clock.Now;
        project.JobStatusSeen = "CLOSED";
        project.Status = DeepCleanStatus.Returning;
        project.UpdatedAt = now;
        var room = await condition.RoomAsync(scope.PropertyId, project.RoomId, now, cancellationToken);
        condition.Set(scope, room, new ConditionChange(Condition.Dirty, ConditionSource.System, Actor.System, now)
        {
            Reason = "deep clean job closed — a departure clean returns the room",
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>A returning room is clean again — request the block's release and close the project.</summary>
    public async Task RoomReadyAsync(RequestScope scope, Guid roomId, DateOnly day, CancellationToken cancellationToken)
    {
        var project = await db.DeepCleans.FirstOrDefaultAsync(
            d => d.PropertyId == scope.PropertyId && d.RoomId == roomId && d.Status == DeepCleanStatus.Returning, cancellationToken);
        if (project is null)
        {
            return;
        }

        var now = clock.Now;
        project.Status = DeepCleanStatus.Done;
        project.DoneOn = day;
        Bump(project, now);
        Announce(scope, project, EventTypes.BlockReleaseRequested, project.BlockCorrelationId ?? $"block:{project.Id}", now);
    }

    private DeepClean Add(Guid propertyId, Guid roomId, DateOnly day, DateTimeOffset at)
    {
        var project = new DeepClean
        {
            Id = Guid.CreateVersion7(), PropertyId = propertyId, RoomId = roomId, DueOn = day, CreatedAt = at, UpdatedAt = at,
        };
        db.DeepCleans.Add(project);
        return project;
    }

    private async Task<DeepClean> RequireAsync(RequestScope scope, Guid id, long expectedVersion, CancellationToken cancellationToken)
    {
        var project = await db.DeepCleans.FirstOrDefaultAsync(d => d.Id == id && d.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("deep_clean", id);
        return project.Version == expectedVersion ? project : throw new ConcurrencyException("deep_clean", id, expectedVersion);
    }

    private static void Bump(DeepClean project, DateTimeOffset at)
    {
        project.UpdatedAt = at;
        project.Version += 1;
    }

    private void Announce(RequestScope scope, DeepClean project, string type, string correlationId, DateTimeOffset at) =>
        events.Append(scope, type, EventTypes.DeepCleanAggregate, project.Id, project.Version, new DeepCleanAnnouncement
        {
            DeepCleanId = project.Id,
            RoomId = project.RoomId,
            PropertyId = project.PropertyId,
            DueOn = project.DueOn.ToString("yyyy-MM-dd"),
            From = project.WindowFrom?.ToString("yyyy-MM-dd"),
            To = project.WindowTo?.ToString("yyyy-MM-dd"),
            Reason = "deep clean",
            CorrelationId = correlationId,
            OccurredAt = at,
        });
}
