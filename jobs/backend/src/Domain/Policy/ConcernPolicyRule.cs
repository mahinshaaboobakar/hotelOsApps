namespace HotelOS.Jobs.Domain.Policy;

/// <summary>
/// The clock for one priority under one policy — settings frame 9: due within,
/// the at-risk point as a share of due, and the two stuck tests.
/// </summary>
public class ConcernPolicyRule
{
    public Guid Id { get; set; }

    public Guid PolicyId { get; set; }

    /// <summary>P1 · P2 · P3.</summary>
    public string Priority { get; set; } = Domain.Priority.P3;

    /// <summary>Null means "same shift": due at the end of the department's presence.</summary>
    public int? DueWithinMinutes { get; set; }

    /// <summary>AT_RISK once this share of the due window has elapsed, 1–99.</summary>
    public int AtRiskPercent { get; set; } = 75;

    /// <summary>STUCK when assigned and not accepted for this long; null disables.</summary>
    public int? NotAcceptedMinutes { get; set; }

    /// <summary>STUCK when accepted and no work session for this long; null disables.</summary>
    public int? NoSessionMinutes { get; set; }

    /// <summary>The owner's ask: the manager becomes accountable already at AT_RISK.</summary>
    public bool ManagerAtRisk { get; set; }

    /// <summary>Whether the clock keeps running when the department is not present (S7 D8).</summary>
    public bool RunsOutsidePresence { get; set; }
}
