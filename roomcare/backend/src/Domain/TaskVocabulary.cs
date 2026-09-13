namespace HotelOS.RoomCare.Domain;

/// <summary>The two service windows of a hotel day — S5 case 2.</summary>
public static class ServiceWindowName
{
    public const string Morning = "MORNING";
    public const string Evening = "EVENING";

    public static readonly IReadOnlyList<string> All = [Morning, Evening];
}

/// <summary>The four services and the area's routine — S0, S3.</summary>
public static class Service
{
    public const string DepartureClean = "DEPARTURE_CLEAN";
    public const string DailyService = "DAILY_SERVICE";
    public const string Turndown = "TURNDOWN";
    public const string Refresh = "REFRESH";
    public const string AreaClean = "AREA_CLEAN";

    public static readonly IReadOnlyList<string> All = [DepartureClean, DailyService, Turndown, Refresh, AreaClean];
}

/// <summary>The priority bands of the property's ladder — S0's order.</summary>
public static class PriorityBand
{
    public const string SoldTonight = "SOLD_TONIGHT";
    public const string Departure = "DEPARTURE";
    public const string Daily = "DAILY";
    public const string Refresh = "REFRESH";

    public static readonly IReadOnlyList<string> All = [SoldTonight, Departure, Daily, Refresh];
}

/// <summary>Whether this service must change the linen — S5 case 4.</summary>
public static class LinenDue
{
    public const string NotDue = "NOT_DUE";
    public const string Due = "DUE";
    public const string Must = "MUST";

    public static readonly IReadOnlyList<string> All = [NotDue, Due, Must];
}

/// <summary>When a service is inspected — copied onto the task so a later edit never rewrites it.</summary>
public static class InspectionRule
{
    public const string None = "NONE";
    public const string Always = "ALWAYS";
    public const string Arrivals = "ARRIVALS";
    public const string EveryNth = "EVERY_NTH";
    public const string Vip = "VIP";

    public static readonly IReadOnlyList<string> All = [None, Always, Arrivals, EveryNth, Vip];
}

/// <summary>A task's lifecycle; how it ended is <see cref="TaskOutcome"/>.</summary>
public static class RoomTaskStatus
{
    public const string PendingPolicy = "PENDING_POLICY";
    public const string Planned = "PLANNED";
    public const string Assigned = "ASSIGNED";
    public const string InProgress = "IN_PROGRESS";
    public const string Ended = "ENDED";
    public const string ClosedByPolicy = "CLOSED_BY_POLICY";

    public static readonly IReadOnlyList<string> All = [PendingPolicy, Planned, Assigned, InProgress, Ended, ClosedByPolicy];

    /// <summary>Still asking for work.</summary>
    public static readonly string[] Open = [PendingPolicy, Planned, Assigned, InProgress];

    /// <summary>Not yet begun — what a reconcile may still change.</summary>
    public static readonly string[] Unstarted = [PendingPolicy, Planned, Assigned];
}

/// <summary>How a room's window ended — every room, every window, has one (S5 c1, c9).</summary>
public static class TaskOutcome
{
    public const string Done = "DONE";
    public const string Partial = "PARTIAL";
    public const string Declined = "DECLINED";
    public const string Dnd = "DND";
    public const string SkippedByGuest = "SKIPPED_BY_GUEST";
    public const string NotReached = "NOT_REACHED";
    public const string SupervisorDndApproved = "SUPERVISOR_DND_APPROVED";
    public const string SupervisorCleaned = "SUPERVISOR_CLEANED";
    public const string SupervisorDecided = "SUPERVISOR_DECIDED";

    public static readonly IReadOnlyList<string> All =
        [Done, Partial, Declined, Dnd, SkippedByGuest, NotReached, SupervisorDndApproved, SupervisorCleaned, SupervisorDecided];

    /// <summary>Outcomes that make a window one without service — S5 c9: declined days count.</summary>
    public static readonly string[] WithoutService = [Declined, Dnd, SkippedByGuest, SupervisorDndApproved];
}

/// <summary>What a partial service did — S5 c1 way 3.</summary>
public static class PartialPart
{
    public const string Bathroom = "BATHROOM";
    public const string Towels = "TOWELS";
    public const string Rubbish = "RUBBISH";
    public const string Bed = "BED";

    public static readonly IReadOnlyList<string> All = [Bathroom, Towels, Rubbish, Bed];
}

/// <summary>What an attendant found at the door — the DND re-check trail.</summary>
public static class AttemptFound
{
    public const string Done = "DONE";
    public const string Partial = "PARTIAL";
    public const string Declined = "DECLINED";
    public const string Dnd = "DND";

    public static readonly IReadOnlyList<string> All = [Done, Partial, Declined, Dnd];
}

