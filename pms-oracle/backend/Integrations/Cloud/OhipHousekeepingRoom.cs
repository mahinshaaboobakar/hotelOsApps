namespace PmsOracle.Integrations.Cloud;

/// <summary>
/// A room's type, as the housekeeping view reports it.
/// </summary>
/// <param name="RoomType">The type code.</param>
/// <param name="RoomClass">OHIP's grouping above the type.</param>
/// <param name="PseudoRoom">
/// Whether this is a bookkeeping construct rather than a physical room.
/// </param>
/// <remarks>
/// R4, and the only place in either flavour that says it. House accounts and
/// group masters arrive room-shaped: they have a number, a type and a status,
/// and they are not rooms. Mapping one onto a canonical room is a permanent
/// error, because the canonical room does not exist — so the flag is carried
/// and the fact is marked rather than filtered away silently.
/// </remarks>
public sealed record OhipRoomType(string? RoomType, string? RoomClass, bool PseudoRoom);

/// <summary>
/// The four status axes OHIP reports for a room, in one object.
/// </summary>
/// <param name="ReservationStatusList">
/// The stays touching this room today — <b>a real array here</b>, where the
/// on-site flavours send the same idea comma-separated in one string (R2).
/// </param>
/// <param name="FrontOfficeStatus">Occupancy, in OHIP's words — <c>Vacant</c>, <c>Occupied</c>.</param>
/// <param name="HousekeepingRoomStatus">The room's condition — and <c>""</c> means pick-up (R5).</param>
/// <param name="HousekeepingStatus">
/// The housekeeping department's own status, which OHIP keeps separately from
/// the room's condition and the on-site flavours do not send at all.
/// </param>
/// <remarks>
/// The reference read all four of these through <b>one</b> parse function,
/// which is why occupancy words and condition words ended up in a single
/// vocabulary. They are four axes and this type keeps them four; the
/// vocabularies sort out which words belong to which.
/// </remarks>
public sealed record OhipHousekeepingStatus(
    IReadOnlyList<string> ReservationStatusList,
    string? FrontOfficeStatus,
    string? HousekeepingRoomStatus,
    string? HousekeepingStatus);

/// <summary>
/// One room from OHIP's housekeeping overview.
/// </summary>
/// <param name="RoomId">The PMS's room number.</param>
/// <param name="RoomType">Its type, carrying the pseudo-room flag.</param>
/// <param name="Housekeeping">The four axes.</param>
/// <remarks>
/// One room, deliberately. OHIP's overview is paged — it returns
/// <c>totalPages</c>, <c>offset</c>, <c>limit</c>, <c>hasMore</c> and
/// <c>totalResults</c>, all of which the reference parsed and then ignored in
/// favour of <c>get(0)</c>. Paging is the poller's concern and the Hub's sync
/// state (R23); normalisation sees one room at a time and is not the place to
/// discover there were more.
/// </remarks>
public sealed record OhipHousekeepingRoom(
    string? RoomId,
    OhipRoomType? RoomType,
    OhipHousekeepingStatus? Housekeeping);
