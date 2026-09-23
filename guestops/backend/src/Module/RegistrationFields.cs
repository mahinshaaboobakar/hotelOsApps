namespace HotelOS.GuestOps.Module;

/// <summary>
/// One box on the registration card — §2.7's field list, as the design proposes it.
/// </summary>
/// <param name="Name">The wire's snake_case name, which is also the configured name.</param>
/// <param name="Label">What the box is called on the card.</param>
/// <param name="Kind">
/// How it is captured: <c>text</c>, <c>date</c>, <c>choice</c> over the
/// property's own list, or <c>held</c> for a row this screen shows and cannot
/// capture.
/// </param>
/// <param name="Tall">Drawn as a paragraph rather than a line.</param>
/// <param name="Secret">Masked at rest, and revealed to the desk that types it.</param>
/// <param name="Placeholder">Shown where the card holds nothing.</param>
public readonly record struct CardBox(
    string Name,
    string Label,
    string Kind = "text",
    bool Tall = false,
    bool Secret = false,
    string? Placeholder = null);

/// <summary>
/// The card's shape — which boxes there are, in what order, paired how.
/// </summary>
/// <remarks>
/// <para>
/// <b>The field list is the design's; the required set is the property's.</b>
/// That is gold frame 15's caption, and it is the whole reason this file holds
/// no required-ness at all. Which of these a card must carry comes from
/// <c>GuestOpsSettings</c> by way of <c>RegistrationRule</c>, separately for a
/// guest from the home country and a guest from anywhere else — so a resort
/// taking weekend guests and a city hotel taking business visas run the same
/// build and ask for different papers.
/// </para>
/// <para>
/// <b>Every name here is a name <c>RegistrationRule.ValueOf</c> knows.</b> A
/// box whose name the rule did not know would read as permanently empty; a
/// required name with no box would be a card reporting a gap it offers no way
/// to close. The tests walk both directions rather than trusting this sentence.
/// </para>
/// <para>
/// <b>The list is longer than frame 15 draws, and the caption is the reason:</b>
/// <i>a field a property does not use is not deleted from the model</i>. The
/// frame draws one property's card, and that property configures neither an
/// issuing authority for a domestic document nor a separate postal code. Both
/// columns exist, both are configurable as required, and a card that could not
/// capture them would be the model with boxes missing rather than fields unused.
/// </para>
/// <para>
/// <b>The address is five boxes where the frame draws one tall one.</b> The
/// record holds line, city, state, country and postal code separately — a
/// filing is made on the parts — so one box that captured all five would write
/// a street into a column named <c>city</c>. Recorded as a deliberate
/// divergence from the drawing rather than silently: it is the owner's to
/// reverse, and the ledger carries it.
/// </para>
/// </remarks>
public static class RegistrationFields
{
    /// <summary>Everything above the conditional block, in the design's order.</summary>
    public static readonly IReadOnlyList<IReadOnlyList<CardBox>> Main =
    [
        [new CardBox("name_as_on_id", "Name as on the ID")],
        [
            new CardBox("date_of_birth", "Date of birth", "date"),

            // **Typed, not chosen, and that is a gap reported as a gap.** The
            // frame draws a country chooser; no list of countries exists in
            // this application or on the platform, and writing one here would
            // put a product's opinion of the world's countries into a hotel
            // application. The hint names the format the record stores.
            new CardBox("nationality", "Nationality", Placeholder: "two-letter country code"),
        ],
        [new CardBox("address_line", "Permanent address", Tall: true)],
        [new CardBox("city", "City"), new CardBox("state", "State or province")],
        [
            new CardBox("country", "Country", Placeholder: "two-letter country code"),
            new CardBox("postal_code", "Postal code"),
        ],
        [
            // The one chooser on the card, because its list is the property's
            // own — `AcceptedIdTypes`. Aadhaar and PAN are one country's
            // vocabulary, an Emirates ID another's, and a fixed enum here would
            // be the country-in-the-product defect this application refuses.
            new CardBox("id_type", "Identity document", "choice"),
            new CardBox("id_number", "Number", Secret: true),
        ],
        [
            new CardBox("id_issuer", "Issued by"),
            new CardBox("id_expiry", "Expires", "date"),
        ],
        [
            new CardBox("arriving_from", "Arriving from"),
            new CardBox("proceeding_to", "Proceeding to"),
        ],
        [
            new CardBox("purpose_of_visit", "Purpose of visit"),
            new CardBox("vehicle_number", "Vehicle", Placeholder: "optional at this property"),
        ],
    ];

    /// <summary>
    /// The block shown only for a guest from outside the property's home country.
    /// </summary>
    /// <remarks>
    /// <b>Conditional on the guest, never on where the software runs.</b> The
    /// property sets its home country, so a hotel in Kochi treats an Emirati
    /// guest this way and a hotel in Dubai treats an Indian guest this way —
    /// from this same list.
    /// </remarks>
    public static readonly IReadOnlyList<IReadOnlyList<CardBox>> Foreign =
    [
        [
            new CardBox("passport_number", "Passport number", Secret: true),
            new CardBox("passport_place", "Place of issue"),
        ],
        [
            new CardBox("passport_issue", "Passport issue", "date"),
            new CardBox("passport_expiry", "Passport expiry", "date"),
        ],
        [
            new CardBox("visa_type", "Visa type"),
            new CardBox("visa_number", "Visa number", Secret: true),
        ],
        [
            new CardBox("visa_issue", "Visa issue", "date"),
            new CardBox("visa_expiry", "Visa expiry", "date"),
        ],
        [
            new CardBox("arrived_in_country_on", "Arrived in the country", "date"),
            new CardBox("port_of_arrival", "Port of arrival"),
        ],
    ];

    /// <summary>The documents and the signature, below the block.</summary>
    /// <remarks>
    /// <b>Both are <c>held</c>, and the screen says so rather than drawing a box
    /// that swallows typing.</b> A scan needs the platform's media service and a
    /// signature needs a pad; neither is here, so neither is offered. A text box
    /// accepting a media reference typed by hand would be a worse lie than the
    /// dead <c>Save</c> this card is replacing.
    /// </remarks>
    public static readonly IReadOnlyList<IReadOnlyList<CardBox>> Closing =
    [
        [new CardBox("documents", "Documents", "held", Placeholder: "none scanned")],
        [
            new CardBox(
                "signature",
                "Signature",
                "held",
                Placeholder: "captured on the pad, or printed and scanned"),
        ],
    ];

    /// <summary>Every box on the card, whichever block it sits in.</summary>
    /// <remarks>
    /// Derived rather than written out, so a box added to one of the three
    /// lists is covered by whatever walks this without anybody remembering to
    /// add it twice.
    /// </remarks>
    public static IEnumerable<CardBox> All
        => Main.Concat(Foreign).Concat(Closing).SelectMany(line => line);
}
