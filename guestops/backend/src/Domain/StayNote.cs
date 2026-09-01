namespace HotelOS.GuestOps.Domain;

/// <summary>A remark about this stay, which dies with it — S19.</summary>
/// <remarks>
/// The distinction from a guest preference is not decorative: a preference
/// should be true next time and lives on <see cref="GuestIdentity"/>; a note is
/// about these nights.
/// </remarks>
public class StayNote
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }

    public string Text { get; set; } = string.Empty;

    public Guid? Author { get; set; }

    public DateTimeOffset At { get; set; }

    public RoomStay? Stay { get; set; }
}
