namespace HotelOS.RoomCare.Module.Views;

/// <summary>The board's strip — the house in counts, and whether the PMS is speaking (frames 1a, 1b).</summary>
public sealed record StripView(
    int Rooms,
    int Dirty,
    int InProgress,
    int Ready,
    int Pending,
    int Blocked,
    int Supervision,
    string? LastFactAt,
    string? SilentSince,
    string At,
    string? Window);

/// <summary>One room on the map and the wall — the same data set for both views.</summary>
public sealed record BoardRoomView(
    string Id,
    string Number,
    string Condition,
    string Source,
    string? SetBy,
    string SetAt,
    string Occupancy,
    int? VacantDays,
    string? SoldAt,
    string? TaskId,
    long? TaskVersion,
    string? Service,
    string? Reduction,
    string? EarliestAt,
    int? Priority,
    string? AttendantId,
    string? Attendant,
    OutcomeView Outcome,
    string? Linen,
    MarksView Marks,
    long Version);

/// <summary>How a room's day stands so far — a kind the screen words, with its time and detail.</summary>
/// <remarks>
/// Kinds: <c>NONE · IN_PROGRESS · DONE · PARTIAL · DECLINED · DND · WAITING · READY · INSPECTION_REQUESTED ·
/// SUPERVISION · DISAGREEMENT · PENDING · NOBODY_AVAILABLE · NEW_SINCE · BLOCKED · ENDED</c>.
/// </remarks>
public sealed record OutcomeView(string Kind, string? At = null, string? Until = null, string? Detail = null, int? Days = null);

/// <summary>The corner marks — each an exception a glance must catch.</summary>
public sealed record MarksView(
    bool SoldTonight,
    bool Dnd,
    bool Disagreement,
    bool Blocked,
    bool Supervision,
    bool Pending,
    bool InProgress,
    bool NewSince,
    bool Manual);

/// <summary>A zone's header and its rooms, in the house's order.</summary>
public sealed record ZoneGroupView(string? ZoneId, string Name, ZoneCountsView Counts, IReadOnlyList<BoardRoomView> Rooms);

public sealed record ZoneCountsView(int Rooms, int Dirty, int InProgress, int Ready, int Dnd, int Blocked, int Supervision, int Pending);

/// <summary>The whole board: the strip, the property's default view, and every zone.</summary>
public sealed record BoardPageView(StripView Strip, string DefaultView, IReadOnlyList<ZoneGroupView> Zones);
