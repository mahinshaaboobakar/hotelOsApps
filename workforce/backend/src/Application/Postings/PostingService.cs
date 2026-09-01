using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Postings;

/// <summary>
/// Postings — who is posted where, as what, and from when.
/// </summary>
/// <remarks>
/// <para>
/// The only writer of <c>workforce.postings</c>, and the aggregate ADR 0116 §6
/// makes department-scoped authorization derive from.
/// </para>
/// <para>
/// <b>On the one outbound call this service makes.</b> <c>EVT-Q3</c> rules that
/// between <i>applications</i> a reply is an event carrying a correlation id and
/// never a blocking call, and preserves request/reply for platform-internal
/// <i>questions</i>. <see cref="IStaffDirectory"/> asks Master Data two
/// questions — does this person have a login, and what is this department's row
/// id. Master Data is the <b>platform</b>, not a neighbouring application;
/// CLAUDE.md's non-negotiable list says applications may read master data, and
/// these are questions rather than commands. Written down here because a
/// synchronous call in an installable application is exactly the shape somebody
/// should challenge, and the answer should be in the file rather than in
/// somebody's memory.
/// </para>
/// </remarks>
public class PostingService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    IStaffDirectory directory,
    IEventAppender events,
    TimeProvider clock)
{
    /// <summary>Post a person to a department.</summary>
    public async Task<Posting> CreateAsync(
        RequestScope scope, CreatePostingCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PostingManage, "property", scope.PropertyId, cancellationToken);

        var code = Normalise(command.DepartmentCode);
        var role = command.JobRole?.Trim() ?? string.Empty;

        if (code.Length == 0)
        {
            throw new InvalidRequestException("department_code is required");
        }

        if (role.Length == 0)
        {
            throw new InvalidRequestException("job_role is required");
        }

        // The department must be one this property has activated — ADR 0119: a
        // property activates canon codes and never invents one. Resolved rather
        // than trusted, because a posting to a department that does not exist
        // here is a posting nothing can ever resolve.
        var departmentId =
            await directory.FindDepartmentIdAsync(scope.PropertyId, code, cancellationToken)
            ?? throw new InvalidRequestException(
                $"department {code} is not activated at this property");

        await RefuseOverlapAsync(scope.PropertyId, command, code, cancellationToken);

        if (command.IsDepartmentHead)
        {
            await RefuseSecondHeadAsync(scope.PropertyId, code, null, cancellationToken);
        }

        var now = clock.GetUtcNow();
        var posting = new Posting
        {
            Id = Uuid7.NewUuid7(),
            PropertyId = scope.PropertyId,
            StaffId = command.StaffId,
            DepartmentCode = code,
            JobRole = role,
            IsPrimary = command.IsPrimary,
            IsDepartmentHead = command.IsDepartmentHead,
            ZoneId = command.ZoneId,
            ReportingManagerStaffId = command.ReportingManagerStaffId,
            EffectiveFrom = command.EffectiveFrom,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        // The identity link is resolved here, and deliberately not stored:
        // `masterdata.staff.user_id` is Master Data's and a copy of it can only
        // go stale. Null is the ordinary answer — most staff have no login, and
        // a posting for such a person is complete, correct, and announces
        // nothing: there is no principal for a tuple to name.
        var userId = await directory.FindUserIdAsync(
            scope.PropertyId, command.StaffId, cancellationToken);

        db.Postings.Add(posting);

        // Appended in the caller's transaction, never sent — the event and its
        // publish_state row commit with the posting or not at all. A gRPC call
        // could not join this transaction, and a crash in the gap would keep the
        // posting and lose its authorization silently.
        Announce(scope, posting, departmentId, userId, PostingAnnouncements.Posted, now);

        if (posting.IsDepartmentHead)
        {
            // Two events from one operation. Headship is its own grant kind —
            // `department#manager` — so it is its own announcement, and folding
            // it into `user.posted` would have widened every kind in the Kernel's
            // table to serve one.
            Announce(
                scope, posting, departmentId, userId,
                PostingAnnouncements.HeadshipStarted, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return posting;
    }

    /// <summary>Amend a posting, without moving it to another person or department.</summary>
    public async Task<Posting> UpdateAsync(
        RequestScope scope, UpdatePostingCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PostingManage, "property", scope.PropertyId, cancellationToken);

        var posting = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(posting, command.ExpectedVersion);

        if (command.JobRole is { } role)
        {
            var trimmed = role.Trim();
            posting.JobRole = trimmed.Length > 0
                ? trimmed
                : throw new InvalidRequestException("job_role cannot be cleared");
        }

        if (command.IsPrimary is { } primary)
        {
            posting.IsPrimary = primary;
        }

        if (command.IsDepartmentHead is { } head && head != posting.IsDepartmentHead)
        {
            if (head)
            {
                await RefuseSecondHeadAsync(
                    scope.PropertyId, posting.DepartmentCode, posting.Id, cancellationToken);
            }

            // **The third and fourth triggers.** Headship changes without the
            // posting starting or finishing, which is why it is a second grant
            // kind rather than a flag read off `user.posted` — and it is why
            // this method, which announced nothing until now, announces.
            await AnnounceHeadshipAsync(scope, posting, head, cancellationToken);

            posting.IsDepartmentHead = head;
        }

        if (command.ZoneId.IsPresent)
        {
            posting.ZoneId = command.ZoneId.Value;
        }

        if (command.ReportingManagerStaffId.IsPresent)
        {
            posting.ReportingManagerStaffId = command.ReportingManagerStaffId.Value;
        }

        posting.UpdatedAt = clock.GetUtcNow();
        posting.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return posting;
    }

    /// <summary>Close a posting's window. The row survives.</summary>
    public async Task<Posting> EndAsync(
        RequestScope scope, EndPostingCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PostingManage, "property", scope.PropertyId, cancellationToken);

        var posting = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(posting, command.ExpectedVersion);

        // A window that ends before it starts cannot be true — refused, not
        // warned. WF-Q16: the platform refuses the physically impossible and
        // warns on a judgment.
        if (command.EffectiveTo < posting.EffectiveFrom)
        {
            throw new InvalidRequestException(
                "effective_to is before the posting started");
        }

        posting.EffectiveTo = command.EffectiveTo;
        posting.UpdatedAt = clock.GetUtcNow();
        posting.Version += 1;

        // Both directions land together or neither does — ADR 0087's addendum
        // records what a one-directional writer produced: *"a posting revoked
        // left its tuple standing, so somebody removed from a property stayed
        // reachable there"*, the direction ADR 0061's invariant forbids.
        var userId = await directory.FindUserIdAsync(
            scope.PropertyId, posting.StaffId, cancellationToken);

        var departmentId = await directory.FindDepartmentIdAsync(
            scope.PropertyId, posting.DepartmentCode, cancellationToken);

        Announce(
            scope, posting, departmentId, userId,
            PostingAnnouncements.PostingEnded, posting.UpdatedAt);

        if (posting.IsDepartmentHead)
        {
            // The second trigger's pair: `posting_ended` withdraws `#posted`,
            // `headship_ended` withdraws `#manager`. Two relations, two tuples,
            // two announcements — and the flag stays on the row, because the
            // posting records that this person headed the department while it
            // ran.
            Announce(
                scope, posting, departmentId, userId,
                PostingAnnouncements.HeadshipEnded, posting.UpdatedAt);
        }

        await db.SaveChangesAsync(cancellationToken);
        return posting;
    }

    /// <summary>One posting, scoped to the caller's property.</summary>
    public async Task<Posting> GetAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        return await LoadAsync(scope, id, cancellationToken);
    }

    /// <summary>Postings at this property, filtered.</summary>
    public async Task<IReadOnlyList<Posting>> ListAsync(
        RequestScope scope, ListPostingsQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        var postings = db.Postings.Where(p => p.PropertyId == scope.PropertyId);

        if (query.StaffId is { } staffId)
        {
            postings = postings.Where(p => p.StaffId == staffId);
        }

        if (!string.IsNullOrWhiteSpace(query.DepartmentCode))
        {
            var code = Normalise(query.DepartmentCode);
            postings = postings.Where(p => p.DepartmentCode == code);
        }

        if (query.ZoneId is { } zoneId)
        {
            postings = postings.Where(p => p.ZoneId == zoneId);
        }

        if (!query.IncludeEnded)
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            postings = postings.Where(p => p.EffectiveTo == null || p.EffectiveTo >= today);
        }

        return await postings
            .OrderBy(p => p.DepartmentCode)
            .ThenBy(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Two open postings for one person in one department cannot both be true.
    /// </summary>
    /// <remarks>
    /// Enforced here rather than by a unique index, because the rule is about
    /// <i>windows</i> and an index cannot compare them. A person may hold the
    /// same posting twice across time — Kitchen until March, Kitchen again from
    /// September — and a uniqueness rule that ignored the window would make
    /// re-hiring somebody impossible.
    /// </remarks>
    private async Task RefuseOverlapAsync(
        Guid propertyId,
        CreatePostingCommand command,
        string code,
        CancellationToken cancellationToken)
    {
        var overlapping = await db.Postings.AnyAsync(
            p => p.PropertyId == propertyId
                 && p.StaffId == command.StaffId
                 && p.DepartmentCode == code
                 && (p.EffectiveTo == null || p.EffectiveTo >= command.EffectiveFrom),
            cancellationToken);

        if (overlapping)
        {
            throw new InvalidRequestException(
                $"this person already holds an overlapping posting in {code}");
        }
    }

    /// <summary>Announce one posting fact, if there is a principal to name.</summary>
    /// <remarks>
    /// <para>
    /// <b>Every gate is here, once.</b> No announcement without an identity link
    /// — most of the workforce has no account, and a posting for such a person is
    /// complete and announces nothing, because there is no principal for a tuple
    /// to grant anything to. No announcement without the department's canonical
    /// id either: the id is what <c>department:{uuid}</c> addresses, and a
    /// consumer cannot write a tuple from a code alone.
    /// </para>
    /// <para>
    /// It appends; it never sends. The event and its <c>publish_state</c> row are
    /// written into the caller's transaction, and the Kernel's Event Publisher
    /// relays afterwards.
    /// </para>
    /// </remarks>
    private void Announce(
        RequestScope scope,
        Posting posting,
        Guid? departmentId,
        Guid? userId,
        string eventType,
        DateTimeOffset occurredAt)
    {
        if (userId is not { } user || departmentId is not { } department)
        {
            return;
        }

        events.Append(
            scope,
            eventType,
            PostingAnnouncements.Aggregate,
            posting.Id,
            posting.Version,
            new PostingAnnouncement(
                UserId: user,
                StaffId: posting.StaffId,
                DepartmentId: department,
                DepartmentCode: posting.DepartmentCode,
                PostingId: posting.Id,
                PropertyId: posting.PropertyId,
                OccurredAt: occurredAt));
    }

    /// <summary>Announce headship starting or ending, resolving what it needs.</summary>
    /// <remarks>
    /// Its own method because <c>UpdateAsync</c> reaches it without having
    /// resolved either identifier — unlike create and end, which already hold
    /// both.
    /// </remarks>
    private async Task AnnounceHeadshipAsync(
        RequestScope scope, Posting posting, bool started, CancellationToken cancellationToken)
    {
        var userId = await directory.FindUserIdAsync(
            scope.PropertyId, posting.StaffId, cancellationToken);

        if (userId is null)
        {
            return;
        }

        var departmentId = await directory.FindDepartmentIdAsync(
            scope.PropertyId, posting.DepartmentCode, cancellationToken);

        Announce(
            scope,
            posting,
            departmentId,
            userId,
            started ? PostingAnnouncements.HeadshipStarted : PostingAnnouncements.HeadshipEnded,
            clock.GetUtcNow());
    }

    /// <summary>A department has one current head, or none.</summary>
    /// <remarks>
    /// <para>
    /// ADR 0063's table names <i>Department → <b>current</b> head Staff</i>, and
    /// two live heads is the same corrupt shape the MOD register refuses: a
    /// question with two answers. The approver resolution reads this flag, so a
    /// second head would put a request in whichever queue the database happened
    /// to return first.
    /// </para>
    /// <para>
    /// <b>Refused, not warned</b> — <c>WF-Q16</c>. Handing headship over is
    /// clearing the flag on one posting and setting it on the other, which is
    /// two deliberate acts rather than one ambiguous state.
    /// </para>
    /// <para>
    /// Found by a test, not by reasoning: a suite that created several heads in
    /// one department got whichever the query returned, and the assertion that
    /// failed was asking the right question of a model that could not answer it.
    /// </para>
    /// </remarks>
    private async Task RefuseSecondHeadAsync(
        Guid propertyId, string code, Guid? excluding, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var taken = await db.Postings.AnyAsync(
            p => p.PropertyId == propertyId
                 && p.DepartmentCode == code
                 && p.IsDepartmentHead
                 && (p.EffectiveTo == null || p.EffectiveTo >= today)
                 && (excluding == null || p.Id != excluding),
            cancellationToken);

        if (taken)
        {
            throw new InvalidRequestException(
                $"{code} already has a department head — end or amend that posting first");
        }
    }

    private async Task<Posting> LoadAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken)
    {
        // Scoped by property in the query itself, so a posting at another
        // property is Not Found rather than Permission Denied — a cross-property
        // read must not confirm that the id exists.
        var posting = await db.Postings.FirstOrDefaultAsync(
            p => p.Id == id && p.PropertyId == scope.PropertyId, cancellationToken);

        return posting ?? throw new NotFoundException("posting", id);
    }

    private static void RequireVersion(Posting posting, long expected)
    {
        if (posting.Version != expected)
        {
            throw new ConcurrencyException("posting", posting.Id, expected);
        }
    }

    private static string Normalise(string? code) =>
        code?.Trim().ToUpperInvariant() ?? string.Empty;
}
