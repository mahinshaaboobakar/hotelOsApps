using Npgsql;
using HotelOS.Platform;
using HotelOS.Applications.TestSupport;
using HotelOS.Platform.TestSupport;
using HotelOS.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// One migrated scratch database, shared by the suite.
/// </summary>
/// <remarks>
/// <para>
/// Shared because migrating a schema costs seconds and characterising an
/// operation costs milliseconds. Tests stay independent by writing rows of their
/// own with fresh ids rather than by rebuilding the database.
/// </para>
/// <para>
/// <b>An absent PostgreSQL fails the run</b> — ADR 0053, and the diagnostic
/// carries the remedy, because a developer meeting this for the first time is a
/// developer who does not yet know the suite needs a database. Present-but-broken
/// throws out of <see cref="InitializeAsync"/> for the same reason: a harness
/// that cannot migrate is a defect, and reporting it as "skipped" is how a suite
/// that never worked stays hidden behind a green run.
/// </para>
/// <para>
/// <b>Never the installed product's cluster</b> — ADR 0104. The harness reaches
/// the development PostgreSQL, and a suite pointed at 15432 would run against a
/// real property's database.
/// </para>
/// </remarks>
public sealed class JobsFixture : IAsyncLifetime
{
    /// <summary>This run's convention — the installer's shape, under this run's names.</summary>
    /// <remarks>
    /// The suffix is the same one the scratch database carries, so everything a
    /// run leaves behind is identifiable as that run's, and nothing it leaves
    /// behind is standing where a real install wants to stand.
    /// </remarks>
    private readonly InstallerConvention _convention = new(JobsDbContext.Schema, Run);

    /// <summary>What names this run's roles and its database alike.</summary>
    private static string Run => Guid.NewGuid().ToString("n")[..8];

    /// <summary>The role this application's runtime connects as.</summary>
    private string ApplicationRole => _convention.AppRole;

    /// <summary>The provisioner, which is also the harness's admin role.</summary>
    /// <remarks>
    /// Declared in the spec so the fixture can reach the scratch database as the
    /// role that creates the schema — the installer's step 4 runs as the
    /// provisioner, not as the application.
    /// </remarks>
    private const string ProvisionerRole = "hotelos_test";

    /// <summary>The cluster's own provisioner — the role the installer uses.</summary>
    /// <remarks>
    /// <para>
    /// <b>This connected as <c>postgres</c>/<c>devroot</c>, which ADR 0156
    /// rejects by name</b>, and the justification written above it was false:
    /// it said <i>"a developer's cluster has no such role"</i>, while
    /// <c>hotelos_provisioner</c> — <c>LOGIN CREATEROLE</c> — has been in
    /// <c>deployment/database/02-roles.sql:169</c> since the initial commit of
    /// 2026-08-24. Verified against this cluster rather than the file:
    /// <c>rolcreaterole</c> is true for it and false for <c>hotelos_test</c>,
    /// which is the half the old comment got right.
    /// </para>
    /// <para>
    /// The comment was last edited on 2026-09-09 and nobody re-checked the claim
    /// under it. A superuser credential in a suite is the kind of thing that
    /// survives on a sentence nobody re-reads, which is why the sentence is
    /// replaced by a measurement here.
    /// </para>
    /// <para>
    /// <b>The convention is unchanged and was never the problem</b>: what gets
    /// created, in what order, with which grants, is
    /// <see cref="InstallerConvention"/>'s and therefore the installer's. Only
    /// who runs it changes, and it now matches the installer exactly — step 4
    /// runs as the provisioner, and so does this (FF's shape, <c>698cab8</c>).
    /// </para>
    /// </remarks>
    private static string ProvisionerConnection(string database) =>
        $"Host={Host};Port={Port};Database={database};Username={ClusterProvisioner};"
        + $"Password={ProvisionerPassword};Pooling=false;Include Error Detail=true";

    /// <summary>The role the platform ships for this work — never a superuser.</summary>
    private const string ClusterProvisioner = "hotelos_provisioner";

    /// <summary>Its password, overridable for a cluster that sets another.</summary>
    private static string ProvisionerPassword =>
        Environment.GetEnvironmentVariable("HOTELOS_PROVISIONER_PASSWORD") ?? "devprovisioner";

