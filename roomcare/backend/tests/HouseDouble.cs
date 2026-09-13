using HotelOS.RoomCare.Application.Abstractions;

namespace HotelOS.RoomCare.Tests;

/// <summary>The house a test stands in — Coral Cove's shape: rooms, types, zones, areas and the people named in the frames.</summary>
public sealed class HouseDouble : IHouse
{
    public PropertyDaySettings? Settings { get; set; } = new("ccr", "Coral Cove Resort", "Asia/Kolkata", new TimeOnly(4, 0));

    public List<HouseRoom> Rooms { get; } = [];

    public List<HouseRoomType> Types { get; } = [new(Standard, "STD", "Standard"), new(Suite, "STE", "Suite")];

    public List<HouseZone> Zones { get; } = [new(ZoneOne, "Z1", "Zone 1 · Garden villas"), new(ZoneTwo, "Z2", "Zone 2 · Lake wing")];

    public List<HouseArea> Areas { get; } = [new(Lobby, "Lobby", "lobby")];

    public Dictionary<Guid, string> Names { get; } = [];

    public Guid Housekeeping { get; } = Guid.CreateVersion7();

    public static readonly Guid Standard = Guid.CreateVersion7();

    public static readonly Guid Suite = Guid.CreateVersion7();

    public static readonly Guid ZoneOne = Guid.CreateVersion7();

    public static readonly Guid ZoneTwo = Guid.CreateVersion7();

    public static readonly Guid Lobby = Guid.CreateVersion7();

    /// <summary>Add a room with a number, of the standard type unless said.</summary>
    public Guid Room(string number, Guid? type = null)
    {
        var id = Guid.CreateVersion7();
        Rooms.Add(new HouseRoom(id, number, type ?? Standard, Rooms.Count));
        return id;
    }

    public Task<PropertyDaySettings?> DaySettingsAsync(Guid propertyId, CancellationToken cancellationToken) => Task.FromResult(Settings);

    public Task<IReadOnlyList<HouseRoom>> RoomsAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HouseRoom>>(Rooms);

    public Task<IReadOnlyList<HouseRoomType>> RoomTypesAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HouseRoomType>>(Types);

    public Task<IReadOnlyList<HouseZone>> ZonesAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HouseZone>>(Zones);

    public Task<IReadOnlyList<HouseArea>> AreasAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HouseArea>>(Areas);

    public Task<Guid?> DepartmentIdAsync(Guid propertyId, string departmentCode, CancellationToken cancellationToken) =>
        Task.FromResult<Guid?>(departmentCode == "HK" ? Housekeeping : null);

    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(userIds.Where(Names.ContainsKey).ToDictionary(id => id, id => Names[id]));
}
