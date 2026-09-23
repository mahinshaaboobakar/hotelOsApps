namespace HotelOS.GuestOps.Domain;

/// <summary>
/// Which fields a card requires, and which of them are missing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pure, and separate from the service for that reason.</b> Whether a guest
/// counts as a visitor and which fields that makes required is a rule a
/// property can be asked to justify years later; it is decided here, over
/// values, so it can be tested exhaustively without a database.
/// </para>
/// <para>
/// <b>The property's answer, never the product's.</b> This type applies a
/// configured set. It contains no field list of its own, and adding one — even
/// "surely everyone needs a name" — would put one jurisdiction's practice into
/// every property's build.
/// </para>
/// </remarks>
public static class RegistrationRule
{
    /// <summary>Whether this nationality counts as from outside — §2.8.</summary>
    /// <param name="nationality">ISO 3166-1 alpha-2, or null when not yet captured.</param>
    /// <param name="homeCountry">The property's configured home country.</param>
    /// <returns>True when the guest is from outside the property's home country.</returns>
    /// <remarks>
    /// <para>
    /// <b>Unknown nationality is not treated as a visitor.</b> A blank field is
    /// a card that has not been filled in, and answering "visitor" would demand
    /// a passport of every guest whose card is merely incomplete — turning a
    /// data-entry gap into a refusal at the desk.
    /// </para>
    /// <para>
    /// Case-insensitive: sources send <c>in</c>, <c>IN</c> and <c>In</c>, and a
    /// case difference deciding whether a passport is demanded would be absurd.
    /// </para>
    /// </remarks>
    public static bool IsVisitor(string? nationality, string homeCountry)
        => !string.IsNullOrWhiteSpace(nationality)
            && !string.IsNullOrWhiteSpace(homeCountry)
            && !nationality.Equals(homeCountry, StringComparison.OrdinalIgnoreCase);

    /// <summary>The set this guest's card must carry.</summary>
    /// <param name="settings">The property's configuration.</param>
    /// <param name="nationality">The guest's nationality, if captured.</param>
    /// <returns>The configured field names, which may be empty.</returns>
    public static IReadOnlyList<string> RequiredFor(
        GuestOpsSettings settings, string? nationality)
        => IsVisitor(nationality, settings.HomeCountry)
            ? settings.RequiredForVisitors
            : settings.RequiredForHomeCountry;

    /// <summary>What the card is still missing.</summary>
    /// <param name="settings">The property's configuration.</param>
    /// <param name="card">The card as captured so far.</param>
    /// <returns>The required field names that carry no value, in configured order.</returns>
    /// <remarks>
    /// <b>Reported, never enforced here.</b> This returns a list; it does not
    /// throw and it does not block a check-in. A guest standing at the desk at
    /// midnight with a missing purpose-of-visit is served, and the card is
    /// completed after — the same reasoning that keeps a reporting obligation
    /// from gating anything (S19b).
    /// </remarks>
    public static IReadOnlyList<string> Missing(GuestOpsSettings settings, Registration card)
        => [.. RequiredFor(settings, card.Nationality).Where(field => !HasValue(card, field))];

    /// <summary>Whether one named field carries a value.</summary>
    /// <remarks>
    /// <b>Defined as <see cref="ValueOf"/> having something in it</b>, so the
    /// names this rule understands and the names a card can draw are one list
    /// rather than two that agree today. Two switches over these twenty-seven
    /// names would drift the first time a field was added, and the drift would
    /// show as a card reporting a field missing with no box to put it in.
    /// </remarks>
    private static bool HasValue(Registration card, string field)
        => Filled(ValueOf(card, field));

    /// <summary>What one named field of the card holds, as text.</summary>
    /// <param name="card">The card as captured so far.</param>
    /// <param name="field">The wire's snake_case name.</param>
    /// <returns>The value, or null where the card has none — or the name is unknown.</returns>
    /// <remarks>
    /// <para>
    /// <b>An unknown field name has no value, and is therefore missing rather
    /// than ignored.</b> A property that configures <c>passport_numbr</c> gets
    /// told the card lacks it, which is visible; silently treating an
    /// unrecognised name as satisfied would make a typo in the configuration
    /// look like compliance.
    /// </para>
    /// <para>
    /// <b>A date leaves as <c>yyyy-MM-dd</c> and nothing else.</b> This is a
    /// value, not a rendering: how a date reads belongs to the reader's locale
    /// and the property's zone (ADR 0174), decided where somebody is looking.
    /// </para>
    /// <para>
    /// <b><c>documents</c> is new here, and it is a behaviour change worth
    /// saying out loud.</b> The old switch had no arm for it, so a property
    /// that required scanned documents was told they were missing <i>however
    /// many it had</i> — the unknown-name arm answering for a name that is not
    /// unknown. The column existed throughout.
    /// </para>
    /// <para>
    /// Names are the wire's snake_case, because that is what the configuration
    /// screen writes and what a property reads back.
    /// </para>
    /// </remarks>
    public static string? ValueOf(Registration card, string field) => field switch
    {
        "name_as_on_id" => card.NameAsOnId,
        "date_of_birth" => Day(card.DateOfBirth),
        "nationality" => card.Nationality,
        "address_line" => card.AddressLine,
        "city" => card.City,
        "state" => card.State,
        "country" => card.Country,
        "postal_code" => card.PostalCode,
        "id_type" => card.IdType,
        "id_number" => card.IdNumber,
        "id_issuer" => card.IdIssuer,
        "id_expiry" => Day(card.IdExpiry),
        "arriving_from" => card.ArrivingFrom,
        "proceeding_to" => card.ProceedingTo,
        "purpose_of_visit" => card.PurposeOfVisit,
        "vehicle_number" => card.VehicleNumber,
        "passport_number" => card.PassportNumber,
        "passport_issue" => Day(card.PassportIssue),
        "passport_expiry" => Day(card.PassportExpiry),
        "passport_place" => card.PassportPlace,
        "visa_type" => card.VisaType,
        "visa_number" => card.VisaNumber,
        "visa_issue" => Day(card.VisaIssue),
        "visa_expiry" => Day(card.VisaExpiry),
        "arrived_in_country_on" => Day(card.ArrivedInCountryOn),
        "port_of_arrival" => card.PortOfArrival,
        "documents" => card.DocumentRefs,
        "signature" => card.SignatureRef,
        _ => null,
    };

    private static string? Day(DateOnly? date) => date?.ToString("yyyy-MM-dd");

    private static bool Filled(string? value) => !string.IsNullOrWhiteSpace(value);
}
