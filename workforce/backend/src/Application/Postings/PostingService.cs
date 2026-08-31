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
/// <b>On the one outbound call this service makes.</b> <c>EVT-Q1</c> rules that
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

        db.Postings.Add(posting);

        // The identity link is resolved here, and deliberately not stored:
        // `masterdata.staff.user_id` is Master Data's and a copy of it can only
        // go stale. Null is the ordinary answer — most staff have no login.
        _ = await directory.FindUserIdAsync(scope.PropertyId, command.StaffId, cancellationToken);

        // ── The announcement is NOT made here, and that is a gate rather than
        //    an omission. See `docs/chapters/03-the-code-round-findings.md`.
        //
        //    Chapter 01 §4 rules the shape — `user.posted` on the `user`
        //    aggregate, the Kernel materialising
        //    `department:{id}#posted@user:{uid}`. Three things it needs do not
        //    exist, all verified in the platform tree on 2026-08-31:
        //
        //    * no consumer maps it. `grants::find` knows four kinds and this is
        //      not one; `user` is deliberately not a registrable TYPE. A
        //      `user.posted` published today maps to `None` and writes no
        //      tuple — silently, which looks exactly like working;
        //    * the announcement has no version it may carry. The platform's
        //      pattern bumps the aggregate row's own version
        //      (`staff.assigned` increments `staff.Version`), and this
        //      application has no user row to bump; a per-posting version
        //      collides on the second posting for one person, against
        //      `uq_events__aggregate_version`;
        //    * and the tuple addresses `department:{uuid}` while a posting
        //      carries the canon code, so the payload must carry both.
        //
        //    Publishing anyway would either violate a unique constraint or
        //    announce into a consumer that drops it. Neither is a thing to
        //    discover later, and inventing a mechanism here would be deciding
        //    a Kernel question inside an application.

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

        if (command.IsDepartmentHead is { } head)
        {
            // Chapter 01 §4 says a department-head posting writes
            // `department#manager`. Nothing writes it yet, for the same reason
            // the posting announcement does not — the mechanism is unbuilt on
            // the Kernel side, and it is a register question rather than
            // something to improvise here. The flag is stored and is what the
            // approver resolution reads.
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

        // `user.posting_ended` belongs here, and is gated with its counterpart —
        // see CreateAsync. Both directions land together or neither does:
        // ADR 0087's addendum records what a one-directional writer produced,
        // *"a posting revoked left its tuple standing"*, which is the direction
        // ADR 0061's invariant forbids.

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
