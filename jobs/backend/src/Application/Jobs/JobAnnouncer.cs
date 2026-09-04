using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Platform;

namespace HotelOS.Jobs.Application.Jobs;

/// <summary>
/// Announcing a job fact — design §3. It <b>appends</b>, never sends: the
/// event and its publish_state row go into the caller's transaction (EVT-Q3).
/// A job raised for another application's request echoes that request's
/// correlation id, which is how GuestOps learns its <c>job_id</c>.
/// </summary>
public class JobAnnouncer(IEventAppender events)
{
    /// <summary>Append one job event with the standard payload.</summary>
    public void Announce(
        RequestScope scope, Job job, string eventType, DateTimeOffset occurredAt, string? detail = null)
    {
        events.Append(
            scope,
            eventType,
            EventTypes.JobAggregate,
            job.Id,
            job.Version,
            new JobAnnouncement
            {
                JobId = job.Id,
                JobNumber = job.JobNumber,
                PropertyId = job.PropertyId,
                DepartmentCode = job.DepartmentCode,
                CategoryId = job.CategoryId,
                ItemId = job.ItemId,
                LocationId = job.LocationId,
                Status = job.JobStatus,
                Priority = job.Priority,
                RaisedKind = job.RaisedKind,
                StayId = job.StayId,
                Detail = detail,
                OccurredAt = occurredAt,
            });
    }
}

/// <summary>The payload every <c>job.*</c> event carries — snake_cased by the appender.</summary>
public sealed class JobAnnouncement
{
    public Guid JobId { get; init; }

    public string JobNumber { get; init; } = string.Empty;

    public Guid PropertyId { get; init; }

    public string DepartmentCode { get; init; } = string.Empty;

    public Guid CategoryId { get; init; }

    public Guid ItemId { get; init; }

    public Guid LocationId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;

    public string RaisedKind { get; init; } = string.Empty;

    public Guid? StayId { get; init; }

    /// <summary>What the event is about beyond the status: the concern, the hold reason, the assignee.</summary>
    public string? Detail { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}
