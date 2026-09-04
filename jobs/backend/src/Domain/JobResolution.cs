namespace HotelOS.Jobs.Domain;

/// <summary>
/// What fixed it — S1 D7: one catalogue resolution plus the plain text box.
/// Item × location × resolution is the reporting concept the owner kept.
/// </summary>
public class JobResolution
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>Null when the resolution was "Other" — then <see cref="Note"/> is required.</summary>
    public Guid? ResolutionId { get; set; }

    public string? Note { get; set; }

    public Guid ResolvedBy { get; set; }

    public DateTimeOffset ResolvedAt { get; set; }
}
