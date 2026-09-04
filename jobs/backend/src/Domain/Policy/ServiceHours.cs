namespace HotelOS.Jobs.Domain.Policy;

/// <summary>The fallback presence window — S7 D8: when Workforce has no roster for a department, these hours say it is present.</summary>
public class ServiceHours
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>Null means the property's own hours.</summary>
    public string? DepartmentCode { get; set; }

    public TimeOnly From { get; set; }

    /// <summary>May be earlier than <see cref="From"/>: the window crosses midnight.</summary>
    public TimeOnly To { get; set; }

    /// <summary>Whether a local time of day falls inside the window.</summary>
    public bool Contains(TimeOnly at) =>
        From <= To ? at >= From && at < To : at >= From || at < To;
}
