using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;

namespace HotelOS.Jobs.Application.Work;

/// <summary>
/// Start, pause, resume, stop — S4. These are the assignee's acts on their own
/// job and ride on the assignment, never on a permission (design §4.1). A
/// pause keeps the session; a stop ends it. The first start moves the job to
/// IN_PROGRESS; PAUSED is never a job status (S2 D2).
/// </summary>
public class WorkSessionService(JobsDbContext db, JobAnnouncer announcer, JobRecords records)
{
    public async Task<JobWorkSession> StartAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken)
    {
        var (job, actor) = await OwnJobAsync(scope, jobId, cancellationToken);
        if (job.JobStatus is not (JobStatus.Accepted or JobStatus.InProgress))
        {
            throw new InvalidRequestException($"job {job.JobNumber} is {job.JobStatus}; accept it first");
        }

        if (await records.OpenSessionAsync(job.Id, cancellationToken) is not null)
        {
            throw new InvalidRequestException($"job {job.JobNumber} already has a session open");
        }

        var now = records.Now;
        var session = new JobWorkSession
        {
            Id = Uuid7.NewUuid7(), JobId = job.Id, PropertyId = job.PropertyId, UserId = actor, StartedAt = now,
        };
        db.WorkSessions.Add(session);

        if (job.JobStatus == JobStatus.Accepted)
        {
            records.Move(scope, job, JobStatus.InProgress, note: "session started");
            announcer.Announce(scope, job, EventTypes.JobStarted, now);
        }
        else
        {
            job.Touch(scope.UserId, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<JobWorkSession> PauseAsync(RequestScope scope, Guid jobId, string? reason, CancellationToken cancellationToken)
    {
        var (job, _) = await OwnJobAsync(scope, jobId, cancellationToken);
        var session = await RunningAsync(job, cancellationToken);
        if (!session.IsRunning)
        {
            throw new InvalidRequestException($"job {job.JobNumber}'s session is already paused");
        }

        session.PausedAt = records.Now;
        session.ResumedAt = null;
        session.PauseReason = reason?.Trim();
        job.Touch(scope.UserId, records.Now);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<JobWorkSession> ResumeAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken)
    {
        var (job, _) = await OwnJobAsync(scope, jobId, cancellationToken);
        var session = await RunningAsync(job, cancellationToken);
        if (!session.IsPaused)
        {
            throw new InvalidRequestException($"job {job.JobNumber}'s session is not paused");
        }

        // A second pause in the same session would lose the first; close it and open a new one instead.
        var now = records.Now;
        session.Stop(now);
        var next = new JobWorkSession
        {
            Id = Uuid7.NewUuid7(), JobId = job.Id, PropertyId = job.PropertyId, UserId = session.UserId, StartedAt = now,
        };
        db.WorkSessions.Add(next);
        job.Touch(scope.UserId, now);
        await db.SaveChangesAsync(cancellationToken);
        return next;
    }

    public async Task<JobWorkSession> StopAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken)
    {
        var (job, _) = await OwnJobAsync(scope, jobId, cancellationToken);
        var session = await RunningAsync(job, cancellationToken);
        var now = records.Now;
        session.Stop(now);
        job.Touch(scope.UserId, now);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    /// <summary>The job, and proof the caller is its assignee.</summary>
    private async Task<(Job Job, Guid Actor)> OwnJobAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken)
    {
        var job = await records.LoadAsync(scope, jobId, cancellationToken);
        var current = await records.CurrentAssignmentAsync(job.Id, cancellationToken)
            ?? throw new InvalidRequestException($"job {job.JobNumber} is not assigned");
        var actor = scope.UserId ?? throw new PermissionDeniedException("assignee", $"job {job.JobNumber}");
        if (current.AssigneeUserId != actor)
        {
            throw new PermissionDeniedException("assignee", $"job {job.JobNumber}");
        }

        return (job, actor);
    }

    private async Task<JobWorkSession> RunningAsync(Job job, CancellationToken cancellationToken) =>
        await records.OpenSessionAsync(job.Id, cancellationToken)
        ?? throw new InvalidRequestException($"job {job.JobNumber} has no session open");
}
