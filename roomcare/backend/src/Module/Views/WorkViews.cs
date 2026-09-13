namespace HotelOS.RoomCare.Module.Views;

/// <summary>An attendant's list — what the supervisor accepted, in the ladder's order (frame 3).</summary>
public sealed record MyRoomsView(int Rooms, int Done, int InProgress, int PlannedMinutes, string At, IReadOnlyList<MyRoomView> Rows, Paging Paging);

public sealed record MyRoomView(
    string TaskId,
    long Version,
    string RoomId,
    string Room,
    string Service,
    string? Reduction,
    string? SoldAt,
    int Priority,
    string? EarliestAt,
    string Linen,
    int? DeclinedDay,
    OutcomeView State,
    string? RecheckAt);

/// <summary>At the door — one room in the attendant's hands (frame 3b).</summary>
public sealed record DoorView(
    MyRoomView Room,
    string? StartedAt,
    int MinutesExpected,
    int ExtraMinutes,
    int MinutesWorked,
    bool Running,
    string InspectionRule,
    IReadOnlyList<PhaseView> Phases,
    IReadOnlyList<string> PartialParts);

public sealed record PhaseView(int Sequence, string Phase, string Status);

/// <summary>The five widgets' answers — one question each (page 56; frame 8).</summary>
public sealed record RoomsReadyView(int Departures, int Ready, int InProgress, int Dirty, string At);

public sealed record ArrivalsWaitingView(int Total, IReadOnlyList<WidgetRoomView> Rows, string At);

public sealed record AttentionView(int Total, IReadOnlyList<WidgetRoomView> Rows, string At);

public sealed record AttendantsNowView(int? OnShift,int InARoom, IReadOnlyList<WidgetPersonView> Rows, string At);

public sealed record PendingView(int Total, IReadOnlyList<WidgetRoomView> Rows, string At);

/// <summary>A widget row that opens the room it names.</summary>
public sealed record WidgetRoomView(string RoomId, string Room, string What, string? At, string Tone);

public sealed record WidgetPersonView(string UserId, string Name, string Room, string Since);
