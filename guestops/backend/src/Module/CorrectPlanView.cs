using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// What correcting a lifecycle fact would do — the owner's C1 dialog, ruled
/// 2026-09-24.
/// </summary>
/// <remarks>
/// <para>
/// <b>A read, so the desk sees what putting a stay back would mean before
/// being able to do it.</b> Only the button needs <c>stay.override</c>.
/// </para>
/// <para>
/// <b>The room is checked, and the conflicting case is NOT DRAWN.</b> The
/// approved frame's own caption says so: <i>"what happens when 214 has been
/// given to somebody else in those seven minutes is not drawn here, and is the
/// one case this pair leaves open."</i> So this answers whether the room is
/// still free and the screen refuses the correction when it is not — it does
/// not invent the resolution, which is a design question the owner has not
/// been asked.
/// </para>
/// <para>
/// <b>The reason list is empty and that is the same gap cancellation has.</b>
/// <see cref="CancelPlanView"/> records it in full: <c>GuestOpsSettings</c>
/// carries registration, reporting and numbering and no reason vocabulary, and
/// frame 16 — the settings screen — configures none either. A hardcoded list
/// here would put a vocabulary nobody chose into the one field a correction is
/// audited by, and it would be a SECOND such list, free to disagree with
/// cancellation's the day either is filled in.
/// </para>
/// </remarks>
/// <param name="db">This application's own schema.</param>
/// <param name="bookings">
/// The booking read, <b>consumed rather than repeated</b>. The reference and
/// the booked days are resolved there, in the property's own zone, and they are
/// the same facts frame 8's cancellation subject carries — a second resolution
/// here would be free to disagree with it the day either changes.
/// </param>
public sealed class CorrectPlanView(GuestOpsDbContext db, BookingReadService bookings)
{
    /// <summary>What correcting this stay would do.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="stayId">The stay the desk is looking at.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>Where the stay would go, what stands in the way, and why.</returns>
    /// <exception cref="NotFoundException">No such stay at this property.</exception>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Guid stayId, CancellationToken cancellationToken)
    {
        var stay = await db.Stays
            .Where(s => s.PropertyId == scope.PropertyId && s.Id == stayId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        // The primary guest, or the state of not having one. "Not yet named"
        // is a state and not a placeholder: a stay a feed sent before the
        // guest was known has no name, and inventing one would attribute a
        // forfeited night to a person who does not exist.
        var guest = await db.Party
            .Where(p => p.StayId == stay.Id)
            .OrderByDescending(p => p.IsPrimary == true)
            .Select(p => p.Guest!.NameAsGiven)
            .FirstOrDefaultAsync(cancellationToken);

        var to = Target(stay.Lifecycle);

        // The reference and the booked days `fRI1` puts in the dialog's second
        // line, from the one place that resolves them.
        var booking = await bookings.GetAsync(scope, stay.BookingId, cancellationToken);

        // **When the no-show was recorded — from the event, because the stay
        // row does not hold it.** `RecordNoShowAsync` writes no such column,
        // and its override row is skipped entirely for a PMS-unknown stay,
        // which is exactly the stay `fRI1` draws (<i>created here</i>). The
        // Activity tab already reads this event for the same instant, so one
        // source means the dialog and the list behind it cannot disagree.
        var noShowAt = to != StayLifecycle.Booked
            ? null
            : await db.Set<StoredEvent>()
                .Where(stored => stored.PropertyId == scope.PropertyId
                    && stored.AggregateType == "stay"
                    && stored.AggregateId == stay.Id
                    && stored.EventType == "stay.no_show")
                .OrderByDescending(stored => stored.OccurredAt)
                .Select(stored => (DateTimeOffset?)stored.OccurredAt)
                .FirstOrDefaultAsync(cancellationToken);

        // **Only a stay going back into a room needs a room.** Reinstating a
        // no-show returns it to `Booked`, which holds no room, so asking about
        // one would be a question with no bearing on the answer.
        var room = to == StayLifecycle.InHouse ? stay.CurrentRoomId : null;

        // The number the desk knows the room by. Null where the room has no
        // number recorded — the screen then says "the room", which is true,
        // rather than printing an id a person cannot match to a door.
        var roomNumber = room is null
            ? null
            : await db.Set<MasterDataRoom>()
                .Where(r => r.Id == room.Value)
                .Select(r => r.RoomNumber)
                .FirstOrDefaultAsync(cancellationToken);

        return new
        {
            stayId = stay.Id.ToString(),
            version = stay.Version,

            // The state the confirm sends. Named on the wire so the screen does
            // not decide it: a dialog that chose the target itself would be a
            // second derivation of a transition the service already rules on.
            to = to?.ToString(),

            from = stay.Lifecycle.ToString(),

            // **Facts, not sentences.** ADR 0175: the screen composes the
            // title, the button and the prose from these; a service that sent
            // "Put this stay back in house?" would own copy it cannot see
            // rendered, and one locale's grammar with it.
            room = roomNumber,

            // The parts of the head's second line — `Rajesh Pillai · 214 ·
            // checked out 07:02 today`. The screen composes it, in the
            // property's zone and the reader's conventions.
            guest = string.IsNullOrWhiteSpace(guest) ? "Not yet named" : guest,
            departedAt = stay.DepartureAt.At?.ToString("O"),

            // **Facts, never a rendering** (ADR 0175). The screen composes
            // `recorded as a no-show 1 Sep 23:14` and the day range, because
            // the month's position and a range's separator are the reader's
            // locale's grammar rather than this service's.
            noShowAt = noShowAt?.ToString("O"),
            reference = booking.Reference,
            arrive = booking.Arrival?.ToString("yyyy-MM-dd"),
            depart = booking.Departure?.ToString("yyyy-MM-dd"),

            // **Three values, and the third is not a missing boolean.** Null
            // means no room is involved at all — reinstating a no-show returns
            // the stay to `Booked`, which holds none — while true and false are
            // answers about a room. Collapsing null into false would tell the
            // desk a room was taken when there is no room in the question.
            roomStillFree = room is null
                ? (bool?)null
                : await FreeAsync(stay, room.Value, cancellationToken),

            // Empty, and the screen draws the field with nothing in it. See the
            // remarks above: this is a recorded gap, not an oversight, and it
            // is the same gap `CancelPlanView.Reasons()` reports.
            reasons = Array.Empty<string>(),
        };
    }

    /// <summary>Where a correction from this state goes.</summary>
    /// <remarks>
    /// <b>The two the frames draw, and nothing else.</b> A departure recorded
    /// in error puts the stay back in house; a no-show that did arrive goes
    /// back to booked, so the desk can then check them in through the ordinary
    /// path rather than through a correction. Null for any other state: the
    /// action is not offered there, and inventing a target would let a screen
    /// correct a stay nobody drew a correction for.
    /// </remarks>
    private static StayLifecycle? Target(StayLifecycle from)
        => from switch
        {
            StayLifecycle.Departed => StayLifecycle.InHouse,
            StayLifecycle.NoShow => StayLifecycle.Booked,
            _ => null,
        };

    /// <summary>Whether any other live stay holds the room this stay held.</summary>
    /// <remarks>
    /// <para>
    /// <b>This asks whether the room is held, not whether the spans
    /// overlap</b>, and it is deliberately the more conservative of the two: a
    /// stay booked into this room for next week counts as holding it, so a
    /// correction that would in fact have been harmless is reported as
    /// conflicting.
    /// </para>
    /// <para>
    /// <b>The conservatism costs a refusal; the alternative costs a double
    /// booking.</b> Refusing wrongly sends the desk to the assignment sheet,
    /// which is a screen that exists and works. Allowing wrongly puts two
    /// stays in one room, which GUEST-Q5 says the platform must be able to
    /// represent and which nobody wants a correction to CREATE silently.
    /// </para>
    /// <para>
    /// Narrowing it to a true span overlap needs both stays' instants and is
    /// owed work rather than a thing to guess at here — and the frame does not
    /// draw the conflicting case at all, so what the screen does with a
    /// narrower answer is undesigned either way.
    /// </para>
    /// </remarks>
    private async Task<bool> FreeAsync(
        RoomStay stay, Guid roomId, CancellationToken cancellationToken)
        => !await db.Stays
            .Where(other => other.PropertyId == stay.PropertyId
                && other.CurrentRoomId == roomId
                && other.Id != stay.Id
                && (other.Lifecycle == StayLifecycle.InHouse
                    || other.Lifecycle == StayLifecycle.Booked))
            .AnyAsync(cancellationToken);
}
