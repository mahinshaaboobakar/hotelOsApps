using System.Text.RegularExpressions;
using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using Npgsql;

namespace HotelOS.Applications.TestSupport;

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
/// <b>Shared by the applications in this repository — ADR 0157.</b> Derived
/// from Jobs' <c>InstallerConvention</c>, which that ADR names as the working
/// model, and parameterised by schema. Unchanged in substance; the citations
/// below are corrected against the source, which had drifted.
/// </para>
/// <para>
/// <b>The source, cited so a drift is findable.</b> Every statement in
/// <see cref="SchemaStatements"/> is from
/// <c>services/kernel/crates/kernel/src/packages/database.rs:188-262</c>
/// (<c>PackageDatabase::statements</c>), in its order — which is the invariant
/// there and here: the owner must exist before the schema it authorises, and
/// the migrator grant must be in place before the application's own
/// <c>migrate</c> runs. Names follow
/// <c>packages/hotelos-package/src/naming.rs:124-125</c>.
/// </para>
/// <para>
/// <b>Why one parameter is enough, and is not merely convenient.</b> The
/// installer forms both role names from the package id <i>mapped</i> to a
/// PostgreSQL identifier, and <c>naming.rs:113-121</c> refuses any package
/// whose declared schema differs from that mapping — <c>SchemaNameMismatch</c>,
/// raised before any collision check. So on an installed property the schema
/// <i>is</i> the role suffix, by refusal rather than by convention, and a
/// fixture that took them separately could express a pair the platform would
/// not install.
/// </para>
/// <para>
/// <b>What is deliberately not recreated:</b> the connection-limit budget, the
/// generated password's provenance, and the uninstall path. Those are the
/// installer's behaviour rather than its output, and a test that reproduced
/// them would be testing the installer — which is the E2E suite's job. The
/// scratch database itself is <see cref="ScratchDatabase"/>'s, and ADR 0157
/// leaves it there.
/// </para>
/// </remarks>
/// <param name="schema">The schema this application's manifest declares.</param>
/// <param name="run">This run's id, which makes every cluster role its own.</param>
public sealed class InstallerConvention(string schema, string run)
{
    /// <summary>The schema this application's manifest declares.</summary>
    public string Schema { get; } = Identifier(schema);

    /// <summary>
    /// <c>NOLOGIN</c>. Owns the schema; migrations assume it — ADR 0029.
    /// </summary>
    /// <remarks>
    /// <b>Suffixed per run, and that is the whole point of this class taking a
    /// run id.</b> The installer's own name is <c>hotelos_owner_&lt;schema&gt;</c>,
    /// and a suite that took it stands exactly where a real install wants to
    /// stand: on 2026-09-05 the Kernel refused to install Jobs on the
    /// development machine with <i>"role hotelos_owner_jobs already exists;
    /// installing would take it over"</i>, because that suite had made it.
    /// Roles are cluster-wide and a scratch database is not, so the isolation
    /// the database gives has to be spelled into the names as well.
    /// </remarks>
    public string OwnerRole { get; } = $"hotelos_owner_{Identifier(schema)}_{Suffix(run)}";

    /// <summary><c>LOGIN</c>. What the running application connects as — this run's.</summary>
    public string AppRole { get; } = $"hotelos_app_{Identifier(schema)}_{Suffix(run)}";

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
    /// Idempotent, because a developer runs a suite more than once and roles are
    /// cluster-scoped rather than database-scoped — the scratch database goes
    /// away, these do not. The password is re-set on every run rather than only
    /// at creation, so a second run does not authenticate with the first run's.
    /// </para>
    /// <para>
    /// <b>It used to say this could not collide with an installed property</b>,
    /// on the grounds that ADR 0104 separates the installed cluster (15432)
    /// from the development one (25432). That is true of an installed
    /// <i>property</i> and false of the <i>development installation</i>, which
    /// runs its Kernel against 25432 — and on 2026-09-05 a real
    /// <c>package install</c> was refused because these roles were already
    /// there. The names are per run now, and <see cref="DropRolesAsync"/> takes
    /// them away again.
    /// </para>
    /// </remarks>
    public async Task EnsureRolesAsync(string adminConnection, string password)
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

