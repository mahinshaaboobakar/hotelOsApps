using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Registrations;

/// <summary>
/// The card the guest signs, and the obligation capturing it creates.
/// </summary>
/// <remarks>
/// <para>
/// <b>One card per stay, written repeatedly.</b> A card is filled in over the
/// length of a check-in — a name, then a document, then a signature — so this
/// is an upsert rather than a create followed by updates. Modelling it as a
/// create would make the desk's second keystroke an error.
/// </para>
/// <para>
/// <b>Capture never blocks.</b> Missing required fields are returned, not
/// thrown: a guest at the desk at midnight is served and the card is completed
/// after. That is S19b's rule about the filing obligation applied to the record
/// that creates it — and the reason <see cref="RegistrationRule.Missing"/>
/// returns a list rather than throwing.
/// </para>
/// <para>
/// <b>No event is published.</b> Section 6's list carries no registration
/// subject: a card is this application's own record and no other application
/// acts on it. Announcing one would invite a consumer to depend on guest
/// identity documents, which is the opposite of what the masking rule
/// (GUEST-Q7) is for.
/// </para>
/// </remarks>
public sealed class RegistrationService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    SettingsService settings,
    TimeProvider clock)
{
    /// <summary>Write the card, minting its number the first time.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="stayId">The stay this card belongs to.</param>
    /// <param name="edit">The values captured so far.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The stored card and what it is still missing.</returns>
    public async Task<CapturedCard> CaptureAsync(
        RequestScope scope,
        Guid stayId,
        RegistrationEdit edit,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RegistrationCapture, ResourceTypes.Stay, stayId, cancellationToken);

        var stay = await db.Stays
            .FirstOrDefaultAsync(
                s => s.Id == stayId && s.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        var configuration = await settings.LoadAsync(scope.PropertyId, cancellationToken);

        var card = await db.Registrations
            .FirstOrDefaultAsync(r => r.StayId == stayId, cancellationToken);

        if (card is null)
        {
            card = new Registration
            {
                StayId = stayId,

                // Minted here and committed with the card, so the series never
                // gains a gap from a card that failed to save.
                CardNumber = SettingsService.MintCardNumber(configuration),
            };

            db.Registrations.Add(card);
        }

        Apply(card, edit);
        card.CapturedBy = scope.UserId;

        if (edit.Signed && card.SignedAt is null)
        {
            card.SignedAt = clock.GetUtcNow();
        }

        await SyncReportingAsync(configuration, stay, card, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new CapturedCard(card, RegistrationRule.Missing(configuration, card));
    }

    /// <summary>The card for a stay, and what it still lacks.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="stayId">The stay.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The card and its missing fields.</returns>
    public async Task<CapturedCard> GetAsync(
        RequestScope scope, Guid stayId, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ReservationRead, ResourceTypes.Stay, stayId, cancellationToken);

        var card = await db.Registrations
            .FirstOrDefaultAsync(r => r.StayId == stayId, cancellationToken)
            ?? throw new NotFoundException("registration", stayId);

        var configuration = await settings.LoadAsync(scope.PropertyId, cancellationToken);
        return new CapturedCard(card, RegistrationRule.Missing(configuration, card));
    }

    /// <summary>Keep the filing obligation in step with the nationality captured.</summary>
    /// <remarks>
    /// <para>
    /// <b>Recomputed on every capture, because nationality arrives late.</b> A
    /// card often starts with a name and gains its nationality minutes later,
    /// and an obligation decided once at check-in would be decided before the
    /// fact that determines it was known.
    /// </para>
    /// <para>
    /// <b>A filing already made is never revised.</b> Once a person has filed
    /// and recorded a receipt, the row is evidence — changing its state because
    /// a nationality was corrected afterwards would rewrite the property's
    /// record of what it asserted, which is the one thing this row exists to
    /// preserve.
    /// </para>
    /// </remarks>
    private async Task SyncReportingAsync(
        GuestOpsSettings configuration,
        RoomStay stay,
        Registration card,
        CancellationToken cancellationToken)
    {
        var reporting = await db.Reporting
            .FirstOrDefaultAsync(r => r.StayId == stay.Id, cancellationToken);

        if (reporting is null)
        {
            reporting = new StayReporting { StayId = stay.Id };
            db.Reporting.Add(reporting);
        }
        else if (reporting.State == ReportingState.Filed)
        {
            return;
        }

        reporting.State = ReportingRule.StateFor(configuration, card.Nationality);
        reporting.Authority = configuration.ReportingAuthority;
        reporting.RequiredBy = reporting.State == ReportingState.Needed
            ? ReportingRule.DueBy(stay.ArrivalAt, configuration.ReportingDueHours)
            : null;
    }

    /// <summary>Copy the captured values onto the card.</summary>
    /// <remarks>
    /// Every field is written, including to null: the desk clearing a mistyped
    /// passport number must be able to clear it. A merge that only wrote
    /// non-null values would make a correction to blank impossible, which is the
    /// commonest correction there is.
    /// </remarks>
    private static void Apply(Registration card, RegistrationEdit edit)
    {
        card.NameAsOnId = edit.NameAsOnId;
        card.DateOfBirth = edit.DateOfBirth;
        card.Nationality = edit.Nationality?.ToUpperInvariant();
        card.AddressLine = edit.AddressLine;
        card.City = edit.City;
        card.State = edit.State;
        card.Country = edit.Country?.ToUpperInvariant();
        card.PostalCode = edit.PostalCode;
        card.IdType = edit.IdType;
        card.IdNumber = edit.IdNumber;
        card.IdIssuer = edit.IdIssuer;
        card.IdExpiry = edit.IdExpiry;
        card.ArrivingFrom = edit.ArrivingFrom;
        card.ProceedingTo = edit.ProceedingTo;
        card.PurposeOfVisit = edit.PurposeOfVisit;
        card.VehicleNumber = edit.VehicleNumber;
        card.PassportNumber = edit.PassportNumber;
        card.PassportIssue = edit.PassportIssue;
        card.PassportExpiry = edit.PassportExpiry;
        card.PassportPlace = edit.PassportPlace;
        card.VisaType = edit.VisaType;
        card.VisaNumber = edit.VisaNumber;
        card.VisaIssue = edit.VisaIssue;
        card.VisaExpiry = edit.VisaExpiry;
        card.ArrivedInCountryOn = edit.ArrivedInCountryOn;
        card.PortOfArrival = edit.PortOfArrival;
        card.DocumentRefs = edit.DocumentRefs;
        card.SignatureRef = edit.SignatureRef;
    }
}

/// <summary>A card, and the required fields it still lacks.</summary>
/// <param name="Card">The stored registration.</param>
/// <param name="Missing">
/// Required field names carrying no value. Reported so a screen can prompt;
/// never a refusal, because an incomplete card must not turn a guest away.
/// </param>
public sealed record CapturedCard(Registration Card, IReadOnlyList<string> Missing);
