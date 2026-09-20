using System.Text.Json;
using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Taking a booking at the desk — the 05 flow's step 4, under
/// <c>stay.create</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The screen could not create a booking until this existed.</b>
/// <c>BookingService.CreateAsync</c> has always been there and is reachable
/// over gRPC and from <c>WalkInCommand</c>, and the module surface served no
/// method that reached it — so New booking could show what was free and then
/// do nothing with it. The capability ledger carried that as a drawn screen
/// with no door.
/// </para>
/// <para>
/// <b>Overbooking is warned about, not refused</b> — the owner's option (b),
/// 2026-09-19, over a flow where a type can sell out between the availability
/// read and the confirm. A desk that knows the hotel is full still takes the
/// booking; what it may not do is take it without being told. So the first
/// call with no room free returns the warning and writes NOTHING, and a second
/// call carrying <c>overbookKnowingly</c> creates it and records that the
/// choice was made.
/// </para>
/// <para>
/// <b>The record is the event, and that is an implementation choice rather
/// than a ruling.</b> The owner ruled that there is a record; the shape was not
/// named. <c>stay.created</c> already carries the facts of a creation and is
/// what the Activity tab reads, so the flag travels there with the person and
/// the time the event already has. A column on the stay is the alternative, and
/// it is the right one the day somebody needs to <i>query</i> for knowingly
/// overbooked stays — which nothing does today.
/// </para>
/// <para>
/// <b>The guest count is carried and filters nothing — because THIS
/// application does not read occupancy, not because the platform lacks it.</b>
/// Master Data's <c>RoomType</c> has held six occupancy fields all along —
/// <c>BaseOccupancy</c>, <c>MaxOccupancy</c>, <c>MaxAdults</c>,
/// <c>MaxChildren</c>, <c>ExtraBedAllowed</c>, <c>MaxExtraBeds</c>
/// (<c>Catalogue.cs:93-99</c>) — and all six are on the wire
/// (<c>masterdata/v1/dto.proto:393-399</c>), with a room-level override at
/// <c>:334</c> and Context composing <c>max_occupancy</c> at
/// <c>context/v1/dto.proto:111</c>. What is missing is here:
/// <c>MasterDataRoomTypeName</c> projects <c>Id</c> and <c>Name</c> and nothing
/// else.
/// </para>
/// <para>
/// So filtering by capacity may be a GuestOps-only change — widening that read
/// model — rather than anything Master Data has to grow, and the question is
/// with the planner. <b>This paragraph said the platform had no such attribute
/// until 2026-09-20</b>, which was my measurement of this application read as a
/// measurement of the estate: I established that GuestOps cannot filter and
/// wrote down that nothing anywhere could. Until the planner answers, the count
/// is carried onto the booking and nothing filters.
/// </para>
/// <para>
/// <b>The check reads availability, so a caller who may create and may not
/// read is refused</b> — by that read's own authorization, in its own words.
/// That is deliberate: a creation that could not check would be an unchecked
/// creation, and taking it anyway would have the platform claim it verified
/// something it never looked at.
/// </para>
/// </remarks>
/// <param name="bookings">Creates the booking and its stays.</param>
/// <param name="availability">What is free, for the check at creation.</param>
public sealed class BookCommand(BookingService bookings, AvailabilityService availability)
{
    /// <summary>Take the booking, or say why it would overbook.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="body">The confirm screen's fields.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>The booking that was created, or the warning that stopped it.</returns>
    public async Task<object?> RunAsync(
        RequestScope scope, JsonElement? body, CancellationToken cancellationToken)
    {
        var draft = Draft.From(body);

        // Read again here rather than trusting what step 2 showed: the whole
        // reason this check exists is that a type can sell out while somebody
        // types a guest's name.
        var free = await FreeAsync(scope, draft, cancellationToken);

        if (free <= 0 && !draft.OverbookKnowingly)
        {
            return new
            {
                created = false,
                overbooks = true,
                free,
                roomTypeId = draft.RoomTypeId.ToString(),
            };
        }

        var booking = await bookings.CreateAsync(
            scope,
            new NewBooking(
                [new NewStay(
                    draft.RoomTypeId,
                    draft.Arrives,
                    draft.Departs,
                    draft.Adults,
                    draft.Children,
                    [new NewGuest(draft.Guest, null, null, draft.Phone, draft.Email, true)],
                    WalkIn: false,
                    Terms: null,
                    KnowinglyOverbooked: free <= 0)],
                Channel: null,
                TravelAgent: null,
                MarketCode: null,
                MealPlan: null,
                ExpectedStayCount: 0),
            cancellationToken);

        return new
        {
            created = true,
            bookingId = booking.Id.ToString(),

            // Stated on the answer as well as recorded, so the screen can say
            // what was done rather than infer it from what it sent.
            overbooked = free <= 0,
        };
    }

