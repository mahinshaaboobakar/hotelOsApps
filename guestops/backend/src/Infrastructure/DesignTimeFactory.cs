using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HotelOS.GuestOps.Infrastructure;

/// <summary>
/// A context for <c>dotnet ef</c>, and for nothing that runs.
/// </summary>
/// <remarks>
/// <para>
/// The EF tools build the application's host to find a context, and this
/// application's host needs a Kernel channel, a Context client and the
/// platform's PII keys — none of which exist while somebody is generating a
/// migration at a console. Without this the tooling reports a DI failure that
/// reads like a broken service.
/// </para>
/// <para>
/// <b>The connection string here is never used to run anything.</b> A migration
/// is generated from the model, and it is <i>applied</i> by
/// <c>HotelOS.GuestOps.dll migrate</c> — through <c>SchemaMigration</c>, as the
/// schema's owner role, with the credential the installer puts in the child's
/// environment. A design-time string that could reach a real database would be
/// one <c>database update</c> away from writing to it as the wrong role.
/// </para>
/// </remarks>
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<GuestOpsDbContext>
{
    /// <param name="args">Passed by the EF tooling; unused.</param>
    public GuestOpsDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<GuestOpsDbContext>()
            .UseSnakeCaseNamingConvention()
            .UseNpgsql(
                "Host=localhost;Database=guestops_design_time",
                npgsql => npgsql.MigrationsHistoryTable("__migrations", GuestOpsDbContext.Schema))
            .Options);
}
