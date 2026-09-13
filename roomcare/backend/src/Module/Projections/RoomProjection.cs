using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>A room's page — the read GuestOps will show the desk through Context, and the link's target (S5 c12).</summary>
public sealed class RoomProjection(RoomCareDbContext db, IHouse house, PropertyClock clock, DayFacts facts)
{
    private const int HistoryDays = 14;

    public async Task<RoomPageView> RoomAsync(RequestScope scope, Guid roomId, CancellationToken cancellationToken)
    {
        var snapshot = await LoadAsync(scope.PropertyId, house, clock, db, cancellationToken);
        if (!snapshot.ById.ContainsKey(roomId))
        {
            throw new NotFoundException("room", roomId);
        }

        var day = await facts.LoadAsync(snapshot, scope.PropertyId, cancellationToken);
        day.States.TryGetValue(roomId, out var state);
        var line = BoardProjection.Row(snapshot, day, roomId);
        var task = day.TaskOf(roomId);
        var today = await TimelineAsync(day, roomId, cancellationToken);
        var jobs = await JobsAsync(day, roomId, cancellationToken);

        return new RoomPageView(
            line,
            snapshot.TypeName(roomId),
            snapshot.ZoneOf(roomId).Name,
            Disagreement(state, day),
            await FactsAsync(snapshot, day, state, roomId, cancellationToken),
            today,
            task is null ? null : await DecisionAsync(day, task, cancellationToken),
            Inspection(day, task),
            jobs,
            await HistoryAsync(scope.PropertyId, roomId, day.Date, cancellationToken),
            day.Lane.FirstOrDefault(l => l.RoomId == roomId && l.IsOpen)?.Id.ToString(),
            day.Policy.WhoLeads);
    }

    private static DisagreementView? Disagreement(RoomState? state, DayFacts.Day day) =>
        state is { HasDisagreement: true }
            ? new DisagreementView(state.Condition, state.ConditionSource, day.Name(state.ConditionSetById), state.ConditionSetAt.ToString("o"),
                state.DisagreementObservedCondition!, state.DisagreementSource ?? ObservationSource.Pms, state.DisagreementObservedAt!.Value.ToString("o"))
            : null;

    private async Task<RoomFactsView> FactsAsync(
        HouseSnapshot snapshot, DayFacts.Day day, RoomState? state, Guid roomId, CancellationToken cancellationToken)
    {
        var plan = snapshot.ById.TryGetValue(roomId, out var room)
            ? await db.DeepCleanPlans.FirstOrDefaultAsync(p => p.PropertyId == day.Policy.PropertyId && p.RoomTypeId == room.RoomTypeId, cancellationToken)
            : null;
        var lastDeep = await db.DeepCleans.Where(d => d.RoomId == roomId && d.DoneOn != null).MaxAsync(d => d.DoneOn, cancellationToken);
        return new RoomFactsView(
            state?.Occupancy ?? Occupancy.Unknown,
            state?.StayStatuses ?? [],
            At(state?.NextSoldAt),
            state?.LinenLastChangedOn?.ToString("yyyy-MM-dd"),
            state?.LinenLastChangedOn?.AddDays(day.Policy.LinenEveryDays).ToString("yyyy-MM-dd"),
            plan is null ? null : (lastDeep?.AddMonths(plan.EveryMonths) ?? day.Date).ToString("yyyy-MM"),
            state?.DaysWithoutService ?? 0,
            state?.SupervisedSince?.ToString("yyyy-MM-dd"));
    }

    /// <summary>What an ending did — the linen, the minutes worked, and what the room became.</summary>
    private static string Ending(DayFacts.Day day, RoomTask task)
    {
        var minutes = day.Sessions.Where(s => s.TaskId == task.Id).Sum(s => s.Minutes);
        var linen = task.LinenDue switch { LinenDue.NotDue => "linen not due", LinenDue.Due => "linen due", _ => task.Outcome == TaskOutcome.Done ? "linen changed" : "linen was due to be changed" };
        var became = task.Outcome switch
        {
            TaskOutcome.Done => "room CLEAN, announced (room.cleaned)",
            TaskOutcome.Partial => $"partial ({string.Join(", ", task.PartialDone.Select(p => p.ToLowerInvariant()))})",
            _ => null,
        };
        return string.Join(" · ", new[] { linen, minutes > 0 ? $"{minutes} min" : null, became }.Where(x => x is not null));
    }

