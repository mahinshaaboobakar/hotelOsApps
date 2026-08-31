using HotelOS.GuestOps.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.Configurations;

/// <summary>The anchor, its times, and the two flags that are two facts.</summary>
public class RoomStayConfiguration : IEntityTypeConfiguration<RoomStay>
{
    public void Configure(EntityTypeBuilder<RoomStay> builder)
    {
        builder.ToTable("room_stays");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Version).IsConcurrencyToken();

        // The two timestamps are value objects, stored as their two parts. An
        // instant without its basis is R13's defect — the report that measures
        // the reservation rather than the guest — so the basis is not optional
        // and is not defaulted away.
        builder.OwnsOne(s => s.ArrivalAt, time =>
        {
            time.Property(t => t.At).HasColumnName("arrival_at");
            time.Property(t => t.Basis).HasColumnName("arrival_basis").IsRequired();
        });

        builder.OwnsOne(s => s.DepartureAt, time =>
        {
            time.Property(t => t.At).HasColumnName("departure_at");
            time.Property(t => t.Basis).HasColumnName("departure_basis").IsRequired();
        });

        // The board's three lists, and the availability query, all filter on
        // (property, lifecycle) and a date. One index rather than three because
        // the leading columns are shared and the desk never asks for a
        // lifecycle across properties.
        builder.HasIndex(s => new { s.PropertyId, s.Lifecycle });
        builder.HasIndex(s => new { s.PropertyId, s.BusinessDate });

        // Availability subtracts stays holding a type over a date range, and
        // the conflict check asks the same question one room wide.
        builder.HasIndex(s => new { s.PropertyId, s.RoomTypeId });
        builder.HasIndex(s => s.CurrentRoomId);

        builder.HasMany(s => s.Assignments)
            .WithOne(a => a.Stay)
            .HasForeignKey(a => a.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Party)
            .WithOne(p => p.Stay)
            .HasForeignKey(p => p.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Absences)
            .WithOne(a => a.Stay)
            .HasForeignKey(a => a.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.ExternalRefs)
            .WithOne(r => r.Stay)
            .HasForeignKey(r => r.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Terms)
            .WithOne(t => t.Stay)
            .HasForeignKey<CommercialTerms>(t => t.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Source)
            .WithOne(x => x.Stay)
            .HasForeignKey<StaySource>(x => x.StayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>A stay's source identifiers, minted with the stay — GUEST-Q8.</summary>
public class StayExternalRefConfiguration : IEntityTypeConfiguration<StayExternalRef>
{
    public void Configure(EntityTypeBuilder<StayExternalRef> builder)
    {
        builder.ToTable("stay_external_refs");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.IntegrationId).HasMaxLength(64).IsRequired();
        builder.Property(r => r.IdentifierKind).HasMaxLength(64).IsRequired();
        builder.Property(r => r.ExternalId).HasMaxLength(200).IsRequired();

        // What answers *"which stay is this fact about"*. Unique on the
        // three-part key, so the second fact for one reservation finds its stay
        // instead of creating a duplicate — the guarantee that makes minting
        // and mapping one operation rather than two (GUEST-Q8).
        builder.HasIndex(r => new { r.IntegrationId, r.IdentifierKind, r.ExternalId })
            .IsUnique()
            .HasDatabaseName("uq_stay_external_refs__identity");
    }
}

/// <summary>The room, over time — a row rather than a value (R8).</summary>
public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("assignments");
        builder.HasKey(a => a.Id);

        // The conflict check reads this: which stays have held this room over
        // these dates. Filtered to the open row, because *"where are they now"*
        // is the common question and the closed rows are history.
        builder.HasIndex(a => new { a.RoomId, a.ReleasedAt });
        builder.HasIndex(a => a.StayId);
    }
}

/// <summary>Party membership. Composite key: one guest, once, per stay.</summary>
public class StayGuestConfiguration : IEntityTypeConfiguration<StayGuest>
{
    public void Configure(EntityTypeBuilder<StayGuest> builder)
    {
        builder.ToTable("stay_guests");
        builder.HasKey(p => new { p.StayId, p.GuestId });

        // Nullable, and stored as such: *"nobody is marked primary"* is a state
        // the source produces (R11), and `false` everywhere says something
        // different from `null` everywhere.
        builder.Property(p => p.IsPrimary);

        builder.HasOne(p => p.Guest)
            .WithMany()
            .HasForeignKey(p => p.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>What a stay does not have, and why — R25.</summary>
public class StayAbsenceConfiguration : IEntityTypeConfiguration<StayAbsence>
{
    public void Configure(EntityTypeBuilder<StayAbsence> builder)
    {
        builder.ToTable("stay_absences");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Field).HasMaxLength(64).IsRequired();
        builder.Property(a => a.RawValue).HasMaxLength(500);

        // One row per field per stay: recording the same absence twice would
        // make the Attention list count a stay more than once.
        builder.HasIndex(a => new { a.StayId, a.Field })
            .IsUnique()
            .HasDatabaseName("uq_stay_absences__field");
    }
}
