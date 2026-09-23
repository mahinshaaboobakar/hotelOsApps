using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using System.Text.Json;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Taking a walk-in: the stay created, then the room given and the guest checked
/// in — one action on the screen, two authorized operations underneath.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two phases — ADR 0193, RC-Q8, RC-Q8a and RC-Q8b-2 (ruled 2026-09-19).</b>
/// Phase 1 is authorized by <c>stay.create</c> at the property, creates the
/// booking and its stay, and commits them with their events. Phase 2 is a
/// separate, later action: <c>stay.assign</c> and the check-in are asked against
/// the stay when phase 2 starts, and the room and the arrival commit together or
/// not at all. The authorization boundary is two-phase even though the desk
/// presses one button.
/// </para>
/// <para>
/// <b>A refused phase 2 leaves the booked, room-less stay, and says so</b>
/// (RC-Q8a). No compensating cancel is invented. So a phase-2 failure is not an
/// error from this command — the stay exists, and an error would reach the screen
/// as <i>nothing was changed</i> — it is part of the answer: the stay that was
/// created, and what became of the second step.
/// </para>
/// <para>
/// <b>What this said until 2026-09-19, and why it went.</b> The three operations
/// ran in ONE transaction, and the class guaranteed that <i>"a refused walk-in
/// leaves nothing."</i> RC-Q8a: that <i>"was never an architectural decision"</i>
/// — and one transaction cannot hold phase 2's authorization, which is asked
/// against an object phase 1 has not committed yet (ADR 0061: its tuples are
/// materialised from <c>stay.created</c> after the event is relayed).
/// </para>
/// <para>
/// <b>Not built, pending a question:</b> RC-Q8b-2 rules that phase 2 answers
/// <i>"not yet available"</i> when the stay's authorization has not been
/// materialised yet, never <i>not permitted</i>. The Kernel's answer has no such
/// outcome — a stay with no tuples is answered as denied — so this command cannot
/// tell the two apart, and reports every refusal as a refusal until the planner
/// names what says <i>not yet</i>.
/// </para>
/// <para>
/// <b>Check-in requires a room, and this refuses without one</b> (S8), in
/// <c>Draft.From</c>, before phase 1 — so a sheet with no room creates nothing.
/// </para>
/// </remarks>
public sealed class WalkInCommand(
    GuestOpsDbContext db,
    BookingService bookings,
    StayAssignmentService assignments,
    StayLifecycleService lifecycle)
{
    /// <summary>Take the walk-in.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="body">The sheet's own fields.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The stay that was created, and whether the guest is in house.</returns>
    public async Task<object?> RunAsync(
        RequestScope scope, JsonElement? body, CancellationToken cancellationToken)
    {
        var draft = Draft.From(body);

        // PHASE 1 — the stay, committed with its events. The service authorizes
        // `stay.create` at the property and commits on its own.
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
                        // afterwards.
                        WalkIn: true,
                        Terms: null),
                ],
                Channel: "walk-in",
                TravelAgent: null,
                MarketCode: null,
                MealPlan: null,
                ExpectedStayCount: 1),
            cancellationToken);

        var stay = booking.Stays.Single();

        // PHASE 2 — a later action, authorized when it starts. The room and the
        // arrival commit together or not at all: a check-in refused after the
        // assign leaves no room assigned.
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // `Initial`: the first room this stay has had. `acceptConflict: false`:
            // the desk is at the counter and can pick another room.
            var assigned = await assignments.AssignAsync(
                scope,
                stay.Id,
                draft.RoomId,
                Domain.AssignmentReason.Initial,
                acceptConflict: false,
                stay.Version,
                cancellationToken);

            // The version the assign produced — `stay` is the tracked instance,
            // so the assign's bump has already moved it.
            var arrived = await lifecycle.CheckInAsync(
                scope, stay.Id, assigned.Version, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return Answer(booking.Id, arrived.Id, checkedIn: true, secondStep: null);
        }
        catch (DomainException refused)
        {
            // Rolled back by disposal. What is committed is phase 1 alone.
            db.ChangeTracker.Clear();
            return Answer(booking.Id, stay.Id, checkedIn: false, secondStep: Kind(refused));
        }
    }

    /// <summary>The answer: what was created, and what became of the second step.</summary>
    private static object Answer(Guid bookingId, Guid stayId, bool checkedIn, string? secondStep)
        => new
        {
            bookingId = bookingId.ToString(),
            stayId = stayId.ToString(),
            checkedIn,

            // Null when the guest is in house. Otherwise why the room and the
            // check-in were not done — a kind the screen words for the desk; the
            // exception's own sentence is for the log, not for the person.
            secondStep,
        };

    /// <summary>Which kind of refusal phase 2 met.</summary>
    private static string Kind(DomainException refused) => refused switch
    {
        PermissionDeniedException => "not-authorized",
        UnavailableException => "unanswered",
        ConcurrencyException => "changed",
        _ => "refused",
    };

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
            => Text(body, name) is { } text && Iso.Day(text) is { } date
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
