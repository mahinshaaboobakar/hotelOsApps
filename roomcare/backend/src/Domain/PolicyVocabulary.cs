namespace HotelOS.RoomCare.Domain;

/// <summary>How the day's work is created — S0: the button (HosPilot may press it) or automatic.</summary>
public static class TriggerMode
{
    public const string Prepare = "PREPARE";
    public const string Automatic = "AUTOMATIC";

    public static readonly IReadOnlyList<string> All = [Prepare, Automatic];
}

/// <summary>Whose word stands when an observation contradicts the condition — S4, per property.</summary>
public static class WhoLeads
{
    public const string RoomCare = "ROOM_CARE";
    public const string Pms = "PMS";

    public static readonly IReadOnlyList<string> All = [RoomCare, Pms];
}

/// <summary>Where stay facts usually come from — what the board expects; it gates nothing.</summary>
public static class StaySource
{
    public const string Pms = "PMS";
    public const string GuestOps = "GUESTOPS";
    public const string Manual = "MANUAL";

    public static readonly IReadOnlyList<string> All = [Pms, GuestOps, Manual];
}

/// <summary>The board's opening view — redline 4.</summary>
public static class BoardView
{
    public const string Map = "MAP";
    public const string Wall = "WALL";

    public static readonly IReadOnlyList<string> All = [Map, Wall];
}

/// <summary>The Room states tab's opening view — redline 5.</summary>
public static class StatesView
{
    public const string Sheet = "SHEET";
    public const string TapGrid = "TAP_GRID";
    public const string Compact = "COMPACT";

    public static readonly IReadOnlyList<string> All = [Sheet, TapGrid, Compact];
}

/// <summary>The property's linen rule — S5 case 4.</summary>
public static class LinenRuleKind
{
    /// <summary>Change every N days; the guest may defer.</summary>
    public const string EveryNDeferrable = "EVERY_N_DEFERRABLE";

    /// <summary>Must be changed by day N, whatever the guest says.</summary>
    public const string MustByN = "MUST_BY_N";

    public static readonly IReadOnlyList<string> All = [EveryNDeferrable, MustByN];
}

/// <summary>The property's towel rule — S5 case 5: a property rule, never the guest's.</summary>
public static class TowelRule
{
    public const string Daily = "DAILY";
    public const string GreenProgramme = "GREEN_PROGRAMME";

    public static readonly IReadOnlyList<string> All = [Daily, GreenProgramme];
}

/// <summary>How the proposal hands rooms out.</summary>
public static class AssignmentStrategy
{
    public const string Continuity = "CONTINUITY";
    public const string SameZone = "SAME_ZONE";
    public const string LowestLoad = "LOWEST_LOAD";

    public static readonly IReadOnlyList<string> All = [Continuity, SameZone, LowestLoad];
}

/// <summary>Whether a departure clean for a room not sold tonight is today's work.</summary>
public static class UnsoldDeparture
{
    public const string Today = "TODAY";
    public const string MayWait = "MAY_WAIT";

    public static readonly IReadOnlyList<string> All = [Today, MayWait];
}
