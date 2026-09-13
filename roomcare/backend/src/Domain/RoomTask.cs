namespace HotelOS.RoomCare.Domain;

/// <summary>One service for one room or area, on one operating day, in one window — chapter 03 §2.3.</summary>
/// <remarks>
/// A task has no human-facing number: the room number and the day are its
/// name. <see cref="Status"/> is the one lifecycle column and
/// <see cref="Outcome"/> says how it ended; there is no mirror of either (§7.10).
/// </remarks>
public class RoomTask
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>Master Data's node — a room, or a public area (S3).</summary>
    public Guid LocationId { get; set; }

    /// <summary>Set when the node is a room.</summary>
    public Guid? RoomId { get; set; }

    public DateOnly OperatingDay { get; set; }

    public string Window { get; set; } = ServiceWindowName.Morning;

    public string Service { get; set; } = Domain.Service.DailyService;

    public string Priority { get; set; } = PriorityBand.Daily;

    public int PriorityRank { get; set; }

    public DateTimeOffset? EarliestAt { get; set; }

    /// <summary>For an area's routine: the scheduled time within the day.</summary>
    public DateTimeOffset? DueAt { get; set; }

    public string LinenDue { get; set; } = Domain.LinenDue.NotDue;

    public int MinutesExpected { get; set; }

    public decimal Credits { get; set; }

    public string InspectionRule { get; set; } = Domain.InspectionRule.None;

    public string? ChecklistRef { get; set; }

    public string Status { get; set; } = RoomTaskStatus.Planned;

    public string? Outcome { get; set; }

    public List<string> PartialDone { get; set; } = [];

    /// <summary>A guest's reduction recorded on their behalf (S5 c1): what is not to be done.</summary>
    public string? Reduction { get; set; }

    public string DecidedBy { get; set; } = Domain.DecidedBy.Prepare;

    public Guid? DecisionRunId { get; set; }

    public DecisionInputs DecisionInputs { get; set; } = new();

    public Guid? AssignedToUserId { get; set; }

    /// <summary>The attendant asked for more time — minutes added to the expectation.</summary>
    public int ExtraMinutes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long Version { get; set; }

    public bool IsOpen => RoomTaskStatus.Open.Contains(Status);

    /// <summary>Record a change and move the version.</summary>
    public void Touch(DateTimeOffset at)
    {
        UpdatedAt = at;
        Version += 1;
    }
}

/// <summary>What a decision saw when it was made — recorded, never re-derived (chapter 03 §2.3).</summary>
public sealed record DecisionInputs
{
    public string? Condition { get; init; }

    public string? Occupancy { get; init; }

    public IReadOnlyList<string> StayStatuses { get; init; } = [];

    public DateTimeOffset? NextSoldAt { get; init; }

    public string? Wish { get; init; }

    public string? Window { get; init; }

    public long RuleVersion { get; init; }

    /// <summary>Why this service and not another — one sentence a person can read.</summary>
    public string? Reason { get; init; }
}
