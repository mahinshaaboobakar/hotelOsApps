using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.Configurations;

/// <summary>
/// How a day's attendance is stored — <c>Application/Attendance</c>'s subject.
/// </summary>
public sealed class AttendanceConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AttendanceRecord> record)
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
    }
}
