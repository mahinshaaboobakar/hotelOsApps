using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Catalogue;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Policies;

/// <summary>
/// How a job finds its promise and its policy — design §2.3, settings frame 11:
/// <b>item → category → department → property</b>, the most specific that
/// exists wins. Done once when the job is raised and stamped on it; the sweep
/// reads the stamp, never the chain.
/// </summary>
public class JobPolicyResolver(JobsDbContext db)
{
    /// <summary>What the chain says for one catalogue item at one property.</summary>
    public async Task<ResolvedPolicy> ResolveAsync(
        Guid propertyId, Item item, CancellationToken cancellationToken)
    {
        var itemPolicy = await db.ItemPolicies
            .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.ItemId == item.Id, cancellationToken);
        var category = await db.Categories
            .FirstAsync(c => c.Id == item.CategoryId, cancellationToken);
        var policy = await MostSpecificAsync(propertyId, category, item, cancellationToken);

        var priority = itemPolicy?.DefaultPriority ?? item.DefaultPriority;
        var due = itemPolicy?.DueWithinMinutes
            ?? item.DueWithinMinutes
            ?? await RuleDueAsync(policy, priority, cancellationToken);

        return new ResolvedPolicy(
            Priority: priority,
            DueWithinMinutes: due,
            RestrictedByDefault: item.RestrictedByDefault,
            ActiveHere: itemPolicy?.ActiveHere ?? true,
            ConcernPolicyId: itemPolicy?.ConcernPolicyId ?? policy?.Id,
            DepartmentCode: category.DepartmentCode,
            AutoAssignTeamId: itemPolicy?.AutoAssign == AutoAssignKind.Team ? itemPolicy.AutoAssignTeamId : null);
    }

    /// <summary>The narrowest policy whose scope contains the item: item, category, department, then the property default.</summary>
    private async Task<ConcernPolicy?> MostSpecificAsync(
        Guid propertyId, Category category, Item item, CancellationToken cancellationToken)
    {
        var candidates = await db.ConcernPolicies
            .Where(p => p.PropertyId == propertyId && p.DeletedAt == null)
            .Where(p => p.DepartmentCode == null || p.DepartmentCode == category.DepartmentCode)
            .Where(p => p.CategoryId == null || p.CategoryId == category.Id)
            .Where(p => p.ItemId == null || p.ItemId == item.Id)
            .ToListAsync(cancellationToken);

        return candidates.OrderByDescending(p => p.Specificity).FirstOrDefault();
    }

    private async Task<int?> RuleDueAsync(ConcernPolicy? policy, string priority, CancellationToken cancellationToken)
    {
        if (policy is null) return null;

        var rule = await db.ConcernRules
            .FirstOrDefaultAsync(r => r.PolicyId == policy.Id && r.Priority == priority, cancellationToken);
        return rule?.DueWithinMinutes;
    }
}

/// <summary>The chain's answer for one item at one property.</summary>
/// <param name="Priority">The catalogue link of the priority chain, after the property's override.</param>
/// <param name="DueWithinMinutes">Null means "same shift".</param>
/// <param name="RestrictedByDefault">S8 D4.</param>
/// <param name="ActiveHere">Whether this property offers the item at all.</param>
/// <param name="ConcernPolicyId">The policy to stamp on the job.</param>
/// <param name="DepartmentCode">The category's department — the job's department.</param>
/// <param name="AutoAssignTeamId">The team AUTO assigns to, when the property said TEAM.</param>
public sealed record ResolvedPolicy(
    string Priority,
    int? DueWithinMinutes,
    bool RestrictedByDefault,
    bool ActiveHere,
    Guid? ConcernPolicyId,
    string DepartmentCode,
    Guid? AutoAssignTeamId);
