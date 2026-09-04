using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Completion;

/// <summary>
/// Resolve, close, reopen — <c>job.complete</c>, frame 4, S1 D7, S2 D3. Resolving
/// stops the running session and records one catalogue resolution or "Other"
/// with a note; the sweep closes after the policy's hours, and until then the
/// job may be reopened. Steps unblock when their parent resolves.
/// </summary>
public class CompletionService(
    JobsDbContext db,
    IKernelAuthorizer authorizer,
    JobAnnouncer announcer,
    JobRecords records)
{
    public async Task<Job> ResolveAsync(RequestScope scope, ResolveCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Complete, "property", scope.PropertyId, cancellationToken);
        var job = await records.LoadAsync(scope, command.JobId, cancellationToken);
        JobRecords.RequireVersion(job, command.ExpectedVersion);
        if (job.JobStatus is not (JobStatus.Accepted or JobStatus.InProgress or JobStatus.OnHold))
        {
            throw new InvalidRequestException($"job {job.JobNumber} is {job.JobStatus} and cannot be resolved");
        }

        await ValidateResolutionAsync(job, command, cancellationToken);
        var now = records.Now;
        if (await records.OpenSessionAsync(job.Id, cancellationToken) is { } session)
        {
            session.Stop(now);
        }

        db.Resolutions.Add(new JobResolution
        {
            Id = Guid.CreateVersion7(), JobId = job.Id, PropertyId = job.PropertyId,
            ResolutionId = command.ResolutionId, Note = command.Note?.Trim(),
            ResolvedBy = scope.UserId ?? Guid.Empty, ResolvedAt = now,
        });
        job.HoldReason = null;
        job.HoldUntil = null;
        records.Move(scope, job, JobStatus.Resolved);
        announcer.Announce(scope, job, EventTypes.JobResolved, now);
        await UnblockStepsAsync(scope, job, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    /// <summary>Close now — a person's act; the sweep does the same after the policy's hours.</summary>
    public async Task<Job> CloseAsync(RequestScope scope, Guid jobId, long expectedVersion, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Complete, "property", scope.PropertyId, cancellationToken);
        var job = await records.LoadAsync(scope, jobId, cancellationToken);
        JobRecords.RequireVersion(job, expectedVersion);
        if (job.JobStatus != JobStatus.Resolved)
        {
            throw new InvalidRequestException($"job {job.JobNumber} is {job.JobStatus}; resolve it first");
        }

        records.Move(scope, job, JobStatus.Closed);
        if (await records.CurrentAssignmentAsync(job.Id, cancellationToken) is { } current)
        {
            current.EndedAt = records.Now;
        }

        announcer.Announce(scope, job, EventTypes.JobClosed, records.Now);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    /// <summary>Reopen inside the window: back to ACCEPTED with the same assignee.</summary>
    public async Task<Job> ReopenAsync(RequestScope scope, Guid jobId, long expectedVersion, string? note, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Complete, "property", scope.PropertyId, cancellationToken);
        var job = await records.LoadAsync(scope, jobId, cancellationToken);
        JobRecords.RequireVersion(job, expectedVersion);
        if (job.JobStatus != JobStatus.Resolved)
        {
            throw new InvalidRequestException($"job {job.JobNumber} is {job.JobStatus}; only RESOLVED reopens");
        }

        var assigned = await records.CurrentAssignmentAsync(job.Id, cancellationToken) is not null;
        records.Move(scope, job, assigned ? JobStatus.Accepted : JobStatus.Raised, note: note?.Trim());
        announcer.Announce(scope, job, EventTypes.JobReopened, records.Now, note?.Trim());
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    private async Task ValidateResolutionAsync(Job job, ResolveCommand command, CancellationToken cancellationToken)
    {
        if (command.ResolutionId is null)
        {
            if (string.IsNullOrWhiteSpace(command.Note))
            {
                throw new InvalidRequestException("a resolution, or a note for Other, is required");
            }

            return;
        }

        var resolution = await db.CatalogueResolutions.FirstOrDefaultAsync(
                r => r.Id == command.ResolutionId && r.DeletedAt == null && r.Active, cancellationToken)
            ?? throw new InvalidRequestException("the resolution does not exist");
        var fits = (resolution.ItemId is null || resolution.ItemId == job.ItemId)
                   && (resolution.CategoryId is null || resolution.CategoryId == job.CategoryId);
        if (!fits)
        {
            throw new InvalidRequestException($"{resolution.Name} is not a resolution for this item");
        }

        if (resolution.NoteRequired && string.IsNullOrWhiteSpace(command.Note))
        {
            throw new InvalidRequestException($"{resolution.Name} needs a note");
        }
    }

    /// <summary>Steps blocked behind this job get their clock: due from now, the step's own promise (S1 D2).</summary>
    private async Task UnblockStepsAsync(RequestScope scope, Job parent, CancellationToken cancellationToken)
    {
        var steps = await db.Jobs
            .Where(j => j.ParentJobId == parent.Id && j.DeletedAt == null && j.JobStatus != JobStatus.Cancelled)
            .OrderBy(j => j.StepNo)
            .ToListAsync(cancellationToken);
        var next = steps.FirstOrDefault(s => JobStatus.IsOpen(s.JobStatus) || s.JobStatus == JobStatus.Scheduled);
        if (next is null) return;

        next.Touch(scope.UserId, records.Now);
        db.ConcernHistory.Add(new JobConcernHistory
        {
            Id = Guid.CreateVersion7(), JobId = next.Id, PropertyId = next.PropertyId,
            Concern = Concern.OnTrack, AccountableRole = LadderRole.Assignee, LadderStep = 0,
            Since = records.Now, Reason = $"unblocked: step {next.StepNo} after {parent.JobNumber} resolved",
            ConcernPolicyId = next.ConcernPolicyId,
        });
    }
}
