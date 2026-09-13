namespace HotelOS.RoomCare.Domain;

/// <summary>The words a room's condition is written in — chapter 03 §2.1.</summary>
/// <remarks>
/// Strings, not CLR enums, with a CHECK constraint per column: a value the
/// database refuses is a value no code path can store, and the wire and the
/// row spell it the same way.
/// </remarks>
public static class Condition
{
    public const string Dirty = "DIRTY";
    public const string Clean = "CLEAN";
    public const string Inspected = "INSPECTED";

    /// <summary>What Room Care may set. Out of order and out of service are the block's, never written here.</summary>
    public static readonly IReadOnlyList<string> All = [Dirty, Clean, Inspected];
}

/// <summary>Who or what set the condition — S4's provenance, shown wherever the condition is shown.</summary>
public static class ConditionSource
{
    public const string Attendant = "ATTENDANT";
    public const string Inspection = "INSPECTION";
    public const string Supervisor = "SUPERVISOR";
    public const string Pms = "PMS";
    public const string GuestOps = "GUESTOPS";
    public const string Manual = "MANUAL";
    public const string System = "SYSTEM";

    public static readonly IReadOnlyList<string> All = [Attendant, Inspection, Supervisor, Pms, GuestOps, Manual, System];

    /// <summary>The deliberate acts the ordering clause protects from an older observation (S4; redline 4 adds manual).</summary>
    public static readonly IReadOnlyList<string> Deliberate = [Attendant, Inspection, Supervisor, Manual];
}

/// <summary>Whether a guest is in the room — observed, never decided by Room Care.</summary>
public static class Occupancy
{
    public const string Vacant = "VACANT";
    public const string Occupied = "OCCUPIED";
    public const string Unknown = "UNKNOWN";

    public static readonly IReadOnlyList<string> All = [Vacant, Occupied, Unknown];
}

/// <summary>Where an inbound fact about a room came from — chapter 03 §2.2.</summary>
public static class ObservationSource
{
    public const string Pms = "PMS";
    public const string GuestOps = "GUESTOPS";
    public const string Manual = "MANUAL";
    public const string Engineering = "ENGINEERING";
    public const string System = "SYSTEM";

    public static readonly IReadOnlyList<string> All = [Pms, GuestOps, Manual, Engineering, System];
}

/// <summary>What Room Care did with an observation — the ordering clause's verdict (S4).</summary>
public static class ObservationOutcome
{
    public const string Applied = "APPLIED";
    public const string OlderThanAct = "OLDER_THAN_ACT";
    public const string DisagreementFlagged = "DISAGREEMENT_FLAGGED";
    public const string AppliedPmsLeads = "APPLIED_PMS_LEADS";

    public static readonly IReadOnlyList<string> All = [Applied, OlderThanAct, DisagreementFlagged, AppliedPmsLeads];
}

/// <summary>The stay statuses a room may carry, as the wire's <c>StayLifecycle</c> names them.</summary>
public static class StayStatus
{
    public const string Booked = "BOOKED";
    public const string CheckedIn = "CHECKED_IN";
    public const string CheckedOut = "CHECKED_OUT";
    public const string Cancelled = "CANCELLED";
    public const string NoShow = "NO_SHOW";
    public const string DueOut = "DUE_OUT";
    public const string Waitlisted = "WAITLISTED";
    public const string Pending = "PENDING";

    public static readonly IReadOnlyList<string> All =
        [Booked, CheckedIn, CheckedOut, Cancelled, NoShow, DueOut, Waitlisted, Pending];
}

/// <summary>Who acted — a kind and an id, never a name (chapter 03 §2).</summary>
public static class ActorKind
{
    public const string User = "USER";
    public const string System = "SYSTEM";
    public const string Application = "APPLICATION";

    public static readonly IReadOnlyList<string> All = [User, System, Application];
}

/// <summary>How a person's act reached Room Care — S7: through HosPilot, still as the person.</summary>
public static class Via
{
    public const string App = "APP";
    public const string HosPilot = "HOSPILOT";

    public static readonly IReadOnlyList<string> All = [App, HosPilot];
}
