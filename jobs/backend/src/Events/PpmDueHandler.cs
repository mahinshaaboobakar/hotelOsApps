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
                // **Not the plan.** `RaisedById` names the PERSON who raised a
                // job, and a PPM plan is not one — writing its id here put a
                // maintenance plan in a column every screen renders as a
                // colleague, and `Naming.Raiser` would have called it "Staff
                // member". Nobody raised this: the Engineering app's plan fell
                // due and the application acted, which `RaisedKind.Application`
                // already says. ADR 0172, and the gap rule: no value stands in
                // for a measurement nobody took.
                RaisedById = null,
                ScheduledFor = payload.ScheduledFor,
                Cycle = cycle,
            },
            cancellationToken);
    }
}
