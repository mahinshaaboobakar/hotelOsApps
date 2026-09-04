namespace HotelOS.Jobs.Domain;

/// <summary>An in-app nudge sent for a concern — S9 D10: in-app only, no channel, no config.</summary>
public class JobNudge
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid ToUserId { get; set; }

    public string Concern { get; set; } = Domain.Concern.AtRisk;

    /// <summary>The role the recipient held when nudged — assignee, supervisor, …</summary>
    public string AsRole { get; set; } = LadderRole.Assignee;

    public DateTimeOffset SentAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
}
