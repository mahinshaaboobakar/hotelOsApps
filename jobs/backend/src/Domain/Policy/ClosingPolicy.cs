namespace HotelOS.Jobs.Domain.Policy;

/// <summary>From RESOLVED to CLOSED — S2 D3, S10 D2, settings frame 5: the auto-close hours and whether the guest is asked.</summary>
public class ClosingPolicy
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>Null means the property default.</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>Hours after RESOLVED before the sweep closes it; the reopen window.</summary>
    public int AutoCloseHours { get; set; } = 4;

    /// <summary>Ask the guest to rate a guest-raised job once it closes.</summary>
    public bool RatingOnClose { get; set; } = true;
}
