using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Application.Postings;

/// <summary>
/// Announcing a posting fact — the gates, in one place, for both callers.
/// </summary>
/// <remarks>
/// <para>
/// <b>Extracted when the second consumer arrived</b>, not before:
/// <see cref="PostingService"/> announces when a posting is written, and
/// <see cref="StaffChangeConsumer"/> announces when a person's identity link
/// changes under postings that already exist. Two copies of these gates would
/// drift, and the drift would be invisible — both would still look like an
/// announcement.
/// </para>
/// <para>
/// It <b>appends</b>; it never sends. The event and its <c>publish_state</c> row
/// go into the caller's transaction and commit with whatever caused them. A gRPC
/// call could not join that transaction, and a crash in the gap would keep the
/// change and lose its authorization silently.
/// </para>
/// </remarks>
public class PostingAnnouncer(IEventAppender events, IStaffDirectory directory)
{
    /// <summary>Announce a fact about a posting, if there is one to announce.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="posting">The posting the fact is about.</param>
    /// <param name="eventType">Which of the four — see <see cref="PostingAnnouncements"/>.</param>
    /// <param name="occurredAt">When.</param>
    /// <param name="staff">
    /// The person's account, resolved by the caller. <c>null</c> when they are
    /// unknown at this property or have no login — both mean nothing is
    /// announced, and neither is an error.
    /// </param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Whether anything was appended.</returns>
    /// <remarks>
    /// <b>Both gates live here.</b> No announcement without an identity link —
    /// most of the workforce has no account, and a posting for such a person is
    /// complete, correct and silent, because there is no principal for a tuple to
    /// name. No announcement without the department's canonical id either: a
    /// consumer cannot address <c>department:{uuid}</c> from a code.
    /// </remarks>
    public async Task<bool> AnnounceAsync(
        RequestScope scope,
        Posting posting,
        string eventType,
        DateTimeOffset occurredAt,
        StaffLink? staff,
        CancellationToken cancellationToken)
    {
        // **The link is passed in, not looked up here.** Every caller has
        // already resolved it — `post` to refuse an unknown staff member, the
        // ending path once for both of its announcements — and a lookup here
        // would read the same row a second and, for a headship, a third time.
        // A characterisation test counts those reads, which is how the
        // duplication was found rather than shipped.
        //
        // Unknown here, or known with no account: either way there is no
        // principal to grant anything to, so nothing is announced.
        if (staff?.UserId is not { } user)
        {
            return false;
        }

        var departmentId = await directory.FindDepartmentIdAsync(
            scope.PropertyId, posting.DepartmentCode, cancellationToken);

        if (departmentId is not { } department)
        {
            return false;
        }

        events.Append(
            scope,
            eventType,
            PostingAnnouncements.Aggregate,
            posting.Id,
            posting.Version,
            new PostingAnnouncement
            {
                UserId = user,
                StaffId = posting.StaffId,
                DepartmentId = department,
                DepartmentCode = posting.DepartmentCode,
                PostingId = posting.Id,
                PropertyId = posting.PropertyId,
                OccurredAt = occurredAt,
            });

        return true;
    }

    /// <summary>Announce a posting starting, and its headship if it carries one.</summary>
    /// <remarks>
    /// The pairing appears three times in this application — on create, on end,
    /// and on a reconciliation — so it is written once. Headship is its own grant
    /// kind, which is why it is a second announcement rather than a flag on the
    /// first.
    /// </remarks>
    public async Task AnnounceStartedAsync(
        RequestScope scope,
        Posting posting,
        DateTimeOffset occurredAt,
        StaffLink? staff,
        CancellationToken cancellationToken)
    {
        await AnnounceAsync(
            scope, posting, PostingAnnouncements.Posted, occurredAt, staff, cancellationToken);

        if (posting.IsDepartmentHead)
        {
            await AnnounceAsync(
                scope, posting, PostingAnnouncements.HeadshipStarted, occurredAt, staff,
                cancellationToken);
        }
    }

    /// <summary>Announce a posting ending, and its headship if it carried one.</summary>
    /// <remarks>
    /// Both directions land together or neither does — ADR 0087's addendum
    /// records what a one-directional writer produced: <i>"a posting revoked left
    /// its tuple standing, so somebody removed from a property stayed reachable
    /// there"</i>, which is the direction ADR 0061's invariant forbids.
    /// </remarks>
    public async Task AnnounceEndedAsync(
        RequestScope scope,
        Posting posting,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        // Resolved once here, for both announcements. The ending path has no
        // earlier lookup to reuse: nothing refuses an unknown staff member on
        // the way out, because a posting that exists is ended whatever Master
        // Data now says about the person.
        var staff = await directory.FindStaffAsync(
            scope.PropertyId, posting.StaffId, cancellationToken);

        await AnnounceAsync(
            scope, posting, PostingAnnouncements.PostingEnded, occurredAt, staff,
            cancellationToken);

        if (posting.IsDepartmentHead)
        {
            await AnnounceAsync(
                scope, posting, PostingAnnouncements.HeadshipEnded, occurredAt, staff,
                cancellationToken);
        }
    }
}
