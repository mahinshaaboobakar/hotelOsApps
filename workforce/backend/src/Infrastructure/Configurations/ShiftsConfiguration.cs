using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.Configurations;

/// <summary>
/// How a shift, its hours and its announcements are stored —
/// <c>Application/Shifts</c>'s subject.
/// </summary>
/// <remarks>
/// Three types, one subject, on the <c>MasterDataRowConfiguration</c> precedent:
/// a catalogue entry, the hours it has had over time, and the boundaries this
/// application has announced are one thing — what a shift <i>is</i> here — and
/// <c>ShiftCatalogueService</c> and <c>ShiftBoundaryAnnouncer</c> are their only
/// writers. Split three ways these would be three files holding one method each,
/// which ADR 0036 refuses as plainly as it refuses the ceiling.
/// </remarks>
public sealed class ShiftsConfiguration :
    IEntityTypeConfiguration<ShiftCatalogueEntry>,
    IEntityTypeConfiguration<ShiftHours>,
    IEntityTypeConfiguration<ShiftBoundary>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShiftCatalogueEntry> entry)
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
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShiftHours> hours)
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
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShiftBoundary> boundary)
    {
        boundary.ToTable("shift_boundaries");

        boundary.HasKey(b => b.Id);

        boundary.Property(b => b.DepartmentCode).HasMaxLength(50).IsRequired();

        // **The whole mechanism is this index.** A scheduled announcement
        // has no state change to ride along with, so the announcement row is
        // the change: it and the event are written in one transaction, and a
        // trigger that fires twice violates this and rolls the whole thing
        // back. Exactly once, from an at-least-once tick.
        //
        // The business date is in the key rather than the instant, because a
        // night shift's end falls on the following calendar day and would
        // otherwise be indistinguishable from the next morning's.
        boundary.HasIndex(b => new
            {
                b.PropertyId,
                b.DepartmentCode,
                b.CatalogueEntryId,
                b.BusinessDate,
                b.Kind,
            })
            .IsUnique()
            .HasDatabaseName("uq_shift_boundaries__announced_once");

        // What a tick reads: everything announced inside the lookback.
        boundary.HasIndex(b => new { b.PropertyId, b.BusinessDate })
            .HasDatabaseName("ix_shift_boundaries__property_date");
    }
}
