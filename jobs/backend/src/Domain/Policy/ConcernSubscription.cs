namespace HotelOS.Jobs.Domain.Policy;

/// <summary>
/// Who is told — S5 D3, settings frame 3: for a role, which concern states
/// reach it, how often the nudge repeats, and for which departments. Separate
/// from the ladder: the ladder says who is accountable, this says who hears.
/// </summary>
public class ConcernSubscription
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>A <see cref="LadderRole"/>.</summary>
    public string Role { get; set; } = LadderRole.Supervisor;

    /// <summary>AT_RISK · BREACHED · STUCK · NOT_TRIAGED (the untriaged stuck).</summary>
    public string Concern { get; set; } = Domain.Concern.Breached;

    /// <summary>Only P1, when set; null means every priority.</summary>
    public string? OnlyPriority { get; set; }

    /// <summary>Null means every department.</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>Minutes between repeated nudges while the state lasts; null means once.</summary>
    public int? RepeatMinutes { get; set; }
}
