using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Workforce.Tests;

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
public sealed class WorkforceFixture : IAsyncLifetime
{
    /// <summary>The role this application's runtime connects as.</summary>
    /// <remarks>
    /// <c>hotelos_app_workforce</c> — <b>the installer's own name</b>, derived
    /// from <c>naming.rs:45-48</c> rather than chosen here. The suite provisions
    /// it to the installer's convention (see <see cref="InstallerConvention"/>),
    /// because a role invented for the tests would be a role no property has.
    /// </remarks>
    private const string ApplicationRole = InstallerConvention.AppRole;

    /// <summary>The provisioner, which is also the harness's admin role.</summary>
    /// <remarks>
    /// Declared in the spec so the fixture can reach the scratch database as the
    /// role that creates the schema — the installer's step 4 runs as the
    /// provisioner, not as the application.
    /// </remarks>
    private const string ProvisionerRole = "hotelos_test";

    /// <summary>The development cluster's provisioner-equivalent.</summary>
    /// <remarks>
    /// <para>
    /// The installer runs step 4 as a dedicated provisioner role holding
    /// <c>CREATEROLE</c> — <c>02-roles.sql</c> grants it. A developer's cluster
    /// has no such role: <c>hotelos_test</c> holds <c>CREATEDB</c> and not
    /// <c>CREATEROLE</c>, which this suite established by being refused
    /// <i>"42501: permission denied to create role"</i>.
    /// </para>
    /// <para>
    /// So the <b>credential</b> differs and the <b>convention</b> does not. What
    /// gets created, in what order, with which grants, is
    /// <see cref="InstallerConvention"/>'s and therefore the installer's; who
    /// runs it is whoever holds the authority on the cluster at hand. Widening
    /// <c>hotelos_test</c> instead would hand every suite on the machine the
    /// ability to mint roles, which is a larger change than this needs.
    /// </para>
    /// </remarks>
    private static string ProvisionerConnection(string database) =>
        $"Host={Host};Port={Port};Database={database};Username=postgres;Password=devroot";

    private static string Host => ScratchDatabase.Target.Split(':')[0];

    private static string Port => ScratchDatabase.Target.Split(':')[1];

    private readonly string _password = $"wf{Guid.NewGuid():N}"[..24];

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

    private async Task PrepareAsync()
    {
        // The cluster roles first: they are cluster-scoped, so they must exist
        // before the scratch database can grant either of them CONNECT.
        await InstallerConvention.EnsureRolesAsync(
            ProvisionerConnection("postgres"), _password);

        _database = await ScratchDatabase.CreateAsync(
            new ScratchDatabaseSpec(
                NamePrefix: "hotelos_workforce_test",
                Roles: new Dictionary<string, string>
                {
                    [ApplicationRole] = _password,
                    [ProvisionerRole] = "devtest",
                }));

        if (_database is null)
        {
            throw new InvalidOperationException(
                "Workforce's characterisation tests require PostgreSQL and could not reach it "
                + $"at {ScratchDatabase.Target}. Run `make db-up db-test-role` in the platform "
                + "checkout and try again. This is a failure rather than a skip: a suite that "
                + "passes without a database reports success having executed nothing.");
        }

        // Not `ScratchDatabase.CreateSchemaAsync`: that creates the schema
        // `AUTHORIZATION hotelos_owner_<schema>` and assumes the role already
        // exists, which is true for a platform service and false for an
        // installed application. This runs the installer's step 4 instead —
        // F7, ruled 2026-08-31.
        await InstallerConvention.ProvisionSchemaAsync(
            ProvisionerConnection(_database.Name), _database.Name);

        // The application's own migration, applied exactly as `migrate` applies
        // it — not `EnsureCreated`. A schema built from the model rather than
        // from the migration is a schema no property will ever have, and it
        // would hide precisely the defect a migration review looks for.
        await using var migrator = Context(_database.MigratorConnectionFor(WorkforceDbContext.Schema));
        await migrator.Database.MigrateAsync();

        // No grant pass here. Step 4's `ALTER DEFAULT PRIVILEGES FOR ROLE
        // <owner>` already covers everything the migration goes on to create,
        // which is the whole reason the installer runs it before step 6 rather
        // than after — and reproducing that ordering is the point of deriving
        // the convention instead of improvising one.

        PropertyId = Uuid7.NewUuid7();
    }

    /// <summary>A context over the scratch database, as the application role.</summary>
    public WorkforceDbContext Context() =>
        Context((_database ?? throw NotInitialised()).ConnectionFor(ApplicationRole));

    private static WorkforceDbContext Context(string connection) =>
        new(new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseSnakeCaseNamingConvention()
            .UseNpgsql(
                connection,
                npgsql => npgsql.MigrationsHistoryTable("__migrations", WorkforceDbContext.Schema))
            .Options);

    /// <summary>A caller scoped to this suite's property.</summary>
    public RequestScope Scope() => new()
    {
        Caller = CallerKind.User,
        PropertyId = PropertyId,
        UserId = Uuid7.NewUuid7(),
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
        PropertyId = Uuid7.NewUuid7(),
        UserId = Uuid7.NewUuid7(),
    };

    private static InvalidOperationException NotInitialised() => new(
        "the fixture has no database — initialisation should have failed before any test ran");

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }
}

/// <summary>One fixture for the whole suite.</summary>
[CollectionDefinition(Name)]
public sealed class WorkforceCollection : ICollectionFixture<WorkforceFixture>
{
    /// <summary>The collection every characterisation class joins.</summary>
    public const string Name = "workforce";
}
