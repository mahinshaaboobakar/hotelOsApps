using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Rota;

/// <summary>
/// The property's workforce policy — one row, created when first written.
/// </summary>
/// <remarks>
/// Slice 3b sets the overtime threshold; slice 4 adds the leave policy to the
/// same row and the same screen. It lives beside the rota because the threshold
/// exists to serve the planning warning, and moving it later would separate a
/// setting from the only thing that reads it.
/// </remarks>
public class PolicyService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    TimeProvider clock)
{
    /// <summary>Set or clear the overtime thresholds.</summary>
    public async Task<WorkforcePolicy> SetOvertimeAsync(
        RequestScope scope, SetOvertimeThresholdCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PolicyManage, "property", scope.PropertyId, cancellationToken);

        Refuse(command.DailyHours, "daily_hours", 24m);
        Refuse(command.WeeklyHours, "weekly_hours", 24m * 7m);

        var now = clock.GetUtcNow();
        var policy = await db.Policies.FirstOrDefaultAsync(
            p => p.PropertyId == scope.PropertyId, cancellationToken);

        if (policy is null)
        {
            // Created on first write rather than at install: a property that has
            // never configured a policy has none, and a row of nulls waiting for
            // one says the same thing less clearly.
            policy = new WorkforcePolicy
            {
                PropertyId = scope.PropertyId,
                CreatedAt = now,
                Version = 0,
            };

            db.Policies.Add(policy);
        }

        policy.OvertimeDailyHours = command.DailyHours;
        policy.OvertimeWeeklyHours = command.WeeklyHours;
        policy.UpdatedAt = now;
        policy.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    /// <summary>The property's policy, or null when it has never set one.</summary>
    /// <remarks>
    /// Null rather than a policy of nulls: <i>"this property has not configured
    /// workforce policy"</i> and <i>"it configured one and left the thresholds
    /// empty"</i> are different answers, and a screen showing the second when the
    /// first is true would claim somebody had made a decision they had not.
    /// </remarks>
    public async Task<WorkforcePolicy?> GetAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        return await db.Policies.FirstOrDefaultAsync(
            p => p.PropertyId == scope.PropertyId, cancellationToken);
    }

    /// <summary>A threshold is a positive number of hours, or absent.</summary>
    /// <remarks>
    /// Zero and negative are refused rather than warned: a threshold of zero
    /// flags every shift ever worked, which is a record that cannot be meant.
    /// The upper bound is the length of the period itself — a daily threshold
    /// above 24 hours can never be crossed, so it is a setting that silently
    /// does nothing.
    /// </remarks>
    private static void Refuse(decimal? hours, string field, decimal ceiling)
    {
        if (hours is null)
        {
            return;
        }

        if (hours <= 0m)
        {
            throw new InvalidRequestException(
                $"{field} is a positive number of hours, or absent to set no threshold");
        }

        if (hours > ceiling)
        {
            throw new InvalidRequestException(
                $"{field} cannot exceed {ceiling} — a threshold that can never be crossed "
                + "is a setting that does nothing");
        }
    }
}
