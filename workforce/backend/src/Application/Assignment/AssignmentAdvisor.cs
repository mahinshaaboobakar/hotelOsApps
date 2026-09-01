using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Assignment;

/// <summary>
/// What a manager should know before putting somebody on a shift.
/// </summary>
/// <remarks>
/// <para>
/// <b>Where the capability register meets the rota</b> — and it consumes what the
/// earlier slices produced rather than adding anything: postings say which
/// department somebody belongs to, the capability register says what has lapsed,
/// leave says who is booked off, and the rota says who is already spoken for.
/// </para>
/// <para>
/// <b>It advises; it never refuses.</b> Nothing here is wired into
/// <c>RotaService</c>, and that is deliberate: a manager covering a sick shift at
/// six in the morning is not helped by a validator. <c>WF-Q16</c> — the platform
/// refuses the physically impossible and warns on a judgment.
/// </para>
/// <para>
/// <b>This is the inward half of slice 7.</b> The outward half — the capability
/// read-view another application asks <i>"who can do X"</i> through — is
/// <b>not built</b>, deliberately: chapter 01 §7 says it <i>"ships when that
/// application can ask"</i>, Jobs has not shipped, and <c>WF-Q6</c> routes the
/// answer through the Context Service and stands recorded as an architect call
/// still open. Designing a cross-service read-view without its consumer is how
/// the shape comes out wrong.
/// </para>
/// </remarks>
public class AssignmentAdvisor(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer)
{
    /// <summary>How far ahead an expiring certification is worth mentioning.</summary>
    /// <remarks>
    /// The same sixty days the Attention list uses. Two horizons that drifted
    /// would put a certificate in one screen's warning and not the other's, and a
    /// manager would reasonably conclude one of them was broken.
    /// </remarks>
    private const int ExpiringWithinDays = 60;

    /// <summary>What is worth saying about putting this person on this day.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="staffId">Who is being considered.</param>
    /// <param name="date">Which day.</param>
    /// <param name="departmentCode">Which department the cell is for.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>
    /// Everything worth knowing. <b>Empty means nothing to say</b> — not that the
    /// assignment was checked and approved, because nothing here approves
    /// anything.
    /// </returns>
    public async Task<IReadOnlyList<Advice>> AdviseAsync(
        RequestScope scope,
        Guid staffId,
        DateOnly date,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var code = departmentCode.Trim().ToUpperInvariant();
        var advice = new List<Advice>();

        await AddLeaveAsync(scope, staffId, date, advice, cancellationToken);
        await AddPostingAsync(scope, staffId, date, code, advice, cancellationToken);
        await AddRosterAsync(scope, staffId, date, advice, cancellationToken);
        await AddCapabilitiesAsync(scope, staffId, date, advice, cancellationToken);

        return advice;
    }

    /// <summary>Leave first, because it is usually the decisive one.</summary>
    /// <remarks>
    /// A requested-but-undecided leave is mentioned too, and separately:
    /// rostering somebody whose request is still open is not wrong, but the
    /// approver should know the rota now assumes an answer.
    /// </remarks>
    private async Task AddLeaveAsync(
        RequestScope scope,
        Guid staffId,
        DateOnly date,
        List<Advice> advice,
        CancellationToken cancellationToken)
    {
        var covering = await db.LeaveRequests
            .Where(r => r.PropertyId == scope.PropertyId
                        && r.StaffId == staffId
                        && r.From <= date
                        && date <= r.To
                        && (r.State == LeaveRequestState.Approved
                            || r.State == LeaveRequestState.Requested))
            .ToListAsync(cancellationToken);

        foreach (var request in covering)
        {
            advice.Add(new Advice(
                request.State == LeaveRequestState.Approved
                    ? AdviceKind.OnApprovedLeave
                    : AdviceKind.LeaveRequested,
                $"leave {request.From:dd MMM}-{request.To:dd MMM}"));
        }
    }

    /// <summary>Covering outside your own department is ordinary, and worth saying.</summary>
    private async Task AddPostingAsync(
        RequestScope scope,
        Guid staffId,
        DateOnly date,
        string code,
        List<Advice> advice,
        CancellationToken cancellationToken)
    {
        var posted = await db.Postings.AnyAsync(
            p => p.PropertyId == scope.PropertyId
                 && p.StaffId == staffId
                 && p.DepartmentCode == code
                 && p.EffectiveFrom <= date
                 && (p.EffectiveTo == null || p.EffectiveTo >= date),
            cancellationToken);

        if (!posted)
        {
            // Not an error. Front office covers a banquet on a busy Saturday, and
            // a system that refused it would be worked around by Monday.
            advice.Add(new Advice(
                AdviceKind.NotPostedToDepartment, $"not posted to {code} on this date"));
        }
    }

    private async Task AddRosterAsync(
        RequestScope scope,
        Guid staffId,
        DateOnly date,
        List<Advice> advice,
        CancellationToken cancellationToken)
    {
        var existing = await db.ShiftAssignments
            .Where(a => a.PropertyId == scope.PropertyId
                        && a.StaffId == staffId
                        && a.Date == date)
            .Select(a => a.DepartmentCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            // The rota replaces rather than refuses, so this is what tells a
            // manager the cell they are about to fill is not empty.
            advice.Add(new Advice(
                AdviceKind.AlreadyRostered, $"already rostered that day in {existing}"));
        }
    }

    /// <summary>What has lapsed, and what is about to.</summary>
    /// <remarks>
    /// <b>Everything they hold, not a required-capability match.</b> No shift
    /// declares what it needs — nothing in the platform says a night shift
    /// requires a fire warden — and inventing that vocabulary here would be a
    /// taxonomy built before a consumer, the mistake this round has already named
    /// three times. What the register can honestly say is <i>this person's
    /// certification has lapsed</i>; the manager knows what the shift needs.
    /// </remarks>
    private async Task AddCapabilitiesAsync(
        RequestScope scope,
        Guid staffId,
        DateOnly date,
        List<Advice> advice,
        CancellationToken cancellationToken)
    {
        var horizon = date.AddDays(ExpiringWithinDays);

        var dated = await db.Capabilities
            .Where(c => c.PropertyId == scope.PropertyId
                        && c.StaffId == staffId
                        && c.ValidUntil != null
                        && c.ValidUntil <= horizon)
            .OrderBy(c => c.ValidUntil)
            .ToListAsync(cancellationToken);

        foreach (var capability in dated)
        {
            // Measured against the DAY BEING FILLED, never against today. A
            // certificate valid now and expired by the shift is the case a
            // manager most needs to see, and checking against today would hide
            // exactly that one.
            var expired = capability.ValidUntil < date;

            advice.Add(new Advice(
                expired ? AdviceKind.CertificationExpired : AdviceKind.CertificationExpiring,
                $"{capability.Name} {(expired ? "expired" : "expires")} "
                + $"{capability.ValidUntil:dd MMM yyyy}"));
        }
    }
}
