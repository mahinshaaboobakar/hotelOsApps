using Grpc.Core;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Grpc;

/// <summary>The registration card — capture and read.</summary>
/// <remarks>
/// <b>An upsert, not a create.</b> A card is filled in over the length of a
/// check-in, so one RPC writes it however many times the desk touches it.
/// </remarks>
public partial class GuestOpsGrpcService
{
    /// <summary>Write the card, minting its number the first time.</summary>
    /// <param name="request">The stay and the values captured so far.</param>
    /// <param name="context">The call.</param>
    /// <returns>The stored card and what it still lacks.</returns>
    public override async Task<Contracts.V1.CapturedCard> CaptureRegistration(
        CaptureRegistrationRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var captured = await registrations.CaptureAsync(
            scope,
            ParseRequired(request.StayId, "stay_id"),
            FromProto(request.Card),
            context.CancellationToken);

        return ToProto(captured);
    }

    /// <summary>The card for a stay, and its missing fields.</summary>
    /// <param name="request">The stay.</param>
    /// <param name="context">The call.</param>
    /// <returns>The stored card.</returns>
    public override async Task<Contracts.V1.CapturedCard> GetRegistration(
        GetRegistrationRequest request, ServerCallContext context)
    {
        var captured = await registrations.GetAsync(
            request.Context.ToScope(CallerContext.Get(context)),
            ParseRequired(request.StayId, "stay_id"),
            context.CancellationToken);

        return ToProto(captured);
    }

    /// <summary>The wire's card, as the application's edit record.</summary>
    /// <remarks>
    /// Empty strings become null rather than empty values: proto3 cannot
    /// distinguish them, and a card storing <c>""</c> for an uncaptured
    /// passport would satisfy a required-field check that should have failed.
    /// </remarks>
    private static Application.Registrations.RegistrationEdit FromProto(Contracts.V1.RegistrationEdit? card)
    {
        card ??= new Contracts.V1.RegistrationEdit();

        return new Application.Registrations.RegistrationEdit(
            OrNull(card.NameAsOnId),
            OptionalDate(card.DateOfBirth, "date_of_birth"),
            OrNull(card.Nationality),
            OrNull(card.AddressLine),
            OrNull(card.City),
            OrNull(card.State),
            OrNull(card.Country),
            OrNull(card.PostalCode),
            OrNull(card.IdType),
            OrNull(card.IdNumber),
            OrNull(card.IdIssuer),
            OptionalDate(card.IdExpiry, "id_expiry"),
            OrNull(card.ArrivingFrom),
            OrNull(card.ProceedingTo),
            OrNull(card.PurposeOfVisit),
            OrNull(card.VehicleNumber),
            OrNull(card.PassportNumber),
            OptionalDate(card.PassportIssue, "passport_issue"),
            OptionalDate(card.PassportExpiry, "passport_expiry"),
            OrNull(card.PassportPlace),
            OrNull(card.VisaType),
            OrNull(card.VisaNumber),
            OptionalDate(card.VisaIssue, "visa_issue"),
            OptionalDate(card.VisaExpiry, "visa_expiry"),
            OptionalDate(card.ArrivedInCountryOn, "arrived_in_country_on"),
            OrNull(card.PortOfArrival),
            OrNull(card.DocumentRefs),
            OrNull(card.SignatureRef),
            card.Signed);
    }

    private static Contracts.V1.CapturedCard ToProto(Application.Registrations.CapturedCard captured)
    {
        var card = captured.Card;

        var message = new Contracts.V1.CapturedCard
        {
            StayId = card.StayId.ToString(),
            CardNumber = card.CardNumber ?? string.Empty,
            SignedAt = ToIso(card.SignedAt),
            CapturedBy = Or(card.CapturedBy),
            Card = new Contracts.V1.RegistrationEdit
            {
                NameAsOnId = card.NameAsOnId ?? string.Empty,
                DateOfBirth = ToIso(card.DateOfBirth),
                Nationality = card.Nationality ?? string.Empty,
                AddressLine = card.AddressLine ?? string.Empty,
                City = card.City ?? string.Empty,
                State = card.State ?? string.Empty,
                Country = card.Country ?? string.Empty,
                PostalCode = card.PostalCode ?? string.Empty,
                IdType = card.IdType ?? string.Empty,
                IdNumber = card.IdNumber ?? string.Empty,
                IdIssuer = card.IdIssuer ?? string.Empty,
                IdExpiry = ToIso(card.IdExpiry),
                ArrivingFrom = card.ArrivingFrom ?? string.Empty,
                ProceedingTo = card.ProceedingTo ?? string.Empty,
                PurposeOfVisit = card.PurposeOfVisit ?? string.Empty,
                VehicleNumber = card.VehicleNumber ?? string.Empty,
                PassportNumber = card.PassportNumber ?? string.Empty,
                PassportIssue = ToIso(card.PassportIssue),
                PassportExpiry = ToIso(card.PassportExpiry),
                PassportPlace = card.PassportPlace ?? string.Empty,
                VisaType = card.VisaType ?? string.Empty,
                VisaNumber = card.VisaNumber ?? string.Empty,
                VisaIssue = ToIso(card.VisaIssue),
                VisaExpiry = ToIso(card.VisaExpiry),
                ArrivedInCountryOn = ToIso(card.ArrivedInCountryOn),
                PortOfArrival = card.PortOfArrival ?? string.Empty,
                DocumentRefs = card.DocumentRefs ?? string.Empty,
                SignatureRef = card.SignatureRef ?? string.Empty,
                Signed = card.SignedAt is not null,
            },
        };

        message.Missing.AddRange(captured.Missing);
        return message;
    }
}
