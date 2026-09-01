using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Postings;

/// <summary>
/// What Workforce does when Master Data says a person changed.
/// </summary>
/// <remarks>
/// <para>
/// <b>Chapter 04 §6, the last part of the ratified <c>AUTHZ-Q20</c>
/// contract.</b> A posting exists for somebody with no login; later they are
/// given one; nothing re-announces, and the person works on with the department
/// access their posting implies and does not have.
/// </para>
/// <para>
/// <b>It is this application's, and the Kernel must not take it</b> — CC's §4,
/// with a reason better than the one this side had: not merely that the Kernel
/// should not originate a grant, but that it <i>cannot</i>, because
/// <i>open posting</i> is a Workforce concept it has no way to enumerate.
/// ADR 0061's line is that the Kernel materialises what is announced and never
/// decides what should be.
/// </para>
/// <para>
/// <b>Methods a subscription will call, not a subscription.</b> The host that
/// delivers <c>staff.updated</c> and <c>staff.exited</c> to an installed
/// application is <c>EVT-Q4</c> and arrives later; these are driven by the facts
/// themselves and are testable today. That is the same shape every other seam in
/// this application has taken, and it is why the seam does not wait for the host.
/// </para>
/// </remarks>
public class StaffChangeConsumer(
    WorkforceDbContext db,
    PostingAnnouncer announcer,
    TimeProvider clock)
{
    /// <summary>A staff member gained an identity link.</summary>
    /// <param name="scope">The property whose postings to reconcile.</param>
    /// <param name="staffId">The person Master Data announced.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>How many postings were announced.</returns>
    /// <remarks>
    /// <para>
    /// Announces <c>user.posted</c> — and <c>user.headship_started</c> where it
    /// applies — for <b>every open posting</b> the person holds here. Nothing
    /// else changes: the postings were already correct, and what was missing was
    /// only ever the announcement.
    /// </para>
    /// <para>
    /// <b>Open only.</b> A posting that ended before the login existed grants
    /// nothing, and announcing it would write a tuple the very next
    /// <c>posting_ended</c> would have to remove — except that one was already
    /// announced and will not come again.
    /// </para>
    /// </remarks>
    public async Task<int> IdentityLinkGainedAsync(
        RequestScope scope, Guid staffId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var postings = await OpenPostingsAsync(scope, staffId, cancellationToken);

        foreach (var posting in postings)
        {
            await announcer.AnnounceStartedAsync(scope, posting, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return postings.Count;
    }

    /// <summary>A staff member lost their identity link.</summary>
    /// <remarks>
    /// The mirror, and it ships with the first by invariant 2: both directions
    /// land or neither does. An account removed while its tuples stand is
    /// somebody who keeps departmental access they no longer have a login for —
    /// harmless until the account name is reissued.
    /// </remarks>
    public async Task<int> IdentityLinkRemovedAsync(
        RequestScope scope, Guid staffId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var postings = await OpenPostingsAsync(scope, staffId, cancellationToken);

        foreach (var posting in postings)
        {
            await announcer.AnnounceEndedAsync(scope, posting, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return postings.Count;
    }

    /// <summary>A staff member left the organization.</summary>
    /// <param name="scope">The property.</param>
    /// <param name="staffId">Who left.</param>
    /// <param name="on">Their last day.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>How many postings were ended.</returns>
    /// <remarks>
    /// <para>
    /// <c>Q25</c> ruled staff exit this application's, and <c>staff.exited</c>
    /// already exists in Master Data. Ending the open postings is the whole of
    /// it: the announcements follow through the <b>ordinary</b> path, so this
    /// needs no authorization handling of its own.
    /// </para>
    /// <para>
    /// <b>The postings are ended, never deleted.</b> A rota worked last March was
    /// worked under one of them, and a person leaving does not unmake the months
    /// they were here. <c>RecordStaffExit</c> is a business event about the
    /// person; this is what it means for their postings.
    /// </para>
    /// </remarks>
    public async Task<int> StaffExitedAsync(
        RequestScope scope, Guid staffId, DateOnly on, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var postings = await OpenPostingsAsync(scope, staffId, cancellationToken);

        foreach (var posting in postings)
        {
            // Never before the posting began: a person whose exit predates a
            // posting is a record that cannot be true, and the posting's own
            // start is the earliest honest end.
            posting.EffectiveTo = on < posting.EffectiveFrom ? posting.EffectiveFrom : on;
            posting.UpdatedAt = now;
            posting.Version += 1;

            await announcer.AnnounceEndedAsync(scope, posting, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return postings.Count;
    }

    /// <summary>The postings this person holds here that have not ended.</summary>
    /// <remarks>
    /// <b>Not filtered on a start date.</b> A posting that begins next Monday is
    /// open, and a login granted today should announce it — the tuple is what
    /// makes Monday work, and waiting for Monday needs a scheduler nobody has
    /// asked for.
    /// </remarks>
    private async Task<IReadOnlyList<Posting>> OpenPostingsAsync(
        RequestScope scope, Guid staffId, CancellationToken cancellationToken) =>
        await db.Postings
            .Where(p => p.PropertyId == scope.PropertyId
                        && p.StaffId == staffId
                        && p.EffectiveTo == null)
            .OrderBy(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);
}