        await ExecuteAsync(connection, $"ALTER ROLE {AppRole} PASSWORD '{Password(password)}'");

        // The two group roles an application is added to at install. They are
        // **cluster bootstrap**, not installer output — `02-roles.sql:227-263`
        // creates them — and a developer's cluster provisioned before
        // `AUTHZ-Q23` landed has `hotelos_masterdata_reader` and not
        // `hotelos_event_appender`, which is what that suite met.
        //
        // Reproduced from that file's own idempotent block, minus its
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

        foreach (var statement in SchemaStatements(databaseName))
        {
            await ExecuteAsync(connection, statement);
        }
    }

    /// <summary>Step 4's statements, in the one order that works.</summary>
    /// <remarks>
    /// <b>Public because the order is the contract.</b> The class exists to say
    /// what the installer produces, and separating that decision from running it
    /// is what lets the order be asserted without a cluster — a rule, in ADR
    /// 0054's split, rather than a connection. It is also the half two other
    /// applications are about to adopt, and an adoption that silently reordered
    /// these would be found at a property rather than here.
    /// </remarks>
    /// <param name="database">The database the application role must reach.</param>
    /// <returns>The statements.</returns>
    public IEnumerable<string> SchemaStatements(string database) =>
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
    /// anywhere cannot be dropped, and the grants a suite makes are in a
    /// database that is about to be dropped anyway.
    /// </para>
    /// <para>
    /// <b>CALL THIS AFTER DROPPING THE DATABASE, AND PASS NO
    /// <paramref name="databaseConnection"/>.</b> That is the shape a caller
    /// wants and it is not the obvious one, so it is written here rather than
    /// left to be rediscovered. Clearing the roles while the database still
    /// stands leaves <c>DROP ROLE</c> failing with <i>"cannot be dropped because
    /// some objects depend on it"</i>: <c>DROP OWNED</c> removes what a role
    /// owns and what it was granted, and the schema's default ACLs still name
    /// it. Dropping the database takes every per-database dependency with it,
    /// after which the roles go cleanly — which is why a leftover from an
    /// earlier run, whose database is long gone, drops by hand without
    /// complaint.
    /// </para>
    /// <para>
    /// The <paramref name="databaseConnection"/> path remains for the caller
    /// that must clear roles while a database is still standing. It re-grants
    /// both roles <c>WITH INHERIT TRUE</c> first, because
    /// <see cref="SchemaStatements"/> ends by revoking the provisioner's
    /// <c>SET</c> and because <c>DROP OWNED BY</c> requires the privileges
    /// <i>of</i> a role rather than the ability to assume it — the installer's
    /// own <c>INHERIT FALSE</c> form fails there with an identical-looking
    /// error.
    /// </para>
    /// </remarks>
    /// <param name="adminConnection">A connection to the cluster, as the provisioner.</param>
    /// <param name="databaseConnection">
    /// A connection to this run's database, as the provisioner — or null, which
    /// is the shape to prefer: see the remarks.
    /// </param>
    /// <returns>When both roles are gone, or when they could not be.</returns>
    public async Task DropRolesAsync(string adminConnection, string? databaseConnection)
    {
        RefuseInstalledProduct(adminConnection);

        try
        {
            // Null when the run failed before the database existed — which is
            // exactly the run whose roles are otherwise never cleaned up
            // (`INSTALL-Q88`). Nothing is owned in a database that was never
            // created, so the cluster-level drop below is sufficient on its own.
            if (databaseConnection is not null)
            {
                await using var database = new NpgsqlConnection(databaseConnection);
                await database.OpenAsync();

                // **Take back what step 4 handed in.** `SchemaStatements` ends
                // with `REVOKE SET OPTION FOR {OwnerRole} FROM CURRENT_USER`,
                // which is the installer's own last statement and is right: the
                // provisioner keeps ADMIN and loses the ability to *become* the
                // owner. But `DROP OWNED BY` requires the privileges of the role
                // whose objects are being dropped —
                //
                //     ERROR:  permission denied to drop objects
                //     DETAIL: Only roles with privileges of role "…" may drop
                //             objects owned by it.
                //
                // — so teardown has to re-assume it, which ADMIN permits and
                // which is what an uninstall does. Without this the drop failed
                // for every run and the catch below hid it: 86 roles on a
                // developer's cluster per passing run.
                //
                // **`INHERIT TRUE`, and that is the whole difference.** Step 4
                // grants `INHERIT FALSE` so the provisioner can *become* the
                // owner without wielding its privileges implicitly, which is
                // right for provisioning. `DROP OWNED BY` asks the opposite
                // question — it requires the privileges *of* the role, not the
                // ability to assume it — so a teardown that re-granted the
                // installer's own form failed with the identical error and
                // looked like the same defect twice.
                await ExecuteAsync(
                    database, $"GRANT {OwnerRole} TO CURRENT_USER WITH SET TRUE, INHERIT TRUE");
                await ExecuteAsync(
                    database, $"GRANT {AppRole} TO CURRENT_USER WITH SET TRUE, INHERIT TRUE");

                await ExecuteAsync(database, $"DROP OWNED BY {AppRole}, {OwnerRole} CASCADE");
            }

            await using var cluster = new NpgsqlConnection(adminConnection);
            await cluster.OpenAsync();
            await ExecuteAsync(cluster, $"DROP ROLE IF EXISTS {AppRole}");
            await ExecuteAsync(cluster, $"DROP ROLE IF EXISTS {OwnerRole}");
        }
        catch (PostgresException failure)
        {
            // **Best effort, and no longer silent.** A failure to tidy must not
            // fail a suite that passed — but ADR 0156 requires that a suite
            // which passes leaves no role behind, so a teardown that quietly
            // gave up turned a standing violation into something nobody could
            // see. It is reported where a test runner shows it, naming the roles
            // so a developer can remove them and the reason so the next person
            // fixes the cause rather than the symptom.
            await Console.Error.WriteLineAsync(
                $"could not remove this run's roles ({AppRole}, {OwnerRole}): "
                + $"{failure.MessageText}. They are cluster-wide and will remain on this "
                + "machine until dropped. ADR 0156 requires a passing suite to leave none.");
        }
    }

    /// <summary>
    /// The connection a migration runs on, as this run's owner.
    /// </summary>
    /// <remarks>
    /// <b>Not <see cref="ScratchDatabase.MigratorConnectionFor"/></b>, which
    /// derives the owner from the schema — <c>hotelos_owner_{schema}</c> — and
    /// so can only ever name the installer's role. The shape is the SDK's,
    /// verbatim: the role and the search path travel as libpq options, so the
    /// session has assumed the owner before the first statement and no
    /// migration can forget a <c>SET ROLE</c>.
    /// </remarks>
    /// <param name="database">This run's scratch database.</param>
    /// <returns>The connection string.</returns>
    public string MigratorConnectionFor(string database)
    {
        var target = ScratchDatabase.Target.Split(':');
        var password = Environment.GetEnvironmentVariable(SchemaMigration.PasswordVariable)
            ?? "devmigrator";

        return $"Host={target[0]};Port={target[1]};Database={database};Username={MigrationRole};"
            + $"Password={password};Options=-c role={OwnerRole} -c search_path={Schema};"
            + "Pooling=false;Include Error Detail=true";
    }

    /// <summary>The installed product's PostgreSQL, which no suite may write to.</summary>
    private const int InstalledProductPort = 15432;

    /// <summary>Refuse a connection that names a real property's cluster.</summary>
    /// <remarks>
    /// <para>
    /// <c>INSTALL-Q88</c>. The run suffix on these role names stops a
    /// <i>collision</i> with an installed property's; it does nothing about a
    /// suite writing to that property's cluster at all, and a pair per run
    /// accumulates there under names that read as the platform's own.
    /// </para>
    /// <para>
    /// <b>Parsed, not matched.</b> <see cref="NpgsqlConnectionStringBuilder"/>
    /// reports the port whatever the spelling or key order, and Npgsql's default
    /// when the string names none — so a connection assembled without an
    /// explicit port cannot silently be the product's.
    /// </para>
    /// </remarks>
    /// <param name="adminConnection">The connection about to be written through.</param>
    /// <exception cref="InvalidOperationException">It is the installed product's.</exception>
    private static void RefuseInstalledProduct(string adminConnection)
    {
        var port = new NpgsqlConnectionStringBuilder(adminConnection).Port;

        if (port != InstalledProductPort)
        {
            return;
        }

        throw new InvalidOperationException(
            $"this harness was asked to write cluster roles on port {InstalledProductPort}, "
            + "which is the INSTALLED product's PostgreSQL — a real property's database on "
            + "this machine. Point the suite at the development cluster (ADR 0104 "
            + "§E2E-Q5(a)); INSTALL-Q88 is the round this cost.");
    }

    /// <summary>A value that may be interpolated into an identifier position.</summary>
    /// <remarks>
    /// Every name below reaches SQL by interpolation, because an identifier
    /// cannot be a parameter. In Jobs' copy the schema was a <c>const</c> and
    /// the run id was the fixture's own; shared, both arrive from a caller, so
    /// the charset check the platform already performs on a declared schema —
    /// <c>naming.rs</c>'s <c>is_plain_identifier</c>, refusing as
    /// <c>UnsafeSchemaName</c> — is performed here for the same reason and
    /// before the value is ever concatenated.
    /// </remarks>
    /// <param name="value">The proposed identifier.</param>
    /// <returns>It, unchanged.</returns>
    /// <exception cref="ArgumentException">It is not a plain identifier.</exception>
    private static string Identifier(string value)
        => Regex.IsMatch(value, "^[a-z][a-z0-9_]{0,46}$")
            ? value
            : throw new ArgumentException(
                $"'{value}' is not a plain PostgreSQL identifier, and every name here reaches "
                + "SQL by interpolation because an identifier cannot be a parameter. The "
                + "platform refuses the same shape at install as UnsafeSchemaName.",
                nameof(value));

    /// <summary>A value appended to a name that already begins with a letter.</summary>
    /// <remarks>
    /// <b>Not <see cref="Identifier"/>, and the difference is not cosmetic.</b> A
    /// run id is a suffix of <c>hotelos_owner_…</c>, so it never stands at the
    /// front and must not be required to begin with a letter. Jobs' fixture
    /// forms one as <c>Guid.NewGuid().ToString("n")[..8]</c> — eight hex
    /// characters, six of whose sixteen possible first characters are digits —
    /// so requiring a leading letter here would have failed roughly three runs
    /// in eight, at random, in code two other applications are adopting. The
    /// charset is still checked, because this reaches SQL the same way.
    /// </remarks>
    /// <param name="value">The proposed suffix.</param>
    /// <returns>It, unchanged.</returns>
    /// <exception cref="ArgumentException">It is not safe in an identifier.</exception>
    private static string Suffix(string value)
        => Regex.IsMatch(value, "^[a-z0-9_]{1,46}$")
            ? value
            : throw new ArgumentException(
                $"'{value}' cannot be part of a PostgreSQL identifier. It is interpolated into "
                + "a role name, which cannot be a parameter.",
                nameof(value));

    /// <summary>A password that cannot end the literal it is written into.</summary>
    /// <param name="value">The proposed password.</param>
    /// <returns>It, with single quotes doubled.</returns>
    private static string Password(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
