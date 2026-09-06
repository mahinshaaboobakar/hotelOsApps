using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Configuration;

/// <summary>
/// The general manager's action — making somebody a jobs manager, and taking it
/// back. Design §4.2, and the chapter's §7 table where it is named
/// <i>"the GM's action"</i>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Jobs publishes; the Kernel grants.</b> The manifest declares
/// <c>user.jobs_manager_granted</c> and <c>user.jobs_manager_revoked</c> with
/// <c>ends: both_in_body</c>, so the Kernel folds each into
/// <c>user:{user_id} → jobs_manager → property:{property_id}</c>. This
/// application never writes a tuple, which is §4.2's own sentence and the
/// reason a grant made here works in every application that asks the graph, not
/// only in this one.
/// </para>
/// <para>
/// <b>The event carries both ends in the body, and that is not redundancy.</b>
/// The envelope names the administrator who decided and the property they were
/// standing in; the person gaining access is somebody else, and reading the
/// envelope would grant the general manager themself. The Kernel says so in as
/// many words, and both mistakes <i>"would look correct in every test where the
/// two coincide"</i>.
/// </para>
/// <para>
/// <b>Both operations are idempotent</b>, deliberately: granting what already
/// stands returns the standing grant and announces nothing — a second event
/// would fold to the same tuple and litter the log with a decision nobody made
/// twice — and revoking what is already gone succeeds. A double-click is not a
/// support question.
/// </para>
/// </remarks>
public class JobsManagerGrants(JobsDbContext db, IEventAppender events, TimeProvider clock)
{
    /// <summary>Who holds it here, now — the screen's list.</summary>
    public async Task<IReadOnlyList<JobsManagerGrant>> LiveAsync(
        RequestScope scope, CancellationToken cancellationToken) =>
        await db.JobsManagerGrants
            .Where(g => g.PropertyId == scope.PropertyId && g.RevokedAt == null)
            .OrderBy(g => g.GrantedAt)
            .ToListAsync(cancellationToken);

    /// <summary>Make somebody a jobs manager at this property.</summary>
    public async Task<JobsManagerGrant> GrantAsync(
        RequestScope scope, Guid userId, CancellationToken cancellationToken)
    {
        // A person cannot be granted property-wide job management by nobody:
        // the envelope's caller is who the audit records, and a scope without
        // one is a service call reaching an action that is a person's.
        var actor = scope.UserId ?? throw new InvalidRequestException(
            "granting a jobs manager is a person's action and this call named no user");

        if (userId == Guid.Empty)
        {
            throw new InvalidRequestException("a jobs-manager grant must name the person it is for");
        }

        var standing = await db.JobsManagerGrants.FirstOrDefaultAsync(
            g => g.PropertyId == scope.PropertyId && g.UserId == userId && g.RevokedAt == null,
            cancellationToken);

        if (standing is not null)
        {
            return standing;
        }

        var granted = new JobsManagerGrant
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            UserId = userId,
            GrantedAt = clock.GetUtcNow(),
            GrantedBy = actor,
        };

        db.JobsManagerGrants.Add(granted);
        Announce(scope, EventTypes.JobsManagerGranted, granted.UserId, granted.GrantedAt);

        await db.SaveChangesAsync(cancellationToken);
        return granted;
    }

    /// <summary>Take it back.</summary>
    public async Task<JobsManagerGrant?> RevokeAsync(
        RequestScope scope, Guid userId, CancellationToken cancellationToken)
    {
        var actor = scope.UserId ?? throw new InvalidRequestException(
            "revoking a jobs manager is a person's action and this call named no user");

        var standing = await db.JobsManagerGrants.FirstOrDefaultAsync(
            g => g.PropertyId == scope.PropertyId && g.UserId == userId && g.RevokedAt == null,
            cancellationToken);

        if (standing is null)
        {
            return null;
        }

        standing.RevokedAt = clock.GetUtcNow();
        standing.RevokedBy = actor;
        Announce(scope, EventTypes.JobsManagerRevoked, standing.UserId, standing.RevokedAt.Value);

        await db.SaveChangesAsync(cancellationToken);
        return standing;
    }

    /// <summary>
    /// Append the declared event — the property is the aggregate, and both ends
    /// are in the body because the Kernel reads them from there.
    /// </summary>
    private void Announce(RequestScope scope, string eventType, Guid userId, DateTimeOffset at) =>
        events.Append(
            scope,
            eventType,
            EventTypes.PropertyAggregate,
            scope.PropertyId,
            // The property is not a row this application versions. `0` says so
            // rather than inventing a number that would look like optimistic
            // concurrency on something nothing here locks.
            entityVersion: 0,
            new JobsManagerAnnouncement
            {
                UserId = userId,
                PropertyId = scope.PropertyId,
                OccurredAt = at,
            });
}

/// <summary>
/// What a jobs-manager grant announces — snake_cased by the appender into the
/// <c>user_id</c> and <c>property_id</c> the Kernel's fold reads.
/// </summary>
public sealed class JobsManagerAnnouncement
{
    /// <summary>The person granted or revoked — never the administrator who decided.</summary>
    public Guid UserId { get; init; }

    /// <summary>The property the grant is at — the tuple's object.</summary>
    public Guid PropertyId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}
