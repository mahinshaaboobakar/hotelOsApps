namespace HotelOS.Jobs.Domain;

/// <summary>
/// The guest's rating — S10 D2: one row, written from the stay link, only for a
/// guest-raised job, only after CLOSED.
/// </summary>
public class JobRating
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid StayId { get; set; }

    /// <summary>1 to 5.</summary>
    public int Stars { get; set; }

    public string? Text { get; set; }

    public DateTimeOffset RatedAt { get; set; }
}
