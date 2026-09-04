namespace HotelOS.Jobs.Domain.Policy;

/// <summary>
/// A named concern policy with a scope — S5 D1–D3 and settings frames 7–11:
/// property default, or a department's, a category's or an item's. The most
/// specific one that matches a job wins. Its clock is <see cref="ConcernPolicyRule"/>
/// rows, its ladder <see cref="ConcernLadderStep"/> rows.
/// </summary>
public class ConcernPolicy
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Null means the property default.</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>Set only with a department: the category level.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Set only with a category: the item level.</summary>
    public Guid? ItemId { get; set; }

    /// <summary>Stuck when NOT_TRIAGED this long — goes to the supervisor.</summary>
    public int UntriagedStuckMinutes { get; set; } = 15;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long Version { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>How specific: 0 property, 1 department, 2 category, 3 item.</summary>
    public int Specificity =>
        ItemId is not null ? 3 : CategoryId is not null ? 2 : DepartmentCode is not null ? 1 : 0;
}
