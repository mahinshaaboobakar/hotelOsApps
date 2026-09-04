using System.Text.Json.Serialization;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Events;

/// <summary>
/// The Engineering app's PPM plan fired — design §3, frame 6: one job per
/// occurrence, raised as APPLICATION with the plan's occurrence tag as
/// <c>cycle</c>. Jobs holds the job, not the calendar (S7). Idempotent on the
/// occurrence: the same plan and tag never raise twice.
/// </summary>
public sealed record PpmDue(
    [property: JsonPropertyName("plan_id")] Guid PlanId,
    [property: JsonPropertyName("occurrence")] string Occurrence,
    [property: JsonPropertyName("item_id")] Guid ItemId,
    [property: JsonPropertyName("location_id")] Guid LocationId,
    [property: JsonPropertyName("asset_id")] Guid? AssetId,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("scheduled_for")] DateOnly? ScheduledFor,
    [property: JsonPropertyName("priority")] string? Priority);

public sealed class PpmDueHandler(JobsDbContext db, JobService jobs) : IEventHandler<PpmDue>
{
    public async Task HandleAsync(RequestScope scope, PpmDue payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var cycle = $"{payload.PlanId:N}:{payload.Occurrence}";
        if (await db.Jobs.AnyAsync(j => j.PropertyId == scope.PropertyId && j.Cycle == cycle, cancellationToken))
        {
            return;
        }

        await jobs.RaiseAsync(
            scope,
            new RaiseJobCommand
            {
                ItemId = payload.ItemId,
                LocationId = payload.LocationId,
                AssetId = payload.AssetId,
                Summary = payload.Summary,
                Priority = payload.Priority,
                RaisedVia = RaisedVia.App,
                RaisedKind = RaisedKind.Application,
                RaisedById = payload.PlanId,
                ScheduledFor = payload.ScheduledFor,
                Cycle = cycle,
            },
            cancellationToken);
    }
}
