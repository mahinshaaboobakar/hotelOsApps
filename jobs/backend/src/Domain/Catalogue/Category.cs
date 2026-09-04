namespace HotelOS.Jobs.Domain.Catalogue;

/// <summary>
/// A catalogue category — S1 D5, organisation-scoped (ruling 3 of 2026-09-03):
/// a name and a department code from the ADR 0119 canon, nothing else.
/// </summary>
public class Category
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>The department whose jobs these are; the job number's middle part.</summary>
    public string DepartmentCode { get; set; } = string.Empty;

    public bool Active { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long Version { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }
}
