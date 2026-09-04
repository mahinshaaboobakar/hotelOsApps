namespace HotelOS.Jobs.Domain;

/// <summary>
/// A group tie between two equal jobs — S1 D2: water then towel, same room.
/// No effect on either clock; a step (parent › child) is the job's own
/// <c>ParentJobId</c>, not a link.
/// </summary>
public class JobLink
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid JobId { get; set; }

    public Guid LinkedJobId { get; set; }

    public Guid? LinkedBy { get; set; }

    public DateTimeOffset At { get; set; }
}