    private static string Host => ScratchDatabase.Target.Split(':')[0];

    private static string Port => ScratchDatabase.Target.Split(':')[1];

    private readonly string _password = $"jobs{Guid.NewGuid():N}"[..24];

    private ScratchDatabase? _database;

    /// <summary>The property every posting in this suite belongs to.</summary>
    public Guid PropertyId { get; private set; }

    /// <summary>Provision and migrate, leaving nothing behind if either fails.</summary>
    /// <remarks>
    /// xUnit never calls <see cref="DisposeAsync"/> for a fixture whose
    /// initialisation threw, so the guard is here rather than trusted to the
    /// harness — Identity's suite leaked a scratch database this way before
    /// either fixture guarded it.
    /// </remarks>
    public async Task InitializeAsync()
    {
        try
        {
            await PrepareAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    /// <summary>One statement, as whoever holds the authority for it.</summary>
    private static async Task GrantAsync(string connection, string sql)
    {
        await using var open = new Npgsql.NpgsqlConnection(connection);
        await open.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(sql, open);
        await command.ExecuteNonQueryAsync();
    }

    private async Task PrepareAsync()
    {
        // The cluster roles first: they are cluster-scoped, so they must exist
        // before the scratch database can grant either of them CONNECT.
        await _convention.EnsureRolesAsync(
            ProvisionerConnection("postgres"), _password);

        _database = await ScratchDatabase.CreateAsync(
            new ScratchDatabaseSpec(
                NamePrefix: "hotelos_jobs_test",
                Roles: new Dictionary<string, string>
                {
                    [ApplicationRole] = _password,
                    [ProvisionerRole] = "devtest",
                    [ClusterProvisioner] = ProvisionerPassword,
                }));

        if (_database is null)
        {
            throw new InvalidOperationException(
                "Jobs's characterisation tests require PostgreSQL and could not reach it "
                + $"at {ScratchDatabase.Target}. Run `make db-up db-test-role` in the platform "
                + "checkout and try again. This is a failure rather than a skip: a suite that "
                + "passes without a database reports success having executed nothing. "
                + $"NOTE: the cluster roles {_convention.OwnerRole} and {_convention.AppRole} "
                + $"were already created on {ScratchDatabase.Target} before this check — they "
                + "are cluster-scoped and must exist before a database can grant them CONNECT. "
                + "Teardown drops them; if the process died instead, they are still there. "
                + "This message used to say only that the database was unreachable, which read "
                + "as nothing happened (INSTALL-Q88).");
        }

        // Not `ScratchDatabase.CreateSchemaAsync`: that creates the schema
        // `AUTHORIZATION hotelos_owner_<schema>` and assumes the role already
        // exists, which is true for a platform service and false for an
        // installed application. This runs the installer's step 4 instead —
        // F7, ruled 2026-08-31.
        // **The scratch database is `hotelos_test`'s, so `hotelos_test` is who can
        // let the provisioner work in it.** The platform grants the provisioner
        // CREATE on the platform's own database and knows nothing about this
        // one; asking this database's owner is the narrowest way to close that
        // and needs no privilege either role lacks. FF's shape, 698cab8.
        await GrantAsync(
            $"Host={Host};Port={Port};Database=postgres;Username={ProvisionerRole};Password=devtest",
            $"GRANT CREATE ON DATABASE \"{_database.Name}\" TO {ClusterProvisioner}");

        await _convention.ProvisionSchemaAsync(
            ProvisionerConnection(_database.Name), _database.Name);

        // The application's own migration, applied exactly as `migrate` applies
        // it — not `EnsureCreated`. A schema built from the model rather than
        // from the migration is a schema no property will ever have, and it
        // would hide precisely the defect a migration review looks for.
        // The platform's shared outbox, from the Kernel's own migrations. An
        // installed application cannot provision this and does not try; the
        // suite does, because a job that is raised appends an event in the same
        // transaction and a round that could not write one would be proving
        // half of every operation. SHELL-Q37 held ten rows on exactly this.
        await PlatformEventStore.ProvisionAsync(ProvisionerConnection(_database.Name));

        await using var migrator = Context(_convention.MigratorConnectionFor(_database.Name));
        await migrator.Database.MigrateAsync();

        // No grant pass here. Step 4's `ALTER DEFAULT PRIVILEGES FOR ROLE
        // <owner>` already covers everything the migration goes on to create,
        // which is the whole reason the installer runs it before step 6 rather
        // than after — and reproducing that ordering is the point of deriving
        // the convention instead of improvising one.

        PropertyId = Guid.CreateVersion7();
    }

    /// <summary>A context over the scratch database, as the application role.</summary>
    /// <summary>The application role's connection — what the hosted service opens in the wire round.</summary>
    public string ApplicationConnection =>
        (_database ?? throw NotInitialised()).ConnectionFor(ApplicationRole);

    public JobsDbContext Context() =>
        Context((_database ?? throw NotInitialised()).ConnectionFor(ApplicationRole));

    private static JobsDbContext Context(string connection) =>
        new(new DbContextOptionsBuilder<JobsDbContext>()
            .UseSnakeCaseNamingConvention()
            .UseNpgsql(
                connection,
                npgsql => npgsql.MigrationsHistoryTable("__migrations", JobsDbContext.Schema))
            .Options);

    /// <summary>What this aggregate announced, in the platform's own store, in order.</summary>
    /// <remarks>
    /// Read with the application's own connection, which holds the appender
    /// role and nothing more — so a row counted here is one the application was
    /// entitled to write, in the schema the Kernel owns.
    /// </remarks>
    public async Task<IReadOnlyList<string>> EventsForAsync(Guid aggregateId)
    {
        await using var connection = new NpgsqlConnection(ApplicationConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT event_type FROM event_store.events WHERE aggregate_id = @id ORDER BY entity_version";
        command.Parameters.AddWithValue("id", aggregateId);

        var types = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) types.Add(reader.GetString(0));
        return types;
    }

    /// <summary>A caller scoped to this suite's property.</summary>
    public RequestScope Scope() => new()
    {
        Caller = CallerKind.User,
        PropertyId = PropertyId,
        UserId = Guid.CreateVersion7(),
    };

    /// <summary>A caller at a different property, for isolation tests.</summary>
    /// <remarks>
    /// A separate property rather than a separate user: what slice 1 must hold is
    /// that a posting is invisible across the tenancy boundary, and a second user
    /// at the same property would characterise nothing.
    /// </remarks>
    public RequestScope OtherPropertyScope() => new()
    {
        Caller = CallerKind.User,
        PropertyId = Guid.CreateVersion7(),
        UserId = Guid.CreateVersion7(),
    };

    private static InvalidOperationException NotInitialised() => new(
        "the fixture has no database — initialisation should have failed before any test ran");

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        // **The database goes first, and this had it the other way round.** The
        // old order reasoned that `DROP OWNED` needs the database the roles own
        // things in — true, and insufficient: `DROP OWNED` clears what a role
        // owns and was granted, while the database's own grants still name it,
        // so `DROP ROLE` then failed with *"cannot be dropped because some
        // objects depend on it"* and the teardown reported it to a stream that
        // was not reading stderr. Two roles survived every passing run, and
        // eight were on this cluster before the count was taken.
        //
        // Dropping the database takes every per-database dependency with it,
        // after which the roles drop cleanly — which is why the leftovers from
        // earlier runs, whose databases were long gone, dropped by hand without
        // a word. `DropRolesAsync` is then passed no database connection, which
        // is its documented shape for a run with nothing to clean inside. FF's
        // ordering, `698cab8`.
        //
        // **Unconditional**, still: a run that threw before the database existed
        // is the one whose roles are otherwise never cleaned up (`INSTALL-Q88`).
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }

        await _convention.DropRolesAsync(ProvisionerConnection("postgres"), null);
    }
}

/// <summary>One fixture for the whole suite.</summary>
[CollectionDefinition(Name)]
public sealed class JobsCollection : ICollectionFixture<JobsFixture>
{
    /// <summary>The collection every characterisation class joins.</summary>
    public const string Name = "jobs";
}
