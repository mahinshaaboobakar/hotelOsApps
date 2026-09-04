using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Temporalio.Activities;

namespace HotelOS.Jobs.Application.Concerns;

/// <summary>The sweep as Temporal executes it — <c>TEMPORAL-Q1</c>, page 62a.</summary>
/// <remarks>
/// <para>
/// The body is the hosted timer's tick moved, not rewritten: the
/// discovery of properties from open jobs, the per-property loop, the order of
/// the three passes and the try/catch that keeps one property's failure off the
/// others are the audited behaviour and stay exactly as they were. Temporal
/// replaces the ticker, not the facts.
/// </para>
/// <para>
/// <b>The provider arrives as a function</b>, because an activity instance must
/// be declared before <c>builder.Build()</c> and cannot do its work without the
/// provider that call returns — page 62a's lazy shape.
/// </para>
/// </remarks>
public sealed class ConcernActivities(Func<IServiceProvider> services)
{
    /// <summary>What the workflow calls — Temporal's own cancellation, nothing else.</summary>
    [Activity]
    public Task SweepAsync() => SweepAsync(ActivityExecutionContext.Current.CancellationToken);

    /// <summary>One tick over every property with jobs — separately callable, so a test can drive it.</summary>
    public async Task SweepAsync(CancellationToken cancellationToken)
    {
        var root = services();
        using var work = root.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var provider = work.ServiceProvider;
        var db = provider.GetRequiredService<JobsDbContext>();
        var identity = provider.GetRequiredService<ServiceIdentity>();
        var log = provider.GetRequiredService<ILogger<ConcernActivities>>();

        var properties = await db.Jobs
            .Where(j => j.DeletedAt == null)
            .Select(j => j.PropertyId).Distinct()
            .ToListAsync(cancellationToken);

        foreach (var property in properties)
        {
            // The scope this work acts in — WF-Q11 (8). Jobs' own service
            // identity, this property, no user: nobody asked, so nobody is
            // recorded as having asked. One per property per tick, because a
            // trace is per unit of work, and 1,440 sweeps a day sharing one is
            // a trace nobody can read.
            var scope = RequestScope.ForBackgroundWork(identity, property);
            try
            {
                await provider.GetRequiredService<DayStart>().RunAsync(scope, cancellationToken);
                await provider.GetRequiredService<ConcernSweep>().RunAsync(scope, cancellationToken);
                await provider.GetRequiredService<AutoClose>().RunAsync(scope, cancellationToken);
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
            {
                // Load-bearing: a failing property never stops the others, and
                // the next tick retries it — which is also why the workflow
                // sets no retry policy of its own.
                log.LogError(failure, "Concern sweep failed for property {PropertyId}; next tick retries", property);
            }
        }
    }
}
