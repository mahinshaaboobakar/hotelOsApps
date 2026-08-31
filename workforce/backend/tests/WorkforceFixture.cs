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
    /// The development test role, because an installed application's own role is
    /// created by the installer and does not exist on a developer's cluster —
    /// which is itself recorded as a finding (chapter 03, <c>AUTHZ-Q23</c>'s
    /// neighbourhood). What this suite characterises is the service's behaviour,
    /// not the grant model, and standing up a role the installer owns would be
    /// testing the installer.
    /// </remarks>
    private const string ApplicationRole = "hotelos_test";

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
        _database = await ScratchDatabase.CreateAsync(
            new ScratchDatabaseSpec(
                NamePrefix: "hotelos_workforce_test",
                Roles: new Dictionary<string, string> { [ApplicationRole] = "devtest" }));

        if (_database is null)
        {
            throw new InvalidOperationException(
                "Workforce's characterisation tests require PostgreSQL and could not reach it "
                + $"at {ScratchDatabase.Target}. Run `make db-up db-test-role` in the platform "
                + "checkout and try again. This is a failure rather than a skip: a suite that "
                + "passes without a database reports success having executed nothing.");
        }

        await _database.CreateSchemaAsync(WorkforceDbContext.Schema);

        // The application's own migration, applied exactly as `migrate` applies
        // it — not `EnsureCreated`. A schema built from the model rather than
        // from the migration is a schema no property will ever have, and it
        // would hide precisely the defect a migration review looks for.
        await using var migrator = Context(_database.MigratorConnectionFor(WorkforceDbContext.Schema));
        await migrator.Database.MigrateAsync();

        // Let the application role reach what the migration just made. The
        // harness grants CONNECT and deliberately nothing else — what a role may
        // reach is the service's safety case rather than the harness's.
        await _database.AsOwnerAsync(
            WorkforceDbContext.Schema,
            $"GRANT USAGE ON SCHEMA {WorkforceDbContext.Schema} TO {ApplicationRole}; "
            + "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA "
            + $"{WorkforceDbContext.Schema} TO {ApplicationRole};");

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
