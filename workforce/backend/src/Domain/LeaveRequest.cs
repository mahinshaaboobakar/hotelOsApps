namespace HotelOS.Workforce.Domain;

/// <summary>Where a request has got to.</summary>
public enum LeaveRequestState
{
    /// <summary>Raised, and waiting on the approver.</summary>
    Requested = 0,

    /// <summary>Granted. The balance was debited when this happened.</summary>
    Approved = 1,

    /// <summary>Refused, with a note. The balance never moved.</summary>
    Declined = 2,

    /// <summary>Withdrawn. If it had been approved, the balance was credited back.</summary>
    Cancelled = 3,
}

/// <summary>Somebody asking to be away.</summary>
/// <remarks>
/// Three fields — type, dates, note — plus who it is <b>for</b>, because
/// Workforce is a manager and HR application and a supervisor raises most of
/// these on somebody's behalf.
/// </remarks>
public class LeaveRequest
{
    /// <summary>This request's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Who would be away.</summary>
    public Guid StaffId { get; set; }

    /// <summary>Which kind of leave.</summary>
    public Guid LeaveTypeId { get; set; }

    /// <summary>The first day away.</summary>
    public DateOnly From { get; set; }

    /// <summary>The last day away.</summary>
    public DateOnly To { get; set; }

    /// <summary>Whole days, inclusive of both ends.</summary>
    /// <remarks>
    /// Computed from the dates rather than stored, so a request whose dates are
    /// corrected cannot keep a day count that disagrees with them. Half-days are
    /// not in v1 and nobody asked for them.
    /// </remarks>
    public decimal Days => To.DayNumber - From.DayNumber + 1;

    /// <summary>Why.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Where it has got to.</summary>
    public LeaveRequestState State { get; set; }

    /// <summary>Who raised it — the account, not the person it is for.</summary>
    /// <remarks>
    /// <c>WF-Q9</c>(b)'s provenance obligation at its third surface. Without it
    /// the record quietly claims a staff member raised something a supervisor
    /// raised for them.
    /// </remarks>
    public Guid? EnteredByUserId { get; set; }

    /// <summary>The approver this request resolved to when it was raised.</summary>
    /// <remarks>
    /// Stored, and this one is deliberate rather than derived: resolving it at
    /// decision time would move the request to a different queue if the person's
    /// posting changed while it waited, and a request that silently changes hands
    /// is one nobody is accountable for.
    /// </remarks>
    public Guid? ApproverStaffId { get; set; }

    /// <summary>When it was decided.</summary>
    public DateTimeOffset? DecidedAt { get; set; }

    /// <summary>What the approver said.</summary>
    public string DecisionNote { get; set; } = string.Empty;

    /// <summary>When it was raised.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }
}
