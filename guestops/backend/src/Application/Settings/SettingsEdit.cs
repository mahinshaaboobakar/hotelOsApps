using HotelOS.GuestOps.Domain;

namespace HotelOS.GuestOps.Application.Settings;

/// <summary>
/// The configuration values a property may set — §2.8.
/// </summary>
/// <param name="HomeCountry">
/// ISO 3166-1 alpha-2. Decides who counts as from outside, and is the one
/// setting that makes the same build serve every market.
/// </param>
/// <param name="RequiredForHomeCountry">Fields required of a home-country guest.</param>
/// <param name="RequiredForVisitors">Fields required of a guest from anywhere else.</param>
/// <param name="AcceptedIdTypes">The property's accepted documents, in its own words.</param>
/// <param name="SignatureRequired">Whether the card must be signed.</param>
/// <param name="PrintOnCheckIn">Whether the card prints as part of check-in.</param>
/// <param name="CardNumberPrefix">The registration series' prefix.</param>
/// <param name="ReportingRequired">Whether this property files with an authority at all.</param>
/// <param name="ReportingAppliesTo">Who the obligation covers.</param>
/// <param name="ReportingAuthority">Which authority, as the property names it.</param>
/// <param name="ReportingDueHours">The deadline as hours after arrival — R18.</param>
/// <remarks>
/// <b>A record rather than the entity.</b> The entity carries
/// <c>NextCardNumber</c> and <c>Version</c>, and neither is a client's to set —
/// the API having nowhere to put them is stronger than validating them away,
/// because a caller cannot then express the mistake.
/// </remarks>
public sealed record SettingsEdit(
    string HomeCountry,
    IReadOnlyList<string> RequiredForHomeCountry,
    IReadOnlyList<string> RequiredForVisitors,
    IReadOnlyList<string> AcceptedIdTypes,
    bool SignatureRequired,
    bool PrintOnCheckIn,
    string CardNumberPrefix,
    bool ReportingRequired,
    ReportingScope ReportingAppliesTo,
    string? ReportingAuthority,
    int ReportingDueHours);
