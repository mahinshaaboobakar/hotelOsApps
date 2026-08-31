namespace HotelOS.GuestOps.Domain;

/// <summary>
/// The card the guest signs at check-in — §15 (g), owner 2026-08-31.
/// </summary>
/// <remarks>
/// <para>
/// <b>The field list is a proposal and the property decides what is
/// required</b>, twice over: once for home-country guests and once for guests
/// from outside. What a jurisdiction demands differs by country and by
/// property, so the product proposes a shape and never a legal minimum.
/// </para>
/// <para>
/// <b>No country is written into this model.</b> A guest is <i>from outside</i>
/// when their nationality is not the property's configured home country — so a
/// hotel in Kochi and a hotel in Dubai run the same build, each treating the
/// other's nationals that way. The block below is filled when it applies, and
/// applies because of the guest, not because of where the software runs.
/// </para>
/// <para>
/// A field a property does not use is <b>not removed from the model</b>, only
/// from its required set: a registration record has to stay readable for years,
/// and deleting a field to tidy a form makes old cards unreadable.
/// </para>
/// </remarks>
public class Registration
{
    public Guid StayId { get; set; }

    /// <summary>The property's own series — the hotelier reference's <c>grcNo</c>.</summary>
    public string? CardNumber { get; set; }

    public string? NameAsOnId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    /// <summary>ISO 3166-1 alpha-2. What decides the block below.</summary>
    public string? Nationality { get; set; }

    public string? AddressLine { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Country { get; set; }

    public string? PostalCode { get; set; }

    /// <summary>The property's configured list, seeded for its country.</summary>
    /// <remarks>
    /// Never a fixed enum in the product: Aadhaar and PAN are one country's
    /// vocabulary, an Emirates ID another's, and a passport everyone's.
    /// </remarks>
    public string? IdType { get; set; }

    public string? IdNumber { get; set; }

    public string? IdIssuer { get; set; }

    public DateOnly? IdExpiry { get; set; }

    public string? ArrivingFrom { get; set; }

    public string? ProceedingTo { get; set; }

    public string? PurposeOfVisit { get; set; }

    /// <summary>Optional at most properties, recorded at many.</summary>
    public string? VehicleNumber { get; set; }

    // --- the from-outside block, filled when it applies ---------------------

    public string? PassportNumber { get; set; }

    public DateOnly? PassportIssue { get; set; }

    public DateOnly? PassportExpiry { get; set; }

    public string? PassportPlace { get; set; }

    public string? VisaType { get; set; }

    public string? VisaNumber { get; set; }

    public DateOnly? VisaIssue { get; set; }

    public DateOnly? VisaExpiry { get; set; }

    public DateOnly? ArrivedInCountryOn { get; set; }

    public string? PortOfArrival { get; set; }

    // --- what was captured --------------------------------------------------

    /// <summary>The platform's media references. Never blobs here.</summary>
    public string? DocumentRefs { get; set; }

    public string? SignatureRef { get; set; }

    public DateTimeOffset? SignedAt { get; set; }

    public Guid? CapturedBy { get; set; }

    public RoomStay? Stay { get; set; }
}

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
