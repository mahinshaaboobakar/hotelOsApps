using Grpc.Core;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Grpc;

/// <summary>Taking a booking — one collaborator, <see cref="BookingService"/>.</summary>
public partial class GuestOpsGrpcService
{
    public override async Task<Contracts.V1.Booking> CreateBooking(
        CreateBookingRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var booking = await bookings.CreateAsync(
            scope,
            new NewBooking(
                [.. request.Stays.Select(ToCommand)],
                Empty(request.Channel),
                Empty(request.TravelAgent),
                Empty(request.MarketCode),
                Empty(request.MealPlan),
                request.ExpectedStayCount),
            context.CancellationToken);

        return new Contracts.V1.Booking
        {
            Id = booking.Id.ToString(),
            PropertyId = booking.PropertyId.ToString(),
            ExpectedStayCount = booking.ExpectedStayCount ?? 0,
        };
    }

    private static Application.Bookings.NewStay ToCommand(Contracts.V1.NewStay request)
        => new(
            ParseRequired(request.RoomTypeId, "room_type_id"),
            ParseDate(request.ArrivalDate, "arrival_date"),
            ParseDate(request.DepartureDate, "departure_date"),
            request.Adults,
            request.Children,
            [.. request.Guests.Select(ToCommand)],
            request.WalkIn,
            ToCommand(request.Terms));

    private static Application.Bookings.NewGuest ToCommand(Contracts.V1.NewGuest request)
        => new(
            request.NameAsGiven,
            Empty(request.NameGiven),
            Empty(request.NameFamily),
            Empty(request.Phone),
            Empty(request.Email),

            // Proto3's `optional` is what carries the difference between *"the
            // source said no"* and *"the source said nothing"*, and R11 needs
            // both: a reservation where nobody is marked primary is a state.
            request.HasIsPrimary ? request.IsPrimary : null);

    /// <summary>The terms, where the desk stated any.</summary>
    /// <remarks>
    /// Absent rather than empty: a stay with no stated terms is a stay whose
    /// source did not say, and an empty row would claim a zero rate and a
    /// missing guarantee as facts.
    /// </remarks>
    private static Domain.CommercialTerms? ToCommand(Contracts.V1.CommercialTerms? terms)
        => terms is null
            ? null
            : new Domain.CommercialTerms
            {
                RateCode = Empty(terms.RateCode),
                RateName = Empty(terms.RateName),
                Amount = ToMoney(terms.Amount),
                GuaranteeCode = Empty(terms.GuaranteeCode),
                GuaranteeDescription = Empty(terms.GuaranteeDescription),
                OnHold = terms.OnHold,
                ReservesInventory = terms.ReservesInventory,
                DepositOffsetDaysFromBooking = Zero(terms.DepositOffsetDaysFromBooking),
                CancelOffsetDaysFromArrival = Zero(terms.CancelOffsetDaysFromArrival),
                CancelDropTime = TimeOnly.TryParse(terms.CancelDropTime, out var drop) ? drop : null,
                PenaltyAmount = ToMoney(terms.PenaltyAmount),
                PenaltyNights = Zero(terms.PenaltyNights),
            };

    /// <summary>An amount, or nothing — never a zero standing in for silence.</summary>
    /// <remarks>
    /// R19: three things or it is not an amount. A message with no currency is
    /// a message that stated no amount, and storing zero would make a free
    /// stay and an unstated rate the same row.
    /// </remarks>
    private static Domain.Money? ToMoney(Contracts.V1.Money? money)
        => money is null || string.IsNullOrWhiteSpace(money.Currency)
            ? null
            : new Domain.Money(
                money.MinorUnits, money.Currency, (Domain.TaxBasis)(int)money.TaxBasis);

    /// <summary>Proto3 has no null: the empty string is absent.</summary>
    private static string? Empty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>And zero is absent, for an offset nobody stated.</summary>
    private static int? Zero(int value) => value == 0 ? null : value;
}
