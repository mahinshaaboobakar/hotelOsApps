namespace HotelOS.Jobs.Domain;

/// <summary>
/// The closed vocabularies a job carries — walkthrough S1.18 and S2. Strings
/// with a CHECK constraint rather than enums, so a value reads the same in the
/// database, on the wire and on the screen, and adding one is a migration
/// rather than a recompile of every reader.
/// </summary>
public static class JobStatus
{
    public const string Scheduled = "SCHEDULED";
    public const string Raised = "RAISED";
    public const string Assigned = "ASSIGNED";
    public const string Accepted = "ACCEPTED";
    public const string InProgress = "IN_PROGRESS";
    public const string OnHold = "ON_HOLD";
    public const string Resolved = "RESOLVED";
    public const string Closed = "CLOSED";
    public const string Cancelled = "CANCELLED";

    /// <summary>Every status, in lifecycle order.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Scheduled, Raised, Assigned, Accepted, InProgress, OnHold, Resolved, Closed, Cancelled,
    ];

    /// <summary>Statuses the board shows and the sweep clocks — an array, so EF Core translates <c>Contains</c>.</summary>
    public static readonly string[] Open = [Raised, Assigned, Accepted, InProgress, OnHold];

    /// <summary>Whether a job in this status can still be worked on.</summary>
    public static bool IsOpen(string status) => Open.Contains(status);

    /// <summary>Whether this status is an end: nothing follows it but reopen.</summary>
    public static bool IsTerminal(string status) => status is Closed or Cancelled;
}

/// <summary>The priority chain's result — S1 D4.</summary>
public static class Priority
{
    public const string P1 = "P1";
    public const string P2 = "P2";
    public const string P3 = "P3";
    public const string NotTriaged = "NOT_TRIAGED";

    public static readonly IReadOnlyList<string> All = [P1, P2, P3, NotTriaged];
}

/// <summary>Who decided the priority — S1 D4: manual, the flow, the catalogue, nobody.</summary>
public static class PriorityDecidedBy
{
    public const string Manual = "MANUAL";
    public const string Flow = "FLOW";
    public const string Catalogue = "CATALOGUE";
    public const string None = "NONE";

    public static readonly IReadOnlyList<string> All = [Manual, Flow, Catalogue, None];
}

/// <summary>The channel a job arrived by — S1 D8.</summary>
public static class RaisedVia
{
    public const string App = "APP";
    public const string Qr = "QR";
    public const string GuestApp = "GUEST_APP";
    public const string WhatsApp = "WHATSAPP";

    public static readonly IReadOnlyList<string> All = [App, Qr, GuestApp, WhatsApp];
}

/// <summary>Who raised it — S1 D8; a guest is identified by the stay, never a user id.</summary>
public static class RaisedKind
{
    public const string Staff = "STAFF";
    public const string Guest = "GUEST";
    public const string Application = "APPLICATION";

    public static readonly IReadOnlyList<string> All = [Staff, Guest, Application];
}

/// <summary>The four concern states the sweep derives — S5 D1. Never a job column.</summary>
public static class Concern
{
    public const string OnTrack = "ON_TRACK";
    public const string AtRisk = "AT_RISK";
    public const string Breached = "BREACHED";
    public const string Stuck = "STUCK";

    public static readonly IReadOnlyList<string> All = [OnTrack, AtRisk, Breached, Stuck];
}

/// <summary>The roles a ladder step names — S5 D2. Roles, never people.</summary>
public static class LadderRole
{
    public const string Assignee = "ASSIGNEE";
    public const string Supervisor = "SUPERVISOR";
    public const string Manager = "MANAGER";
    public const string JobsManager = "JOBS_MANAGER";
    public const string GeneralManager = "GENERAL_MANAGER";

    public static readonly IReadOnlyList<string> All =
        [Assignee, Supervisor, Manager, JobsManager, GeneralManager];
}
