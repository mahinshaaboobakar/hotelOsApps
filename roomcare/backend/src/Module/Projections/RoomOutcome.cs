using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Module.Views;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>"Outcome so far" — the one rule every screen uses to say where a room's day stands (frame 1b's column).</summary>
public static class RoomOutcome
{
    public static OutcomeView Of(
        DayFacts.Day day, RoomState? state, RoomTask? task, TaskAssignment? assignment, bool blocked, MarksView marks)
    {
        if (blocked)
        {
            return new OutcomeView("BLOCKED");
        }

        if (state is { HasDisagreement: true })
        {
            return new OutcomeView("DISAGREEMENT", At(state.DisagreementObservedAt), Detail: state.DisagreementObservedCondition);
        }

        if (state?.SupervisedSince is not null && task?.IsOpen == true && day.Lane.Any(l => l.RoomId == state.RoomId && l.IsOpen))
        {
            return new OutcomeView("SUPERVISION", Days: state.DaysWithoutService + 1);
        }

        if (task is null)
        {
            return marks.NewSince ? new OutcomeView("NEW_SINCE", At(day.LastRun?.At)) : new OutcomeView("NONE");
        }

        return task.Status switch
        {
            RoomTaskStatus.PendingPolicy => new OutcomeView("PENDING", Detail: task.DecisionInputs.Reason),
            RoomTaskStatus.Planned when assignment is null => new OutcomeView("NOBODY_AVAILABLE"),
            RoomTaskStatus.Planned or RoomTaskStatus.Assigned => Waiting(day, task),
            RoomTaskStatus.InProgress => new OutcomeView("IN_PROGRESS", At(day.Sessions.Where(s => s.TaskId == task.Id).Min(s => (DateTimeOffset?)s.StartedAt))),
            _ => Ended(day, task),
        };
    }

    private static OutcomeView Waiting(DayFacts.Day day, RoomTask task)
    {
        var last = day.LastAttempt(task);
        if (last?.Found == AttemptFound.Dnd)
        {
            return new OutcomeView("DND", At(last.At), At(last.At.AddMinutes(day.Policy.DndRecheckMinutes)));
        }

        return task.EarliestAt is { } earliest && earliest > day.Now.Instant
            ? new OutcomeView("WAITING", Until: At(earliest))
            : new OutcomeView("NONE");
    }

    private static OutcomeView Ended(DayFacts.Day day, RoomTask task)
    {
        var at = At(day.Attempts.Where(a => a.TaskId == task.Id).Max(a => (DateTimeOffset?)a.At) ?? task.UpdatedAt);
        var inspection = day.Inspections.FirstOrDefault(p => p.TaskId == task.Id);
        return task.Outcome switch
        {
            TaskOutcome.Done or TaskOutcome.SupervisorCleaned when inspection?.Status == PhaseStatus.Active => new OutcomeView("INSPECTION_REQUESTED", at),
            TaskOutcome.Done or TaskOutcome.SupervisorCleaned when inspection?.Status == PhaseStatus.Done => new OutcomeView("READY", At(inspection.EndedAt)),
            TaskOutcome.Done or TaskOutcome.SupervisorCleaned => new OutcomeView("DONE", at),
            TaskOutcome.Partial => new OutcomeView("PARTIAL", at, Detail: string.Join(", ", task.PartialDone.Select(p => p.ToLowerInvariant()))),
            TaskOutcome.Declined => new OutcomeView("DECLINED", at),
            _ => new OutcomeView("ENDED", at, Detail: task.Outcome),
        };
    }
}
