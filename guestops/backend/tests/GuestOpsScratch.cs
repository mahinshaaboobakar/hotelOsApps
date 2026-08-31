using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// A migrated <c>guestops</c> schema of this run's own.
/// </summary>
/// <remarks>
/// <para>
/// <b>The mechanism is the platform's</b> — <see cref="ScratchDatabase"/>. What
/// stays here is this application's own meaning: which schema, which role, which
/// migrations and which grants. That is the line the shared harness draws, and
/// a fifth copy of the create/grant/drop dance is what it exists to prevent.
/// </para>
/// <para>
/// <b>An installed application is provisioned exactly like a platform service.</b>
/// <c>02-roles.sql</c> already carries <c>hotelos_owner_guestops</c> and
/// <c>hotelos_app_guestops</c>, so nothing had to be widened to run this: the
/// suite migrates as <c>hotelos_migrator</c> assuming the owner, and then
/// connects as the application role, never as the owner and never privileged.
/// A suite that ran as the owner would pass through a missing grant.
/// </para>
/// </remarks>
public sealed class GuestOpsScratch : IAsyncDisposable
{
    /// <summary>What this service connects as in production.</summary>
    private const string AppRole = "hotelos_app_guestops";

    private readonly ScratchDatabase _database;

    private GuestOpsScratch(ScratchDatabase database, string connection)
    {
        _database = database;
        Connection = connection;
    }

    /// <summary>How a test reaches it — as the application role.</summary>
    public string Connection { get; }

    private static string AppPassword =>
        Environment.GetEnvironmentVariable("HOTELOS_GUESTOPS_DB_PASSWORD") ?? "devguestops";

    /// <summary>Create it, migrate it, and grant what the service runs under.</summary>
    /// <returns>A prepared database.</returns>
    /// <exception cref="InvalidOperationException">PostgreSQL did not answer.</exception>
    /// <remarks>
    /// <b>Absent fails the run</b> — ADR 0053, and the diagnostic names the
    /// address that was tried and the command that provides it. A suite that
    /// reported "skipped" here would look green on a machine where it has never
    /// once executed, which this platform has already shipped twice.
    /// </remarks>
    public static async Task<GuestOpsScratch> CreateAsync()
    {
        var spec = new ScratchDatabaseSpec(
            "hotelos_guestops_test",
            new Dictionary<string, string> { [AppRole] = AppPassword });

        var database = await ScratchDatabase.CreateAsync(spec)
            ?? throw new InvalidOperationException(
                $"could not reach PostgreSQL at {ScratchDatabase.Target} — GuestOps's tests "
                + "need the development database. Start it with `make db-up` and apply the "
                + "roles with `make db-roles`, or point HOTELOS_TEST_DB_PORT at another one. "
                + "Never 15432: that is the installed product's (ADR 0104 §E2E-Q5(a)).");

        try
        {
            await database.CreateSchemaAsync(GuestOpsDbContext.Schema);

            await using (var context = Context(
                SchemaMigration.ConnectionFor(
                    database.ConnectionFor(AppRole),
                    GuestOpsDbContext.Schema,
                    Environment.GetEnvironmentVariable(SchemaMigration.PasswordVariable)
                        ?? "devmigrator")))
            {
                await context.Database.MigrateAsync();
            }

            // What 04-grants.sql gives the application role, applied by the owner
            // because the owner is what holds them. Without it every test would
            // fail on permissions rather than on behaviour.
            await database.AsOwnerAsync(
                GuestOpsDbContext.Schema,
                $"GRANT USAGE ON SCHEMA {GuestOpsDbContext.Schema} TO {AppRole};"
                + $"GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA "
                + $"{GuestOpsDbContext.Schema} TO {AppRole};"
                + $"GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA "
                + $"{GuestOpsDbContext.Schema} TO {AppRole};");
        }
        catch
        {
            // A failed preparation must not leave the database behind — ADR 0033.
            // The original failure is what the reader needs, so a failure to
            // clean up must not replace it.
            try
            {
                await database.DisposeAsync();
            }
            catch (InvalidOperationException)
            {
                // Nothing useful to add over the failure about to surface.
            }

            throw;
        }

        return new GuestOpsScratch(database, database.ConnectionFor(AppRole));
    }

    /// <summary>A context over this database, as the application role.</summary>
    /// <returns>A new context; the caller owns it.</returns>
    public GuestOpsDbContext Context() => Context(Connection);

    private static GuestOpsDbContext Context(string connection) =>
        new(
            new DbContextOptionsBuilder<GuestOpsDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(
                    connection,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__migrations", GuestOpsDbContext.Schema))
                .Options);

    /// <summary>Drop it.</summary>
    /// <returns>When the database is gone.</returns>
    public ValueTask DisposeAsync() => _database.DisposeAsync();
}
