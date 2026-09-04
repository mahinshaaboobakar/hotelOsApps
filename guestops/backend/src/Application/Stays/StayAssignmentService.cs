using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Stays;

/// <summary>
/// The room a stay occupies — given, and moved.
/// </summary>
/// <remarks>
/// <para>
/// One operation on the model: the open assignment closes and a new one opens.
/// What differs is the <b>fact published</b> — <c>stay.assigned</c> for the
/// first room and <c>stay.room_changed</c> for every later one, because R8
/// requires a room change to be distinguishable from an update, and Room Care
/// flips two axes on a move while Jobs may have work open against either room.
/// </para>
/// <para>
/// <b>An upgrade is an assignment</b> — GUEST-Q8 (b). A better room on unchanged
/// terms leaves the sale as booked; it becomes an amendment only when the booked
/// type or the terms themselves change. The test is what changed, not what the
/// guest got.
/// </para>
/// </remarks>
public sealed class StayAssignmentService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    IEventAppender events,
    TimeProvider clock)
{
    /// <summary>Give the stay a room, or move it to another.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="stayId">The stay being assigned.</param>
    /// <param name="roomId">Master Data's room.</param>
    /// <param name="reason">Initial, move, upgrade or correction.</param>
    /// <param name="acceptConflict">
    /// Whether the caller has seen the conflict and means it. The check warns
    /// and never forbids, because GUEST-Q5 made a double-booked room a possible
    /// truth.
    /// </param>
    /// <param name="version">The version the caller last read.</param>
    /// <param name="cancellationToken">The call's token.</param>
    public async Task<RoomStay> AssignAsync(
        RequestScope scope,
        Guid stayId,
        Guid roomId,
        AssignmentReason reason,
        bool acceptConflict,
        long version,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.StayAssign, ResourceTypes.Stay, stayId, cancellationToken);

        var stay = await db.Stays
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(
                s => s.Id == stayId && s.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        if (stay.Version != version)
        {
            throw new ConcurrencyException("stay", stayId, version);
        }

        if (!acceptConflict)
        {
            var clash = await ConflictingStayAsync(scope.PropertyId, roomId, stay, cancellationToken);
            if (clash is not null)
            {
                // **Warns; never forbids.** GUEST-Q5 made a double-booked room a
                // possible truth — when staff answer "two different stays" to a
                // candidate link, the second stay is real and the room is
                // genuinely double-booked — so a hard block here would put a
                // ruled outcome out of reach. It names the other stay and lets
                // a person decide.
                throw new InUseException("room", roomId, $"stay {clash}");
            }
        }

        var now = clock.GetUtcNow();
        var open = stay.Assignments.FirstOrDefault(a => a.ReleasedAt is null);
        var previousRoom = open?.RoomId;

        if (open is not null)
        {
            open.ReleasedAt = now;
        }

        db.Assignments.Add(new Assignment
        {
            Id = Guid.CreateVersion7(),
            StayId = stay.Id,
            RoomId = roomId,
            AssignedAt = now,
            AssignedBy = scope.UserId,
            Reason = reason,
        });

        // The projection of the open row, resolved here. The request has
        // nowhere to put it, which is what makes the mistake inexpressible
        // rather than merely rejected.
        stay.CurrentRoomId = roomId;
        stay.UpdatedBy = scope.UserId;
        stay.Version += 1;

        await ClearAbsenceAsync(stayId, AbsentFields.Assignment, cancellationToken);

        if (previousRoom is null)
        {
            events.Append(scope, "stay.assigned", "stay", stay.Id, stay.Version, new
            {
                stay_id = stay.Id,
                property_id = stay.PropertyId,
                room_id = roomId,
            });
        }
        else
        {
            events.Append(scope, "stay.room_changed", "stay", stay.Id, stay.Version, new
            {
                stay_id = stay.Id,
                property_id = stay.PropertyId,
                from_room_id = previousRoom,
                to_room_id = roomId,
                reason = reason.ToString(),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return stay;
    }

    /// <summary>
    /// Another stay holding this room over the same nights, if there is one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The conflict check, one room wide — the small half of availability, and
    /// the one that stops the worst outcome. It runs on every assignment and
    /// every move, in <b>both modes</b>: GUEST-Q4 removed the second mode, so
    /// there is no branch here for a PMS-connected property.
    /// </para>
    /// <para>
    /// Only stays that <b>hold</b> the room count — a cancelled or departed one
    /// does not, and a waitlisted one never held a room at all.
    /// </para>
    /// </remarks>
    private async Task<Guid?> ConflictingStayAsync(
        Guid propertyId, Guid roomId, RoomStay stay, CancellationToken cancellationToken)
    {
        var arrival = stay.ArrivalAt.Date;
        var departure = stay.DepartureAt.Date;

        if (arrival is null || departure is null)
        {
            // Without dates there is nothing to overlap. A stay whose dates are
            // unknown is an incomplete record, not a conflict — and inventing a
            // range to compare against would be inventing the fact.
            return null;
        }

        var holding = new[]
        {
            StayLifecycle.Pending, StayLifecycle.Booked, StayLifecycle.InHouse,
        };

        var clash = await db.Stays
            .Where(s => s.PropertyId == propertyId
                        && s.Id != stay.Id
                        && s.CurrentRoomId == roomId
                        && holding.Contains(s.Lifecycle))
            .Select(s => new { s.Id, Arrival = s.ArrivalAt.At, Departure = s.DepartureAt.At })
            .ToListAsync(cancellationToken);

        // The overlap is evaluated here rather than in the query because the
        // dates are inside an owned value object and the comparison is over
        // dates, not instants — a departure at 11:00 and an arrival at 14:00 on
        // one day are not an overlap, and a naive instant comparison would say
        // they were.
        foreach (var other in clash)
        {
            var otherArrival = other.Arrival is { } a ? DateOnly.FromDateTime(a.DateTime) : null as DateOnly?;
            var otherDeparture = other.Departure is { } d ? DateOnly.FromDateTime(d.DateTime) : null as DateOnly?;

            if (otherArrival is null || otherDeparture is null)
            {
                continue;
            }

            if (otherArrival < departure && arrival < otherDeparture)
            {
                return other.Id;
            }
        }

        return null;
    }

    private async Task ClearAbsenceAsync(
        Guid stayId, string field, CancellationToken cancellationToken)
    {
        var absence = await db.Absences
            .FirstOrDefaultAsync(a => a.StayId == stayId && a.Field == field, cancellationToken);

        if (absence is not null)
        {
            db.Absences.Remove(absence);
        }
    }
}
