using HotelOS.Jobs.Application.Concerns;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>The evaluator to the minute — settings frame 9's worked example: P1 raised 13:30, due 14:10.</summary>
public class ConcernEvaluatorTests
{
    private static readonly DateTimeOffset Raised = new(2026, 9, 2, 13, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Due = Raised.AddMinutes(40);

    private static readonly ConcernPolicyRule P1 = new()
    {
        Priority = Priority.P1, DueWithinMinutes = 40, AtRiskPercent = 75, NotAcceptedMinutes = 8, NoSessionMinutes = 15,
        ManagerAtRisk = false, RunsOutsidePresence = true,
    };

    private static readonly IReadOnlyList<ConcernLadderStep> Ladder =
    [
        new() { Priority = Priority.P1, StepNo = 1, Role = LadderRole.Assignee, Trigger = Concern.AtRisk, DelayMinutes = 0 },
        new() { Priority = Priority.P1, StepNo = 2, Role = LadderRole.Supervisor, Trigger = Concern.Breached, DelayMinutes = 0 },
        new() { Priority = Priority.P1, StepNo = 3, Role = LadderRole.Manager, Trigger = Concern.Breached, DelayMinutes = 15 },
        new() { Priority = Priority.P1, StepNo = 4, Role = LadderRole.JobsManager, Trigger = Concern.Breached, DelayMinutes = 45 },
    ];

    private static ConcernEvaluator.Facts Working(DateTimeOffset accepted) => new(
        JobStatus.InProgress, Priority.P1, Raised, Due, Raised.AddMinutes(2), accepted, true, null, false, true);

    [Theory]
    [InlineData(13, 59, Concern.OnTrack, 0, LadderRole.Assignee)]
    [InlineData(14, 1, Concern.AtRisk, 1, LadderRole.Assignee)]
    [InlineData(14, 10, Concern.Breached, 2, LadderRole.Supervisor)]
    [InlineData(14, 24, Concern.Breached, 2, LadderRole.Supervisor)]
    [InlineData(14, 25, Concern.Breached, 3, LadderRole.Manager)]
    [InlineData(14, 55, Concern.Breached, 4, LadderRole.JobsManager)]
    public void The_clock_and_the_ladder_to_the_minute(int hour, int minute, string concern, int step, string role)
    {
        var now = new DateTimeOffset(2026, 9, 2, hour, minute, 0, TimeSpan.Zero);

        var verdict = ConcernEvaluator.Evaluate(Working(Raised.AddMinutes(16)), P1, Ladder, 15, now);

        Assert.Equal((concern, step, role), (verdict.Concern, verdict.LadderStep, verdict.Role));
    }

    [Fact]
    public void Manager_at_risk_lifts_the_manager_to_step_three_already_at_at_risk()
    {
        var rule = new ConcernPolicyRule { Priority = Priority.P1, DueWithinMinutes = 40, AtRiskPercent = 75, ManagerAtRisk = true, RunsOutsidePresence = true };

        var verdict = ConcernEvaluator.Evaluate(Working(Raised.AddMinutes(16)), rule, Ladder, 15, Raised.AddMinutes(31));

        Assert.Equal((Concern.AtRisk, 3, LadderRole.Manager), (verdict.Concern, verdict.LadderStep, verdict.Role));
    }

    [Fact]
    public void Stuck_when_assigned_and_not_accepted_in_time_goes_to_the_supervisor()
    {
        var facts = new ConcernEvaluator.Facts(JobStatus.Assigned, Priority.P1, Raised, Due, Raised.AddMinutes(2), null, false, null, false, true);

        Assert.Equal(Concern.OnTrack, ConcernEvaluator.Evaluate(facts, P1, Ladder, 15, Raised.AddMinutes(9)).Concern);
        var stuck = ConcernEvaluator.Evaluate(facts, P1, Ladder, 15, Raised.AddMinutes(10));
        Assert.Equal((Concern.Stuck, LadderRole.Supervisor), (stuck.Concern, stuck.Role));
        Assert.Contains("not accepted", stuck.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Stuck_when_accepted_and_never_started()
    {
        var facts = new ConcernEvaluator.Facts(JobStatus.Accepted, Priority.P1, Raised, Due, Raised.AddMinutes(2), Raised.AddMinutes(5), false, null, false, true);

        var stuck = ConcernEvaluator.Evaluate(facts, P1, Ladder, 15, Raised.AddMinutes(20));

        Assert.Equal(Concern.Stuck, stuck.Concern);
        Assert.Contains("no work session", stuck.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Untriaged_has_no_clock_until_the_untriaged_stuck_minutes_pass()
    {
        var facts = new ConcernEvaluator.Facts(JobStatus.Raised, Priority.NotTriaged, Raised, null, null, null, false, null, false, true);

        Assert.Equal(Concern.OnTrack, ConcernEvaluator.Evaluate(facts, null, [], 15, Raised.AddMinutes(14)).Concern);
        Assert.Equal((Concern.Stuck, LadderRole.Supervisor), ConcernEvaluator.Evaluate(facts, null, [], 15, Raised.AddMinutes(15)) is var v ? (v.Concern, v.Role) : default);
    }

    [Fact]
    public void On_hold_stops_the_clock_and_a_passed_hold_date_is_stuck()
    {
        var until = Raised.AddDays(2);
        var held = new ConcernEvaluator.Facts(JobStatus.OnHold, Priority.P1, Raised, Due, null, null, false, until, false, true);

        Assert.Equal(Concern.OnTrack, ConcernEvaluator.Evaluate(held, P1, Ladder, 15, Raised.AddHours(5)).Concern);
        Assert.Equal(Concern.Stuck, ConcernEvaluator.Evaluate(held, P1, Ladder, 15, until.AddMinutes(1)).Concern);
    }

    [Fact]
    public void A_blocked_step_and_an_absent_department_pause_the_clock_unless_the_rule_runs_outside_presence()
    {
        var blocked = Working(Raised.AddMinutes(5)) with { ParentOpen = true };
        Assert.Equal(Concern.OnTrack, ConcernEvaluator.Evaluate(blocked, P1, Ladder, 15, Due.AddHours(3)).Concern);

        var absent = Working(Raised.AddMinutes(5)) with { DepartmentPresent = false };
        Assert.Equal(Concern.Breached, ConcernEvaluator.Evaluate(absent, P1, Ladder, 15, Due.AddMinutes(1)).Concern);
        var pausing = new ConcernPolicyRule { Priority = Priority.P1, DueWithinMinutes = 40, AtRiskPercent = 75, RunsOutsidePresence = false };
        Assert.Equal(Concern.OnTrack, ConcernEvaluator.Evaluate(absent, pausing, Ladder, 15, Due.AddMinutes(1)).Concern);
    }
}
