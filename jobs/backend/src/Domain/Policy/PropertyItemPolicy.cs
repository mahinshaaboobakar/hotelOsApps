namespace HotelOS.Jobs.Domain.Policy;

/// <summary>
/// One property's take on one catalogue item — S1 D12: activation, a display
/// name, and the overrides that win over the item's own defaults.
/// </summary>
public class PropertyItemPolicy
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid ItemId { get; set; }

    public bool ActiveHere { get; set; } = true;

    public string? DisplayName { get; set; }

    public string? DefaultPriority { get; set; }

    public int? DueWithinMinutes { get; set; }

    public Guid? ConcernPolicyId { get; set; }

    /// <summary>USER · TEAM — what AUTO picks from (S3 D1).</summary>
    public string AutoAssign { get; set; } = AutoAssignKind.User;

    public Guid? AutoAssignTeamId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long Version { get; set; }
}

/// <summary>What the flow assigns to when nobody picks.</summary>
public static class AutoAssignKind
{
    public const string User = "USER";
    public const string Team = "TEAM";
}
