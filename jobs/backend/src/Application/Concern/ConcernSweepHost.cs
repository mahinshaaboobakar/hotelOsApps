using HotelOS.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotelOS.Jobs.Application.Concerns;

/// <summary>
/// Runs the sweep every sixty seconds, overlap SKIP — S5 D1's cadence. The
/// design named Temporal Cron as the trigger; no Temporal client exists in the
/// application SDK today (build finding, 2026-09-04), so this hosted timer
/// stands in with the same contract: one tick a minute, a slow tick is not
/// followed by a second one, and a failing property never stops the others.
/// The day-start and auto-close passes ride the same tick.
/// </summary>
public sealed class ConcernSweepHost(
    IServiceScopeFactory scopes,
    ILogger<ConcernSweepHost> log) : BackgroundService
{
    /// <summary>The cadence the walkthrough locked.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // PeriodicTimer never queues ticks: a tick that takes ninety seconds
            // is followed by the next due one, never by a catch-up — overlap SKIP.
            await TickAsync(stoppingToken);
        }
    }

    /// <summary>One tick over every property with jobs. Public so a test can drive it.</summary>
    public async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        var properties = await db.Jobs
            .Where(j => j.DeletedAt == null)
            .Select(j => j.PropertyId).Distinct()
            .ToListAsync(cancellationToken);

        foreach (var property in properties)
        {
            try
            {
                await scope.ServiceProvider.GetRequiredService<DayStart>().RunAsync(property, cancellationToken);
                await scope.ServiceProvider.GetRequiredService<ConcernSweep>().RunAsync(property, cancellationToken);
                await scope.ServiceProvider.GetRequiredService<AutoClose>().RunAsync(property, cancellationToken);
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
            {
                log.LogError(failure, "Concern sweep failed for property {PropertyId}; next tick retries", property);
            }
        }
    }
}
