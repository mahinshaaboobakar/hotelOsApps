using HotelOS.Jobs.Application.Calendar;
using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Concerns;

/// <summary>
/// A scheduled job becomes RAISED at 00:00 of its day in the property's zone,
/// and its clock starts then — S2 D3, frame 6. Runs on the sweep's tick; a
/// job whose day has come is raised at most once.
/// </summary>
public class DayStart(
    JobsDbContext db,
    IPropertyDirectory directory,
    JobAnnouncer announcer,
    JobRecords records)
{
    /// <summary>Raise the day's scheduled jobs at one property. Returns how many.</summary>
    /// <remarks>The scope is the tick's, and names the property — see <see cref="ConcernSweep"/>.</remarks>
    public async Task<int> RunAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var today = await TodayAsync(scope.PropertyId, cancellationToken);
        var due = await db.Jobs
            .Where(j => j.PropertyId == scope.PropertyId && j.DeletedAt == null)
            .Where(j => j.JobStatus == JobStatus.Scheduled && j.ScheduledFor != null && j.ScheduledFor <= today)
            .ToListAsync(cancellationToken);
        if (due.Count == 0) return 0;

        foreach (var job in due)
        {
            records.Move(scope, job, JobStatus.Raised, byWhat: "SWEEP", note: $"day {job.ScheduledFor:yyyy-MM-dd} began");
            announcer.Announce(scope, job, EventTypes.JobCreated, records.Now, "scheduled day began");
        }

        await db.SaveChangesAsync(cancellationToken);
        return due.Count;
    }

    /// <summary>Today in the property's zone — refused by name when it has none, never UTC (the reference's rule).</summary>
    private async Task<DateOnly> TodayAsync(Guid propertyId, CancellationToken cancellationToken) =>
        (await PropertyCalendar.ForAsync(directory, propertyId, cancellationToken)).DayOf(records.Now);
}
