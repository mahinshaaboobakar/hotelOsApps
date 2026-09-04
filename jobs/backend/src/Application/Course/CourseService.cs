using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Course;

/// <summary>
/// Changing a job's course after it exists — <c>job.amend</c>, design §4.1: hold
/// and resume (S9 D2), reschedule, re-prioritise, restrict, link (S1 D2).
/// Cancelling is its own verb and its own service.
/// </summary>
public class CourseService(
    JobsDbContext db,
    IKernelAuthorizer authorizer,
    JobAnnouncer announcer,
    JobRecords records)
{
    /// <summary>Put on hold: a reason, a date, and the clock stops (S9 D2).</summary>
    public async Task<Job> HoldAsync(RequestScope scope, HoldCommand command, CancellationToken cancellationToken)
    {
        var job = await AmendableAsync(scope, command.JobId, command.ExpectedVersion, cancellationToken);
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            throw new InvalidRequestException("a hold needs a reason");
        }

        if (job.JobStatus is not (JobStatus.Assigned or JobStatus.Accepted or JobStatus.InProgress))
        {
            throw new InvalidRequestException($"job {job.JobNumber} is {job.JobStatus} and cannot be held");
        }

        var now = records.Now;
        if (await records.OpenSessionAsync(job.Id, cancellationToken) is { } session)
        {
            session.Stop(now);
        }

        job.HoldReason = command.Reason.Trim();
        job.HoldUntil = command.Until;
        records.Move(scope, job, JobStatus.OnHold, note: job.HoldReason);
        announcer.Announce(scope, job, EventTypes.JobHeld, now, job.HoldReason);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    /// <summary>Take off hold: back to ACCEPTED (or ASSIGNED), the clock resumes where it stopped.</summary>
    public async Task<Job> ResumeAsync(RequestScope scope, Guid jobId, long expectedVersion, CancellationToken cancellationToken)
    {
        var job = await AmendableAsync(scope, jobId, expectedVersion, cancellationToken);
        if (job.JobStatus != JobStatus.OnHold)
        {
            throw new InvalidRequestException($"job {job.JobNumber} is not on hold");
        }

        var held = job.HoldUntil;
        job.HoldReason = null;
        job.HoldUntil = null;
        var current = await records.CurrentAssignmentAsync(job.Id, cancellationToken);
        var to = current is null ? JobStatus.Raised : current.AcceptedAt is null ? JobStatus.Assigned : JobStatus.Accepted;
        records.Move(scope, job, to, note: held is { } u ? $"resumed; was due back {u:u}" : "resumed");
        announcer.Announce(scope, job, EventTypes.JobResumed, records.Now);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    /// <summary>Priority, schedule, restricted, a link — whichever the command carries.</summary>
    public async Task<Job> AmendAsync(RequestScope scope, AmendCommand command, CancellationToken cancellationToken)
    {
        var job = await AmendableAsync(scope, command.JobId, command.ExpectedVersion, cancellationToken);
        JobRecords.RequireOpen(job);

        if (command.Priority is { } priority)
        {
            if (!Priority.All.Contains(priority)) throw new InvalidRequestException($"priority {priority} is not known");
            job.Priority = priority;
            job.PriorityDecidedBy = PriorityDecidedBy.Manual;
        }

        if (command.ScheduledFor.IsPresent)
        {
            Reschedule(scope, job, command.ScheduledFor.Value);
        }

        if (command.Restricted is { } restricted)
        {
            job.Restricted = restricted;
        }

        if (command.LinkJobId is { } other)
        {
            await LinkAsync(scope, job, other, cancellationToken);
        }

        job.Touch(scope.UserId, records.Now);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    /// <summary>A manager may clear the date to raise a scheduled job now, or move it (S2 D3).</summary>
    private void Reschedule(RequestScope scope, Job job, DateOnly? day)
    {
        if (job.JobStatus != JobStatus.Scheduled && day is not null)
        {
            throw new InvalidRequestException($"job {job.JobNumber} has already started; it cannot be scheduled");
        }

        job.ScheduledFor = day;
        if (day is null && job.JobStatus == JobStatus.Scheduled)
        {
            records.Move(scope, job, JobStatus.Raised, note: "raised now");
        }
    }

    /// <summary>A group tie between equals, both at this property, once (S1 D2).</summary>
    private async Task LinkAsync(RequestScope scope, Job job, Guid otherId, CancellationToken cancellationToken)
    {
        if (otherId == job.Id) throw new InvalidRequestException("a job cannot be linked to itself");
        _ = await records.LoadAsync(scope, otherId, cancellationToken);
        var exists = await db.Links.AnyAsync(
            l => (l.JobId == job.Id && l.LinkedJobId == otherId) || (l.JobId == otherId && l.LinkedJobId == job.Id),
            cancellationToken);
        if (exists) return;

        db.Links.Add(new JobLink
        {
            Id = Guid.CreateVersion7(), PropertyId = job.PropertyId, JobId = job.Id,
            LinkedJobId = otherId, LinkedBy = scope.UserId, At = records.Now,
        });
    }

    private async Task<Job> AmendableAsync(RequestScope scope, Guid jobId, long expectedVersion, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Amend, "property", scope.PropertyId, cancellationToken);
        var job = await records.LoadAsync(scope, jobId, cancellationToken);
        JobRecords.RequireVersion(job, expectedVersion);
        return job;
    }
}
