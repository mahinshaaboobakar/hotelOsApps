using System.Text.Json;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Creating a stay and checking it in, in one action — gold frame 10.
/// </summary>
/// <remarks>
/// <para>
/// <b>One action, because booking and arrival are one moment</b> (S13). A
/// two-step <i>create, then check in</i> would leave a stay in <c>Booked</c>
/// that nobody ever comes back to, and the walk-in ratio — a number every hotel
/// reports on — cannot be recovered later if the flag is not set when the stay
/// is created.
/// </para>
/// <para>
/// <b>Three services, one order, and no transaction spanning them.</b> Create,
/// assign, check in. Each is its own service's operation with its own event,
/// and the reason they are not wrapped together here is that they are already
/// three business facts: a booking was taken, a room was given, a guest
/// arrived. A failure part-way leaves the earlier facts standing, which is
/// what actually happened at the desk.
/// </para>
/// <para>
/// <b>Check-in requires a room, and this refuses without one</b> (S8) — the one
/// hard gate the assignment ruling creates. It refuses <i>before</i> creating
/// anything, so a rejected walk-in leaves no half-made booking behind.
/// </para>
/// </remarks>
public sealed class WalkInCommand(
    BookingService bookings,
    StayAssignmentService assignments,
    StayLifecycleService lifecycle)
{
    /// <summary>Take the walk-in.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="body">The sheet's own fields.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The stay that was created, and the room it is in.</returns>
    public async Task<object?> RunAsync(
        RequestScope scope, JsonElement? body, CancellationToken cancellationToken)
    {
        var draft = Draft.From(body);

        var booking = await bookings.CreateAsync(
            scope,
            new NewBooking(
                Stays:
                [
                    new NewStay(
                        RoomTypeId: draft.RoomTypeId,
                        ArrivalDate: draft.Arrives,
                        DepartureDate: draft.Departs,
                        Adults: draft.Adults,
                        Children: 0,
                        Guests:
                        [
                            new NewGuest(
                                NameAsGiven: draft.Guest,
                                NameGiven: null,
                                NameFamily: null,
                                Phone: draft.Phone,
                                Email: draft.Email,
                                IsPrimary: true),
                        ],

                        // The flag, set at creation because it is unrecoverable
                        // afterwards. This is the whole reason the walk-in is
                        // its own command rather than a create followed by a
                        // check-in the desk happens to do next.
                        WalkIn: true,
                        Terms: null),
                ],
                Channel: "walk-in",
                TravelAgent: null,
                MarketCode: null,
                MealPlan: null,

                // One stay, and we are the source — so the expected count is
                // known exactly rather than being a claim somebody else made.
                ExpectedStayCount: 1),
            cancellationToken);

        var stay = booking.Stays.Single();

        // `Initial` because this is the first room the stay has ever had, and
        // `acceptConflict: false` because a walk-in is not the case for
        // overriding a double booking — the desk is standing at the counter and
        // can pick another room. The override path exists on the assignment
        // screen, where a person has the other stay in front of them.
        await assignments.AssignAsync(
            scope,
            stay.Id,
            draft.RoomId,
            Domain.AssignmentReason.Initial,
            acceptConflict: false,
            stay.Version,
            cancellationToken);

        var arrived = await lifecycle.CheckInAsync(
            scope, stay.Id, stay.Version + 1, cancellationToken);

        return new
        {
            bookingId = booking.Id.ToString(),
            stayId = arrived.Id.ToString(),
            status = "In house",
        };
    }

    /// <summary>
    /// What the sheet sent, validated before anything is written.
    /// </summary>
    /// <remarks>
    /// A record rather than six locals, so the refusals are all in one place
    /// and the command below reads as the three operations it performs. Every
    /// field is required: this is a compose surface, and a walk-in missing its
    /// room or its dates is a sheet the desk has not finished rather than a
    /// stay to create with the gaps left open.
    /// </remarks>
    private sealed record Draft(
        string Guest,
        string? Phone,
        string? Email,
        Guid RoomTypeId,
        Guid RoomId,
        DateOnly Arrives,
        DateOnly Departs,
        int Adults)
    {
        /// <summary>Read the sheet, refusing what cannot be a walk-in.</summary>
        public static Draft From(JsonElement? body)
        {
            if (body is not { ValueKind: JsonValueKind.Object } sheet)
            {
                throw new InvalidRequestException("a walk-in needs the sheet's fields");
            }

            var guest = Text(sheet, "guest")
                ?? throw new InvalidRequestException("a walk-in needs a name");

            var roomTypeId = Id(sheet, "roomTypeId")
                ?? throw new InvalidRequestException("a walk-in needs a room type");

            // The one hard gate. Refused here rather than at check-in, so a
            // walk-in with no room leaves nothing behind to clean up.
            var roomId = Id(sheet, "roomId")
                ?? throw new InvalidRequestException(
                    "check-in needs a room; assign one before creating the stay");

            var arrives = Date(sheet, "arrives")
                ?? throw new InvalidRequestException("a walk-in needs an arrival date");

            var departs = Date(sheet, "departs")
                ?? throw new InvalidRequestException("a walk-in needs a departure date");

            if (departs < arrives)
            {
                throw new InvalidRequestException("the departure is before the arrival");
            }

            return new Draft(
                guest,
                Text(sheet, "phone"),
                Text(sheet, "email"),
                roomTypeId,
                roomId,
                arrives,
                departs,

                // One adult unless the desk said otherwise. A party size is a
                // count the sheet may legitimately leave alone, unlike a room
                // or a date, and zero adults is not a stay anybody takes.
                Math.Max(1, Number(sheet, "adults")));
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
    }
}
