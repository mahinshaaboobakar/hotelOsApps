namespace HotelOS.Jobs.Domain;

/// <summary>
/// The lean job — walkthrough S1.18's twenty-three columns, plus the three the
/// design chapter adds (<see cref="Restricted"/>, the hold pair) and the two a
/// child step carries (<see cref="ParentJobId"/>, <see cref="StepNo"/>).
/// Everything else about a job is a satellite table.
/// </summary>
public class Job
{
    public Guid Id { get; set; }

    /// <summary><c>MRN-ENG-142</c>: property code upper, root department, one counter per property (S1 D3).</summary>
    public string JobNumber { get; set; } = string.Empty;

    public Guid PropertyId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid ItemId { get; set; }

    /// <summary>One node of Master Data's location tree (S1 D1).</summary>
    public Guid LocationId { get; set; }

    public Guid? AssetId { get; set; }

    public string DepartmentCode { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string? Details { get; set; }

    public string Priority { get; set; } = Domain.Priority.NotTriaged;

    public string PriorityDecidedBy { get; set; } = Domain.PriorityDecidedBy.None;

    public string RaisedVia { get; set; } = Domain.RaisedVia.App;

    public string RaisedKind { get; set; } = Domain.RaisedKind.Staff;

    /// <summary>The user for STAFF, the application's own reference for APPLICATION, null for GUEST.</summary>
    public Guid? RaisedById { get; set; }

    /// <summary>NOT NULL when <see cref="RaisedKind"/> is GUEST — the stay is the guest's identity (S1 D9).</summary>
    public Guid? StayId { get; set; }

    /// <summary>Set means SCHEDULED until 00:00 of that day, property time (S2 D3).</summary>
    public DateOnly? ScheduledFor { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public string JobStatus { get; set; } = Domain.JobStatus.Raised;

    /// <summary>The occurrence tag the raiser sent — never a schedule (S7).</summary>
    public string? Cycle { get; set; }

    public bool Restricted { get; set; }

    public string? HoldReason { get; set; }

    public DateTimeOffset? HoldUntil { get; set; }

    public Guid? ParentJobId { get; set; }

    public int? StepNo { get; set; }

    /// <summary>The policy the job resolved to when raised — stamped, so a later edit never rewrites its past.</summary>
    public Guid? ConcernPolicyId { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long Version { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public string? DeleteReason { get; set; }

    /// <summary>Whether the board shows it and the sweep clocks it.</summary>
    public bool IsOpen => Domain.JobStatus.IsOpen(JobStatus) && DeletedAt is null;

    /// <summary>A child step whose parent has not resolved carries a stopped clock (S1 D2).</summary>
    public bool IsStep => ParentJobId is not null;

    /// <summary>Move to a status, bumping the version and the audit pair.</summary>
    public void MoveTo(string status, Guid? by, DateTimeOffset at)
    {
        JobStatus = status;
        UpdatedBy = by;
        UpdatedAt = at;
        Version += 1;
    }

    /// <summary>Touch the audit pair without a status change.</summary>
    public void Touch(Guid? by, DateTimeOffset at)
    {
        UpdatedBy = by;
        UpdatedAt = at;
        Version += 1;
    }
}
