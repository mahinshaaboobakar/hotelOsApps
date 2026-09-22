using HotelOS.Applications.TestSupport;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// A migrated <c>guestops</c> schema of this run's own, provisioned as the
/// installer would provision it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The database is the platform's, the convention is the applications'.</b>
/// <see cref="ScratchDatabase"/> creates and drops the database and issues
/// <c>GRANT CONNECT</c>; it issues no role statement at all. Everything about
/// <i>which</i> roles exist and what they may do is
/// <see cref="InstallerConvention"/>'s, shared by the applications in this
/// repository — ADR 0157. Neither is restated here.
/// </para>
/// <para>
/// <b>What this replaced, and why it was red.</b> The fixture used to name the
/// installer's own role, <c>hotelos_app_guestops</c>, and rely on
/// <c>02-roles.sql</c> to have created it. <c>INSTALL-Q76</c> removed both
/// application roles from bootstrap — an application's schema and roles are its
/// <i>install's</i> to create — and the install generates that role's password
/// and seals it. So on any machine where GuestOps is installed the role exists
/// with a password nobody can restore, and 41 tests failed <c>28P01</c> at
/// connect before a single assertion: 17 in <c>DeskTests</c>, 10 in
/// <c>InboundFactTests</c>, 8 in <c>ReconciliationTests</c>, 6 in
/// <c>StayListTests</c>. ADR 0156 is the ruling; this is it applied.
/// </para>
/// <para>
/// <b>The credential, which had to be established rather than copied.</b> ADR
/// 0156 rejects <c>postgres</c>/<c>devroot</c> by name — it can create
/// cluster-level roles and privileges, and it is what wrote two roles onto the
/// owner's live property. <c>hotelos_test</c> cannot serve either: it holds
/// <c>CREATEDB</c> and not <c>CREATEROLE</c>. What remains is the role the
/// platform ships for exactly this work — <c>hotelos_provisioner</c>, which
/// <c>02-roles.sql:169</c> creates <c>LOGIN CREATEROLE</c> and which holds
/// <c>hotelos_masterdata_reader</c> and <c>hotelos_event_appender</c> with
/// <c>ADMIN OPTION</c>, so it can hand this run's role the two grants an
/// installed application gets. <b>The installer runs step 4 as the provisioner;
/// so does this.</b>
/// </para>
/// <para>
/// <b>Order, because two of these cannot be swapped.</b> The roles are created
/// before the database, because <see cref="ScratchDatabase"/> issues
/// <c>GRANT CONNECT</c> to every role the spec names and a grant to a role that
/// does not exist fails. The schema is created after the database and before the
/// migration, because a migration assumes its owner.
/// </para>
/// <para>
/// <b>And the roles are removed at the end as well as on failure</b> — ADR 0156.
/// Roles are cluster-wide and a scratch database is not, so a suite that passes
/// and leaves its roles has left them on a developer's cluster for good. The run
/// suffix is what makes any leftover identifiable and harmless; it is not a
/// reason to leave one.
/// </para>
/// </remarks>
public sealed class GuestOpsScratch : IAsyncDisposable
{
    /// <summary>What names this run's roles, so nothing it creates is anyone else's.</summary>
    private static string Run => Guid.NewGuid().ToString("n")[..8];

    /// <summary>The role the installer runs step 4 as — <c>02-roles.sql:169</c>.</summary>
    private const string ProvisionerRole = "hotelos_provisioner";

    /// <summary>Creates databases, and owns the ones it creates.</summary>
    private const string TestRole = "hotelos_test";

    private readonly InstallerConvention _convention;
    private readonly ScratchDatabase _database;

    private GuestOpsScratch(
        InstallerConvention convention, ScratchDatabase database, string connection)
    {
        _convention = convention;
        _database = database;
        Connection = connection;
    }

    /// <summary>How a test reaches it — as the application role, never the owner.</summary>
    /// <remarks>
    /// A suite that connected as the owner would pass straight through a missing
    /// grant, which is one class of defect this shape exists to catch.
    /// </remarks>
    public string Connection { get; }

    private static string Host => ScratchDatabase.Target.Split(':')[0];

    private static string Port => ScratchDatabase.Target.Split(':')[1];

    private static string ProvisionerPassword =>
        Environment.GetEnvironmentVariable("HOTELOS_PROVISIONER_PASSWORD") ?? "devprovisioner";

