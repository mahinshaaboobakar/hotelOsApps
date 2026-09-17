using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotelOS.Workforce.Infrastructure;

/// <summary>
/// Whether this application can reach its own schema, and what stopped it when
/// it cannot.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not <c>AddDbContextCheck</c>, because that check cannot say why.</b> It
/// calls <c>CanConnectAsync</c>, which swallows the provider's exception and
/// returns <c>false</c> — so the probe reports
/// <i>"Health check postgresql with status Unhealthy … with message 'null'"</i>
/// and an operator is left with a verdict and no reason.
/// </para>
/// <para>
/// <b>This application said exactly that forty-nine times on the owner's
/// property</b>, one second after a clean start, while the Kernel's installer
/// reported only that Workforce never answered. Two rounds were spent narrowing
/// by the timing of the refusals — 555 ms on the first, 8–20 ms on every one
/// after — because the sentence naming the cause was being discarded inside
/// this check.
/// </para>
/// <para>
/// <b>It opens a connection and reads the schema, rather than only
/// connecting</b>: a role that can log in but cannot see its own tables is not
/// healthy, and that distinction is exactly what an install's grants get wrong.
/// <c>28P01</c> and <c>3D000</c> are different problems with different owners,
/// and a boolean cannot tell them apart.
/// </para>
/// <para>
/// <b>Jobs wrote this class first and its remarks describe this defect in its
/// own words</b>, after the same fifteen silent seconds on its first
/// Kernel-launched install. The two are near-identical and deliberately not
/// shared: each names its own <c>DbContext</c> and its own table, which is the
/// part that makes the check mean anything. If a third application writes it,
/// the shape belongs in the SDK — that is a platform decision rather than one
/// this file should take by refactoring across a repository boundary.
/// </para>
/// </remarks>
/// <param name="db">This application's context, over its own schema.</param>
public sealed class DatabaseReachable(WorkforceDbContext db) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.OpenConnectionAsync(cancellationToken);

            // A read, not a ping. Connecting proves the credential; counting
            // proves the grant, and an install gets the second wrong far more
            // often than the first.
            var postings = await db.Postings.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy(
                $"{postings} posting(s) in {WorkforceDbContext.Schema}");
        }
        catch (Exception failure)
        {
            // The type AND the message. Npgsql puts the SQLSTATE in the type's
            // own `SqlState`, but the message is the sentence a person acts on,
            // and the type is what distinguishes a refused password from an
            // absent database when the message is terse.
            return HealthCheckResult.Unhealthy(
                $"{failure.GetType().Name}: {failure.Message}", failure);
        }
        finally
        {
            // Closed on every path. The probe runs on a timer, and a check that
            // leaked a connection per run would exhaust the pool and then report
            // the exhaustion as the fault.
            await db.Database.CloseConnectionAsync();
        }
    }
}
