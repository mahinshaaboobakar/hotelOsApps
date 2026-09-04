using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Settings;

/// <summary>
/// Creating and editing a concern policy — <c>job.configure</c>, settings frames
/// 7–10: name and scope, the clock per priority, the ladder per priority. One
/// policy per scope; saving replaces the clock and ladder whole.
/// </summary>
public class ConcernPolicyService(JobsDbContext db, IKernelAuthorizer authorizer, TimeProvider clock)
{
    public async Task<ConcernPolicy> SaveAsync(RequestScope scope, ConcernPolicyCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Configure, "property", scope.PropertyId, cancellationToken);
        Validate(command);

        var now = clock.GetUtcNow();
        var policy = command.Id is { } id
            ? await db.ConcernPolicies.FirstOrDefaultAsync(p => p.Id == id && p.PropertyId == scope.PropertyId, cancellationToken)
              ?? throw new NotFoundException("concern_policy", id)
            : new ConcernPolicy { Id = Uuid7.NewUuid7(), PropertyId = scope.PropertyId, CreatedAt = now };
        if (command.Id is not null && policy.Version != command.ExpectedVersion)
        {
            throw new ConcurrencyException("concern_policy", policy.Id, command.ExpectedVersion ?? 0);
        }

        var department = command.DepartmentCode?.Trim().ToUpperInvariant();
        var taken = await db.ConcernPolicies.AnyAsync(
            p => p.PropertyId == scope.PropertyId && p.Id != policy.Id && p.DeletedAt == null
                 && p.DepartmentCode == department && p.CategoryId == command.CategoryId && p.ItemId == command.ItemId,
            cancellationToken);
        if (taken) throw new InvalidRequestException("a policy for that scope already exists; open it instead");

        policy.Name = command.Name.Trim();
        policy.DepartmentCode = department;
        policy.CategoryId = command.CategoryId;
        policy.ItemId = command.ItemId;
        policy.UntriagedStuckMinutes = command.UntriagedStuckMinutes;
        policy.UpdatedAt = now;
        policy.Version += 1;
        if (command.Id is null) db.ConcernPolicies.Add(policy);

        await ReplaceClockAsync(policy, command, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    /// <summary>Refused while any item policy still points at it; otherwise soft-deleted (ADR 0062 vocabulary).</summary>
    public async Task DeleteAsync(RequestScope scope, Guid policyId, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Configure, "property", scope.PropertyId, cancellationToken);
        var policy = await db.ConcernPolicies.FirstOrDefaultAsync(p => p.Id == policyId && p.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("concern_policy", policyId);
        if (await db.ItemPolicies.AnyAsync(i => i.ConcernPolicyId == policyId, cancellationToken))
        {
            throw new InUseException("concern_policy", policyId, "property_item_policy");
        }

        policy.DeletedAt = clock.GetUtcNow();
        policy.Version += 1;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ReplaceClockAsync(ConcernPolicy policy, ConcernPolicyCommand command, CancellationToken cancellationToken)
    {
        db.ConcernRules.RemoveRange(await db.ConcernRules.Where(r => r.PolicyId == policy.Id).ToListAsync(cancellationToken));
        db.LadderSteps.RemoveRange(await db.LadderSteps.Where(s => s.PolicyId == policy.Id).ToListAsync(cancellationToken));

        foreach (var rule in command.Rules)
        {
            db.ConcernRules.Add(new ConcernPolicyRule
            {
                Id = Uuid7.NewUuid7(), PolicyId = policy.Id, Priority = rule.Priority,
                DueWithinMinutes = rule.DueWithinMinutes, AtRiskPercent = rule.AtRiskPercent,
                NotAcceptedMinutes = rule.NotAcceptedMinutes, NoSessionMinutes = rule.NoSessionMinutes,
                ManagerAtRisk = rule.ManagerAtRisk, RunsOutsidePresence = rule.RunsOutsidePresence,
            });
        }

        foreach (var step in command.Ladder)
        {
            db.LadderSteps.Add(new ConcernLadderStep
            {
                Id = Uuid7.NewUuid7(), PolicyId = policy.Id, Priority = step.Priority, StepNo = step.StepNo,
                Role = step.Role, Trigger = step.Trigger, DelayMinutes = step.DelayMinutes,
            });
        }
    }

    private static void Validate(ConcernPolicyCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) throw new InvalidRequestException("name is required");
        if (command.CategoryId is not null && string.IsNullOrWhiteSpace(command.DepartmentCode))
        {
            throw new InvalidRequestException("a category policy needs its department");
        }

        if (command.ItemId is not null && command.CategoryId is null)
        {
            throw new InvalidRequestException("an item policy needs its category");
        }

        if (command.Rules.Select(r => r.Priority).Distinct().Count() != command.Rules.Count
            || command.Rules.Any(r => r.Priority is not (Priority.P1 or Priority.P2 or Priority.P3)))
        {
            throw new InvalidRequestException("one rule per priority, P1 to P3");
        }

        if (command.Rules.Any(r => r.AtRiskPercent is < 1 or > 99))
        {
            throw new InvalidRequestException("at_risk_percent is 1 to 99");
        }

        if (command.Ladder.Any(s => !LadderRole.All.Contains(s.Role) || s.Trigger is not (Concern.AtRisk or Concern.Breached) || s.DelayMinutes < 0))
        {
            throw new InvalidRequestException("a ladder step is a role, AT_RISK or BREACHED, and a non-negative delay");
        }

        if (command.Ladder.GroupBy(s => (s.Priority, s.StepNo)).Any(g => g.Count() > 1))
        {
            throw new InvalidRequestException("ladder steps are numbered once per priority");
        }
    }
}
