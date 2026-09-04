namespace HotelOS.Jobs.Domain;

/// <summary>
/// Who holds a job — a person or a team, from today's roster (S3 D1). One open
/// row per job; a reassignment ends the old row and opens a new one, so the
/// history is the table.
/// </summary>
public class JobAssignment
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>The person, when it is a person.</summary>
    public Guid? AssigneeUserId { get; set; }

    /// <summary>Workforce's team, when it is a team — waits for the team object (S3 D1 request).</summary>
    public Guid? TeamId { get; set; }

    /// <summary><c>MANUAL</c> by a person, <c>AUTO</c> by the flow.</summary>
    public string How { get; set; } = AssignmentHow.Manual;

    public Guid? AssignedBy { get; set; }

    public DateTimeOffset AssignedAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    /// <summary>Set when reassigned away, or when the job ends.</summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>Whether this is the row the board shows as "Assigned to".</summary>
    public bool IsCurrent => EndedAt is null;
}

/// <summary>How an assignment came to be.</summary>
public static class AssignmentHow
{
    public const string Manual = "MANUAL";
    public const string Auto = "AUTO";
}