    private static string TestPassword =>
        Environment.GetEnvironmentVariable("HOTELOS_TEST_ROLE_PASSWORD") ?? "devtest";

    private static string As(string role, string password, string database) =>
        $"Host={Host};Port={Port};Database={database};Username={role};Password={password};"
        + "Pooling=false;Include Error Detail=true";

    /// <summary>Provision it as the installer would, and migrate it.</summary>
    /// <param name="withEventStore">
    /// True for a test that reads <c>StoredEvent</c>. It costs four of the
    /// Kernel's migrations, so it is not the default.
    /// </param>
    /// <returns>A prepared database.</returns>
    /// <exception cref="InvalidOperationException">PostgreSQL did not answer.</exception>
    /// <remarks>
    /// <b>Absent fails the run</b> — ADR 0053, and the diagnostic names the
    /// address that was tried and the command that provides it. A suite that
    /// reported "skipped" here would look green on a machine where it has never
    /// once executed, which this platform has already shipped twice.
    /// </remarks>
    public static async Task<GuestOpsScratch> CreateAsync(bool withEventStore = false)
    {
        var convention = new InstallerConvention(GuestOpsDbContext.Schema, Run);
        var password = $"guestops{Guid.NewGuid():N}"[..24];
        var cluster = As(ProvisionerRole, ProvisionerPassword, "postgres");

        // Before the database, because the spec's GRANT CONNECT names them.
        await convention.EnsureRolesAsync(cluster, password);

        ScratchDatabase? database = null;

        try
        {
            database = await ScratchDatabase.CreateAsync(
                new ScratchDatabaseSpec(
                    NamePrefix: "hotelos_guestops_test",
                    Roles: new Dictionary<string, string>
                    {
                        [convention.AppRole] = password,
                        [ProvisionerRole] = ProvisionerPassword,
                    }))
                ?? throw new InvalidOperationException(
                    $"could not reach PostgreSQL at {ScratchDatabase.Target} — GuestOps's tests "
                    + "need the development database. Start it with `make db-up` and apply the "
                    + "roles with `make db-bootstrap`, or point HOTELOS_TEST_DB_PORT at another "
                    + "one. Never 15432: that is the installed product's (ADR 0104 §E2E-Q5(a)).");

            await PrepareAsync(convention, database, withEventStore);

            return new GuestOpsScratch(
                convention, database, database.ConnectionFor(convention.AppRole));
        }
        catch
        {
            // **Both, and in this order.** A failed preparation must not leave
            // the database behind (ADR 0033) and must not leave this run's
            // cluster roles behind either (ADR 0156) — and the roles are the
            // half that outlives the database, because dropping a database
            // drops nothing cluster-scoped. The original failure is what the
            // reader needs, so neither cleanup may replace it.
            await DiscardAsync(convention, database);
            throw;
        }
    }

    /// <summary>Grant, create the schema, and migrate — the installer's order.</summary>
    /// <param name="convention">This run's names.</param>
    /// <param name="database">The scratch database.</param>
    /// <param name="withEventStore">Whether the platform's event store is needed.</param>
    /// <returns>When the application role can use its schema.</returns>
    private static async Task PrepareAsync(
        InstallerConvention convention, ScratchDatabase database, bool withEventStore)
    {
        // The scratch database is `hotelos_test`'s, so `hotelos_test` is who can
        // let the provisioner create in it. The platform grants the provisioner
        // CREATE on the platform's own database and knows nothing about this
        // one; asking this database's owner is the narrowest way to close that,
        // and needs no privilege either role does not already hold.
        await ExecuteAsync(
            As(TestRole, TestPassword, "postgres"),
            $"GRANT CREATE ON DATABASE \"{database.Name}\" TO {ProvisionerRole}");

        await convention.ProvisionSchemaAsync(
            As(ProvisionerRole, ProvisionerPassword, database.Name), database.Name);

        // **The platform's own event store, for the tests that read it.**
        // `AddPlatformEventStore()` puts `StoredEvent` in the model, so a read
        // that touches it — `ActivityView` is one — fails on a database with no
        // `event_store`. A property always has one: `03-schemas.sql` creates it
        // and the Kernel's migrations fill it, so a scratch database is the only
        // place it can be missing. Run from the Kernel's own migration text
        // rather than a table shaped like it — ADR 0157's shared support.
        //
        // **Asked for, not always.** Provisioning it unconditionally added four
        // migration files to every harness, which made each setup slow enough
        // that more of them overlapped — and the suite failed with
        // `53300: too many connections for role "hotelos_migrator"`, in
        // `StayListTests`, which does not use the event store at all. A cost
        // paid by every test to serve five is how one stream's addition becomes
        // another's flake.
        if (withEventStore)
        {
            await PlatformEventStore.ProvisionAsync(
                As(ProvisionerRole, ProvisionerPassword, database.Name));
        }

        // As the owner, by way of the migrator — the shape the SDK uses, where
        // the role and search path travel as libpq options so no migration can
        // forget a SET ROLE.
        await using var context = Context(convention.MigratorConnectionFor(database.Name));
        await context.Database.MigrateAsync();
    }

