using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Cancellation;

/// <summary>
/// Cancelling — <c>job.cancel</c>, split from amend by the architect on
/// 2026-09-04: a terminal outcome with a reason, complete's sibling. Cancelling
/// a parent cancels its open steps; closing never does (S1 D2).
/// </summary>
public class CancellationService(
    JobsDbContext db,
    IKernelAuthorizer authorizer,
    JobAnnouncer announcer,
    JobRecords records)
{
    public async Task<Job> CancelAsync(RequestScope scope, CancelCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Cancel, "property", scope.PropertyId, cancellationToken);
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            throw new InvalidRequestException("cancelling needs a reason");
        }

        var job = await records.LoadAsync(scope, command.JobId, cancellationToken);
        JobRecords.RequireVersion(job, command.ExpectedVersion);
        JobRecords.RequireOpen(job);

        await EndAsync(scope, job, command.Reason.Trim(), cancellationToken);

        var steps = await db.Jobs
            .Where(j => j.ParentJobId == job.Id && j.DeletedAt == null)
            .Where(j => j.JobStatus != JobStatus.Closed && j.JobStatus != JobStatus.Cancelled)
            .ToListAsync(cancellationToken);
        foreach (var step in steps)
        {
            await EndAsync(scope, step, $"parent {job.JobNumber} cancelled", cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    private async Task EndAsync(RequestScope scope, Job job, string reason, CancellationToken cancellationToken)
    {
        var now = records.Now;
        if (await records.OpenSessionAsync(job.Id, cancellationToken) is { } session)
        {
            session.Stop(now);
        }

        if (await records.CurrentAssignmentAsync(job.Id, cancellationToken) is { } current)
        {
            current.EndedAt = now;
        }

        job.HoldReason = null;
        job.HoldUntil = null;
        records.Move(scope, job, JobStatus.Cancelled, note: reason);
        announcer.Announce(scope, job, EventTypes.JobCancelled, now, reason);
    }
}
