using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Jobs;

/// <summary>
/// What every job service does to a job record before and after its own act:
/// find it inside the caller's property, refuse a stale version, and write the
/// status-history row. One home, so the guards cannot drift between services.
/// </summary>
public class JobRecords(JobsDbContext db, TimeProvider clock)
{
    /// <summary>The job, in this property, not deleted — or NotFound.</summary>
    public async Task<Job> LoadAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken) =>
        await db.Jobs.FirstOrDefaultAsync(
            j => j.Id == jobId && j.PropertyId == scope.PropertyId && j.DeletedAt == null,
            cancellationToken)
        ?? throw new NotFoundException("job", jobId);

    /// <summary>Refuse a write against a version the caller did not see.</summary>
    public static void RequireVersion(Job job, long expected)
    {
        if (job.Version != expected)
        {
            throw new ConcurrencyException("job", job.Id, expected);
        }
    }

    /// <summary>Refuse an act on a job that has ended.</summary>
    public static void RequireOpen(Job job)
    {
        if (!JobStatus.IsOpen(job.JobStatus) && job.JobStatus != JobStatus.Scheduled)
        {
            throw new InvalidRequestException(
                $"job {job.JobNumber} is {job.JobStatus} and cannot be changed");
        }
    }

    /// <summary>Move the job and record the transition in one motion.</summary>
    public void Move(RequestScope scope, Job job, string to, string? byWhat = null, string? note = null)
    {
        var now = clock.GetUtcNow();
        var from = job.JobStatus;
        job.MoveTo(to, scope.UserId, now);
        db.StatusHistory.Add(new JobStatusHistory
        {
            Id = Guid.CreateVersion7(),
            JobId = job.Id,
            PropertyId = job.PropertyId,
            FromStatus = from,
            ToStatus = to,
            ByUserId = scope.UserId,
            ByWhat = byWhat,
            At = now,
            Note = note,
        });
    }

    /// <summary>The assignment row the board shows as "Assigned to", if any.</summary>
    public Task<JobAssignment?> CurrentAssignmentAsync(Guid jobId, CancellationToken cancellationToken) =>
        db.Assignments.FirstOrDefaultAsync(a => a.JobId == jobId && a.EndedAt == null, cancellationToken);

    /// <summary>The running or paused session, if any.</summary>
    public Task<JobWorkSession?> OpenSessionAsync(Guid jobId, CancellationToken cancellationToken) =>
        db.WorkSessions.FirstOrDefaultAsync(s => s.JobId == jobId && s.StoppedAt == null, cancellationToken);

    /// <summary>Now, from the one clock every service shares.</summary>
    public DateTimeOffset Now => clock.GetUtcNow();
}
