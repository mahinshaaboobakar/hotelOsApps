using HotelOS.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotelOS.Jobs.Infrastructure;

/// <summary>
/// Whether this application can reach its own schema, and what stopped it when
/// it cannot.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not <c>AddDbContextCheck</c>, because that check cannot say why.</b> It
/// calls <c>CanConnectAsync</c>, which swallows the provider's exception and
/// returns <c>false</c> — so a failed start logs
/// <i>"Health check postgresql with status Unhealthy … with message 'null'"</i>
/// and the installer reports only that the application never answered.
/// </para>
/// <para>
/// The first Kernel-launched install of Jobs spent fifteen seconds saying
/// exactly that, twice, and was undone; the reason was not in any log on the
/// machine. A probe that reports what it found costs one class and turns that
/// into a sentence an operator can act on.
/// </para>
/// <para>
/// It opens a connection and reads the schema, rather than only connecting: an
/// application whose role can log in but cannot see its own tables is not
/// healthy, and that distinction is exactly what an install's grants get wrong.
/// </para>
/// </remarks>
public sealed class DatabaseReachable(JobsDbContext db) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            var jobs = await db.Jobs.CountAsync(cancellationToken);
            return HealthCheckResult.Healthy($"{jobs} job(s) in {JobsDbContext.Schema}");
        }
        catch (Exception failure)
        {
            // The message, not just the status: whoever reads this is holding
            // an installer, and "Unhealthy" tells them nothing they can fix.
            return HealthCheckResult.Unhealthy(
                $"{failure.GetType().Name}: {failure.Message}", failure);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
