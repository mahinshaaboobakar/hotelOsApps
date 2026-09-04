namespace HotelOS.Jobs.Domain.Policy;

/// <summary>
/// One rung of the accountable ladder — S5 D2, settings frame 10: a role, the
/// concern state that reaches it, and a delay after that state. Roles resolve
/// to today's people through Context, so a roster change never edits a policy.
/// </summary>
public class ConcernLadderStep
{
    public Guid Id { get; set; }

    public Guid PolicyId { get; set; }

    public string Priority { get; set; } = Domain.Priority.P3;

    /// <summary>1-based, in climbing order.</summary>
    public int StepNo { get; set; }

    /// <summary>A <see cref="LadderRole"/>.</summary>
    public string Role { get; set; } = LadderRole.Assignee;

    /// <summary>AT_RISK or BREACHED — when this rung starts to count.</summary>
    public string Trigger { get; set; } = Concern.Breached;

    /// <summary>Minutes after the trigger before the rung is reached.</summary>
    public int DelayMinutes { get; set; }
}
