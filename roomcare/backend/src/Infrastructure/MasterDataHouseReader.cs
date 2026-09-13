using HotelOS.RoomCare.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Infrastructure;

/// <summary>Reads the property's house from Master Data's tables through the install grant — never over the wire.</summary>
/// <remarks>
/// Absent, deleted and another property's rows all answer the same way: not
/// here. Lifecycle filtering (<c>active</c>, <c>deleted_at</c>) is the reader's,
/// because reading tables inherits what the RPC surface used to apply.
/// </remarks>
public sealed class MasterDataHouseReader(RoomCareDbContext db) : IHouse
{
    /// <summary>The public-area location types — S3's nodes, as Master Data spells them.</summary>
    public static readonly string[] AreaTypes = ["corridor", "lobby", "restaurant", "pool", "terrace", "back_of_house"];

    public async Task<PropertyDaySettings?> DaySettingsAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await db.MasterDataProperties
            .Where(p => p.Id == propertyId)
            .Select(p => new { p.Code, p.Timezone, p.BusinessDayBoundary })
            .FirstOrDefaultAsync(cancellationToken);
        return property is null || string.IsNullOrWhiteSpace(property.Timezone)
            ? null
            : new PropertyDaySettings(property.Code, property.Timezone, property.BusinessDayBoundary);
    }

    public async Task<IReadOnlyList<HouseRoom>> RoomsAsync(Guid propertyId, CancellationToken cancellationToken) =>
        await db.MasterDataRooms
            .Where(r => r.PropertyId == propertyId && r.Active && r.DeletedAt == null)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.RoomNumber)
            .Select(r => new HouseRoom(r.Id, r.RoomNumber, r.RoomTypeId, r.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HouseRoomType>> RoomTypesAsync(Guid propertyId, CancellationToken cancellationToken) =>
        await db.MasterDataRoomTypes
            .Where(t => t.PropertyId == propertyId && t.DeletedAt == null)
            .OrderBy(t => t.Code)
            .Select(t => new HouseRoomType(t.Id, t.Code, t.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HouseZone>> ZonesAsync(Guid propertyId, CancellationToken cancellationToken) =>
        await db.MasterDataZones
            .Where(z => z.PropertyId == propertyId && z.Active && z.DeletedAt == null)
            .OrderBy(z => z.Code)
            .Select(z => new HouseZone(z.Id, z.Code, z.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HouseArea>> AreasAsync(Guid propertyId, CancellationToken cancellationToken) =>
        await db.MasterDataLocations
            .Where(l => l.PropertyId == propertyId && l.Active && l.DeletedAt == null && AreaTypes.Contains(l.LocationType))
            .OrderBy(l => l.Name)
            .Select(l => new HouseArea(l.Id, l.Name, l.LocationType))
            .ToListAsync(cancellationToken);

    public Task<Guid?> DepartmentIdAsync(Guid propertyId, string departmentCode, CancellationToken cancellationToken) =>
        db.MasterDataDepartments
            .Where(d => d.PropertyId == propertyId && d.DeletedAt == null && EF.Functions.ILike(d.Code, departmentCode))
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var ids = userIds.Select(id => (Guid?)id).ToList();
        var rows = await db.MasterDataStaff
            .Where(s => s.UserId != null && ids.Contains(s.UserId) && s.DeletedAt == null)
            .Select(s => new { s.UserId, s.DisplayName })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(r => r.UserId!.Value).ToDictionary(g => g.Key, g => g.First().DisplayName);
    }
}
