using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Rating;

/// <summary>
/// The guest rates — S10 D2: from the stay link, only their own guest-raised
/// job, only once it is CLOSED, once. No staff permission: the stay is the
/// credential, carried by the guest app's service call.
/// </summary>
public class RatingService(JobsDbContext db, JobAnnouncer announcer, JobRecords records)
{
    public async Task<JobRating> RateAsync(
        RequestScope scope, Guid jobId, Guid stayId, int stars, string? text, CancellationToken cancellationToken)
    {
        var job = await records.LoadAsync(scope, jobId, cancellationToken);
        if (job.RaisedKind != RaisedKind.Guest || job.StayId != stayId)
        {
            throw new PermissionDeniedException("stay", $"job {job.JobNumber}");
        }

        if (job.JobStatus != JobStatus.Closed)
        {
            throw new InvalidRequestException($"job {job.JobNumber} is {job.JobStatus}; a rating waits for CLOSED");
        }

        if (stars is < 1 or > 5) throw new InvalidRequestException("stars must be 1 to 5");
        if (await db.Ratings.AnyAsync(r => r.JobId == job.Id, cancellationToken))
        {
            throw new InvalidRequestException($"job {job.JobNumber} is already rated");
        }

        var rating = new JobRating
        {
            Id = Uuid7.NewUuid7(), JobId = job.Id, PropertyId = job.PropertyId, StayId = stayId,
            Stars = stars, Text = text?.Trim(), RatedAt = records.Now,
        };
        db.Ratings.Add(rating);
        job.Touch(null, records.Now);
        announcer.Announce(scope, job, EventTypes.JobRated, records.Now, stars.ToString());
        await db.SaveChangesAsync(cancellationToken);
        return rating;
    }
}
