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
    public async Task<int> RunAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var today = await TodayAsync(propertyId, cancellationToken);
        var due = await db.Jobs
            .Where(j => j.PropertyId == propertyId && j.DeletedAt == null)
            .Where(j => j.JobStatus == JobStatus.Scheduled && j.ScheduledFor != null && j.ScheduledFor <= today)
            .ToListAsync(cancellationToken);
        if (due.Count == 0) return 0;

        var scope = new RequestScope { Caller = CallerKind.Service, ServiceName = "jobs", PropertyId = propertyId };
        foreach (var job in due)
        {
            records.Move(scope, job, JobStatus.Raised, byWhat: "SWEEP", note: $"day {job.ScheduledFor:yyyy-MM-dd} began");
            announcer.Announce(scope, job, EventTypes.JobCreated, records.Now, "scheduled day began");
        }

        await db.SaveChangesAsync(cancellationToken);
        return due.Count;
    }

    /// <summary>Today in the property's zone; UTC when Master Data has no zone for it.</summary>
    private async Task<DateOnly> TodayAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var zone = await directory.FindTimezoneAsync(propertyId, cancellationToken);
        var now = records.Now;
        if (zone is not null && (TimeZoneInfo.TryFindSystemTimeZoneById(zone, out var info)
            || (TimeZoneInfo.TryConvertIanaIdToWindowsId(zone, out var windows)
                && TimeZoneInfo.TryFindSystemTimeZoneById(windows, out info))))
        {
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, info).DateTime);
        }

        return DateOnly.FromDateTime(now.UtcDateTime);
    }
}
