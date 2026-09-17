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

    /// <summary>
    /// What distinguishes THIS run's cluster roles from every other one's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Required, not a precaution.</b> <c>CLAUDE.md</c>: <i>tests touching
    /// shared machine state — a CNG key container, a certificate-store entry, a
    /// database row — must name that state per test.</i> A cluster-scoped role
    /// is shared machine state more thoroughly than a row is: it outlives the
    /// scratch database entirely, so a fixed name collides on two axes at once —
    /// with a parallel run, and with a real installation.
    /// </para>
    /// <para>
    /// <b>This file argued the other way and was wrong.</b> Its case was that
    /// <i>a role invented for the tests would be a role no property has</i>, and
    /// that the suite would then characterise against something that does not
    /// exist. What the suite proves is the SHAPE of the installer's step 4 —
    /// which role owns the schema, which connects, which grants are in place —
    /// and the shape is unchanged by a suffix. What the fixed name bought was a
    /// resemblance; what it cost was a real property's application role, reset
    /// to a test password while the Kernel's sealed secret held the real one.
    /// </para>
    /// <para>
    /// Computed once per process, so every test in a run shares one pair and a
    /// second run on the same cluster shares nothing with it.
    /// </para>
    /// </remarks>
    private static readonly string Run = Guid.NewGuid().ToString("n")[..8];

    /// <summary><c>NOLOGIN</c>. Owns the schema; migrations assume it — ADR 0029.</summary>
    public static readonly string OwnerRole = $"hotelos_owner_workforce_{Run}";

    /// <summary><c>LOGIN</c>. What the running application connects as.</summary>
    public static readonly string AppRole = $"hotelos_app_workforce_{Run}";

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
    public static async Task<Provisioned> EnsureRolesAsync(
        string adminConnection, string password)
    {
        await RefuseInstallationAsync(adminConnection);

        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();

        // **Created with its password, never ALTERed into one.** This ran an
        // unconditional ALTER ROLE after an idempotent create - so on a cluster
        // that already held an installation it reset a REAL property's
        // application role to a test value, while the Kernel's sealed secret
        // went on holding the real one. That is a 28P01 at the next start of an
        // application this suite never touched, and it cost a day to trace.
        //
        // The password now exists only on the CREATE, which the role's absence
        // gates. A role this fixture did not create is one whose password it
        // does not know and must not decide.
        var owner = await CreateIfAbsentAsync(
            connection, OwnerRole, $"CREATE ROLE {OwnerRole} NOLOGIN");

        var app = await CreateIfAbsentAsync(
            connection, AppRole, $"CREATE ROLE {AppRole} LOGIN PASSWORD '{password}'");

        // Found rather than created, so somebody else owns it - an installation,
        // or a previous run that never reached its teardown. The suite cannot
        // connect as it either way, and saying so here beats a 28P01 from three
        // layers down that reads like a broken test rather than a dirty cluster.
        if (!app)
        {
            throw new InvalidOperationException(
                $"the role {AppRole} already exists on this cluster, so this harness did "
                + "not create it and does not know its password. It will NOT reset it: "
                + "resetting it is what put a real property's application role out of "
                + "step with the Kernel's sealed secret and produced 28P01 at the next "
                + "start of an application this suite never ran against. Either the "
                + "cluster holds an installation, or a previous run left the role "
                + $"behind - DROP ROLE {AppRole}; as a superuser clears the second, and "
                + "the first is not this suite's cluster to use.");
        }

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

        // The two group roles above are left out deliberately: they are cluster
        // bootstrap that `02-roles.sql` owns, shared by every application, and
        // dropping them on one suite's teardown would take them from the others.
        return new Provisioned(app, owner);
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
    private static async Task RefuseInstallationAsync(string adminConnection)
    {
        var port = new NpgsqlConnectionStringBuilder(adminConnection).Port;

        // **The port first, without dialling anything.** It was never wrong,
        // only insufficient — and it is the half that still works on a cluster
        // this harness cannot reach. A guard that had to connect in order to
        // refuse would be unable to refuse a refused connection, and would dial
        // the installed product in the course of deciding not to touch it.
        if (port == InstalledProductPort)
        {
            throw new InvalidOperationException(
                $"port {InstalledProductPort} is the INSTALLED product's PostgreSQL — a "
                + "real property's database on this machine. This harness creates roles "
                + "under the installer's own names, so they collide with that property's "
                + "by construction and survive every teardown, roles being cluster-scoped "
                + "(ADR 0104 §E2E-Q5(a); INSTALL-Q88 is the round this cost the first "
                + "time).");
        }

        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT FROM pg_database WHERE datname = 'hotelos')", connection);

        // **Keyed on what the cluster HOLDS, not on which port it answers.**
        // This compared against 15432 and returned early for anything else - so
        // it allowed 25432, and the owner drives the development stack as their
        // product: desktop to 25051 to the dev Kernel to 25432. The port and the
        // hazard coincided only while a development machine had no property on
        // it, and stopped coinciding the day one did.
        //
        // The guard's old message named the hazard exactly right and then
        // pointed the suite at the cluster where it was about to happen.
        //
        // **And its stated reason has since expired, which is recorded rather
        // than quietly replaced.** It said: *this harness creates roles under the
        // INSTALLER's own names, so they collide with that property's by
        // construction.* Since the roles became run-suffixed that is false - the
        // names are unique per run, cannot collide with a property's, and are
        // dropped by a teardown gated on having created them.
        //
        // A justification that decays into an argument for REMOVING the thing it
        // justifies is its own hazard: a reader meeting a dead reason concludes
        // the constraint expired. So the reason is corrected here and the
        // refusal is kept, because whether it should still refuse is a decision
        // and not a repair. What survives it is the PORT check above, whose
        // reason is untouched: 15432 is a real property's data whatever this
        // harness names its roles.
        if (await command.ExecuteScalarAsync() is not true)
        {
            return;
        }

        throw new InvalidOperationException(
            $"the cluster on port {port} holds a HotelOS installation - it has a hotelos "
            + "database, and this harness creates roles and a scratch database on the "
            + "cluster it is pointed at. Point the suite at a cluster with no "
            + "installation (ADR 0104 E2E-Q5(a)); INSTALL-Q88 is the round this cost the "
            + "first time. NOTE: this refusal's original reason no longer holds - see the "
            + "remark at RefuseInstallationAsync - and it is retained pending a ruling "
            + "rather than because that reason still stands.");
    }

    /// <summary>Create a role, or report that somebody else already had.</summary>
    /// <param name="connection">An open admin connection.</param>
    /// <param name="role">The role name.</param>
    /// <param name="create">The <c>CREATE ROLE</c> to run when it is absent.</param>
    /// <returns>Whether THIS call created it.</returns>
    /// <remarks>
    /// Two statements rather than one idempotent <c>DO</c> block, because the
    /// answer is the point: a block that creates-if-absent cannot tell its caller
    /// which happened, and both dangerous operations here turn on that
    /// distinction. PostgreSQL has no <c>CREATE ROLE IF NOT EXISTS</c>, and
    /// catching 42710 would also swallow a genuine permission failure.
    /// </remarks>
    private static async Task<bool> CreateIfAbsentAsync(
        NpgsqlConnection connection, string role, string create)
    {
        await using var exists = new NpgsqlCommand(
            $"SELECT EXISTS (SELECT FROM pg_roles WHERE rolname = '{role}')", connection);

        if (await exists.ExecuteScalarAsync() is true)
        {
            return false;
        }

        await ExecuteAsync(connection, create);
        return true;
    }

    /// <summary>Which of the two cluster roles this run brought into being.</summary>
    /// <param name="AppRole">Whether this run created the application role.</param>
    /// <param name="OwnerRole">Whether this run created the owner role.</param>
    /// <remarks>
    /// Carried from provisioning to teardown rather than re-derived there: by the
    /// time the suite finishes, <i>does this role exist</i> no longer
    /// distinguishes <i>mine</i> from <i>an installation's</i>, and that is
    /// exactly the question the drop has to answer.
    /// </remarks>
    public sealed record Provisioned(bool AppRole, bool OwnerRole);

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
    public static async Task DropRolesAsync(string adminConnection, Provisioned created)
    {
        await RefuseInstallationAsync(adminConnection);

        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();

        // **Only what this run created.** Roles are cluster-scoped and the
        // scratch database is not, so a teardown that dropped by NAME would take
        // an installation's role with it - and DROP OWNED BY first, which is
        // what makes the drop reliable, would take what that role owns as well.
        // Sixteen tables, in the case this was measured against.
        var mine = new (string Role, bool Created)[]
        {
            (AppRole, created.AppRole),
            (OwnerRole, created.OwnerRole),
        };

        foreach (var (role, _) in mine.Where(one => one.Created))
        {
            await DropAsync(connection, role);
        }
    }

    /// <summary>Drop one role, and what it owns in this database.</summary>
    private static async Task DropAsync(NpgsqlConnection connection, string role)
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

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
