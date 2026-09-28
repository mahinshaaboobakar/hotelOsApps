using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.Configurations;

/// <summary>How a posting is stored — <c>Application/Postings</c>'s subject.</summary>
public sealed class PostingsConfiguration : IEntityTypeConfiguration<Posting>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Posting> posting)
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
    }
}
