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
/// <b>The suite migrates as <c>hotelos_migrator</c> assuming the owner, then
/// connects as the application role</b> — never as the owner and never
/// privileged, because a suite that ran as the owner would pass through a
/// missing grant. That part is unchanged and is why the shape was chosen.
/// </para>
/// <para>
/// <b>THE PREMISE UNDER IT IS NO LONGER TRUE, AND THE 41 TESTS BEHIND THIS
/// FIXTURE DO NOT RUN.</b> This paragraph used to read: <i>"An installed
/// application is provisioned exactly like a platform service. 02-roles.sql
/// already carries <c>hotelos_owner_guestops</c> and <c>hotelos_app_guestops</c>,
/// so nothing had to be widened to run this."</i> It was true when written.
/// </para>
/// <para>
/// <c>INSTALL-Q76</c> removed both roles from bootstrap — <c>02-roles.sql</c>
/// now says an application's schema and roles are <b>its install's</b> to create,
/// and that the provisioner is deliberately unable to name them in advance. The
/// install therefore generates the application role's password and seals it
/// (<c>packages/database.rs:145</c> creates the <c>Uuid</c>, <c>:198</c> issues
/// the <c>CREATE ROLE … PASSWORD</c>, <c>:441</c> seals it). So on any machine
/// where GuestOps has been installed, <c>hotelos_app_guestops</c> exists with a
/// password nobody can restore, and <see cref="AppPassword"/>'s default cannot
/// authenticate: 41 tests fail <c>28P01</c> at connect, before a single
/// assertion — 17 in <c>DeskTests</c>, 10 in <c>InboundFactTests</c>, 8 in
/// <c>ReconciliationTests</c>, 6 in <c>StayListTests</c>. <b>What they assert has
/// been seen by nobody since.</b>
/// </para>
/// <para>
/// <b>Two things a reader should not conclude from this, because both send
/// somebody to the wrong remedy.</b> The platform does <i>not</i> still provide
/// the role — reading the old sentence and going to look is exactly the path
/// that ends in asking for a cluster-role write. And running this suite has
/// never been able to damage an installed application: <see cref="ScratchDatabase"/>
/// issues <c>CREATE DATABASE</c>, <c>GRANT CONNECT</c> and <c>DROP DATABASE</c>
/// and no role statement of any kind, and this fixture adds none. The collision
/// is on the cluster-wide role <i>name</i>, not on the installed database.
/// </para>
/// <para>
/// <b>The shape is frozen until <c>INSTALL-Q98</c> rules</b>, which asks which of
/// two patterns already in the tree is standard for a package's own backend
/// suite: this one, or the sibling applications' <c>postgres</c>/<c>devroot</c>
/// against a scratch database. It is not to be changed to go green — a suite
/// that switched in the meantime is harder to switch back than one that is red.
/// Note also that <see cref="AppRole"/> is a <c>const</c>: only the password
/// reads the environment, so pointing the suite at a differently-named role is
/// not available without changing this file.
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
