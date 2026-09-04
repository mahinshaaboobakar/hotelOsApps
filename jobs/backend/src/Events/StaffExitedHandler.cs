using System.Text.Json.Serialization;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Events;

/// <summary>
/// An assignee has left the property — design §3: their open assignments end
/// and the jobs go back to RAISED for AUTO to pick again on the next raise or
/// a supervisor to reassign. Sessions stop with their worked time kept.
/// </summary>
public sealed record StaffExited(
    [property: JsonPropertyName("user_id")] Guid? UserId,
    [property: JsonPropertyName("staff_id")] Guid StaffId);

public sealed class StaffExitedHandler(JobsDbContext db, JobRecords records) : IEventHandler<StaffExited>
{
    public async Task HandleAsync(RequestScope scope, StaffExited payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        if (payload.UserId is not { } user) return;

        var assignments = await db.Assignments
            .Where(a => a.PropertyId == scope.PropertyId && a.AssigneeUserId == user && a.EndedAt == null)
            .ToListAsync(cancellationToken);
        var now = records.Now;
        foreach (var assignment in assignments)
        {
            assignment.EndedAt = now;
            var job = await db.Jobs.FirstAsync(j => j.Id == assignment.JobId, cancellationToken);
            if (await records.OpenSessionAsync(job.Id, cancellationToken) is { } session)
            {
                session.Stop(now);
            }

            if (job.JobStatus is JobStatus.Assigned or JobStatus.Accepted or JobStatus.InProgress)
            {
                records.Move(scope, job, JobStatus.Raised, byWhat: "STAFF_EXIT", note: "assignee left the property");
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
