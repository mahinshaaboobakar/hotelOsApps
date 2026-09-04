using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Assignment;
using HotelOS.Jobs.Application.Policies;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Catalogue;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Jobs;

/// <summary>
/// Raising a job — frame 3, S1: the catalogue item decides the department, the
/// priority chain decides the priority (manual → flow → catalogue → NOT_TRIAGED,
/// S1 D4), the policy chain decides the promise, and the number is taken from
/// the property's counter. One transaction, one <c>job.created</c>.
/// </summary>
public class JobService(
    JobsDbContext db,
    IKernelAuthorizer authorizer,
    IPropertyDirectory directory,
    JobPolicyResolver policies,
    JobNumbering numbering,
    AssignmentService assignment,
    JobAnnouncer announcer,
    JobRecords records)
{
    public async Task<Job> RaiseAsync(
        RequestScope scope, RaiseJobCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.Create, "property", scope.PropertyId, cancellationToken);
        Validate(command);

        var item = await db.Items.FirstOrDefaultAsync(
                i => i.Id == command.ItemId && i.DeletedAt == null && i.Active, cancellationToken)
            ?? throw new InvalidRequestException("the catalogue item does not exist or is retired");
        var resolved = await policies.ResolveAsync(scope.PropertyId, item, cancellationToken);
        if (!resolved.ActiveHere)
        {
            throw new InvalidRequestException($"{item.Name} is not offered at this property");
        }

        if (!await directory.LocationExistsAsync(scope.PropertyId, command.LocationId, cancellationToken))
        {
            throw new InvalidRequestException("location_id is not a place at this property");
        }

        var now = records.Now;
        var job = Build(scope, command, item, resolved, now);
        job.JobNumber = await numbering.NextAsync(scope.PropertyId, resolved.DepartmentCode, cancellationToken);
        await PlaceStepAsync(job, cancellationToken);
        db.Jobs.Add(job);
        RecordBirth(scope, job, command, now);
        announcer.Announce(scope, job, EventTypes.JobCreated, now);

        if (job.JobStatus == JobStatus.Raised)
        {
            await assignment.AssignOnRaiseAsync(scope, job, command, resolved, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    /// <summary>The lean job's columns from the command, the item and the chain's answer.</summary>
    private static Job Build(RequestScope scope, RaiseJobCommand command, Item item, ResolvedPolicy resolved, DateTimeOffset now)
    {
        var (priority, decidedBy) = Prioritise(command, resolved);
        var job = new Job
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            CategoryId = item.CategoryId,
            ItemId = item.Id,
            LocationId = command.LocationId,
            AssetId = command.AssetId,
            DepartmentCode = resolved.DepartmentCode,
            Summary = command.Summary.Trim(),
            Details = command.Details?.Trim(),
            Priority = priority,
            PriorityDecidedBy = decidedBy,
            RaisedVia = command.RaisedVia,
            RaisedKind = command.RaisedKind,
            RaisedById = command.RaisedById,
            StayId = command.StayId,
            ScheduledFor = command.ScheduledFor,
            Cycle = command.Cycle?.Trim(),
            Restricted = command.Restricted ?? resolved.RestrictedByDefault,
            ParentJobId = command.ParentJobId,
            ConcernPolicyId = resolved.ConcernPolicyId,
            JobStatus = command.ScheduledFor is null ? JobStatus.Raised : JobStatus.Scheduled,
            CreatedBy = scope.UserId,
            CreatedAt = now,
            UpdatedBy = scope.UserId,
            UpdatedAt = now,
            Version = 1,
        };
        job.DueAt = DueAt(job, resolved, now);
        return job;
    }

    /// <summary>The three rows every job is born with: its first status, its raising text as a note, its first concern.</summary>
    private void RecordBirth(RequestScope scope, Job job, RaiseJobCommand command, DateTimeOffset now)
    {
        db.StatusHistory.Add(new JobStatusHistory
        {
            Id = Guid.CreateVersion7(), JobId = job.Id, PropertyId = job.PropertyId,
            FromStatus = string.Empty, ToStatus = job.JobStatus, ByUserId = scope.UserId,
            ByWhat = command.RaisedKind == RaisedKind.Staff ? null : command.RaisedKind, At = now,
        });
        db.Notes.Add(new JobNote
        {
            Id = Guid.CreateVersion7(), JobId = job.Id, PropertyId = job.PropertyId,
            AuthorKind = command.RaisedKind, AuthorId = command.RaisedById, Text = job.Summary, At = now,
        });
        db.ConcernHistory.Add(new JobConcernHistory
        {
            Id = Guid.CreateVersion7(), JobId = job.Id, PropertyId = job.PropertyId,
            Concern = Concern.OnTrack, AccountableRole = LadderRole.Assignee, LadderStep = 0,
            Since = now, Reason = "raised", ConcernPolicyId = job.ConcernPolicyId,
        });
    }

    private static void Validate(RaiseJobCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Summary))
        {
            throw new InvalidRequestException("summary is required");
        }

        if (!RaisedVia.All.Contains(command.RaisedVia) || !RaisedKind.All.Contains(command.RaisedKind))
        {
            throw new InvalidRequestException("raised_via and raised_kind must be from the vocabulary");
        }

        if (command.RaisedKind == RaisedKind.Guest && command.StayId is null)
        {
            throw new InvalidRequestException("a guest-raised job needs the stay_id — the stay is the guest");
        }

        if (command.Priority is { } p && !Priority.All.Contains(p))
        {
            throw new InvalidRequestException($"priority {p} is not P1, P2, P3 or NOT_TRIAGED");
        }
    }

    /// <summary>The chain: manual → flow → catalogue → NOT_TRIAGED (S1 D4).</summary>
    private static (string Priority, string DecidedBy) Prioritise(RaiseJobCommand command, ResolvedPolicy resolved)
    {
        if (command.Priority is { } manual) return (manual, PriorityDecidedBy.Manual);
        if (command.FlowPriority is { } flow) return (flow, PriorityDecidedBy.Flow);
        if (Priority.All.Contains(resolved.Priority) && resolved.Priority != Priority.NotTriaged)
        {
            return (resolved.Priority, PriorityDecidedBy.Catalogue);
        }

        return (Priority.NotTriaged, PriorityDecidedBy.None);
    }

    /// <summary>Due from the promise; a scheduled job's clock starts on its day (S2 D3).</summary>
    private static DateTimeOffset? DueAt(Job job, ResolvedPolicy resolved, DateTimeOffset now)
    {
        if (resolved.DueWithinMinutes is not { } minutes || job.Priority == Priority.NotTriaged) return null;

        var start = job.ScheduledFor is { } day
            ? new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : now;
        return start.AddMinutes(minutes);
    }

    /// <summary>A child step takes the next number under its parent (S1 D2, one level only).</summary>
    private async Task PlaceStepAsync(Job job, CancellationToken cancellationToken)
    {
        if (job.ParentJobId is not { } parentId) return;

        var parent = await db.Jobs.FirstOrDefaultAsync(
                j => j.Id == parentId && j.PropertyId == job.PropertyId && j.DeletedAt == null, cancellationToken)
            ?? throw new InvalidRequestException("parent_job_id is not a job at this property");
        if (parent.IsStep)
        {
            throw new InvalidRequestException("a step cannot have steps — one level only (S1 D2)");
        }

        var last = await db.Jobs
            .Where(j => j.ParentJobId == parentId)
            .MaxAsync(j => (int?)j.StepNo, cancellationToken);
        job.StepNo = (last ?? 0) + 1;
    }
}
