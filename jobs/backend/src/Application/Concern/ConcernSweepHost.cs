using Microsoft.Extensions.Hosting;

namespace HotelOS.Jobs.Application.Concerns;

/// <summary>
/// The in-process ticker — every sixty seconds, overlap SKIP, S5 D1's cadence.
/// </summary>
/// <remarks>
/// <para>
/// <b>Being replaced by <see cref="ConcernSweepWorkflow"/>, and kept until the
/// Schedule is confirmed firing on this installation</b> — TEMPORAL-Q1, page
/// 62a's order. Until INSTALL-Q69 closes, a property may have no Temporal at
/// all, where the reconciler correctly does nothing; deleting the timer first
/// would leave those properties with no sweep. Both running for one release
/// sweeps twice a minute, which is harmless — the sweep is idempotent — and a
/// property with neither silently stops escalating jobs.
/// </para>
/// <para>
/// It holds no body of its own any more: the tick is
/// <see cref="ConcernActivities.SweepAsync(CancellationToken)"/>, so the two
/// triggers cannot drift into meaning different things while both exist.
/// </para>
/// </remarks>
public sealed class ConcernSweepHost(ConcernActivities sweep) : BackgroundService
{
    /// <summary>The cadence the walkthrough locked.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // PeriodicTimer never queues ticks: a tick that takes ninety seconds
            // is followed by the next due one, never by a catch-up — overlap
            // SKIP, which is what the Schedule is required to reproduce.
            await sweep.SweepAsync(stoppingToken);
        }
    }
}
