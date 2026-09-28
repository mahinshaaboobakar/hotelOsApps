using HotelOS.Workforce.Infrastructure.Configurations;
using HotelOS.Workforce.Infrastructure.ReadModels;
using HotelOS.Platform;
using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Infrastructure;

/// <summary>
/// This application's own schema, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The application-bundle rule (ADR 0051): an application brings its own
/// schema, its own migrations and its own lifecycle. <b>It touches no other
/// schema</b> — not <c>masterdata</c>, not <c>identity</c>. A staff member is
/// read through Master Data's gRPC surface, never by joining to its tables, and
/// the grants in <c>04-grants.sql</c> are what make that a rule rather than a
/// convention.
/// </para>
/// <para>
/// The event store lives here too, through the SDK's model extension: an event
/// and its <c>publish_state</c> queue row are written in the <b>caller's
/// transaction</b>, with the change that caused them. A gRPC call cannot join a
/// transaction, so routing an announcement through the Kernel would put the
/// state change and its announcement in two transactions with a gap — and a
/// crash in that gap keeps the posting and loses its authorization, silently and
/// in the safe-looking direction.
/// </para>
/// <para>
/// <b>It declares the model and configures none of it.</b> Every subject's
/// mapping lives in <c>Configurations/</c>; this is the composition root ADR 0042
/// describes, and it holds no subject.
/// </para>
/// </remarks>
public class WorkforceDbContext(DbContextOptions<WorkforceDbContext> options)
    : DbContext(options)
{
    /// <summary>The one schema this application owns.</summary>
    /// <remarks>
    /// Named as a constant because three separate things must agree on it: the
    /// model below, the migrations history table, and the <c>migrate</c> verb in
    /// <c>Program.cs</c>. A literal repeated three times is a literal that
    /// disagrees with itself after the first rename.
    /// </remarks>
    public const string Schema = "workforce";

    /// <summary>Every posting this property holds, open or closed.</summary>
    public DbSet<Posting> Postings => Set<Posting>();

    /// <summary>What people here can do, dated or not.</summary>
    public DbSet<Capability> Capabilities => Set<Capability>();

    /// <summary>The shifts this property offers.</summary>
    public DbSet<ShiftCatalogueEntry> ShiftCatalogue => Set<ShiftCatalogueEntry>();

    /// <summary>The hours each of them has had, over time.</summary>
    public DbSet<ShiftHours> ShiftHours => Set<ShiftHours>();

    /// <summary>The Manager on Duty register.</summary>
    public DbSet<DutyAssignment> Duties => Set<DutyAssignment>();

    /// <summary>The rota — one person, one day, one shift.</summary>
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();

    /// <summary>What each property configures about how its workforce is run.</summary>
    public DbSet<WorkforcePolicy> Policies => Set<WorkforcePolicy>();

    /// <summary>The kinds of leave this property grants.</summary>
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    /// <summary>Every movement of every balance, and the balance itself is their sum.</summary>
    public DbSet<LeaveLedgerEntry> LeaveLedger => Set<LeaveLedgerEntry>();

    /// <summary>Somebody asking to be away.</summary>
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    /// <summary>Staff asking to exchange shifts with a colleague.</summary>
    public DbSet<SwapProposal> SwapProposals => Set<SwapProposal>();

    /// <summary>What actually happened, one person to one business day.</summary>
    public DbSet<AttendanceRecord> Attendance => Set<AttendanceRecord>();

    /// <summary>Named groups of posted staff, formed to be assigned work.</summary>
    public DbSet<Team> Teams => Set<Team>();

    /// <summary>Who is in each of them, and when they were.</summary>
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    /// <summary>Which shift boundaries this application has announced.</summary>
    public DbSet<ShiftBoundary> ShiftBoundaries => Set<ShiftBoundary>();

    // ── Master Data, read through the grant — ADR 0092 §4 ────────────────────
    //
    // Keyless and excluded from this application's migrations, exactly as the
    // event store above is and for the same reason: another component owns
    // these rows. Install step 4 issues `GRANT hotelos_masterdata_reader TO
    // hotelos_app_workforce`, which is the whole of this application's access —
    // read, and no write it could express if it wanted to.

    /// <summary>Canonical people, organization-scoped.</summary>
    public DbSet<StaffRow> MasterDataStaff => Set<StaffRow>();

    /// <summary>Canonical departments, one property's.</summary>
    public DbSet<DepartmentRow> MasterDataDepartments => Set<DepartmentRow>();

    /// <summary>Which property a person is scoped to — ADR 0052.</summary>
    public DbSet<StaffPropertyScopeRow> MasterDataStaffScopes =>
        Set<StaffPropertyScopeRow>();

    /// <summary>The property itself.</summary>
    public DbSet<PropertyRow> MasterDataProperties => Set<PropertyRow>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // The SDK's event store — one definition, never reimplemented per
        // service. Two event appenders drift, and one of them stops writing the
        // queue row.
        //
        // **It does not live in this schema, and it is not this application's to
        // create.** The configuration names `event_store.events` and
        // `event_store.publish_state` explicitly and marks both
        // `ExcludeFromMigrations()`: the Kernel owns that schema and migrates it,
        // and the relationship an application has with it is the one it has with
        // a write-ahead log — it appends, it does not own, and it cannot modify.
        // Scaffolding a `CREATE TABLE events` from here would put two components
        // in charge of one table.
        //
        // Corrected after reading the scaffolded migration: this comment
        // previously claimed `HasDefaultSchema` placed the event store in the
        // `workforce` schema, which the generated SQL disproves — one table,
        // `postings`, and no event store. A comment asserting an outcome nothing
        // checks is the failure CLAUDE.md names, and this one survived a review.
        modelBuilder.AddPlatformEventStore();

        // Master Data's rows, mapped read-only. One configuration class for the
        // four, because they are one subject — what this application may read
        // of somebody else's schema — rather than four unrelated types.
        var masterData = new MasterDataRowConfiguration();
        modelBuilder.ApplyConfiguration<StaffRow>(masterData);
        modelBuilder.ApplyConfiguration<DepartmentRow>(masterData);
        modelBuilder.ApplyConfiguration<StaffPropertyScopeRow>(masterData);
        modelBuilder.ApplyConfiguration<PropertyRow>(masterData);

        // ── this application's own nine subjects ─────────────────────────────
        //
        // **One configuration per subject, and the subjects are `Application/`'s
        // own.** Not one per entity: `Application/Shifts` writes the catalogue,
        // its hours and its announced boundaries, and splitting those three
        // apart would produce three files holding one method each — which
        // ADR 0036 refuses in the same sentence as it sets the ceiling. The
        // grouping was measured rather than chosen: for all fifteen entities,
        // exactly one `Application/` subject calls `Add`, `Update` or `Remove`
        // on its set, and that subject is the one named here.
        //
        // Applied by name rather than by `ApplyConfigurationsFromAssembly`. The
        // scan is one line and it makes the model a function of what happens to
        // be in the assembly — a configuration nobody meant to add takes effect
        // silently, and a reader cannot tell from here what shapes the model.
        // This list can be read against `Configurations/`.
        modelBuilder.ApplyConfiguration(new PostingsConfiguration());
        modelBuilder.ApplyConfiguration(new CapabilitiesConfiguration());
        modelBuilder.ApplyConfiguration(new DutiesConfiguration());
        modelBuilder.ApplyConfiguration(new SwapsConfiguration());
        modelBuilder.ApplyConfiguration(new AttendanceConfiguration());

        // The four that configure more than one type each. The explicit type
        // argument is what picks the interface, exactly as Master Data's does
        // above: one instance, several `Configure` overloads.
        var shifts = new ShiftsConfiguration();
        modelBuilder.ApplyConfiguration<ShiftCatalogueEntry>(shifts);
        modelBuilder.ApplyConfiguration<ShiftHours>(shifts);
        modelBuilder.ApplyConfiguration<ShiftBoundary>(shifts);

        var rota = new RotaConfiguration();
        modelBuilder.ApplyConfiguration<ShiftAssignment>(rota);
        modelBuilder.ApplyConfiguration<WorkforcePolicy>(rota);

        var leave = new LeaveConfiguration();
        modelBuilder.ApplyConfiguration<LeaveType>(leave);
        modelBuilder.ApplyConfiguration<LeaveLedgerEntry>(leave);
        modelBuilder.ApplyConfiguration<LeaveRequest>(leave);

        var teams = new TeamsConfiguration();
        modelBuilder.ApplyConfiguration<Team>(teams);
        modelBuilder.ApplyConfiguration<TeamMember>(teams);
    }
}
