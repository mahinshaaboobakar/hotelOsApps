using System.Text.Json.Serialization;
using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.DeepCleans;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Events;

/// <summary><c>job.created</c> — Jobs answered one of Room Care's asks; the envelope's correlation id says which (EVT-Q3).</summary>
/// <remarks>
/// Matched on the envelope, never on a field Room Care guessed Jobs would add:
/// a consumer handling Room Care's event appends in that event's scope, so its
/// correlation id is the one Room Care sent — nothing assumed beyond the event
/// boundary.
/// </remarks>
public sealed class JobCreatedHandler(RoomCareDbContext db, DeepCleanService deepCleans) : IEventHandler<JobAnnounced>
{
    public async Task HandleAsync(RequestScope scope, JobAnnounced payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var correlation = envelope.CorrelationId;
        if (string.IsNullOrEmpty(correlation))
        {
            return;
        }

        var issue = await db.Issues.FirstOrDefaultAsync(i => i.PropertyId == scope.PropertyId && i.CorrelationId == correlation, cancellationToken);
        if (issue is { JobId: null })
        {
            issue.JobId = payload.JobId;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        await deepCleans.JobCreatedAsync(scope.PropertyId, correlation, payload.JobId, cancellationToken);
    }
}

/// <summary><c>job.closed</c> — a job against a room goes into the room's day ("extra service 16:50 · J-1183", S5 c8).</summary>
public sealed class JobClosedHandler(RoomCareDbContext db, DeepCleanService deepCleans, PropertyClock clock, IHouse house)
    : IEventHandler<JobAnnounced>
{
    public async Task HandleAsync(RequestScope scope, JobAnnounced payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        await deepCleans.JobClosedAsync(scope, payload.JobId, cancellationToken);

        if (payload.LocationId is not { } location
            || (await house.RoomsAsync(scope.PropertyId, cancellationToken)).All(r => r.Id != location)
            || await db.JobTouches.AnyAsync(t => t.EventId == envelope.EventId, cancellationToken))
        {
            return;
        }

        var day = (await clock.AtAsync(scope.PropertyId, envelope.OccurredAt, cancellationToken)).Day;
        var task = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId && t.RoomId == location && t.OperatingDay == day)
            .OrderBy(t => t.Window)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        db.JobTouches.Add(new TaskJobTouch
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            RoomId = location,
            OperatingDay = day,
            TaskId = task,
            JobId = payload.JobId,
            JobNumber = payload.JobNumber,
            ClosedAt = payload.OccurredAt ?? envelope.OccurredAt,
            Summary = payload.Summary ?? payload.Detail,
            EventId = envelope.EventId,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>The fields of Jobs' announcement Room Care reads — Jobs' own names.</summary>
public sealed record JobAnnounced(
    [property: JsonPropertyName("job_id")] Guid JobId,
    [property: JsonPropertyName("job_number")] string? JobNumber,
    [property: JsonPropertyName("location_id")] Guid? LocationId,
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("detail")] string? Detail,
    [property: JsonPropertyName("occurred_at")] DateTimeOffset? OccurredAt);
