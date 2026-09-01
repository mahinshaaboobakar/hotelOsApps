namespace HotelOS.GuestOps.Application.Registrations;

/// <summary>
/// The card as captured so far — section 2.7's field list.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every field is nullable and none is validated for presence here.</b> What
/// a card must carry is the property's configuration, not the product's
/// opinion: a required set lives in <c>GuestOpsSettings</c> and is applied by
/// <c>RegistrationRule</c>. A required-ness baked into this record would be one
/// jurisdiction's practice compiled into every property's build.
/// </para>
/// <para>
/// <b>What is deliberately absent is what a client may not set:</b> the card
/// number is minted from the property's series, <c>CapturedBy</c> is the
/// authenticated caller, and <c>SignedAt</c> is the server's clock. The API
/// having nowhere to put them is stronger than validating them away.
/// </para>
/// </remarks>
/// <param name="NameAsOnId">The guest's name as the document spells it.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Nationality">ISO 3166-1 alpha-2 — what decides the visitor block.</param>
/// <param name="AddressLine">Street address.</param>
/// <param name="City">City.</param>
/// <param name="State">State or province.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country of residence.</param>
/// <param name="PostalCode">Postal code.</param>
/// <param name="IdType">One of the property's configured accepted types.</param>
/// <param name="IdNumber">The document's number.</param>
/// <param name="IdIssuer">Who issued it.</param>
/// <param name="IdExpiry">When it expires.</param>
/// <param name="ArrivingFrom">Where the guest travelled from.</param>
/// <param name="ProceedingTo">Where the guest travels next.</param>
/// <param name="PurposeOfVisit">Why they are here.</param>
/// <param name="VehicleNumber">Optional at most properties, recorded at many.</param>
/// <param name="PassportNumber">Passport number.</param>
/// <param name="PassportIssue">Passport issue date.</param>
/// <param name="PassportExpiry">Passport expiry date.</param>
/// <param name="PassportPlace">Where the passport was issued.</param>
/// <param name="VisaType">Visa type, in the issuing country's vocabulary.</param>
/// <param name="VisaNumber">Visa number.</param>
/// <param name="VisaIssue">Visa issue date.</param>
/// <param name="VisaExpiry">Visa expiry date.</param>
/// <param name="ArrivedInCountryOn">When the guest entered the country.</param>
/// <param name="PortOfArrival">Where they entered.</param>
/// <param name="DocumentRefs">The platform's media references. Never blobs.</param>
/// <param name="SignatureRef">The captured signature's media reference.</param>
/// <param name="Signed">
/// Whether this capture completes the signature. The moment is the server's:
/// a client-supplied signing time on a legal-ish record is a value nobody can
/// corroborate.
/// </param>
public sealed record RegistrationEdit(
    string? NameAsOnId = null,
    DateOnly? DateOfBirth = null,
    string? Nationality = null,
    string? AddressLine = null,
    string? City = null,
    string? State = null,
    string? Country = null,
    string? PostalCode = null,
    string? IdType = null,
    string? IdNumber = null,
    string? IdIssuer = null,
    DateOnly? IdExpiry = null,
    string? ArrivingFrom = null,
    string? ProceedingTo = null,
    string? PurposeOfVisit = null,
    string? VehicleNumber = null,
    string? PassportNumber = null,
    DateOnly? PassportIssue = null,
    DateOnly? PassportExpiry = null,
    string? PassportPlace = null,
    string? VisaType = null,
    string? VisaNumber = null,
    DateOnly? VisaIssue = null,
    DateOnly? VisaExpiry = null,
    DateOnly? ArrivedInCountryOn = null,
    string? PortOfArrival = null,
    string? DocumentRefs = null,
    string? SignatureRef = null,
    bool Signed = false);
