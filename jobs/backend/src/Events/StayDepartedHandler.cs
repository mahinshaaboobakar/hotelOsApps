using System.Text.Json.Serialization;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Events;

/// <summary>
/// The stay that raised a job has left — S10 D2's rating window closes, and an
/// open guest-raised job gets a note saying the guest is gone, so the person
/// working it knows before knocking. Nothing is closed on the guest's behalf.
/// </summary>
public sealed record StayDeparted([property: JsonPropertyName("stay_id")] Guid StayId);

public sealed class StayDepartedHandler(JobsDbContext db, TimeProvider clock) : IEventHandler<StayDeparted>
{
    public async Task HandleAsync(RequestScope scope, StayDeparted payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var open = await db.Jobs
            .Where(j => j.PropertyId == scope.PropertyId && j.StayId == payload.StayId && j.DeletedAt == null)
            .Where(j => JobStatus.Open.Contains(j.JobStatus))
            .ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        foreach (var job in open)
        {
            var already = await db.Notes.AnyAsync(
                n => n.JobId == job.Id && n.AuthorKind == RaisedKind.Application && n.Text.StartsWith("Guest departed"), cancellationToken);
            if (already) continue;

            db.Notes.Add(new JobNote
            {
                Id = Guid.CreateVersion7(), JobId = job.Id, PropertyId = job.PropertyId,
                // No time in the words: a UTC time composed here is one every reader
                // at the property must convert (ADR 0174). The note's own At is drawn
                // in the property's form, and says when.
                AuthorKind = RaisedKind.Application, Text = "Guest departed; the room may be empty.",
                Internal = true, At = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
