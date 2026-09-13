namespace HotelOS.RoomCare.Module.Views;

/// <summary>Setup's first and third tabs — windows, trigger and the rules, with the live version (frames 7a, 7c).</summary>
public sealed record SetupView(PolicyView Policy, IReadOnlyList<WindowView> Windows, string? ChangedBy);

public sealed record PolicyView(
    string TriggerMode,
    string WhoLeads,
    string StaySource,
    string BoardDefaultView,
    string StatesDefaultView,
    string OnDepartureCondition,
    string LinenRuleKind,
    int LinenEveryDays,
    string Towels,
    bool TurndownEnabled,
    int RefreshAfterDays,
    int DndRecheckMinutes,
    int SupervisorAfterDays,
    IReadOnlyList<string> PriorityLadder,
    string AssignmentStrategy,
    string UnsoldDeparture,
    long Version,
    string? ChangedAt);

public sealed record WindowView(string Window, bool Enabled, string Starts, string Ends, bool AllowAssignmentOutside, long Version);

/// <summary>Services andminutes for one room type (frame 7b).</summary>
public sealed record ServicesView(
    IReadOnlyList<RoomTypeView> RoomTypes,
    string? RoomTypeId,
    IReadOnlyList<ServiceRowView> Services,
    bool InspectionApplicationInstalled);

public sealed record RoomTypeView(string Id, string Code, string Name, int Rooms);

public sealed record ServiceRowView(string Service, int Minutes, decimal Credits, IReadOnlyList<string> Phases, string InspectionRule, string? ChecklistRef, long Version, bool Saved);

/// <summary>Assignment andzones — the strategy, and each zone's rooms (frame 7d).</summary>
public sealed record ZonesView(string Strategy, IReadOnlyList<ZoneRowView> Zones, int Rooms, int Unzoned, IReadOnlyList<ZoneRoomView> RoomsList);

public sealed record ZoneRowView(string ZoneId, string Code, string Name, int Rooms, string? FirstRoom, string? LastRoom, string? Since);

public sealed record ZoneRoomView(string RoomId, string Number, string? ZoneId);

/// <summary>Areas — Master Data's public nodes and each one's routine, paged (frame 7e).</summary>
public sealed record AreasView(int Areas, int WithRoutine, IReadOnlyList<AreaRowView> Rows, Paging Paging);

public sealed record AreaRowView(string LocationId, string Name, string Kind, IReadOnlyList<string> Times, int? Minutes, bool Enabled, long Version);

/// <summary>The deep-clean plan per room type (frame 7f).</summary>
public sealed record DeepCleanPlanView(IReadOnlyList<PlanRowView> Rows);

public sealed record PlanRowView(string RoomTypeId, string RoomType, int? EveryMonths, int Rooms, int DueThisQuarter, long Version);

/// <summary>Property-wide access — the general manager's one grant (frame 7g).</summary>
public sealed record GrantsView(IReadOnlyList<GrantRowView> Grants);

public sealed record GrantRowView(string UserId, string Name, string GrantedAt, string? GrantedBy);
