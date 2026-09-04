namespace HotelOS.Jobs.Domain.Policy;

/// <summary>
/// Whether a department is present now — S7: kept by Workforce's
/// <c>shift.started</c> / <c>shift.ended</c> fan-out when the department follows
/// shifts, by <see cref="ServiceHours"/> otherwise, and "off" means the
/// property clock runs regardless (S7 D8).
/// </summary>
public class DepartmentPresence
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>The owner's on/off. Off: jobs run on the property clock.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Presence comes from the shift fan-out; service hours are the fallback.</summary>
    public bool FollowShifts { get; set; } = true;

    /// <summary>What the last fan-out said.</summary>
    public bool Staffed { get; set; }

    public DateTimeOffset? Since { get; set; }

    /// <summary>How many people the last shift.started reported, for the Live tab.</summary>
    public int OnShift { get; set; }
}
