namespace HotelOS.Jobs.Domain;

/// <summary>
/// A dated reminder on a job — S9 D2 (the hold warning, made from policy) and
/// S9 D3 (a person's own). Separate from escalation: it says "remember", never
/// "you are accountable".
/// </summary>
public class JobReminder
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid ForUserId { get; set; }

    public DateTimeOffset RemindAt { get; set; }

    public string Note { get; set; } = string.Empty;

    /// <summary><c>HOLD</c> from the hold policy, <c>MANUAL</c> from a person.</summary>
    public string Kind { get; set; } = ReminderKind.Manual;

    public DateTimeOffset? FiredAt { get; set; }
}

/// <summary>Where a reminder came from.</summary>
public static class ReminderKind
{
    public const string Manual = "MANUAL";
    public const string Hold = "HOLD";
}
