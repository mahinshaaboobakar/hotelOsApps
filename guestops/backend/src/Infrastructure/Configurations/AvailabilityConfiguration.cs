using HotelOS.GuestOps.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.Configurations;

/// <summary>The seller's control — GUEST-Q7.</summary>
public class StopSellConfiguration : IEntityTypeConfiguration<StopSell>
{
    public void Configure(EntityTypeBuilder<StopSell> builder)
    {
        builder.ToTable("stop_sells");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Reason).HasMaxLength(500);

        // Availability subtracts these per type over a date range.
        builder.HasIndex(s => new { s.PropertyId, s.RoomTypeId, s.FromDate, s.ToDate });

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_stop_sells__range", "to_date >= from_date"));
    }
}

/// <summary>
/// What EngineeringOps has taken out of order, as this application heard it.
/// </summary>
/// <remarks>
/// Keyed on the room: one open out-of-order period per room at a time, which is
/// what an event stream of *placed* and *returned* produces. A second row for
/// one room would mean two facts nobody reconciled.
/// </remarks>
public class RoomOutOfOrderConfiguration : IEntityTypeConfiguration<RoomOutOfOrder>
{
    public void Configure(EntityTypeBuilder<RoomOutOfOrder> builder)
    {
        builder.ToTable("rooms_out_of_order");
        builder.HasKey(r => r.RoomId);

        builder.HasIndex(r => new { r.PropertyId, r.FromDate, r.ToDate });
    }
}
