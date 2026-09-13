namespace HotelOS.RoomCare.Domain;

/// <summary>One phase of a task — the property's phases for the service, copied at creation (chapter 03 §2.4).</summary>
public class TaskPhase
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid PropertyId { get; set; }

    public string Phase { get; set; } = Domain.Phase.Clean;

    public int Sequence { get; set; }

    public string Status { get; set; } = PhaseStatus.Pending;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public Guid? ByUserId { get; set; }

    public string? Note { get; set; }
}

/// <summary>One time an attendant reached the door — the DND re-check trail (S5 c1 way 4).</summary>
public class TaskAttempt
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid PropertyId { get; set; }

    public DateTimeOffset At { get; set; }

    public Guid ByUserId { get; set; }

    public string Found { get; set; } = AttemptFound.Done;

    public List<string> PartialDone { get; set; } = [];

    public string? Note { get; set; }
}

/// <summary>One hand-over of a task; the current one has no end (chapter 03 §2.4).</summary>
public class TaskAssignment
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid UserId { get; set; }

    public string AssignedByKind { get; set; } = ActorKind.User;

    public Guid? AssignedById { get; set; }

    public string Via { get; set; } = Domain.Via.App;

    public DateTimeOffset AssignedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public string? EndReason { get; set; }

    public string Mode { get; set; } = AssignmentMode.Manual;

    public bool IsCurrent => EndedAt is null;
}

/// <summary>One stretch of one person's work on a task — accumulates across pauses (survey F24).</summary>
public class TaskWorkSession
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public string? EndReason { get; set; }

    public int Minutes { get; set; }

    public bool IsRunning => EndedAt is null;

    /// <summary>Close the stretch and count its whole minutes.</summary>
    public void Stop(DateTimeOffset at, string reason)
    {
        EndedAt = at;
        EndReason = reason;
        Minutes = (int)Math.Max(0, Math.Round((at - StartedAt).TotalMinutes));
    }
}

/// <summary>One transition or decision on a task, with its reason (chapter 03 §2.4).</summary>
public class TaskHistory
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid PropertyId { get; set; }

    public DateTimeOffset At { get; set; }

    public string ByKind { get; set; } = ActorKind.User;

    public Guid? ById { get; set; }

    public string Via { get; set; } = Domain.Via.App;

    public string Kind { get; set; } = HistoryKind.Transition;

    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }

    public string? Reason { get; set; }
}

/// <summary>A job closed against the task's room on its day — so the day reads whole (S5 c8).</summary>
public class TaskJobTouch
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid RoomId { get; set; }

    public DateOnly OperatingDay { get; set; }

    /// <summary>The day's task it sits beside, when one exists.</summary>
    public Guid? TaskId { get; set; }

    public Guid JobId { get; set; }

    public string? JobNumber { get; set; }

    public DateTimeOffset ClosedAt { get; set; }

    public string? Summary { get; set; }

    public Guid EventId { get; set; }
}

/// <summary>A fault an attendant found during a clean — their own record, never lost (redline 2).</summary>
public class TaskIssue
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid TaskId { get; set; }

    public Guid RoomId { get; set; }

    /// <summary>Jobs' catalogue id, when Jobs is installed; opaque here.</summary>
    public string? ItemHint { get; set; }

    public string Note { get; set; } = string.Empty;

    public Guid? MediaId { get; set; }

    public Guid ByUserId { get; set; }

    public DateTimeOffset At { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>From <c>job.created</c> on the correlation id; null until Jobs answers.</summary>
    public Guid? JobId { get; set; }
}
