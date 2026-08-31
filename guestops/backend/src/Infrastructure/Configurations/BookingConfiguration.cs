using HotelOS.GuestOps.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.Configurations;

/// <summary>The group, and the identifiers a source knows it by.</summary>
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);

        // Every read is property-scoped: a session names one property, and a
        // list that scanned the table would be one predicate away from a
        // cross-property read.
        builder.HasIndex(b => b.PropertyId);

        builder.Property(b => b.Version).IsConcurrencyToken();

        builder.HasMany(b => b.Stays)
            .WithOne(s => s.Booking)
            .HasForeignKey(s => s.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.ExternalRefs)
            .WithOne(r => r.Booking)
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>A booking's source identifiers — R10, CONN-Q8.</summary>
public class BookingExternalRefConfiguration : IEntityTypeConfiguration<BookingExternalRef>
{
    public void Configure(EntityTypeBuilder<BookingExternalRef> builder)
    {
        builder.ToTable("booking_external_refs");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.IntegrationId).HasMaxLength(64).IsRequired();
        builder.Property(r => r.IdentifierKind).HasMaxLength(64).IsRequired();
        builder.Property(r => r.ExternalId).HasMaxLength(200).IsRequired();

        // The three-part key CONN-Q8 ruled: one integration's one kind of
        // identifier resolves to one booking. Unique so a second fact carrying
        // the same reference finds the booking rather than making another —
        // which is what makes replay idempotent by construction rather than by
        // a consumer's diligence.
        builder.HasIndex(r => new { r.IntegrationId, r.IdentifierKind, r.ExternalId })
            .IsUnique()
            .HasDatabaseName("uq_booking_external_refs__identity");
    }
}