/// <summary>Who decided a task should exist.</summary>
public static class DecidedBy
{
    public const string Prepare = "PREPARE";
    public const string Automatic = "AUTOMATIC";
    public const string Supervisor = "SUPERVISOR";
    public const string System = "SYSTEM";

    public static readonly IReadOnlyList<string> All = [Prepare, Automatic, Supervisor, System];
}

/// <summary>The phases of a service — S0: strip, clean, make up, done, inspect.</summary>
public static class Phase
{
    public const string Strip = "STRIP";
    public const string Clean = "CLEAN";
    public const string MakeUp = "MAKE_UP";
    public const string Done = "DONE";
    public const string Inspect = "INSPECT";

    public static readonly IReadOnlyList<string> All = [Strip, Clean, MakeUp, Done, Inspect];
}

/// <summary>Where one phase stands.</summary>
public static class PhaseStatus
{
    public const string Pending = "PENDING";
    public const string Active = "ACTIVE";
    public const string Done = "DONE";
    public const string Skipped = "SKIPPED";
    public const string Failed = "FAILED";

    public static readonly IReadOnlyList<string> All = [Pending, Active, Done, Skipped, Failed];
}

/// <summary>Why an assignment row ended.</summary>
public static class AssignmentEnd
{
    public const string Reassigned = "REASSIGNED";
    public const string Ended = "ENDED";
    public const string ShiftEnded = "SHIFT_ENDED";
    public const string StaffExited = "STAFF_EXITED";

    public static readonly IReadOnlyList<string> All = [Reassigned, Ended, ShiftEnded, StaffExited];
}

/// <summary>How an assignment was made — the strategy's proposal, accepted, or a person's hand.</summary>
public static class AssignmentMode
{
    public const string Proposed = "PROPOSED";
    public const string Accepted = "ACCEPTED";
    public const string Manual = "MANUAL";

    public static readonly IReadOnlyList<string> All = [Proposed, Accepted, Manual];
}

/// <summary>Why a stretch of work stopped.</summary>
public static class SessionEnd
{
    public const string Pause = "PAUSE";
    public const string End = "END";
    public const string Reassigned = "REASSIGNED";

    public static readonly IReadOnlyList<string> All = [Pause, End, Reassigned];
}

/// <summary>What a history row records.</summary>
public static class HistoryKind
{
    public const string Transition = "TRANSITION";
    public const string Reduction = "REDUCTION";
    public const string Reprioritised = "REPRIORITISED";
    public const string SupervisorDecision = "SUPERVISOR_DECISION";
    public const string DisagreementCleared = "DISAGREEMENT_CLEARED";
    public const string ExtraTime = "EXTRA_TIME";

    public static readonly IReadOnlyList<string> All =
        [Transition, Reduction, Reprioritised, SupervisorDecision, DisagreementCleared, ExtraTime];
}

/// <summary>Why a room is in the supervisor's lane — chapter 03 §2.5.</summary>
public static class SupervisionReason
{
    public const string DaysWithoutService = "DAYS_WITHOUT_SERVICE";
    public const string Disagreement = "DISAGREEMENT";
    public const string ArrivalBeforeWindow = "ARRIVAL_BEFORE_WINDOW";
    public const string NobodyAvailable = "NOBODY_AVAILABLE";

    public static readonly IReadOnlyList<string> All = [DaysWithoutService, Disagreement, ArrivalBeforeWindow, NobodyAvailable];
}

/// <summary>The supervisor's decision, final (S5 c9).</summary>
public static class SupervisionDecision
{
    public const string DndApproved = "DND_APPROVED";
    public const string Clean = "CLEAN";
    public const string Other = "OTHER";

    public static readonly IReadOnlyList<string> All = [DndApproved, Clean, Other];
}

/// <summary>A press of the button: the first builds the day, every later one reconciles (S0).</summary>
public static class RunKind
{
    public const string First = "FIRST";
    public const string Reconcile = "RECONCILE";

    public static readonly IReadOnlyList<string> All = [First, Reconcile];
}

/// <summary>Where a planned deep clean stands — chapter 03 §2.9.</summary>
public static class DeepCleanStatus
{
    public const string Planned = "PLANNED";
    public const string BlockRequested = "BLOCK_REQUESTED";
    public const string Blocked = "BLOCKED";
    public const string InProgress = "IN_PROGRESS";
    public const string Returning = "RETURNING";
    public const string Done = "DONE";
    public const string Cancelled = "CANCELLED";

    public static readonly IReadOnlyList<string> All = [Planned, BlockRequested, Blocked, InProgress, Returning, Done, Cancelled];

    /// <summary>Not yet finished or abandoned.</summary>
    public static readonly string[] Open = [Planned, BlockRequested, Blocked, InProgress, Returning];
}
