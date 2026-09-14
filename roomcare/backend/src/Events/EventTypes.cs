namespace HotelOS.RoomCare.Events;

/// <summary>Every event Room Care publishes or consumes, by the names the manifest declares — chapter 03 §3.</summary>
public static class EventTypes
{
    /// <summary>The room's aggregate — <c>entity_version</c> is <c>room_state.version</c>.</summary>
    public const string RoomAggregate = "room";

    /// <summary>The task's aggregate — chapter 03 §3.2; the same word <c>model.fga</c> uses for the object type.</summary>
    public const string TaskAggregate = "room_task";

    public const string RunAggregate = "prepare_run";

    public const string SupervisionAggregate = "room_supervision";

    public const string DeepCleanAggregate = "deep_clean";

    public const string IssueAggregate = "task_issue";

    public const string RestockAggregate = "restock";

    /// <summary>A grant's aggregate — the grant row, never the shared property aggregate (see <c>ManagerGrants</c>).</summary>
    public const string ManagerGrantAggregate = "roomcare_manager_grant";

    public const string RoomCleaned = "room.cleaned";
    public const string RoomInspected = "room.inspected";
    public const string RoomConditionChanged = "room.condition_changed";

    public const string DayPrepared = "roomcare.day.prepared";
    public const string TaskCreated = "roomcare.task.created";
    public const string TaskAssigned = "roomcare.task.assigned";
    public const string TaskStarted = "roomcare.task.started";
    public const string TaskAttempted = "roomcare.task.attempted";
    public const string TaskEnded = "roomcare.task.ended";
    public const string TaskReduced = "roomcare.task.reduced";
    public const string SupervisionOpened = "roomcare.supervision.opened";
    public const string SupervisionDecided = "roomcare.supervision.decided";
    public const string DisagreementFlagged = "roomcare.disagreement.flagged";
    public const string DisagreementCleared = "roomcare.disagreement.cleared";
    public const string InspectionRequested = "roomcare.inspection.requested";
    public const string RoomRestocked = "roomcare.room.restocked";
    public const string IssueFound = "roomcare.issue.found";
    public const string DeepCleanDue = "roomcare.deep_clean.due";
    public const string BlockRequested = "roomcare.block.requested";
    public const string BlockReleaseRequested = "roomcare.block.release_requested";
    public const string ServiceMissed = "roomcare.service_missed";
    public const string ManagerGranted = "user.roomcare_manager_granted";
    public const string ManagerRevoked = "user.roomcare_manager_revoked";

    /// <summary>What Room Care publishes — the manifest's list, asserted equal by a test.</summary>
    public static readonly IReadOnlyList<string> Published =
    [
        RoomCleaned, RoomInspected, RoomConditionChanged, DayPrepared, TaskCreated, TaskAssigned, TaskStarted,
        TaskAttempted, TaskEnded, TaskReduced, SupervisionOpened, SupervisionDecided, DisagreementFlagged,
        DisagreementCleared, InspectionRequested, RoomRestocked, IssueFound, DeepCleanDue, BlockRequested,
        BlockReleaseRequested, ServiceMissed, ManagerGranted, ManagerRevoked,
    ];

    public const string RoomStateObserved = "room.state_observed";
    public const string StayArrived = "stay.arrived";
    public const string StayDeparted = "stay.departed";
    public const string StayRoomChanged = "stay.room_changed";
    public const string StayCorrected = "stay.corrected";
    public const string UserPosted = "user.posted";
    public const string UserPostingEnded = "user.posting_ended";
    public const string ShiftStarted = "shift.started";
    public const string ShiftEnded = "shift.ended";
    public const string JobCreated = "job.created";
    public const string JobClosed = "job.closed";
    public const string StaffExited = "staff.exited";

    /// <summary>What Room Care consumes — the manifest's list, asserted equal by a test.</summary>
    public static readonly IReadOnlyList<string> Subscribed =
    [
        RoomStateObserved, StayArrived, StayDeparted, StayRoomChanged, StayCorrected, UserPosted,
        UserPostingEnded, ShiftStarted, ShiftEnded, JobCreated, JobClosed, StaffExited,
    ];
}
