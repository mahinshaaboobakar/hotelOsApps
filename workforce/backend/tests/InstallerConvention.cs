using Npgsql;

namespace HotelOS.Workforce.Tests;

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
public static class InstallerConvention
{
    /// <summary>The schema this application's manifest declares.</summary>
    public const string Schema = "workforce";

    /// <summary><c>NOLOGIN</c>. Owns the schema; migrations assume it — ADR 0029.</summary>
    public const string OwnerRole = "hotelos_owner_workforce";

    /// <summary><c>LOGIN</c>. What the running application connects as.</summary>
    public const string AppRole = "hotelos_app_workforce";

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
    /// <b>It could collide with an installed property, and this is where that
    /// is stopped.</b> This paragraph used to assert the opposite — <i>"the
    /// development cluster is on 25432 and the installed product's on 15432,
    /// and they are separate clusters"</i> — which is true of the ports and
    /// says nothing about which one a suite was pointed at.
    /// <c>HOTELOS_TEST_DB_PORT</c> chooses, and two roles under the installer's
    /// own names were created on a real property's cluster
    /// (<c>INSTALL-Q88</c>). The refusal below is that sentence made
    /// enforceable.
    /// </para>
    /// <para>
    /// <b>Checked here as well as at the port</b>, because this method opens a
    /// <i>superuser</i> connection and writes cluster-scoped roles. It takes a
    /// connection string from its caller, so it must not depend on that caller
    /// having been careful: the guard belongs on the dangerous operation, not
    /// only on the value that usually feeds it.
    /// </para>
    /// </remarks>
    public static async Task EnsureRolesAsync(string adminConnection, string password)
    {
        RefuseInstalledProduct(adminConnection);

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
    public static async Task ProvisionSchemaAsync(string databaseConnection, string databaseName)
    {
        await using var connection = new NpgsqlConnection(databaseConnection);
        await connection.OpenAsync();

        foreach (var statement in Statements(databaseName))
        {
            await ExecuteAsync(connection, statement);
        }
    }

    /// <summary>Step 4's statements, in the one order that works.</summary>
    /// <param name="database">The database the application role must reach.</param>
    /// <returns>The statements.</returns>
    private static IEnumerable<string> Statements(string database) =>
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

    /// <summary>The installed product's PostgreSQL, which no suite may write to.</summary>
    private const int InstalledProductPort = 15432;

    /// <summary>Refuse a connection that names a real property's cluster.</summary>
    /// <remarks>
    /// <para>
    /// <b>Parsed, not matched.</b> <c>NpgsqlConnectionStringBuilder</c> reports
    /// the port whatever the string's spelling or key order, and reports
    /// Npgsql's default when the string names none — so a connection assembled
    /// without an explicit port cannot silently be the product's.
    /// </para>
    /// </remarks>
    private static void RefuseInstalledProduct(string adminConnection)
    {
        var port = new NpgsqlConnectionStringBuilder(adminConnection).Port;

        if (port != InstalledProductPort)
        {
            return;
        }

        throw new InvalidOperationException(
            $"this harness was asked to create cluster roles on port {InstalledProductPort}, "
            + "which is the INSTALLED product's PostgreSQL — a real property's database on "
            + "this machine. It creates roles under the installer's own names, so they would "
            + "collide with that property's and survive every teardown, roles being "
            + "cluster-scoped. Point the suite at the development cluster (ADR 0104 "
            + "§E2E-Q5(a)); INSTALL-Q88 is the round this cost.");
    }

    /// <summary>
    /// Remove the two roles this convention created.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Roles are cluster objects and the scratch database is not</b>, so
    /// dropping the database was never cleanup for them — every run left them
    /// behind, and a run that failed before the database existed left them with
    /// nothing at all to show it had run.
    /// </para>
    /// <para>
    /// <b>Only this application's pair.</b> The shared names —
    /// <c>hotelos_masterdata_reader</c>, <c>hotelos_event_appender</c> — belong
    /// to the cluster and to every other suite on it; dropping those would make
    /// this harness's teardown somebody else's failure.
    /// </para>
    /// <para>
    /// <c>DROP OWNED BY</c> first: it revokes the grants held in this database
    /// and drops what the role owns here, which is what makes the drop
    /// reliable rather than a dependency error at the end of a green run.
    /// </para>
    /// </remarks>
    public static async Task DropRolesAsync(string adminConnection)
    {
        RefuseInstalledProduct(adminConnection);

        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();

        foreach (var role in new[] { AppRole, OwnerRole })
        {
            await ExecuteAsync(
                connection,
                $"""
                 DO $$
                 BEGIN
                     IF EXISTS (SELECT FROM pg_roles WHERE rolname = '{role}') THEN
                         DROP OWNED BY {role};
                         DROP ROLE {role};
                     END IF;
                 END
                 $$;
                 """);
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
