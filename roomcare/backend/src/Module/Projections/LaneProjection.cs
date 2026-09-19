using HotelOS.Contracts.Common.V1;
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

        var rooms = open.Concat(decided).Select(s => s.RoomId).Distinct().ToList();
        var from = day.Date.AddDays(-7);
        var earlier = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId && t.RoomId != null && rooms.Contains(t.RoomId.Value) && t.OperatingDay >= from && t.OperatingDay < day.Date)
            .Select(t => new PastService(t.RoomId!.Value, t.OperatingDay, t.Outcome))
            .ToListAsync(cancellationToken);

        var rows = new List<LaneRowView>();
        rows.AddRange(open.Select(s => Row(snapshot, day, s, null, earlier)));
        rows.AddRange(disagreements.Select(s => Disagreement(snapshot, day, s)));
        rows.AddRange(decided.Select(s => Row(snapshot, day, s, deciders.GetValueOrDefault(s.DecidedByUserId!.Value), earlier)));

        var slice = HotelOS.Platform.Paging.Of(new PagedRequest { Page = page, PageSize = PageSize });
        return new SupervisionView(
            open.Count + disagreements.Count,
            decided.Count,
            day.Now.Instant.ToString("o"),
            rows.Skip(slice.Skip).Take(slice.PageSize).ToList(),
            new Views.Paging(slice.Page, slice.PageSize, rows.Count));
    }

    /// <summary>One earlier day's service on a room — what the lane tells the supervisor about the days before.</summary>
    private sealed record PastService(Guid RoomId, DateOnly Day, string? Outcome);

    private static LaneRowView Row(HouseSnapshot snapshot, DayFacts.Day day, RoomSupervision lane, string? decidedBy, IReadOnlyList<PastService> earlier)
    {
        day.States.TryGetValue(lane.RoomId, out var state);
        var task = day.TaskOf(lane.RoomId);
        var known = new List<KnownView>();
        if (lane.Reason == SupervisionReason.DaysWithoutService && state is not null)
        {
            // The days that made the count, oldest first — "declined at the door 03 Sep · DND both windows 04 Sep".
            foreach (var past in earlier.Where(p => p.RoomId == lane.RoomId).GroupBy(p => p.Day).OrderBy(g => g.Key).TakeLast(state.DaysWithoutService))
            {
                var outcomes = past.Select(p => p.Outcome).Distinct().ToList();
                var words = outcomes.Count == 1 && past.Count() > 1 ? $"{Word(outcomes[0])} both windows" : string.Join(", ", outcomes.Select(Word));
                known.Add(new KnownView(words, null, past.Key.ToString("yyyy-MM-dd")));
            }
        }

        known.AddRange(Known(day, lane, state, task));
        var sinceDay = lane.Reason == SupervisionReason.DaysWithoutService && state is not null
            ? day.Date.AddDays(-state.DaysWithoutService).ToString("yyyy-MM-dd")
            : null;
        return new LaneRowView(
            lane.Id.ToString(), lane.RoomId.ToString(), snapshot.Number(lane.RoomId), state?.Version ?? 0,
            lane.Reason, lane.OpenedAt.ToString("o"), sinceDay, known,
            lane.Reason == SupervisionReason.DaysWithoutService ? (state?.DaysWithoutService ?? 0) + 1 : null,
            task?.Id.ToString(), task?.Version, lane.Decision, decidedBy, At(lane.DecidedAt), lane.Note);
    }

    private static LaneRowView Disagreement(HouseSnapshot snapshot, DayFacts.Day day, RoomState state) => new(
        null, state.RoomId.ToString(), snapshot.Number(state.RoomId), state.Version, SupervisionReason.Disagreement,
        state.DisagreementObservedAt!.Value.ToString("o"),
        null,
        [
            new KnownView($"ours {state.Condition.ToLowerInvariant()} ({day.Name(state.ConditionSetById) ?? state.ConditionSource.ToLowerInvariant()})", At(state.ConditionSetAt)),
            new KnownView($"{(state.DisagreementSource ?? ObservationSource.Pms).ToLowerInvariant()} {state.DisagreementObservedCondition!.ToLowerInvariant()}", At(state.DisagreementObservedAt)),
        ],
        null, day.TaskOf(state.RoomId)?.Id.ToString(), day.TaskOf(state.RoomId)?.Version, null, null, null, null);

    private static string Word(string? outcome) => outcome switch
    {
        TaskOutcome.Declined => "declined at the door",
        TaskOutcome.Dnd => "DND",
        TaskOutcome.SkippedByGuest => "skipped by the guest",
        TaskOutcome.SupervisorDndApproved => "DND approved",
        null => "open",
        _ => outcome.ToLowerInvariant().Replace('_', ' '),
    };

    private static IReadOnlyList<KnownView> Known(DayFacts.Day day, RoomSupervision lane, RoomState? state, RoomTask? task)
    {
        var known = new List<KnownView>();
        switch (lane.Reason)
        {
            case SupervisionReason.DaysWithoutService:
                if (day.LastAttempt(task) is { } last)
                {
                    known.Add(new KnownView($"today at the door: {last.Found.ToLowerInvariant()}", At(last.At)));
                }

                known.Add(new KnownView((state?.Occupancy ?? Occupancy.Unknown).ToLowerInvariant(), null));
                break;
            case SupervisionReason.NobodyAvailable:
                known.Add(new KnownView($"{task?.Service.ToLowerInvariant().Replace('_', ' ') ?? "service"} · nobody posted to Housekeeping could take it", null));
                if (state?.NextSoldAt is { } sold)
                {
                    known.Add(new KnownView("sold", At(sold)));
                }

                break;
            case SupervisionReason.ArrivalBeforeWindow:
                known.Add(new KnownView("arrival before the window opens", At(state?.NextSoldAt)));
                break;
            default:
                known.Add(new KnownView(lane.Note ?? string.Empty, null));
                break;
        }

        return known;
    }
}
