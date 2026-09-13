using HotelOS.Platform;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Temporalio.Activities;

namespace HotelOS.RoomCare.Application.Tick;

/// <summary>The tick's one activity — a pass per property, each in its own scope, one failure never stopping the rest.</summary>
public sealed class TickActivities(Func<IServiceProvider> services)
{
    [Activity]
    public Task SweepAsync() => SweepAsync(ActivityExecutionContext.Current.CancellationToken);

    public async Task SweepAsync(CancellationToken cancellationToken)
    {
        var root = services();
        using var listing = root.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var identity = listing.ServiceProvider.GetRequiredService<ServiceIdentity>();
        var environment = listing.ServiceProvider.GetService<PlatformEnvironment>();
        var log = listing.ServiceProvider.GetRequiredService<ILogger<TickActivities>>();

        // The property this installation serves, and any other that already has
        // rows — one installation is one property (ADR 0065 §5), so the list is
        // normally one long.
        var db = listing.ServiceProvider.GetRequiredService<RoomCareDbContext>();
        var properties = await db.Policies.Select(p => p.PropertyId)
            .Union(db.RoomStates.Select(r => r.PropertyId))
            .Distinct()
            .ToListAsync(cancellationToken);
        if (environment?.PropertyId is { } served && !properties.Contains(served))
        {
            properties.Add(served);
        }

        foreach (var property in properties)
        {
            using var work = root.GetRequiredService<IServiceScopeFactory>().CreateScope();
            try
            {
                await work.ServiceProvider.GetRequiredService<TickPass>()
                    .RunAsync(RequestScope.ForBackgroundWork(identity, property), cancellationToken);
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
            {
                log.LogError(failure, "Room Care tick failed for property {PropertyId}; the next tick retries", property);
            }
        }
    }
}
