using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;

namespace HotelOS.Jobs.Application.Concerns;

/// <summary>
/// The rule that turns a job's facts into a concern state and a ladder step —
/// S5 D1–D2, design §6. Pure: no clock of its own, no database, so it can be
/// characterised to the minute. The sweep feeds it and writes what it says.
/// </summary>
public static class ConcernEvaluator
{
    /// <summary>What the sweep needs to know about one job at one instant.</summary>
    public sealed record Facts(
        string Status,
        string Priority,
        DateTimeOffset RaisedAt,
        DateTimeOffset? DueAt,
        DateTimeOffset? AssignedAt,
        DateTimeOffset? AcceptedAt,
        bool HasSession,
        DateTimeOffset? HoldUntil,
        bool ParentOpen,
        bool DepartmentPresent);

    /// <summary>The evaluator's answer.</summary>
    public sealed record Verdict(string Concern, int LadderStep, string Role, string Reason);

    private static readonly Verdict Quiet = new(Domain.Concern.OnTrack, 0, LadderRole.Assignee, "on track");

    /// <summary>Evaluate at <paramref name="now"/> under the policy's rule and ladder for the job's priority.</summary>
    public static Verdict Evaluate(
        Facts facts, ConcernPolicyRule? rule, IReadOnlyList<ConcernLadderStep> ladder,
        int untriagedStuckMinutes, DateTimeOffset now)
    {
        if (facts.ParentOpen) return new Verdict(Domain.Concern.OnTrack, 0, LadderRole.Assignee, "blocked behind parent; clock stopped");
        if (facts.Status == JobStatus.OnHold) return OnHold(facts, now);
        if (facts.Priority == Priority.NotTriaged) return Untriaged(facts, untriagedStuckMinutes, now);
        if (rule is null) return Quiet;
        if (!facts.DepartmentPresent && !rule.RunsOutsidePresence)
        {
            return new Verdict(Domain.Concern.OnTrack, 0, LadderRole.Assignee, "department not present; clock paused");
        }

        if (Stuck(facts, rule, now) is { } stuck) return stuck;

        var state = Domain.Concern.OnTrack;
        var reason = "on track";
        DateTimeOffset? since = null;
        if (facts.DueAt is { } due)
        {
            var window = due - facts.RaisedAt;
            var atRisk = facts.RaisedAt + window * (rule.AtRiskPercent / 100.0);
            if (now >= due) { state = Domain.Concern.Breached; reason = $"{(int)(now - due).TotalMinutes} min over due"; since = due; }
            else if (now >= atRisk) { state = Domain.Concern.AtRisk; reason = $"{rule.AtRiskPercent} % of {(int)window.TotalMinutes} min"; since = atRisk; }
        }

        if (state == Domain.Concern.OnTrack) return Quiet;

        var (step, role) = Climb(ladder, facts.Priority, state, since!.Value, facts.DueAt!.Value, rule, now);
        return new Verdict(state, step, role, reason);
    }

    private static Verdict OnHold(Facts facts, DateTimeOffset now) =>
        facts.HoldUntil is { } until && now > until
            ? new Verdict(Domain.Concern.Stuck, 2, LadderRole.Supervisor, $"hold date {until:yyyy-MM-dd} passed")
            : new Verdict(Domain.Concern.OnTrack, 0, LadderRole.Assignee, "on hold; clock stopped");

    private static Verdict Untriaged(Facts facts, int minutes, DateTimeOffset now) =>
        now - facts.RaisedAt >= TimeSpan.FromMinutes(minutes)
            ? new Verdict(Domain.Concern.Stuck, 2, LadderRole.Supervisor, $"not triaged {minutes} min")
            : new Verdict(Domain.Concern.OnTrack, 0, LadderRole.Assignee, "not triaged; no clock");

    /// <summary>The two stuck tests: assigned and not accepted; accepted and never started.</summary>
    private static Verdict? Stuck(Facts facts, ConcernPolicyRule rule, DateTimeOffset now)
    {
        if (facts.Status == JobStatus.Assigned && facts.AcceptedAt is null
            && rule.NotAcceptedMinutes is { } accept && facts.AssignedAt is { } assigned
            && now - assigned >= TimeSpan.FromMinutes(accept))
        {
            return new Verdict(Domain.Concern.Stuck, 2, LadderRole.Supervisor, $"not accepted {accept} min");
        }

        if (facts.Status == JobStatus.Accepted && !facts.HasSession
            && rule.NoSessionMinutes is { } start && facts.AcceptedAt is { } accepted
            && now - accepted >= TimeSpan.FromMinutes(start))
        {
            return new Verdict(Domain.Concern.Stuck, 2, LadderRole.Supervisor, $"no work session {start} min");
        }

        return null;
    }

    /// <summary>Which rung has been reached: each rung triggers on a state, plus its delay; the highest reached holds it. Manager-at-risk lifts to rung 3 and never lets it fall back.</summary>
    private static (int Step, string Role) Climb(
        IReadOnlyList<ConcernLadderStep> ladder, string priority, string state,
        DateTimeOffset atRiskSince, DateTimeOffset due, ConcernPolicyRule rule, DateTimeOffset now)
    {
        var reached = (Step: 1, Role: LadderRole.Assignee);
        foreach (var rung in ladder.Where(s => s.Priority == priority).OrderBy(s => s.StepNo))
        {
            var from = rung.Trigger == Domain.Concern.AtRisk ? atRiskSince : due;
            var triggered = rung.Trigger == Domain.Concern.AtRisk || state == Domain.Concern.Breached;
            if (triggered && now >= from.AddMinutes(rung.DelayMinutes))
            {
                reached = (rung.StepNo, rung.Role);
            }
        }

        if (rule.ManagerAtRisk && reached.Step < 3)
        {
            reached = (3, LadderRole.Manager);
        }

        return reached;
    }
}
