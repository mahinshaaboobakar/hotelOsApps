namespace HotelOS.Jobs.Application.Settings;

/// <summary>A policy with its clock and ladder — settings frames 8–10, saved whole.</summary>
public sealed record ConcernPolicyCommand
{
    public Guid? Id { get; init; }

    public long? ExpectedVersion { get; init; }

    public required string Name { get; init; }

    public string? DepartmentCode { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? ItemId { get; init; }

    public int UntriagedStuckMinutes { get; init; } = 15;

    public required IReadOnlyList<RuleCommand> Rules { get; init; }

    public required IReadOnlyList<LadderStepCommand> Ladder { get; init; }
}

/// <summary>One priority's clock.</summary>
public sealed record RuleCommand(
    string Priority,
    int? DueWithinMinutes,
    int AtRiskPercent,
    int? NotAcceptedMinutes,
    int? NoSessionMinutes,
    bool ManagerAtRisk,
    bool RunsOutsidePresence);

/// <summary>One rung.</summary>
public sealed record LadderStepCommand(string Priority, int StepNo, string Role, string Trigger, int DelayMinutes);

/// <summary>Who is told — one row of frame 3.</summary>
public sealed record SubscriptionCommand(
    string Role, string Concern, string? OnlyPriority, string? DepartmentCode, int? RepeatMinutes);

/// <summary>A department's presence switches — frame 2.</summary>
public sealed record PresenceCommand(string DepartmentCode, bool Enabled, bool FollowShifts);

/// <summary>Service hours for a department, or the property when the code is null.</summary>
public sealed record ServiceHoursCommand(string? DepartmentCode, TimeOnly From, TimeOnly To);

/// <summary>Auto-close and rating — frame 5.</summary>
public sealed record ClosingCommand(string? DepartmentCode, int AutoCloseHours, bool RatingOnClose);

/// <summary>Holds — frame 4.</summary>
public sealed record HoldPolicyCommand(int MaxHoldDays, int WarnDaysBefore, string WarnRole, bool WarnAssigneeOnDay);
