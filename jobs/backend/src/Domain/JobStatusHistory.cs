namespace HotelOS.Jobs.Domain;

/// <summary>One status transition of a job — S2; the History tab's status rows.</summary>
public class JobStatusHistory
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid PropertyId { get; set; }

    public string FromStatus { get; set; } = string.Empty;

    public string ToStatus { get; set; } = string.Empty;

    /// <summary>Null when the sweep, the flow or a consumer moved it.</summary>
    public Guid? ByUserId { get; set; }

    /// <summary>What moved it when no user did: <c>AUTO</c>, <c>SWEEP</c>, <c>PPM</c>, <c>GUEST</c>.</summary>
    public string? ByWhat { get; set; }

    public DateTimeOffset At { get; set; }

    public string? Note { get; set; }
}
