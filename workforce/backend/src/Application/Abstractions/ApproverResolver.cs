using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Abstractions;

/// <summary>
/// Who decides for this person — resolved from this application's own postings.
/// </summary>
/// <remarks>
/// <para>
/// <b>One rule, one queue</b>, ruled 2026-08-31: the reporting manager when the
/// posting names one, the department head otherwise. Chapter 01 said <i>"the
/// reporting manager or department head"</i> with no precedence, which is two
/// queues and no way to say which one a request is in.
/// </para>
/// <para>
/// <b>Extracted when the second consumer arrived</b>, not before. Leave and swap
/// proposals resolve the same question, and CLAUDE.md's rule is exactly this
/// shape: <i>"if you are about to write it in a second service, it belongs in a
/// package"</i> — here, in one class both use. Two copies of an approver rule
/// drift, and the day they disagree a request sits in a queue nobody is watching.
/// </para>
/// <para>
/// <b>This is why Workforce can answer the question at all.</b> ADR 0116 §6
/// makes department membership derive from Workforce postings, permanently — so
/// the application that owns the posting is the only one that can say whose
/// request this is.
/// </para>
/// </remarks>
public class ApproverResolver(WorkforceDbContext db)
{
    /// <summary>The staff member who decides for <paramref name="staffId"/>.</summary>
    /// <param name="propertyId">The property.</param>
    /// <param name="staffId">Whose request.</param>
    /// <param name="on">The day to resolve postings as of.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>
    /// The approver, or <c>null</c> when there is none to name — an unposted
    /// person, or a department head, whose own requests go a rung up.
    /// </returns>
    public async Task<Guid?> ResolveAsync(
        Guid propertyId, Guid staffId, DateOnly on, CancellationToken cancellationToken)
    {
        var posting = await db.Postings
            .Where(p => p.PropertyId == propertyId
                        && p.StaffId == staffId
                        && p.EffectiveFrom <= on
                        && (p.EffectiveTo == null || p.EffectiveTo >= on))
            .OrderByDescending(p => p.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        if (posting is null)
        {
            return null;
        }

        if (posting.ReportingManagerStaffId is { } manager)
        {
            return manager;
        }

        // The department head, found the way every question about a department is
        // answered here — through a posting.
        var head = await db.Postings
            .Where(p => p.PropertyId == propertyId
                        && p.DepartmentCode == posting.DepartmentCode
                        && p.IsDepartmentHead
                        && p.StaffId != staffId
                        && (p.EffectiveTo == null || p.EffectiveTo >= on))
            .FirstOrDefaultAsync(cancellationToken);

        // INTERIM — a department head's own request resolves to null.
        //
        // The precedence ruling names the **general manager** for this rung, and
        // `general_manager` is one of ADR 0114 §5's two unwritten Workforce-era
        // hooks: nothing writes it, so there is no holder to name. An unassigned
        // queue somebody can see beats an invented one.
        //
        // **Resolves to the general manager when the access model writes the
        // role; removed in that change.**
        return head?.StaffId;
    }
}
