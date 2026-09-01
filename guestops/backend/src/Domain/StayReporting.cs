namespace HotelOS.GuestOps.Domain;

/// <summary>Where a reporting obligation has got to.</summary>
public enum ReportingState
{
    /// <summary>This stay must be filed and has not been.</summary>
    Needed = 1,

    /// <summary>A person filed it, and recorded the receipt.</summary>
    Filed = 2,

    /// <summary>The property's policy does not cover this stay.</summary>
    NotRequired = 3,
}

/// <summary>
/// Telling an authority about a guest — S19b.
/// </summary>
/// <remarks>
/// <para>
/// <b>A per-property capability, never a country's law compiled into the
/// product.</b> A property that has no obligation configures it off and never
/// sees the flag. The policy, this flag, and the record of a filing are
/// GuestOps's; <b>the submission is not</b>.
/// </para>
/// <para>
/// <b>HotelOS submits nothing.</b> Sending guest data to an authority is an
/// integration, and every integration on this platform is a connector — which
/// this would be the first <i>outbound</i> one of, landing on the write-back
/// capability <c>CONN-Q5</c> deferred. Recorded on that row: a statutory filing
/// is a distinct capability class — a legal assertion, no silent retry, and the
/// receipt is part of the record.
/// </para>
/// <para>
/// <b><see cref="Reference"/> is the receipt, and that is why this record exists
/// ahead of any connector.</b> The row is the property's evidence that it
/// complied, so its shape does not change when submission is automated: a person
/// files and records the receipt now, a connector records the same receipt
/// later, on the same row.
/// </para>
/// <para>
/// <b>And the flag never gates anything.</b> A stay with an outstanding filing
/// checks in, is served and checks out — A1's ruling applied to this
/// application's own obligation rather than a neighbour's capability.
/// </para>
/// </remarks>
public class StayReporting
{
    public Guid StayId { get; set; }

    /// <summary>Computed from the property's offset — "within 24 hours of arrival".</summary>
    public DateOnly? RequiredBy { get; set; }

    public ReportingState State { get; set; }

    public DateTimeOffset? FiledAt { get; set; }

    public Guid? FiledBy { get; set; }

    /// <summary>Which authority, as the property named it.</summary>
    public string? Authority { get; set; }

    /// <summary>The receipt the authority gave back.</summary>
    public string? Reference { get; set; }

    public RoomStay? Stay { get; set; }
}
