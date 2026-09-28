using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.Configurations;

/// <summary>
/// How a Manager on Duty span is stored — <c>Application/Duties</c>'s subject.
/// </summary>
public sealed class DutiesConfiguration : IEntityTypeConfiguration<DutyAssignment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DutyAssignment> duty)
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
    }
}
