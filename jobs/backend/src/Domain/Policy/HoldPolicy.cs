namespace HotelOS.Jobs.Domain.Policy;

/// <summary>Waiting with a date — S9 D2, settings frame 4: what a hold must carry and who is warned before <c>hold_until</c>.</summary>
public class HoldPolicy
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>Longest a hold may run before it is STUCK for the supervisor.</summary>
    public int MaxHoldDays { get; set; } = 30;

    /// <summary>Days before <c>hold_until</c> the first warning goes.</summary>
    public int WarnDaysBefore { get; set; } = 1;

    /// <summary>The <see cref="LadderRole"/> the first warning goes to.</summary>
    public string WarnRole { get; set; } = LadderRole.Supervisor;

    /// <summary>Warn the assignee on the day itself.</summary>
    public bool WarnAssigneeOnDay { get; set; } = true;
}
