using HotelOS.GuestOps.Application.Registrations;
using HotelOS.Platform;
using System.Text.Json;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Saving the registration card — gold frame 15, under <c>registration.capture</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The card's <c>Save</c> has existed since the screen was drawn and wrote
/// nothing.</b> It was dead in the frames on purpose — the boxes rendered
/// values and captured none — and the ledger carried the lie. This is the door
/// it was always supposed to reach.
/// </para>
/// <para>
/// <b>The whole card is written, every time, including to blank.</b>
/// <c>RegistrationService.Apply</c> writes every field so that clearing a
/// mistyped passport number is possible, which is the commonest correction
/// there is. The consequence is that the screen must send back everything it
/// was given — a save carrying only the boxes somebody touched would blank the
/// rest — and that is why the two rows this screen cannot capture, the scans
/// and the signature, still travel on it.
/// </para>
/// <para>
/// <b>There is nowhere to put a signing time, and that is the point.</b>
/// <c>RegistrationEdit.Signed</c> exists and this command does not read it: no
/// surface here captures a signature, so a body claiming one would record a
/// guest's assent that nobody obtained. It lands with the pad that takes it.
/// </para>
/// <para>
/// <b>Nothing is validated for presence.</b> What a card must carry is the
/// property's configuration; a save with fields still missing succeeds and says
/// what is missing, because a guest at the desk at midnight is served and the
/// card is completed after (S19b).
/// </para>
/// </remarks>
/// <param name="registrations">Writes the card and keeps the filing in step.</param>
public sealed class RegistrationCommand(RegistrationService registrations)
{
    /// <summary>Write the card as the desk has it.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="body">The stay, and every box's value.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>The number now on the card, and what it still lacks.</returns>
    /// <exception cref="InvalidRequestException">No stay, or no values.</exception>
    public async Task<object?> RunAsync(
        RequestScope scope,
        JsonElement? body,
        CancellationToken cancellationToken)
    {
        if (body is not { ValueKind: JsonValueKind.Object } sheet)
        {
            throw new InvalidRequestException("a card needs a stay and its values");
        }

        var stayId = Id(sheet, "stayId")
            ?? throw new InvalidRequestException("a card needs the stay it belongs to");

        if (!sheet.TryGetProperty("values", out var values)
            || values.ValueKind != JsonValueKind.Object)
        {
            // **Refused rather than read as an empty card.** An absent `values`
            // and a card whose every box is blank are different things, and
            // treating the first as the second would let a malformed request
            // erase a completed registration.
            throw new InvalidRequestException("a card needs the values its boxes hold");
        }

        var captured = await registrations.CaptureAsync(
            scope, stayId, Edit(values), cancellationToken);

        return new
        {
            series = captured.Card.CardNumber,
            missing = captured.Missing,
            signedAt = captured.Card.SignedAt?.ToString("O"),
        };
    }

    /// <summary>The body's boxes, as the service's edit.</summary>
    /// <remarks>
    /// <b>Named one by one, and the names are checked against the card's own
    /// list.</b> This is the only place the wire's <c>snake_case</c> meets the
    /// record's properties, and a box added to <c>RegistrationFields</c> and
    /// forgotten here would silently stop saving — which is why the test walks
    /// <c>RegistrationFields.All</c> and asserts every name survives a round
    /// trip, rather than trusting this list to stay complete.
    /// </remarks>
    private static RegistrationEdit Edit(JsonElement it)
        => new(
            NameAsOnId: Text(it, "name_as_on_id"),
            DateOfBirth: Date(it, "date_of_birth"),
            Nationality: Text(it, "nationality"),
            AddressLine: Text(it, "address_line"),
            City: Text(it, "city"),
            State: Text(it, "state"),
            Country: Text(it, "country"),
            PostalCode: Text(it, "postal_code"),
            IdType: Text(it, "id_type"),
            IdNumber: Text(it, "id_number"),
            IdIssuer: Text(it, "id_issuer"),
            IdExpiry: Date(it, "id_expiry"),
            ArrivingFrom: Text(it, "arriving_from"),
            ProceedingTo: Text(it, "proceeding_to"),
            PurposeOfVisit: Text(it, "purpose_of_visit"),
            VehicleNumber: Text(it, "vehicle_number"),
            PassportNumber: Text(it, "passport_number"),
            PassportIssue: Date(it, "passport_issue"),
            PassportExpiry: Date(it, "passport_expiry"),
            PassportPlace: Text(it, "passport_place"),
            VisaType: Text(it, "visa_type"),
            VisaNumber: Text(it, "visa_number"),
            VisaIssue: Date(it, "visa_issue"),
            VisaExpiry: Date(it, "visa_expiry"),
            ArrivedInCountryOn: Date(it, "arrived_in_country_on"),
            PortOfArrival: Text(it, "port_of_arrival"),

            // Carried through rather than captured. The screen sends back what
            // it was given, because the write is whole-card and these two rows
            // would otherwise be cleared by every save.
            DocumentRefs: Text(it, "documents"),
            SignatureRef: Text(it, "signature"));

    /// <summary>One box's text, or null where it holds nothing.</summary>
    /// <remarks>
    /// <b>Blank is null.</b> A desk clearing a box sends an empty string, and
    /// storing that would make the card hold a value that is not a value —
    /// which <c>RegistrationRule</c> would then have to treat as missing
    /// anyway, in a second place.
    /// </remarks>
    private static string? Text(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()!.Trim()
                : null;

    /// <summary>One box's date, as <c>yyyy-MM-dd</c>.</summary>
    /// <remarks>
    /// <b>Unparseable is null rather than an error, and that is deliberate:</b>
    /// a date this cannot read is a box the property has not filled in, and
    /// refusing the whole card for it would mean a half-typed birthday loses
    /// the address somebody just entered. The property's required set is what
    /// says the field is wanted, and the answer reports it missing.
    /// </remarks>
    private static DateOnly? Date(JsonElement body, string name)
        => Text(body, name) is { } text ? Iso8601.Day(text) : null;

    /// <summary>The stay this card belongs to.</summary>
    private static Guid? Id(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && Guid.TryParse(value.GetString(), out var id)
                ? id
                : null;
}
