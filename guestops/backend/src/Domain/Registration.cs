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
