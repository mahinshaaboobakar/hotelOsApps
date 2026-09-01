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

                // Zero-length is refused here as well as in the service —
                // WF-Q17. A span that ends where it starts is not a
                // round-the-clock shift, and no writer may introduce one.
                table.HasCheckConstraint(
                    "ck_shift_hours__span_not_empty",
                    "starts_at IS NULL OR starts_at <> ends_at");

                table.HasCheckConstraint(
                    "ck_shift_hours__second_span_not_empty",
                    "second_starts_at IS NULL OR second_starts_at <> second_ends_at");
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

        modelBuilder.Entity<ShiftAssignment>(assignment =>
        {
            assignment.ToTable("shift_assignments", table =>
            {
                // Both stated or neither. A half-written one-off span is a cell
                // whose hours nobody can compute.
                table.HasCheckConstraint(
                    "ck_shift_assignments__override_complete",
                    "(override_starts_at IS NULL) = (override_ends_at IS NULL)");

                table.HasCheckConstraint(
                    "ck_shift_assignments__department_present",
                    "length(btrim(department_code)) > 0");
            });

            assignment.HasKey(a => a.Id);

            assignment.Property(a => a.DepartmentCode).HasMaxLength(50).IsRequired();
            assignment.Property(a => a.Version).IsConcurrencyToken();

            // **One shift per person per day.** A split shift is one catalogue
            // entry with two spans, so a second row on one day would be a second
            // shift — a thing the rota does not offer. Expressible as an index
            // because there is no window to compare, unlike a posting.
            assignment.HasIndex(a => new { a.PropertyId, a.StaffId, a.Date })
                .IsUnique()
                .HasDatabaseName("uq_shift_assignments__property_staff_date");

            // The week grid's query: one department, seven days.
            assignment.HasIndex(a => new { a.PropertyId, a.Date, a.DepartmentCode })
                .HasDatabaseName("ix_shift_assignments__property_date_department");
        });

        modelBuilder.Entity<WorkforcePolicy>(policy =>
        {
            policy.ToTable("policies", table =>
            {
                // A threshold of zero flags every shift ever worked, and a
                // negative one cannot be meant. Refused in the service too, where
                // the message can say why; here so no other writer can bypass it.
                table.HasCheckConstraint(
                    "ck_policies__overtime_daily_positive",
                    "overtime_daily_hours IS NULL OR overtime_daily_hours > 0");

                table.HasCheckConstraint(
                    "ck_policies__overtime_weekly_positive",
                    "overtime_weekly_hours IS NULL OR overtime_weekly_hours > 0");
            });

            // The property id *is* the key: one property has one policy, and a
            // surrogate would admit a second row nothing could choose between.
            policy.HasKey(p => p.PropertyId);

            policy.Property(p => p.OvertimeDailyHours).HasPrecision(5, 2);
            policy.Property(p => p.OvertimeWeeklyHours).HasPrecision(5, 2);
            policy.Property(p => p.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<LeaveType>(type =>
        {
            type.ToTable("leave_types", table =>
                table.HasCheckConstraint(
                    "ck_leave_types__accrual_not_negative",
                    "accrual_per_month IS NULL OR accrual_per_month >= 0"));

            type.HasKey(t => t.Id);

            type.Property(t => t.Code).HasMaxLength(32).IsRequired();
            type.Property(t => t.Name).HasMaxLength(120).IsRequired();
            type.Property(t => t.AccrualPerMonth).HasPrecision(5, 2);
            type.Property(t => t.Version).IsConcurrencyToken();

            // The code is stable within the property and reports group on it, so
            // two types sharing one would merge a year of history. Unfiltered by
            // `active`, unlike the shift catalogue: a retired type's ledger
            // entries still name it, and reusing the code would attribute them to
            // whatever replaced it.
            type.HasIndex(t => new { t.PropertyId, t.Code })
                .IsUnique()
                .HasDatabaseName("uq_leave_types__property_code");
        });

        modelBuilder.Entity<LeaveLedgerEntry>(entry =>
        {
            entry.ToTable("leave_ledger", table =>
                table.HasCheckConstraint(
                    "ck_leave_ledger__days_not_zero",
                    "days <> 0"));

            entry.HasKey(e => e.Id);

            entry.Property(e => e.Days).HasPrecision(6, 2);
            entry.Property(e => e.Note).HasMaxLength(500).IsRequired();

            // The balance query: one person, summed by type.
            entry.HasIndex(e => new { e.PropertyId, e.StaffId, e.LeaveTypeId })
                .HasDatabaseName("ix_leave_ledger__property_staff_type");
        });

        modelBuilder.Entity<LeaveRequest>(request =>
        {
            request.ToTable("leave_requests", table =>
                table.HasCheckConstraint(
                    "ck_leave_requests__range_ordered",
                    "leave_to >= leave_from"));

            request.HasKey(r => r.Id);

            request.Property(r => r.Note).HasMaxLength(1000).IsRequired();
            request.Property(r => r.DecisionNote).HasMaxLength(1000).IsRequired();
            request.Property(r => r.Version).IsConcurrencyToken();

            // `Days` is computed from the two dates and has nowhere to be stored
            // — a count that could disagree with the range it came from is the
            // derived-projection defect one field wide.
            request.Ignore(r => r.Days);

            // Two column names, because `from` and `to` are reserved words in
            // enough dialects that a bare one is a migration failure waiting for
            // whichever database somebody tries next.
            request.Property(r => r.From).HasColumnName("leave_from");
            request.Property(r => r.To).HasColumnName("leave_to");

            // The approver's queue, and the overlap check.
            request.HasIndex(r => new { r.PropertyId, r.ApproverStaffId, r.State })
                .HasDatabaseName("ix_leave_requests__property_approver_state");

            request.HasIndex(r => new { r.PropertyId, r.StaffId, r.From })
                .HasDatabaseName("ix_leave_requests__property_staff_from");
        });

        modelBuilder.Entity<SwapProposal>(proposal =>
        {
            proposal.ToTable("swap_proposals", table =>
                table.HasCheckConstraint(
                    "ck_swap_proposals__two_people",
                    "proposer_staff_id <> colleague_staff_id"));

            proposal.HasKey(p => p.Id);

            proposal.Property(p => p.Note).HasMaxLength(1000).IsRequired();
            proposal.Property(p => p.DecisionNote).HasMaxLength(1000).IsRequired();
            proposal.Property(p => p.Version).IsConcurrencyToken();

            // "What needs me" — one query serving the colleague and the approver,
            // because in a small hotel they are the same person as often as not.
            proposal.HasIndex(p => new { p.PropertyId, p.State, p.ColleagueStaffId })
                .HasDatabaseName("ix_swap_proposals__property_state_colleague");

            proposal.HasIndex(p => new { p.PropertyId, p.State, p.ApproverStaffId })
                .HasDatabaseName("ix_swap_proposals__property_state_approver");
        });

        modelBuilder.Entity<AttendanceRecord>(record =>
        {
            record.ToTable("attendance", table =>
            {
                // A departure with no arrival is not a day anybody worked.
                table.HasCheckConstraint(
                    "ck_attendance__out_needs_in",
                    "out_at IS NULL OR in_at IS NOT NULL");

                // Provenance, at the database. A manual record names the account
                // that entered it; a device or import names the reading it came
                // from. A record with neither cannot be audited, and no writer
                // may introduce one — not the service, not a future importer.
                table.HasCheckConstraint(
                    "ck_attendance__provenance",
                    "(source = 0 AND recorded_by_user_id IS NOT NULL) "
                    + "OR (source <> 0 AND external_reference IS NOT NULL)");
            });

            record.HasKey(r => r.Id);

            record.Property(r => r.Note).HasMaxLength(1000).IsRequired();
            record.Property(r => r.ExternalReference).HasMaxLength(200);
            record.Property(r => r.Version).IsConcurrencyToken();

            // Worked, Attended and StillIn are computed from the two times. None
            // has anywhere to be stored, which is what keeps them from disagreeing
            // with the times they come from.
            record.Ignore(r => r.Worked);
            record.Ignore(r => r.Attended);
            record.Ignore(r => r.StillIn);

            // One record per person per business day. A second would be a second
            // answer to "what did they do that day", and the shift that crosses
            // midnight is already one record by construction — the business date
            // is the platform's, not the calendar's.
            record.HasIndex(r => new { r.PropertyId, r.StaffId, r.BusinessDate })
                .IsUnique()
                .HasDatabaseName("uq_attendance__property_staff_date");

            // The day sheet, and the still-signed-in query.
            record.HasIndex(r => new { r.PropertyId, r.BusinessDate })
                .HasDatabaseName("ix_attendance__property_date");
        });
    }
}
