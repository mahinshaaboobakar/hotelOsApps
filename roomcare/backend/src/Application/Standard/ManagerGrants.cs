using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Standard;

/// <summary>The general manager's grant of property-wide Room Care access — announced; the Kernel folds it (AUTHZ-Q25).</summary>
/// <remarks>
/// <para>
/// Room Care writes no tuple. The row is the record of an action — the
/// Property-wide access tab (frame 7g) shows whom the manager has granted, and a
/// revocation is a column, because an audit asks who held it and when.
/// </para>
/// <para>
/// <b>The events' aggregate is the grant row, not the property.</b>
/// <c>event_store.events</c> is unique on
/// <c>(aggregate_type, aggregate_id, entity_version)</c> and is shared by every
/// writer; Master Data already announces the property aggregate with its own
/// versions, so an application announcing on <c>property:{id}</c> would collide
/// with them (ADR 0125's reason for grant aggregates). The grant row's own id,
/// version 1 granted and 2 revoked, cannot collide with anything. The
/// manifest's grant kind declares <c>aggregate: roomcare_manager_grant</c> to
/// match — Workforce's <c>posting</c> is the precedent.
/// </para>
/// </remarks>
public sealed class ManagerGrants(RoomCareDbContext db, Gate gate, IEventAppender events, TimeProvider clock)
{
    public async Task<IReadOnlyList<RoomCareManagerGrant>> LiveAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        return await db.ManagerGrants
            .Where(g => g.PropertyId == scope.PropertyId && g.RevokedAt == null)
            .OrderBy(g => g.GrantedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<RoomCareManagerGrant> GrantAsync(RequestScope scope, Guid userId, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        var actor = Actor.PersonOf(scope, "granting property-wide Room Care access");
        if (userId == Guid.Empty)
        {
            throw new InvalidRequestException("a grant names the person it is for");
        }

        var standing = await db.ManagerGrants.FirstOrDefaultAsync(
            g => g.PropertyId == scope.PropertyId && g.UserId == userId && g.RevokedAt == null, cancellationToken);
        if (standing is not null)
        {
            return standing;
        }

        var granted = new RoomCareManagerGrant
        {
            Id = Guid.CreateVersion7(), PropertyId = scope.PropertyId, UserId = userId, GrantedAt = clock.GetUtcNow(), GrantedBy = actor,
        };
        db.ManagerGrants.Add(granted);
        Announce(scope, EventTypes.ManagerGranted, granted, 1, granted.GrantedAt);
        await db.SaveChangesAsync(cancellationToken);
        return granted;
    }

    public async Task<RoomCareManagerGrant?> RevokeAsync(RequestScope scope, Guid userId, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        var actor = Actor.PersonOf(scope, "revoking property-wide Room Care access");
        var standing = await db.ManagerGrants.FirstOrDefaultAsync(
            g => g.PropertyId == scope.PropertyId && g.UserId == userId && g.RevokedAt == null, cancellationToken);
        if (standing is null)
        {
            return null;
        }

        standing.RevokedAt = clock.GetUtcNow();
        standing.RevokedBy = actor;
        Announce(scope, EventTypes.ManagerRevoked, standing, 2, standing.RevokedAt.Value);
        await db.SaveChangesAsync(cancellationToken);
        return standing;
    }

    private void Announce(RequestScope scope, string type, RoomCareManagerGrant grant, long version, DateTimeOffset at) =>
        events.Append(scope, type, EventTypes.ManagerGrantAggregate, grant.Id, version, new ManagerGrantAnnouncement
        {
            UserId = grant.UserId,
            PropertyId = grant.PropertyId,
            OccurredAt = at,
        });
}
