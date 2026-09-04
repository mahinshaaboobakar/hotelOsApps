namespace HotelOS.Jobs.Domain;

/// <summary>
/// One concern transition of a job — S5 D1: the sweep's output, never a job
/// column. "How many reached the manager this month" is one query over these
/// rows. The first row stamps the policy the job resolved to.
/// </summary>
public class JobConcernHistory
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>ON_TRACK · AT_RISK · BREACHED · STUCK.</summary>
    public string Concern { get; set; } = Domain.Concern.OnTrack;

    /// <summary>The ladder role accountable from this moment.</summary>
    public string AccountableRole { get; set; } = LadderRole.Assignee;

    /// <summary>Which step of the ladder, 1-based; 0 before any step applies.</summary>
    public int LadderStep { get; set; }

    /// <summary>Who that role resolved to when the sweep ran; null when nobody held it.</summary>
    public Guid? AccountableUserId { get; set; }

    public DateTimeOffset Since { get; set; }

    /// <summary>Why — "75 % of 40 min", "not accepted 8 min", "resumed from hold".</summary>
    public string Reason { get; set; } = string.Empty;

    public Guid? ConcernPolicyId { get; set; }
}
