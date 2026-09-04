namespace HotelOS.Jobs.Domain;

/// <summary>A note on a job — the guest's raising text is the first one (frame 2d).</summary>
public class JobNote
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>STAFF, GUEST or APPLICATION — the same vocabulary as <see cref="RaisedKind"/>.</summary>
    public string AuthorKind { get; set; } = RaisedKind.Staff;

    public Guid? AuthorId { get; set; }

    public string Text { get; set; } = string.Empty;

    /// <summary>Not shown to the guest.</summary>
    public bool Internal { get; set; }

    public DateTimeOffset At { get; set; }
}
