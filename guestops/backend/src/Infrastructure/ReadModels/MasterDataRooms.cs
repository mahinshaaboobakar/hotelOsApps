using HotelOS.GuestOps.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.ReadModels;

/// <summary>
/// One row of <c>masterdata.rooms</c>, as this application reads it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Keyless, and excluded from this application's migrations.</b> The table is
/// Master Data's: this application holds <c>SELECT</c> on that schema and no DDL
/// anywhere near it, so generating it from here would put two components in
/// charge of one table and the second one would lose.
/// </para>
/// <para>
/// Four columns, and only four. A read model that mirrored the room would drift
/// into being a copy of master data, which is the thing the constitution's
/// no-duplicated-master-data rule forbids — so this carries what availability
/// needs to count and nothing a screen might be tempted to render.
/// </para>
/// </remarks>
public sealed class MasterDataRoom
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid RoomTypeId { get; set; }

    /// <summary>ADR 0062's flag. An inactive room is not sellable.</summary>
    /// <remarks>
    /// A wing closed for renovation is inactive and very much still there, so
    /// it must not be counted as available — and it must not be confused with a
    /// deleted room, which is what <c>deleted_at</c> answers.
    /// </remarks>
    public bool Active { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Keyless, over Master Data's table. Never written.</summary>
public sealed class MasterDataRoomConfiguration : IEntityTypeConfiguration<MasterDataRoom>
{
    public void Configure(EntityTypeBuilder<MasterDataRoom> builder)
    {
        builder.HasNoKey();
        builder.ToTable("rooms", "masterdata", table => table.ExcludeFromMigrations());
    }
}

/// <summary>Rooms per type, counted at the moment of asking.</summary>
public sealed class RoomInventory(GuestOpsDbContext db) : IRoomInventory
{
    public async Task<IReadOnlyDictionary<Guid, int>> CountByTypeAsync(
        Guid propertyId,
        IReadOnlyCollection<Guid> roomTypeIds,
        CancellationToken cancellationToken)
    {
        var query = db.Set<MasterDataRoom>()
            .Where(r => r.PropertyId == propertyId && r.Active && r.DeletedAt == null);

        if (roomTypeIds.Count > 0)
        {
            query = query.Where(r => roomTypeIds.Contains(r.RoomTypeId));
        }

        // One grouped count rather than a row per room: a five-hundred-room
        // property asked for a fortnight would otherwise carry every room into
        // memory to be counted there.
        return await query
            .GroupBy(r => r.RoomTypeId)
            .Select(g => new { RoomTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomTypeId, x => x.Count, cancellationToken);
    }
}
