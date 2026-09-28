using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.Configurations;

/// <summary>How a capability is stored — <c>Application/Capabilities</c>'s subject.</summary>
public sealed class CapabilitiesConfiguration : IEntityTypeConfiguration<Capability>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Capability> capability)
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
    }
}
