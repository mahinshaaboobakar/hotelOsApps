using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Concerns;

/// <summary>
/// RESOLVED becomes CLOSED after the closing policy's hours — S2 D3, settings
/// frame 5: the department's hours, else the property's, else four. Until then
/// the job may be reopened. Runs on the sweep's tick.
/// </summary>
public class AutoClose(JobsDbContext db, JobAnnouncer announcer, JobRecords records)
{
    /// <summary>Close what the policy makes due at one property. Returns how many.</summary>
    /// <remarks>The scope is the tick's, and names the property — see <see cref="ConcernSweep"/>.</remarks>
    public async Task<int> RunAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var policies = await db.ClosingPolicies.Where(p => p.PropertyId == scope.PropertyId).ToListAsync(cancellationToken);
        var resolved = await db.Jobs
            .Where(j => j.PropertyId == scope.PropertyId && j.DeletedAt == null && j.JobStatus == JobStatus.Resolved)
            .ToListAsync(cancellationToken);
        if (resolved.Count == 0) return 0;

        var now = records.Now;
        var closed = 0;
        foreach (var job in resolved)
        {
            var hours = policies.FirstOrDefault(p => p.DepartmentCode == job.DepartmentCode)?.AutoCloseHours
                ?? policies.FirstOrDefault(p => p.DepartmentCode == null)?.AutoCloseHours
                ?? 4;
            var resolvedAt = await db.Resolutions.Where(r => r.JobId == job.Id)
                .MaxAsync(r => (DateTimeOffset?)r.ResolvedAt, cancellationToken) ?? job.UpdatedAt;
            if (now - resolvedAt < TimeSpan.FromHours(hours)) continue;

            records.Move(scope, job, JobStatus.Closed, byWhat: "SWEEP", note: $"auto-close after {hours} h");
            if (await records.CurrentAssignmentAsync(job.Id, cancellationToken) is { } current)
            {
                current.EndedAt = now;
            }

            announcer.Announce(scope, job, EventTypes.JobClosed, now, "auto-close");
            closed += 1;
        }

        await db.SaveChangesAsync(cancellationToken);
        return closed;
    }
}
