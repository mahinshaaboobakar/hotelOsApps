namespace HotelOS.Jobs.Domain;

/// <summary>A photo or file on a job — the media itself is the platform's; this is the reference.</summary>
public class JobAttachment
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid MediaId { get; set; }

    public string Name { get; set; } = string.Empty;

    public long Bytes { get; set; }

    public Guid? AddedBy { get; set; }

    public DateTimeOffset At { get; set; }
}
