using HotelOS.GuestOps.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.Configurations;

/// <summary>This application's own configuration, one row per property.</summary>
/// <remarks>
/// Keyed on the property, so the row cannot exist twice and a second one cannot
/// be created by a race — a property with two registration policies is a
/// question nobody could answer at an inspection.
/// </remarks>
public class GuestOpsSettingsConfiguration : IEntityTypeConfiguration<GuestOpsSettings>
{
    public void Configure(EntityTypeBuilder<GuestOpsSettings> builder)
    {
        builder.ToTable("settings");
        builder.HasKey(s => s.PropertyId);

        builder.Property(s => s.HomeCountry).HasMaxLength(2).IsRequired();
        builder.Property(s => s.CardNumberPrefix).HasMaxLength(32).IsRequired();
        builder.Property(s => s.ReportingAuthority).HasMaxLength(200);

        // The field sets and the accepted document list are the property's own
        // vocabulary, stored as text arrays rather than joined tables: nothing
        // references them, they are read whole on every check-in, and a lookup
        // table would add a join to serve an ordering nobody queries.
        builder.Property(s => s.RequiredForHomeCountry).HasColumnType("text[]");
        builder.Property(s => s.RequiredForVisitors).HasColumnType("text[]");
        builder.Property(s => s.AcceptedIdTypes).HasColumnType("text[]");

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_settings__due_hours", "reporting_due_hours > 0"));
    }
}
