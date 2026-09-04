using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Settings;

/// <summary>
/// The rest of what a property configures — <c>job.configure</c>, settings frames
/// 3–5: who is told (subscriptions), holds, and closing and rating.
/// </summary>
public class ClosingHoldService(JobsDbContext db, IKernelAuthorizer authorizer)
{
    /// <summary>Replace the property's subscriptions whole — the table is the screen.</summary>
    public async Task<IReadOnlyList<ConcernSubscription>> SaveSubscriptionsAsync(
        RequestScope scope, IReadOnlyList<SubscriptionCommand> commands, CancellationToken cancellationToken)
    {
        await ConfigurerAsync(scope, cancellationToken);
        foreach (var command in commands)
        {
            if (!LadderRole.All.Contains(command.Role)) throw new InvalidRequestException($"role {command.Role} is not known");
            if (!Concern.All.Contains(command.Concern) && command.Concern != "NOT_TRIAGED")
            {
                throw new InvalidRequestException($"concern {command.Concern} is not known");
            }
        }

        db.Subscriptions.RemoveRange(await db.Subscriptions.Where(s => s.PropertyId == scope.PropertyId).ToListAsync(cancellationToken));
        var rows = commands.Select(c => new ConcernSubscription
        {
            Id = Uuid7.NewUuid7(), PropertyId = scope.PropertyId, Role = c.Role, Concern = c.Concern,
            OnlyPriority = c.OnlyPriority, DepartmentCode = c.DepartmentCode?.Trim().ToUpperInvariant(),
            RepeatMinutes = c.RepeatMinutes,
        }).ToList();
        db.Subscriptions.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);
        return rows;
    }

    public async Task<ClosingPolicy> SaveClosingAsync(RequestScope scope, ClosingCommand command, CancellationToken cancellationToken)
    {
        await ConfigurerAsync(scope, cancellationToken);
        if (command.AutoCloseHours < 0) throw new InvalidRequestException("auto_close_hours cannot be negative");

        var code = command.DepartmentCode?.Trim().ToUpperInvariant();
        var policy = await db.ClosingPolicies.FirstOrDefaultAsync(
            p => p.PropertyId == scope.PropertyId && p.DepartmentCode == code, cancellationToken);
        if (policy is null)
        {
            policy = new ClosingPolicy { Id = Uuid7.NewUuid7(), PropertyId = scope.PropertyId, DepartmentCode = code };
            db.ClosingPolicies.Add(policy);
        }

        policy.AutoCloseHours = command.AutoCloseHours;
        policy.RatingOnClose = command.RatingOnClose;
        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<HoldPolicy> SaveHoldAsync(RequestScope scope, HoldPolicyCommand command, CancellationToken cancellationToken)
    {
        await ConfigurerAsync(scope, cancellationToken);
        if (!LadderRole.All.Contains(command.WarnRole)) throw new InvalidRequestException($"role {command.WarnRole} is not known");
        if (command.MaxHoldDays < 1 || command.WarnDaysBefore < 0) throw new InvalidRequestException("hold days must be sensible");

        var policy = await db.HoldPolicies.FirstOrDefaultAsync(p => p.PropertyId == scope.PropertyId, cancellationToken);
        if (policy is null)
        {
            policy = new HoldPolicy { Id = Uuid7.NewUuid7(), PropertyId = scope.PropertyId };
            db.HoldPolicies.Add(policy);
        }

        policy.MaxHoldDays = command.MaxHoldDays;
        policy.WarnDaysBefore = command.WarnDaysBefore;
        policy.WarnRole = command.WarnRole;
        policy.WarnAssigneeOnDay = command.WarnAssigneeOnDay;
        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    private Task ConfigurerAsync(RequestScope scope, CancellationToken cancellationToken) =>
        authorizer.RequireAsync(scope, Permissions.Configure, "property", scope.PropertyId, cancellationToken);
}
