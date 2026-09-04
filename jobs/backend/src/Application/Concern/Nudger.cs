using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Concerns;

/// <summary>
/// Who is told — S5 D3, S9 D10: for a verdict, the subscriptions that match it
/// name the roles; the roles resolve to today's people; each gets an in-app
/// nudge row, repeated at the subscription's interval while the state lasts.
/// Nothing here chooses a channel, because there is none.
/// </summary>
public class Nudger(JobsDbContext db, IPropertyDirectory directory)
{
    public async Task NudgeAsync(
        Job job, ConcernEvaluator.Verdict verdict, JobAssignment? assignment,
        JobConcernHistory? last, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (verdict.Concern == Domain.Concern.OnTrack) return;

        var subscriptions = await db.Subscriptions
            .Where(s => s.PropertyId == job.PropertyId && s.Concern == verdict.Concern)
            .Where(s => s.DepartmentCode == null || s.DepartmentCode == job.DepartmentCode)
            .Where(s => s.OnlyPriority == null || s.OnlyPriority == job.Priority)
            .ToListAsync(cancellationToken);
        if (subscriptions.Count == 0) return;

        var fresh = last is null || last.Concern != verdict.Concern;
        foreach (var subscription in subscriptions)
        {
            foreach (var user in await RecipientsAsync(job, subscription, assignment, cancellationToken))
            {
                if (fresh || await DueAgainAsync(job, user, verdict.Concern, subscription, now, cancellationToken))
                {
                    db.Nudges.Add(new JobNudge
                    {
                        Id = Guid.CreateVersion7(), JobId = job.Id, PropertyId = job.PropertyId, ToUserId = user,
                        Concern = verdict.Concern, AsRole = subscription.Role, SentAt = now,
                    });
                }
            }
        }
    }

    private async Task<IReadOnlyList<Guid>> RecipientsAsync(
        Job job, ConcernSubscription subscription, JobAssignment? assignment, CancellationToken cancellationToken)
    {
        if (subscription.Role == LadderRole.Assignee)
        {
            return assignment?.AssigneeUserId is { } assignee ? [assignee] : [];
        }

        return await directory.ResolveRoleAsync(job.PropertyId, job.DepartmentCode, subscription.Role, cancellationToken);
    }

    /// <summary>A repeat is due when the last nudge to this person for this state is older than the interval.</summary>
    private async Task<bool> DueAgainAsync(
        Job job, Guid user, string concern, ConcernSubscription subscription, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (subscription.RepeatMinutes is not { } every) return false;

        var lastSent = await db.Nudges
            .Where(n => n.JobId == job.Id && n.ToUserId == user && n.Concern == concern)
            .MaxAsync(n => (DateTimeOffset?)n.SentAt, cancellationToken);
        return lastSent is null || now - lastSent.Value >= TimeSpan.FromMinutes(every);
    }
}
