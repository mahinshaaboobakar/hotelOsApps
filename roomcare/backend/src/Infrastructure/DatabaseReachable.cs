using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotelOS.RoomCare.Infrastructure;

/// <summary>The health probe — answers with what it found, never a bare status.</summary>
/// <remarks>
/// Not <c>AddDbContextCheck</c>: it swallows the provider's exception and
/// answers <c>false</c>, so an installer reports only that the application
/// never answered. Jobs' first Kernel-launched install spent its budget on that.
/// </remarks>
public sealed class DatabaseReachable(RoomCareDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            var rooms = await db.RoomStates.CountAsync(cancellationToken);
            return HealthCheckResult.Healthy($"{rooms} room(s) in {RoomCareDbContext.Schema}");
        }
        catch (Exception failure)
        {
            return HealthCheckResult.Unhealthy($"{failure.GetType().Name}: {failure.Message}", failure);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