    /// <summary>How many rooms of this type are free across the stay's nights.</summary>
    /// <remarks>
    /// The worst night, which is what the availability read already computes per
    /// type: a type with one room free on the middle night cannot take a
    /// four-night stay, and the smallest number across the span is the one that
    /// decides.
    /// </remarks>
    private async Task<int> FreeAsync(
        RequestScope scope, Draft draft, CancellationToken cancellationToken)
    {
        // The nights, not the days: a stay arriving on the 3rd and leaving on
        // the 7th occupies four nights and not the 7th, and asking about the
        // departure day would refuse a booking over a type that is full that
        // night for somebody else.
        var rows = await availability.GetAsync(
            scope, draft.Arrives, draft.Departs.AddDays(-1), [draft.RoomTypeId], cancellationToken);

        // The worst night decides. `AvailabilityView` collapses the same rows
        // for display and keeps every count; this needs one number, so it takes
        // the minimum rather than reusing a projection built for a table.
        return rows.Count == 0 ? 0 : rows.Min(row => row.Free);
    }

    /// <summary>The confirm screen's fields, refusing what cannot be a booking.</summary>
    private sealed record Draft(
        string Guest,
        string? Phone,
        string? Email,
        Guid RoomTypeId,
        DateOnly Arrives,
        DateOnly Departs,
        int Adults,
        int Children,
        bool OverbookKnowingly)
    {
        /// <summary>Read the screen's fields, refusing what cannot be a booking.</summary>
        /// <param name="body">What the confirm step sent.</param>
        /// <returns>The draft.</returns>
        /// <exception cref="InvalidRequestException">A field a booking cannot do without.</exception>
        public static Draft From(JsonElement? body)
        {
            if (body is not { ValueKind: JsonValueKind.Object } sheet)
            {
                throw new InvalidRequestException("a booking needs the confirm screen's fields");
            }

            var guest = Text(sheet, "guest")
                ?? throw new InvalidRequestException("a booking needs a name");

            var roomTypeId = Id(sheet, "roomTypeId")
                ?? throw new InvalidRequestException("a booking needs a room type");

            var arrives = Date(sheet, "arrives")
                ?? throw new InvalidRequestException("a booking needs an arrival date");

            var departs = Date(sheet, "departs")
                ?? throw new InvalidRequestException("a booking needs a departure date");

            if (departs < arrives)
            {
                throw new InvalidRequestException("the departure is before the arrival");
            }

            return new Draft(
                guest,
                Text(sheet, "phone"),
                Text(sheet, "email"),
                roomTypeId,
                arrives,
                departs,

                // One adult unless the desk said otherwise — the same reading
                // the walk-in takes, and for the same reason: a party size is a
                // count the screen may leave alone, and zero adults is not a
                // stay anybody takes.
                Math.Max(1, Number(sheet, "adults")),
                Number(sheet, "children"),
                Flag(sheet, "overbookKnowingly"));
        }

        private static string? Text(JsonElement body, string name)
            => body.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString())
                    ? value.GetString()
                    : null;

        private static Guid? Id(JsonElement body, string name)
            => Text(body, name) is { } text && Guid.TryParse(text, out var id) ? id : null;

        private static DateOnly? Date(JsonElement body, string name)
            => Text(body, name) is { } text && DateOnly.TryParse(text, out var date)
                ? date
                : null;

        private static int Number(JsonElement body, string name)
            => body.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.Number
                && value.TryGetInt32(out var parsed)
                    ? parsed
                    : 0;

        /// <summary>True only where the screen said so — absent is not consent.</summary>
        private static bool Flag(JsonElement body, string name)
            => body.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.True;
    }
}
