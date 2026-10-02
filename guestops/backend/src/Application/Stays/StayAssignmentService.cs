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
    IBusinessDay businessDay,
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
            var clash = await ConflictingStayAsync(
                scope.PropertyId, roomId, stay, await businessDay.ZoneAsync(scope, cancellationToken), cancellationToken);
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

    /// <summary>Let the stay's room go, and announce that the room is free.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="stay">The stay, tracked.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>True where a room was released; false where the stay held none.</returns>
    /// <remarks>
    /// <para>
    /// <b>ADR 0365 §2 and §3 — and the mechanism was already here.</b> A room is
    /// not held by a field, it is held by an <see cref="Assignment"/> record
    /// carrying <c>ReleasedAt</c>, which every room MOVE above already stamps. The
    /// defect was that cancel, no-show and check-out never called it. <i>No new
    /// column and no new concept: this is the move's first half, on its own.</i>
    /// </para>
    /// <para>
    /// <b>⚠ IT RESOLVES THE PROJECTION, BECAUSE NOTHING RESOLVES IT FOR US — and
    /// ADR 0365 §4 says otherwise.</b> That section reads <i>"close the open
    /// assignment and the projection follows"</i>. Measured 2026-10-02:
    /// <c>CurrentRoomId</c> is a plain <c>Guid?</c> with an index, <b>no value
    /// converter and no computed column</b>, set by hand at <c>:107</c>. Nothing
    /// recomputes it, so a release that closed the record alone would leave the
    /// field naming a room the stay no longer holds — and four readers take it as
    /// current truth, among them <c>WatchlistView.cs:48</c>, which reads
    /// <c>== null</c> as <i>no room</i>.
    /// </para>
    /// <para>
    /// <b>So the projection is resolved HERE, which is what §4 actually
    /// protects.</b> Its hazard is a <i>second writer</i>: three lifecycle methods
    /// each nulling the field by hand. Keeping it in the service that owns the
    /// record means the projection has one maintainer on the release path exactly
    /// as it has one on the assign path — symmetric, and the callers cannot get it
    /// wrong because they never touch it.
    /// </para>
    /// <para>
    /// <b>It authorizes nothing, on <c>SettingsService.LoadAsync</c>'s
    /// precedent.</b> Every caller has already required <c>stay.override</c> on
    /// the stay; asking for <c>stay.assign</c> as well would mean a person who may
    /// check a guest out also needs the permission to move rooms.
    /// </para>
    /// <para>
    /// <b>The announcement is the room's, not the guest's</b> — ADR 0365 §5, the
    /// owner's <i>"two events"</i>. Leaving is a fact about a guest and a free
    /// room is a fact about a room; told only <c>stay.departed</c>, Room Care would
    /// have to reason about GuestOps' model to conclude the room is free. <i>The
    /// subject's spelling is the architect's proposal recorded in §5, not the
    /// owner's words, and is correctable in one word.</i>
    /// </para>
    /// </remarks>
    internal async Task<bool> ReleaseAsync(
        RequestScope scope, RoomStay stay, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stay);

        // **The open row is QUERIED, never read off stay.Assignments** — and that
        // is not a style choice. Nothing in this application enables lazy loading;
        // `AssignAsync` Includes the collection and
        // `StayLifecycleService.RequireWritableAsync` does not. A release that
        // trusted the navigation would find it EMPTY on all three of its callers,
        // return false, and silently leave the room held — a no-op that no test
        // asserting "a room was released" would catch unless it looked at the row.
        var open = await db.Assignments
            .FirstOrDefaultAsync(
                a => a.StayId == stay.Id && a.ReleasedAt == null, cancellationToken);

        if (open is null) return false;

        open.ReleasedAt = clock.GetUtcNow();

        // The projection, resolved here for the same reason it is resolved on the
        // assign path: it is not the truth, and it has one maintainer.
        stay.CurrentRoomId = null;
        stay.UpdatedBy = scope.UserId;

        // **The version is advanced, and the event store is what requires it.**
        // `uq_events__aggregate_version` is unique on
        // (aggregate_type, aggregate_id, entity_version), so two events about one
        // stay in one transaction CANNOT share a version — the second insert fails
        // with 23505. ADR 0365 §5 ruled two events without that constraint in view,
        // and a release that reused the caller's version made `stay.departed` and
        // `stay.room_released` collide.
        //
        // It is also the honest value: the release changed this stay, so the change
        // gets its own version exactly as a room MOVE does at `:109`. Callers must
        // read the version from the returned stay rather than computing one more
        // than they sent — which `WalkInPhasesTests` already learned the hard way.
        stay.Version += 1;

        events.Append(scope, "stay.room_released", "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            property_id = stay.PropertyId,
            room_id = open.RoomId,
            released_at = open.ReleasedAt,
        });

        return true;
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
        Guid propertyId, Guid roomId, RoomStay stay, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        // Compared on the property's calendar (ADR 0174); with no zone there is
        // no day to compare, which is no conflict to report — never a UTC guess.
        var arrival = stay.ArrivalAt.DateIn(zone);
        var departure = stay.DepartureAt.DateIn(zone);

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
            var otherArrival = new StayTime(other.Arrival, TimeBasis.Observed).DateIn(zone);
            var otherDeparture = new StayTime(other.Departure, TimeBasis.Observed).DateIn(zone);

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
