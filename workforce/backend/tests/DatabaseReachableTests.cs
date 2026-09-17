using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The probe says what it found, and these are about the SENTENCE rather than
/// the verdict.
/// </summary>
/// <remarks>
/// <para>
/// <c>AddDbContextCheck</c> reported <c>Unhealthy</c> with <c>message 'null'</c>
/// forty-nine times on the owner's property, one second after a clean start,
/// and two rounds were spent narrowing by the timing of the refusals because
/// the reason was being discarded inside the check.
/// </para>
/// <para>
/// <b>Asserting the verdict alone would not have failed on that defect</b> —
/// the old check returned <c>Unhealthy</c> too, correctly, and said nothing.
/// So what is asserted here is that a reason exists, names the exception, and
/// reaches the caller.
/// </para>
/// <para>
/// No fixture and no database: a connection that cannot be made is the subject,
/// and port 1 refuses on every machine without arranging anything.
/// </para>
/// </remarks>
public class DatabaseReachableTests
{
    /// <summary>A context pointed somewhere nothing is listening.</summary>
    private static WorkforceDbContext Refusing() => new(
        new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=nothing;Username=nobody;"
                       + "Password=nothing;Timeout=2;Command Timeout=2")
            .Options);

    [Fact]
    public async Task An_unreachable_database_is_reported_with_the_reason_and_not_only_a_verdict()
    {
        await using var db = Refusing();

        var found = await new DatabaseReachable(db).CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, found.Status);

        // The half the old check could not supply. `Unhealthy` with a null
        // description is a verdict an operator cannot act on, and it is what
        // this class exists to stop.
        Assert.NotNull(found.Description);
        Assert.NotEmpty(found.Description);

        // The exception travels too, so a log sink that renders one has it.
        Assert.NotNull(found.Exception);

        // And the type is named in the text, because a terse provider message
        // is what makes a refused password and an absent database read alike.
        Assert.Contains(found.Exception!.GetType().Name, found.Description);
    }

    [Fact]
    public async Task The_check_closes_its_connection_even_when_it_fails()
    {
        await using var db = Refusing();

        // Run it repeatedly: a check that leaked a connection per run would
        // exhaust the pool and then report the exhaustion as the fault — a
        // probe that becomes the thing it is reporting on.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var found = await new DatabaseReachable(db).CheckHealthAsync(
                new HealthCheckContext(), CancellationToken.None);

            Assert.Equal(HealthStatus.Unhealthy, found.Status);
        }

        Assert.Equal(
            System.Data.ConnectionState.Closed,
            db.Database.GetDbConnection().State);
    }
}
