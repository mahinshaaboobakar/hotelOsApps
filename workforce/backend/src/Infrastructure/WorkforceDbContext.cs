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

        modelBuilder.Entity<Posting>(posting =>
        {
            posting.ToTable("postings", table =>
            {
                // A window that ends before it starts is not a judgment call —
                // it is a record that cannot be true, so it is refused. WF-Q16:
                // the platform refuses the physically impossible and warns on a
                // judgment.
                table.HasCheckConstraint(
                    "ck_postings__window_ordered",
                    "effective_to IS NULL OR effective_to >= effective_from");

                // The canon code is a code, not free text — ADR 0119. Length is
                // Master Data's `departments.code` (50), so a value that fits
                // there fits here and a mismatch cannot be introduced by this
                // side.
                table.HasCheckConstraint(
                    "ck_postings__department_code_present",
                    "length(btrim(department_code)) > 0");
            });

            posting.HasKey(p => p.Id);

            posting.Property(p => p.DepartmentCode).HasMaxLength(50).IsRequired();
            posting.Property(p => p.JobRole).HasMaxLength(200).IsRequired();

            // Optimistic concurrency. EF checks it on every update, so a second
            // supervisor's save fails loudly instead of overwriting the first.
            posting.Property(p => p.Version).IsConcurrencyToken();

            // The query every screen makes: who works in this property, now.
            posting.HasIndex(p => new { p.PropertyId, p.StaffId })
                .HasDatabaseName("ix_postings__property_staff");

            // And the one the Context resolver makes: who has this zone.
            posting.HasIndex(p => new { p.PropertyId, p.ZoneId })
                .HasDatabaseName("ix_postings__property_zone");

            // Departmental listing — the People screen's filter, and the
            // authorization backfill's read.
            posting.HasIndex(p => new { p.PropertyId, p.DepartmentCode })
                .HasDatabaseName("ix_postings__property_department");

            // **No unique index on (property, staff, department).**
            //
            // A person may hold the same posting twice across time: posted to
            // Kitchen until March, posted to Kitchen again from September. The
            // window is what distinguishes them, and a uniqueness rule that
            // ignored the window would make re-hiring somebody impossible.
            //
            // Overlapping open postings for one person and department *are*
            // wrong, and that is enforced in the service where the window can be
            // compared, not by an index that cannot express it.
        });

        modelBuilder.Entity<Capability>(capability =>
        {
            capability.ToTable("capabilities", table =>
                table.HasCheckConstraint(
                    "ck_capabilities__name_present",
                    "length(btrim(name)) > 0"));

            capability.HasKey(c => c.Id);

            capability.Property(c => c.Name).HasMaxLength(200).IsRequired();
            capability.Property(c => c.Note).HasMaxLength(1000).IsRequired();
            capability.Property(c => c.Version).IsConcurrencyToken();

            // One person cannot hold one capability twice — a second "fire
            // warden" row is two expiry dates for one fact, and the register
            // would show them as both current and lapsed. Unlike a posting, this
            // *is* expressible as an index, because there is no window to
            // compare: renewing amends the row rather than adding one.
            capability.HasIndex(c => new { c.PropertyId, c.StaffId, c.Name })
                .IsUnique()
                .HasDatabaseName("uq_capabilities__property_staff_name");

            // The Attention list and the register both scan by expiry within a
            // property. Nulls are the majority — abilities — and both queries
            // exclude them, so the index carries only the rows that lapse.
            capability.HasIndex(c => new { c.PropertyId, c.ValidUntil })
                .HasFilter("valid_until IS NOT NULL")
                .HasDatabaseName("ix_capabilities__property_expiry");
        });

        modelBuilder.Entity<ShiftCatalogueEntry>(entry =>
        {
            entry.ToTable("shift_catalogue", table =>
                table.HasCheckConstraint(
                    "ck_shift_catalogue__code_present",
                    "length(btrim(short_code)) > 0"));

            entry.HasKey(e => e.Id);

            entry.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entry.Property(e => e.ShortCode).HasMaxLength(8).IsRequired();
            entry.Property(e => e.Colour).HasMaxLength(32).IsRequired();
            entry.Property(e => e.Version).IsConcurrencyToken();

            // Two live shifts sharing a code would be two shifts that look
            // identical in a rota cell and on paper — the failure the
            // typed-not-derived rule exists to prevent, reached by a different
            // route. Filtered on `active`, so a retired code can be reused.
            entry.HasIndex(e => new { e.PropertyId, e.ShortCode })
                .IsUnique()
                .HasFilter("active")
                .HasDatabaseName("uq_shift_catalogue__property_code");
        });

        modelBuilder.Entity<ShiftHours>(hours =>
        {
            hours.ToTable("shift_hours", table =>
            {
                // A window that ends before it starts cannot be true. The
                // *times* within a day may run backwards — that is a night
                // shift — but the effective window may not.
                table.HasCheckConstraint(
                    "ck_shift_hours__window_ordered",
                    "effective_to IS NULL OR effective_to >= effective_from");

                // Both stated or neither: neither is an off shift, and one is a
                // half-written one.
                table.HasCheckConstraint(
                    "ck_shift_hours__span_complete",
                    "(starts_at IS NULL) = (ends_at IS NULL)");

                table.HasCheckConstraint(
                    "ck_shift_hours__second_span_complete",
                    "(second_starts_at IS NULL) = (second_ends_at IS NULL)");

                // No second span without a first.
                table.HasCheckConstraint(
                    "ck_shift_hours__second_needs_first",
                    "second_starts_at IS NULL OR starts_at IS NOT NULL");
            });

            hours.HasKey(h => h.Id);

            // One open revision per shift: the series has a single current set
            // of hours, and `Reschedule` closes the previous one in the same
            // transaction as it adds the next.
            hours.HasIndex(h => h.CatalogueEntryId)
                .IsUnique()
                .HasFilter("effective_to IS NULL")
                .HasDatabaseName("uq_shift_hours__one_open_revision");

            // Resolving what was worked on a date — the query WF-Q15 exists for.
            hours.HasIndex(h => new { h.CatalogueEntryId, h.EffectiveFrom })
                .HasDatabaseName("ix_shift_hours__entry_from");
        });

        modelBuilder.Entity<DutyAssignment>(duty =>
        {
            duty.ToTable("duties", table =>
                table.HasCheckConstraint(
                    "ck_duties__span_ordered",
                    "ends_at > starts_at"));

            duty.HasKey(d => d.Id);

            duty.Property(d => d.DutyType).HasMaxLength(32).IsRequired();
            duty.Property(d => d.HandoverNote).HasMaxLength(2000).IsRequired();
            duty.Property(d => d.Version).IsConcurrencyToken();

            // "Who is MOD now" and the week strip both scan a property's spans
            // by time. **Not a unique index** — two duties overlapping is
            // refused in the service, where two spans can be compared; an index
            // cannot express an overlap, which is exactly what WF-Q8 changed.
            duty.HasIndex(d => new { d.PropertyId, d.StartsAt })
                .HasDatabaseName("ix_duties__property_start");
        });
    }
}