    /// <summary>Take away everything this run created.</summary>
    /// <param name="convention">This run's names.</param>
    /// <param name="database">Its database, or null if it never existed.</param>
    /// <returns>When both are gone, or when they could not be.</returns>
    private static async Task DiscardAsync(
        InstallerConvention convention, ScratchDatabase? database)
    {
        try
        {
            // **The database first, and this order was arrived at the hard
            // way.** Roles-first leaves `DROP ROLE` failing with *"cannot be
            // dropped because some objects depend on it"*: `DROP OWNED BY`
            // clears what a role owns and what it was granted, and the schema's
            // default ACLs still name it. Dropping the database takes every
            // per-database dependency with it, after which the roles drop
            // cleanly — which is why every leftover from an earlier run, whose
            // database was long gone, dropped by hand without complaint.
            //
            // `DropRolesAsync` is then passed no database connection, which is
            // its documented shape for a run with no database to clean inside.
            if (database is not null)
            {
                await database.DisposeAsync();
            }

            await convention.DropRolesAsync(
                As(ProvisionerRole, ProvisionerPassword, "postgres"), null);
        }
        catch (InvalidOperationException)
        {
            // Nothing useful to add over the failure this is cleaning up after.
        }
        catch (NpgsqlException)
        {
            // Likewise. The run suffix is what makes a leftover findable.
        }
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

    /// <summary>
    /// Master Data's <c>room_types</c> in this scratch database, with these
    /// rows, readable by the application role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>For the views that read a room type's name.</b> This database
    /// provisions the <c>guestops</c> schema and nothing else, so
    /// <c>BookingView</c> and <c>CancelPlanView</c> threw <c>42P01</c> before
    /// reaching anything a test was about — <c>RoomStay.RoomTypeId</c> is
    /// non-nullable, so there is no arrangement in which those views skip the
    /// read. Room Care's <c>MasterDataStaffAsync</c> is this shape for
    /// <c>masterdata.staff</c>, and this follows it.
    /// </para>
    /// <para>
    /// <b>Created as the provisioner and read as the application role</b>, which
    /// is the split an install produces: EF creates the table from
    /// <see cref="MasterDataRoomTypeSource"/>'s mapping — no hand-written DDL —
    /// and the grant that follows is the one an installed application gets.
    /// Without the grant the application role would see a schema and not a
    /// table, which is a different failure from the one this closes and worth
    /// keeping distinguishable.
    /// </para>
    /// </remarks>
    /// <param name="rows">The room types, as Master Data holds them.</param>
    /// <returns>When the table exists, holds them, and is readable.</returns>
    public async Task MasterDataRoomTypesAsync(IEnumerable<MasterDataRoomTypeSource.Row> rows)
    {
        var provisioner = As(ProvisionerRole, ProvisionerPassword, _database.Name);

        await using var source = new MasterDataRoomTypeSource(provisioner);
        await source.Database.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();

        source.RoomTypes.AddRange(rows);
        await source.SaveChangesAsync();

        await ExecuteAsync(
            provisioner,
            $"GRANT USAGE ON SCHEMA masterdata TO {_convention.AppRole}; "
            + $"GRANT SELECT ON masterdata.room_types TO {_convention.AppRole}");
    }

    private static async Task ExecuteAsync(string connection, string sql)
    {
        await using var open = new NpgsqlConnection(connection);
        await open.OpenAsync();
        await using var command = new NpgsqlCommand(sql, open);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Drop the database and this run's roles.</summary>
    /// <returns>When both are gone.</returns>
    public async ValueTask DisposeAsync() => await DiscardAsync(_convention, _database);
}
