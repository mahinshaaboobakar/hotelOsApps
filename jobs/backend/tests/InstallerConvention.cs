using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using Npgsql;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// The installer's step 4, recreated to specification for a scratch database.
/// </summary>
/// <remarks>
/// <para>
/// <b>Derived, never invented</b> — the ruling on <c>F7</c>, 2026-08-31. A role
/// this fixture mints to its own taste would be a role no property has, and the
/// suite would then characterise against something that does not exist. A role
/// <i>derived from the installer's convention</i> is the installer's, and drift
/// between the two is caught where ADR 0054 puts connections: the install-chain
/// E2E, not here.
/// </para>
/// <para>
/// The precedent is <c>tests/mtls_fixtures/mod.rs:181</c>, which recreates an
/// installer-owned artifact to specification for the same reason.
/// </para>
/// <para>
/// <b>The source, cited so a drift is findable.</b> Every statement below is
/// from <c>services/kernel/crates/kernel/src/packages/database.rs:180-256</c>
/// (<c>PackageDatabase::statements</c>), in its order — which is the invariant
/// there and here: the owner must exist before the schema it authorises, and the
/// migrator grant must be in place before the application's own <c>migrate</c>
/// runs. Names follow <c>kernel-core/src/package/naming.rs:45-48</c> —
/// <c>hotelos_owner_</c> and <c>hotelos_app_</c>.
/// </para>
/// <para>
/// <b>What is deliberately not recreated:</b> the connection-limit budget, the
/// generated password's provenance, and the uninstall path. Those are the
/// installer's behaviour rather than its output, and a test that reproduced them
/// would be testing the installer — which is the E2E suite's job.
/// </para>
/// </remarks>
public sealed class InstallerConvention(string run)
{
    /// <summary>The schema this application's manifest declares.</summary>
    public const string Schema = "jobs";

    /// <summary>
    /// <c>NOLOGIN</c>. Owns the schema; migrations assume it — ADR 0029.
    /// </summary>
    /// <remarks>
    /// <b>Suffixed per run, and that is the whole point of this class taking a
    /// run id.</b> The installer's own name is <c>hotelos_owner_jobs</c>, and a
    /// suite that took it stands exactly where a real install wants to stand:
    /// on 2026-09-05 the Kernel refused to install Jobs on the development
    /// machine with <i>"role hotelos_owner_jobs already exists; installing
    /// would take it over"</i>, because this suite had made it. Roles are
    /// cluster-wide and a scratch database is not, so the isolation the
    /// database gives has to be spelled into the names as well.
    /// </remarks>
    public string OwnerRole { get; } = $"hotelos_owner_jobs_{run}";

    /// <summary><c>LOGIN</c>. What the running application connects as — this run's.</summary>
    public string AppRole { get; } = $"hotelos_app_jobs_{run}";

    /// <summary>The role a migration runs as — <c>store/mod.rs:53</c>.</summary>
    public const string MigrationRole = "hotelos_migrator";

    /// <summary>The read window every application gets and none may write.</summary>
    public const string MasterDataReader = "hotelos_masterdata_reader";

    /// <summary>The ability to announce what it did — <c>AUTHZ-Q23</c>.</summary>
    public const string EventAppender = "hotelos_event_appender";

    /// <summary>
    /// Create the two cluster roles, as the installer would.
    /// </summary>
    /// <param name="adminConnection">A connection to the cluster, as the provisioner.</param>
    /// <param name="password">This run's password for the application role.</param>
    /// <returns>When both roles exist and the password is current.</returns>
    /// <remarks>
    /// <para>
    /// Idempotent, because a developer runs this suite more than once and roles
    /// are cluster-scoped rather than database-scoped — the scratch database goes
    /// away, these do not. The password is re-set on every run rather than only
    /// at creation, so a second run does not authenticate with the first run's.
    /// </para>
    /// <para>
    /// <b>It used to say this could not collide with an installed property</b>,
    /// on the grounds that ADR 0104 separates the installed cluster (15432) from
    /// the development one (25432). That is true of an installed <i>property</i>
    /// and false of the <i>development installation</i>, which runs its Kernel
    /// against 25432 — and on 2026-09-05 a real <c>package install</c> was
    /// refused because these roles were already there. The names are per run
    /// now, and <see cref="DropRolesAsync"/> takes them away again.
    /// </para>
    /// </remarks>
    public async Task EnsureRolesAsync(string adminConnection, string password)
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();

