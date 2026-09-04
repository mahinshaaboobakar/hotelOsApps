using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Catalogue;

/// <summary>
/// A property's take on the catalogue — <c>job.configure</c>, S1 D12, frame 7's
/// property tab: activate or switch off an item here, rename it, override its
/// priority, promise and policy, and say what AUTO assigns to.
/// </summary>
public class PropertyCatalogueService(JobsDbContext db, IKernelAuthorizer authorizer, TimeProvider clock)
{
    public async Task<PropertyItemPolicy> SaveAsync(RequestScope scope, ItemPolicyCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Configure, "property", scope.PropertyId, cancellationToken);
        Validate(command);
        _ = await db.Items.FirstOrDefaultAsync(i => i.Id == command.ItemId && i.DeletedAt == null, cancellationToken)
            ?? throw new InvalidRequestException("item_id is not a catalogue item");
        if (command.ConcernPolicyId is { } policyId
            && !await db.ConcernPolicies.AnyAsync(p => p.Id == policyId && p.PropertyId == scope.PropertyId, cancellationToken))
        {
            throw new InvalidRequestException("concern_policy_id is not a policy of this property");
        }

        var policy = await db.ItemPolicies.FirstOrDefaultAsync(
            p => p.PropertyId == scope.PropertyId && p.ItemId == command.ItemId, cancellationToken);
        if (policy is null)
        {
            policy = new PropertyItemPolicy { Id = Guid.CreateVersion7(), PropertyId = scope.PropertyId, ItemId = command.ItemId };
            db.ItemPolicies.Add(policy);
        }

        policy.ActiveHere = command.ActiveHere;
        policy.DisplayName = command.DisplayName?.Trim();
        policy.DefaultPriority = command.DefaultPriority;
        policy.DueWithinMinutes = command.DueWithinMinutes;
        policy.ConcernPolicyId = command.ConcernPolicyId;
        policy.AutoAssign = command.AutoAssign;
        policy.AutoAssignTeamId = command.AutoAssign == AutoAssignKind.Team ? command.AutoAssignTeamId : null;
        policy.UpdatedAt = clock.GetUtcNow();
        policy.Version += 1;
        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    private static void Validate(ItemPolicyCommand command)
    {
        if (command.DefaultPriority is { } p && p is not (Priority.P1 or Priority.P2 or Priority.P3))
        {
            throw new InvalidRequestException("default_priority must be P1, P2, P3 or empty");
        }

        if (command.AutoAssign is not (AutoAssignKind.User or AutoAssignKind.Team))
        {
            throw new InvalidRequestException("auto_assign must be USER or TEAM");
        }

        if (command.AutoAssign == AutoAssignKind.Team && command.AutoAssignTeamId is null)
        {
            throw new InvalidRequestException("auto_assign TEAM needs auto_assign_team_id");
        }

        if (command.DueWithinMinutes is <= 0) throw new InvalidRequestException("due_within_minutes must be positive");
    }
}
