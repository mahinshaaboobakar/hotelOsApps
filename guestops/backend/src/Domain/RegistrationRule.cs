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
    /// <para>
    /// <b>An unknown field name is missing, not ignored.</b> A property that
    /// configures <c>passport_numbr</c> gets told the card lacks it, which is
    /// visible; silently treating an unrecognised name as satisfied would make
    /// a typo in the configuration look like compliance.
    /// </para>
    /// <para>
    /// Names are the wire's snake_case, because that is what the configuration
    /// screen writes and what a property reads back.
    /// </para>
    /// </remarks>
    private static bool HasValue(Registration card, string field) => field switch
    {
        "name_as_on_id" => Filled(card.NameAsOnId),
        "date_of_birth" => card.DateOfBirth is not null,
        "nationality" => Filled(card.Nationality),
        "address_line" => Filled(card.AddressLine),
        "city" => Filled(card.City),
        "state" => Filled(card.State),
        "country" => Filled(card.Country),
        "postal_code" => Filled(card.PostalCode),
        "id_type" => Filled(card.IdType),
        "id_number" => Filled(card.IdNumber),
        "id_issuer" => Filled(card.IdIssuer),
        "id_expiry" => card.IdExpiry is not null,
        "arriving_from" => Filled(card.ArrivingFrom),
        "proceeding_to" => Filled(card.ProceedingTo),
        "purpose_of_visit" => Filled(card.PurposeOfVisit),
        "vehicle_number" => Filled(card.VehicleNumber),
        "passport_number" => Filled(card.PassportNumber),
        "passport_issue" => card.PassportIssue is not null,
        "passport_expiry" => card.PassportExpiry is not null,
        "passport_place" => Filled(card.PassportPlace),
        "visa_type" => Filled(card.VisaType),
        "visa_number" => Filled(card.VisaNumber),
        "visa_issue" => card.VisaIssue is not null,
        "visa_expiry" => card.VisaExpiry is not null,
        "arrived_in_country_on" => card.ArrivedInCountryOn is not null,
        "port_of_arrival" => Filled(card.PortOfArrival),
        "signature" => Filled(card.SignatureRef),
        _ => false,
    };

    private static bool Filled(string? value) => !string.IsNullOrWhiteSpace(value);
}