        // PostgreSQL has no `CREATE ROLE IF NOT EXISTS`, and the alternative —
        // catching 42710 — would also swallow a genuine permission failure.
        await ExecuteAsync(
            connection,
            $"""
             DO $$
             BEGIN
                 IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{OwnerRole}') THEN
                     CREATE ROLE {OwnerRole} NOLOGIN;
                 END IF;
                 IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{AppRole}') THEN
                     CREATE ROLE {AppRole} LOGIN;
                 END IF;
             END
             $$;
             """);

        await ExecuteAsync(connection, $"ALTER ROLE {AppRole} PASSWORD '{password}'");

        // The two group roles an application is added to at install. They are
        // **cluster bootstrap**, not installer output — `02-roles.sql:227-263`
        // creates them — and a developer's cluster provisioned before
        // `AUTHZ-Q23` landed has `hotelos_masterdata_reader` and not
        // `hotelos_event_appender`, which is what this suite met.
        //
        // Reproduced verbatim from that file's own idempotent block, minus its
        // `GRANT … TO hotelos_provisioner`: the provisioner is the platform's
        // role and this fixture is not it. The alternative was to require every
        // developer to re-run `make db-bootstrap` before an application's suite
        // would start, which makes a stale cluster look like a broken test.
        await ExecuteAsync(
            connection,
            $"""
             DO $$
             BEGIN
                 IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{MasterDataReader}') THEN
                     CREATE ROLE {MasterDataReader} NOLOGIN;
                 END IF;
                 IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{EventAppender}') THEN
                     CREATE ROLE {EventAppender} NOLOGIN;
                 END IF;
             END
             $$;
             """);
    }

    /// <summary>
    /// Create the schema and grant it, in the installer's order.
    /// </summary>
    /// <param name="databaseConnection">A connection to the scratch database, as the provisioner.</param>
    /// <param name="databaseName">That database, for the CONNECT grant.</param>
    /// <returns>When the application role can reach its schema.</returns>
    public async Task ProvisionSchemaAsync(string databaseConnection, string databaseName)
    {
        await using var connection = new NpgsqlConnection(databaseConnection);
        await connection.OpenAsync();

        foreach (var statement in Statements(databaseName))
        {
            await ExecuteAsync(connection, statement);
        }
    }

    /// <summary>
    /// The platform's own event store, on a scratch database — what an
    /// installed application cannot provision and must not invent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>event_store</c> belongs to the Kernel: <c>03-schemas.sql</c> creates
    /// it and the Kernel's own migrations fill it, and neither is something a
    /// package runs. The wired round nevertheless has to write events — a job
    /// that is raised announces one in the same transaction — so this runs
    /// <b>the Kernel's own migration text, read from the platform checkout</b>,
    /// rather than a table shaped like it. A hand-written copy would be a
    /// second definition of the platform's most shared table, and the round
    /// would be proving Jobs against a schema no property has.
    /// </para>
    /// <para>
    /// The event appender role is the one the application already holds by
    /// <c>AUTHZ-Q23</c>; what it gains here is the schema to use it on.
    /// </para>
    /// </remarks>
    /// <param name="databaseConnection">The provisioner's connection to the scratch database.</param>
    /// <exception cref="InvalidOperationException">The platform checkout was not found.</exception>
    public static async Task ProvisionEventStoreAsync(string databaseConnection)
    {
        var migrations = PlatformMigrations();
        await using var connection = new NpgsqlConnection(databaseConnection);
        await connection.OpenAsync();

        await ExecuteAsync(connection, "CREATE SCHEMA IF NOT EXISTS event_store");

        foreach (var file in migrations.GetFiles("*.sql").OrderBy(f => f.Name))
        {
            await ExecuteAsync(connection, await File.ReadAllTextAsync(file.FullName));
        }

        foreach (var statement in new[]
        {
            $"GRANT USAGE ON SCHEMA event_store TO {EventAppender}",
            $"GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA event_store TO {EventAppender}",
        })
        {
            await ExecuteAsync(connection, statement);
        }
    }

    /// <summary>The Kernel's event-store migrations, found by walking up to the platform checkout.</summary>
    private static DirectoryInfo PlatformMigrations()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = new DirectoryInfo(Path.Combine(
                directory.FullName, "HosPilotOS", "services", "kernel", "crates", "kernel",
                "migrations", "event_store"));
            if (candidate.Exists) return candidate;
            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "the platform checkout was not found beside this one, so the Kernel's event-store "
            + "migrations cannot be read. The wired round needs them: an application may not "
            + "invent event_store, and a suite that substituted a table shaped like it would be "
            + "proving Jobs against a schema no property has.");
    }

    /// <summary>Step 4's statements, in the one order that works.</summary>
    /// <param name="database">The database the application role must reach.</param>
    /// <returns>The statements.</returns>
    private IEnumerable<string> Statements(string database) =>
    [
        // PostgreSQL 16 grants a CREATEROLE role ADMIN on the roles it creates
        // but **not SET**, and `CREATE SCHEMA … AUTHORIZATION` requires the
        // creator be able to SET ROLE to the owner. `INHERIT FALSE` so the
        // provisioner wields none of the owner's privileges implicitly.
        $"GRANT {OwnerRole} TO CURRENT_USER WITH SET TRUE, INHERIT FALSE",
        $"CREATE SCHEMA {Schema} AUTHORIZATION {OwnerRole}",
        $"GRANT {OwnerRole} TO {MigrationRole}",
        $"GRANT CONNECT ON DATABASE \"{database}\" TO {AppRole}",

        // Everything schema-scoped is the owner's to grant, so it is granted as
        // the owner — the provisioner created the schema but does not own it.
        $"SET ROLE {OwnerRole}",
        $"GRANT USAGE ON SCHEMA {Schema} TO {AppRole}",
        $"GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {Schema} TO {AppRole}",
        $"ALTER DEFAULT PRIVILEGES FOR ROLE {OwnerRole} IN SCHEMA {Schema} "
            + $"GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {AppRole}",
        $"ALTER DEFAULT PRIVILEGES FOR ROLE {OwnerRole} IN SCHEMA {Schema} "
            + $"GRANT USAGE, SELECT ON SEQUENCES TO {AppRole}",
        "RESET ROLE",

        // The read window, and the ability to announce what it did. The two
        // grants an application holds on somebody else's schema, handed out the
        // same way — `AUTHZ-Q23`.
        $"GRANT {MasterDataReader} TO {AppRole}",
        $"GRANT {EventAppender} TO {AppRole}",

        // Handed back last: the provisioner keeps ADMIN, which is what DROP ROLE
        // needs at uninstall, and loses the ability to *become* the owner.
        $"REVOKE SET OPTION FOR {OwnerRole} FROM CURRENT_USER",
    ];

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
    /// <summary>
    /// Take this run's roles away again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A killed run leaves them, which is why the names carry the run id: a
    /// leftover is identifiable, harmless, and — unlike the installer's own
    /// names — in nobody's way. Dropping is best-effort for the same reason.
    /// </para>
    /// <para>
    /// <c>DROP OWNED</c> first, because a role that has been granted anything
    /// anywhere cannot be dropped, and the grants this suite made are in a
    /// database that is about to be dropped anyway.
    /// </para>
    /// </remarks>
    /// <param name="adminConnection">A connection to the cluster, as the provisioner.</param>
    /// <param name="databaseConnection">A connection to this run's database, as the provisioner.</param>
    /// <returns>When both roles are gone, or when they could not be.</returns>
    public async Task DropRolesAsync(string adminConnection, string databaseConnection)
    {
        try
        {
            await using (var database = new NpgsqlConnection(databaseConnection))
            {
                await database.OpenAsync();
                await ExecuteAsync(database, $"DROP OWNED BY {AppRole}, {OwnerRole} CASCADE");
            }

            await using var cluster = new NpgsqlConnection(adminConnection);
            await cluster.OpenAsync();
            await ExecuteAsync(cluster, $"DROP ROLE IF EXISTS {AppRole}");
            await ExecuteAsync(cluster, $"DROP ROLE IF EXISTS {OwnerRole}");
        }
        catch (PostgresException)
        {
            // Best effort. The run id in the name is what makes a leftover
            // findable, and a failure to tidy must not fail a suite that passed.
        }
    }

    /// <summary>
    /// The connection a migration runs on, as this run's owner.
    /// </summary>
    /// <remarks>
    /// <b>Not <c>ScratchDatabase.MigratorConnectionFor</c></b>, which derives the
    /// owner from the schema — <c>hotelos_owner_{schema}</c> — and so can only
    /// ever name the installer's role. The shape is the SDK's, verbatim: the
    /// role and the search path travel as libpq options, so the session has
    /// assumed the owner before the first statement and no migration can forget
    /// a <c>SET ROLE</c>.
    /// </remarks>
    /// <param name="database">This run's scratch database.</param>
    /// <returns>The connection string.</returns>
    public string MigratorConnectionFor(string database)
    {
        var host = ScratchDatabase.Target.Split(':')[0];
        var port = ScratchDatabase.Target.Split(':')[1];
        var password = Environment.GetEnvironmentVariable(SchemaMigration.PasswordVariable) ?? "devmigrator";

        return $"Host={host};Port={port};Database={database};Username={MigrationRole};"
            + $"Password={password};Options=-c role={OwnerRole} -c search_path={Schema};"
            + "Pooling=false;Include Error Detail=true";
    }
}
