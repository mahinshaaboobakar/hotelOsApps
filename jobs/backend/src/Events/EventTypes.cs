namespace HotelOS.Jobs.Events;

/// <summary>
/// The event types this application publishes and consumes — design §3, domain
/// <c>job</c>. Named for what happened, never for who produced it (ADR 0006).
/// </summary>
public static class EventTypes
{
    /// <summary>The aggregate every job event names.</summary>
    public const string JobAggregate = "job";

    public const string JobCreated = "job.created";
    public const string JobAssigned = "job.assigned";
    public const string JobAccepted = "job.accepted";
    public const string JobStarted = "job.started";
    public const string JobHeld = "job.held";
    public const string JobResumed = "job.resumed";
    public const string JobResolved = "job.resolved";
    public const string JobClosed = "job.closed";
    public const string JobCancelled = "job.cancelled";
    public const string JobReopened = "job.reopened";
    public const string JobRated = "job.rated";
    public const string JobConcernChanged = "job.concern_changed";

    /// <summary>Consumed: the Engineering app's PPM plan fired (design §3).</summary>
    public const string PpmDue = "maintenance.ppm.due";

    /// <summary>Consumed: Workforce's shift fan-out — requested, S7.</summary>
    public const string ShiftStarted = "shift.started";
    public const string ShiftEnded = "shift.ended";

    /// <summary>Consumed: the stay that raised a job has left (S10 D2 window).</summary>
    public const string StayDeparted = "stay.departed";

    /// <summary>Consumed: an assignee has left the property.</summary>
    public const string StaffExited = "staff.exited";
}
