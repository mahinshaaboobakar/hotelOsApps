using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>The Supervision lane — the rooms waiting on a supervisor, and those decided today (frame 5).</summary>
/// <remarks>
/// A standing disagreement is in the lane whether or not a lane row was opened
/// for it — it is read from the room, where it lives (S4). Paged at twelve like
/// every list; short by nature.
/// </remarks>
public sealed class LaneProjection(RoomCareDbContext db, IHouse house, PropertyClock clock, DayFacts facts)
{
    public const int PageSize = 12;

    public async Task<SupervisionView> LaneAsync(RequestScope scope, int page, CancellationToken cancellationToken)
    {
        var snapshot = await LoadAsync(scope.PropertyId, house, clock, db, cancellationToken);
        var day = await facts.LoadAsync(snapshot, scope.PropertyId, cancellationToken);
        var open = await db.Supervision.Where(s => s.PropertyId == scope.PropertyId && s.Decision == null).ToListAsync(cancellationToken);
        var decided = day.Lane.Where(s => s.Decision is not null).ToList();
        var disagreements = day.States.Values.Where(s => s.HasDisagreement).ToList();
        var deciders = await house.NamesAsync(decided.Select(d => d.DecidedByUserId!.Value).Distinct().ToList(), cancellationToken);

        var rows = new List<LaneRowView>();
        rows.AddRange(open.Select(s => Row(snapshot, day, s, null)));
        rows.AddRange(disagreements.Select(s => Disagreement(snapshot, day, s)));
        rows.AddRange(decided.Select(s => Row(snapshot, day, s, deciders.GetValueOrDefault(s.DecidedByUserId!.Value))));

        var size = PageSize;
        return new SupervisionView(
            open.Count + disagreements.Count,
            decided.Count,
            day.Now.Instant.ToString("o"),
            rows.Skip(Math.Max(0, page) * size).Take(size).ToList(),
            new Views.Paging(Math.Max(0, page), size, rows.Count));
    }

    private static LaneRowView Row(HouseSnapshot snapshot, DayFacts.Day day, RoomSupervision lane, string? decidedBy)
    {
        day.States.TryGetValue(lane.RoomId, out var state);
        var task = day.TaskOf(lane.RoomId);
        return new LaneRowView(
            lane.Id.ToString(), lane.RoomId.ToString(), snapshot.Number(lane.RoomId), state?.Version ?? 0,
            lane.Reason, lane.OpenedAt.ToString("o"), Known(day, lane, state, task),
            task?.Id.ToString(), task?.Version, lane.Decision, decidedBy, At(lane.DecidedAt), lane.Note);
    }

    private static LaneRowView Disagreement(HouseSnapshot snapshot, DayFacts.Day day, RoomState state) => new(
        null, state.RoomId.ToString(), snapshot.Number(state.RoomId), state.Version, SupervisionReason.Disagreement,
        state.DisagreementObservedAt!.Value.ToString("o"),
        $"ours {state.Condition.ToLowerInvariant()} ({state.ConditionSource.ToLowerInvariant()} {state.ConditionSetAt:o}) · "
        + $"{(state.DisagreementSource ?? ObservationSource.Pms).ToLowerInvariant()} {state.DisagreementObservedCondition!.ToLowerInvariant()} ({state.DisagreementObservedAt:o})",
        day.TaskOf(state.RoomId)?.Id.ToString(), day.TaskOf(state.RoomId)?.Version, null, null, null, null);

    private static string Known(DayFacts.Day day, RoomSupervision lane, RoomState? state, RoomTask? task) => lane.Reason switch
    {
        SupervisionReason.DaysWithoutService =>
            $"{state?.DaysWithoutService ?? 0} days without service · {(state?.Occupancy ?? Occupancy.Unknown).ToLowerInvariant()}"
            + (day.LastAttempt(task) is { } last ? $" · last at the door: {last.Found.ToLowerInvariant()} {last.At:o}" : string.Empty),
        SupervisionReason.NobodyAvailable =>
            $"{task?.Service.ToLowerInvariant().Replace('_', ' ') ?? "service"} · nobody posted to Housekeeping could take it"
            + (state?.NextSoldAt is { } sold ? $" · sold {sold:o}" : string.Empty),
        SupervisionReason.ArrivalBeforeWindow =>
            $"arrival {state?.NextSoldAt:o} · before the window opens" + (day.Window is null ? " · window closed" : string.Empty),
        _ => lane.Note ?? string.Empty,
    };
}
