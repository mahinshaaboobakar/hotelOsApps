using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Concerns;

/// <summary>
/// The 60-second sweep — S5 D1, design §6: one query over every open job at a
/// property, the evaluator's verdict for each, a <c>job_concern_history</c> row
/// only when the verdict changes, and the nudges the subscriptions ask for.
/// No per-job timers. The stamped policy is read; the chain is not.
/// </summary>
public class ConcernSweep(
    JobsDbContext db,
    IPropertyDirectory directory,
    Nudger nudger,
    JobAnnouncer announcer,
    TimeProvider clock)
{
    /// <summary>Sweep one property. Returns how many jobs changed state.</summary>
    public async Task<int> RunAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var scope = new RequestScope { Caller = CallerKind.Service, ServiceName = "jobs", PropertyId = propertyId };
        var jobs = await db.Jobs
            .Where(j => j.PropertyId == propertyId && j.DeletedAt == null)
            .Where(j => JobStatus.Open.Contains(j.JobStatus))
            .ToListAsync(cancellationToken);
        if (jobs.Count == 0) return 0;

        var policies = await PoliciesAsync(propertyId, cancellationToken);
        var presence = await db.Presence.Where(p => p.PropertyId == propertyId)
            .ToDictionaryAsync(p => p.DepartmentCode, cancellationToken);
        var ids = jobs.Select(j => j.Id).ToList();
        var assignments = await db.Assignments.Where(a => ids.Contains(a.JobId) && a.EndedAt == null)
            .ToDictionaryAsync(a => a.JobId, cancellationToken);
        var sessions = await db.WorkSessions.Where(s => ids.Contains(s.JobId)).Select(s => s.JobId).Distinct()
            .ToHashSetAsync(cancellationToken);
        var latest = await LatestConcernAsync(ids, cancellationToken);
        var parents = jobs.Where(j => j.ParentJobId is not null).Select(j => j.ParentJobId!.Value).Distinct().ToList();
        var openParents = await db.Jobs.Where(j => parents.Contains(j.Id) && j.JobStatus != JobStatus.Resolved
                && j.JobStatus != JobStatus.Closed && j.JobStatus != JobStatus.Cancelled)
            .Select(j => j.Id).ToHashSetAsync(cancellationToken);

        var changed = 0;
        foreach (var job in jobs)
        {
            var (rule, ladder, untriaged) = policies.For(job);
            var present = !presence.TryGetValue(job.DepartmentCode, out var p) || !p.Enabled || p.Staffed;
            assignments.TryGetValue(job.Id, out var assignment);
            var facts = new ConcernEvaluator.Facts(
                job.JobStatus, job.Priority, job.CreatedAt, job.DueAt, assignment?.AssignedAt,
                assignment?.AcceptedAt, sessions.Contains(job.Id), job.HoldUntil,
                job.ParentJobId is { } parent && openParents.Contains(parent), present);
            var verdict = ConcernEvaluator.Evaluate(facts, rule, ladder, untriaged, now);
            latest.TryGetValue(job.Id, out var last);

            if (last is null || last.Concern != verdict.Concern || last.LadderStep != verdict.LadderStep)
            {
                await RecordAsync(scope, job, verdict, assignment, now, cancellationToken);
                changed += 1;
            }

            await nudger.NudgeAsync(job, verdict, assignment, last, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return changed;
    }

    private async Task RecordAsync(
        RequestScope scope, Job job, ConcernEvaluator.Verdict verdict, JobAssignment? assignment,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        var accountable = verdict.Role == LadderRole.Assignee
            ? assignment?.AssigneeUserId
            : (await directory.ResolveRoleAsync(job.PropertyId, job.DepartmentCode, verdict.Role, cancellationToken))
                .FirstOrDefault();

        db.ConcernHistory.Add(new JobConcernHistory
        {
            Id = Uuid7.NewUuid7(), JobId = job.Id, PropertyId = job.PropertyId,
            Concern = verdict.Concern, AccountableRole = verdict.Role, LadderStep = verdict.LadderStep,
            AccountableUserId = accountable == Guid.Empty ? null : accountable, Since = now,
            Reason = verdict.Reason, ConcernPolicyId = job.ConcernPolicyId,
        });
        announcer.Announce(scope, job, EventTypes.JobConcernChanged, now, verdict.Concern);
    }

    private async Task<Dictionary<Guid, JobConcernHistory>> LatestConcernAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        var rows = await db.ConcernHistory.Where(c => ids.Contains(c.JobId)).ToListAsync(cancellationToken);
        return rows.GroupBy(c => c.JobId).ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Since).First());
    }

    private async Task<PolicySet> PoliciesAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var policies = await db.ConcernPolicies.Where(p => p.PropertyId == propertyId).ToListAsync(cancellationToken);
        var rules = await db.ConcernRules.Where(r => policies.Select(p => p.Id).Contains(r.PolicyId)).ToListAsync(cancellationToken);
        var steps = await db.LadderSteps.Where(s => policies.Select(p => p.Id).Contains(s.PolicyId)).ToListAsync(cancellationToken);
        return new PolicySet(policies, rules, steps);
    }

    /// <summary>The property's policies, indexed for the loop.</summary>
    private sealed class PolicySet(List<ConcernPolicy> policies, List<ConcernPolicyRule> rules, List<ConcernLadderStep> steps)
    {
        public (ConcernPolicyRule? Rule, IReadOnlyList<ConcernLadderStep> Ladder, int Untriaged) For(Job job)
        {
            var policy = policies.FirstOrDefault(p => p.Id == job.ConcernPolicyId);
            if (policy is null) return (null, [], 15);

            return (
                rules.FirstOrDefault(r => r.PolicyId == policy.Id && r.Priority == job.Priority),
                steps.Where(s => s.PolicyId == policy.Id).ToList(),
                policy.UntriagedStuckMinutes);
        }
    }
}