    private async Task<IReadOnlyList<TimelineEntryView>> TimelineAsync(DayFacts.Day day, Guid roomId, CancellationToken cancellationToken)
    {
        var taskIds = day.Tasks.Where(t => t.RoomId == roomId).Select(t => t.Id).ToList();
        var history = await db.History.Where(h => taskIds.Contains(h.TaskId)).ToListAsync(cancellationToken);
        var start = day.Now.InstantOf(day.Date, day.Now.Settings.Boundary);
        var seen = await db.Observations.Where(o => o.RoomId == roomId && o.RecordedAt >= start).ToListAsync(cancellationToken);
        var issues = await db.Issues.Where(i => i.RoomId == roomId && i.At >= start).ToListAsync(cancellationToken);
        var people = history.Where(h => h.ById is not null).Select(h => h.ById!.Value).Concat(seen.Where(o => o.ByUserId != null).Select(o => o.ByUserId!.Value)).Distinct().ToList();
        var names = await house.NamesAsync(people, cancellationToken);
        string? Name(Guid? id) => id is { } x && names.TryGetValue(x, out var n) ? n : day.Name(id);

        var service = day.Tasks.Where(t => t.RoomId == roomId).ToDictionary(t => t.Id, t => t.Service);
        return history.Select(h => h.ToStatus == RoomTaskStatus.Ended && day.Tasks.FirstOrDefault(t => t.Id == h.TaskId) is { Outcome: not null } ended
                ? new TimelineEntryView(h.At.ToUniversalTime().ToString("o"), "ENDED", ended.Outcome, Ending(day, ended), Name(h.ById))
                : new TimelineEntryView(h.At.ToUniversalTime().ToString("o"), h.Kind, h.ToStatus,
                string.Join(" · ", new[] { h.ToStatus is RoomTaskStatus.Planned or RoomTaskStatus.PendingPolicy && h.FromStatus is null ? service.GetValueOrDefault(h.TaskId)?.ToLowerInvariant().Replace('_', ' ') : null, h.Reason }.Where(x => !string.IsNullOrEmpty(x))),
                Name(h.ById)))
            .Concat(day.Attempts.Where(a => taskIds.Contains(a.TaskId) && a.Found is AttemptFound.Dnd or AttemptFound.Declined)
                .Select(a => new TimelineEntryView(a.At.ToUniversalTime().ToString("o"), "ATTEMPT", a.Found, a.Note ?? string.Empty, Name(a.ByUserId))))
            .Concat(seen.Select(o => new TimelineEntryView(o.RecordedAt.ToUniversalTime().ToString("o"), "OBSERVED", o.Outcome,
                $"{o.Source.ToLowerInvariant()}: {o.Condition?.ToLowerInvariant() ?? o.Occupancy?.ToLowerInvariant() ?? "—"}", Name(o.ByUserId))))
            .Concat(issues.Select(i => new TimelineEntryView(i.At.ToUniversalTime().ToString("o"), "ISSUE", null, i.Note, Name(i.ByUserId))))
            .OrderBy(e => e.At)
            .ToList();
    }

    private async Task<DecisionView> DecisionAsync(DayFacts.Day day, RoomTask task, CancellationToken cancellationToken)
    {
        var run = task.DecisionRunId is { } runId ? await db.PrepareRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken) : null;
        var runBy = run?.ById is { } by ? (await house.NamesAsync([by], cancellationToken)).GetValueOrDefault(by) : null;
        var inputs = task.DecisionInputs;
        return new DecisionView(inputs.Condition, inputs.Occupancy, inputs.StayStatuses, At(inputs.NextSoldAt), inputs.Window, inputs.RuleVersion,
            inputs.Reason, task.Service, task.MinutesExpected, day.PriorityOf(task), task.InspectionRule, task.DecidedBy, runBy);
    }

    private static InspectionCardView Inspection(DayFacts.Day day, RoomTask? task)
    {
        var phase = task is null ? null : day.Inspections.FirstOrDefault(p => p.TaskId == task.Id);
        return new InspectionCardView(false, task?.InspectionRule ?? InspectionRule.None, At(phase?.StartedAt), At(phase?.EndedAt),
            phase?.Status is PhaseStatus.Done or PhaseStatus.Failed ? phase.Status : null, phase?.Note);
    }

    private async Task<IReadOnlyList<JobTouchView>> JobsAsync(DayFacts.Day day, Guid roomId, CancellationToken cancellationToken)
    {
        var touches = await db.JobTouches.Where(t => t.RoomId == roomId && t.OperatingDay == day.Date).ToListAsync(cancellationToken);
        var start = day.Now.InstantOf(day.Date, day.Now.Settings.Boundary);
        var issues = await db.Issues.Where(i => i.RoomId == roomId && i.At >= start).ToListAsync(cancellationToken);
        var names = await house.NamesAsync(issues.Select(i => i.ByUserId).Distinct().ToList(), cancellationToken);
        return touches.Select(t => new JobTouchView(t.JobId.ToString(), t.JobNumber, t.ClosedAt.ToString("o"), t.Summary, "CLOSED", null))
            .Concat(issues.Where(i => i.JobId != null).Select(i => new JobTouchView(i.JobId!.Value.ToString(), null, i.At.ToString("o"), i.Note, "RAISED", names.GetValueOrDefault(i.ByUserId))))
            .OrderBy(j => j.At)
            .ToList();
    }

    private async Task<IReadOnlyList<HistoryDayView>> HistoryAsync(Guid propertyId, Guid roomId, DateOnly today, CancellationToken cancellationToken)
    {
        var from = today.AddDays(-HistoryDays);
        var tasks = await db.Tasks.Where(t => t.PropertyId == propertyId && t.RoomId == roomId && t.OperatingDay >= from && t.OperatingDay < today)
            .ToListAsync(cancellationToken);
        return tasks.GroupBy(t => t.OperatingDay).OrderByDescending(g => g.Key)
            .Select(g => new HistoryDayView(g.Key.ToString("yyyy-MM-dd"), g.Select(t => t.Service).ToList(), g.Select(t => t.Outcome).ToList()))
            .ToList();
    }
}
