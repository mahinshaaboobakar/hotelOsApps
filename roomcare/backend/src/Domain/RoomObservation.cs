namespace HotelOS.RoomCare.Domain;

/// <summary>One inbound fact about a room, applied or not — never edited (chapter 03 §2.2).</summary>
/// <remarks>
/// Every observation is a row whether or not a window is open, so there is no
/// branch in which a change is "outside a window" and lost (§7.2, survey F2).
/// </remarks>
public class RoomObservation
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid PropertyId { get; set; }

    public string Source { get; set; } = ObservationSource.Pms;

    public DateTimeOffset OccurredAt { get; set; }

    public DateOnly? OperatingDay { get; set; }

    public string? Occupancy { get; set; }

    public string? Condition { get; set; }

    /// <summary>What was said about stays; null when the fact said nothing about them.</summary>
    public List<string>? StayStatuses { get; set; }

    public DateTimeOffset? NextSoldAt { get; set; }

    public bool? IsPseudoRoom { get; set; }

    public bool Applied { get; set; }

    public string Outcome { get; set; } = ObservationOutcome.Applied;

    /// <summary>The event it came from — idempotency on redelivery. Null for a person's entry.</summary>
    public Guid? EventId { get; set; }

    /// <summary>Who entered it, when the source is a person (redline 4's manual source).</summary>
    public Guid? ByUserId { get; set; }

    public string? Via { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
}
