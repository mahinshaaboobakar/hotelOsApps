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
    }
}
