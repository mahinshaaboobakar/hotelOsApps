using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>The Prepare screen — the window's state, what changed since the last press and what the next would do, the proposal (frame 2).</summary>
/// <remarks>
/// "On the next press" runs the decision over the changed room without saving —
/// the same function the press runs, so the preview cannot disagree with it.
/// </remarks>
public sealed class PrepareProjection(RoomCareDbContext db, IHouse house, PropertyClock clock, StandardReader standard)
{
    public const int PageSize = 12;

    public async Task<PrepareView> PrepareAsync(RequestScope scope, int page, CancellationToken cancellationToken)
    {
        var snapshot = await LoadAsync(scope.PropertyId, house, clock, db, cancellationToken);
        var now = snapshot.Now;
        var policy = await standard.PolicyAsync(scope.PropertyId, cancellationToken);
        var windows = await standard.WindowsAsync(scope.PropertyId, cancellationToken);
        var (window, day) = WindowTimes.Target(windows, now, null);
        var (opens, closes) = WindowTimes.Of(window, day, now);
        var run = await db.PrepareRuns.Where(r => r.PropertyId == scope.PropertyId && r.OperatingDay == day && r.Window == window.Window)
            .OrderByDescending(r => r.At).FirstOrDefaultAsync(cancellationToken);
        var firstRun = await db.PrepareRuns.Where(r => r.PropertyId == scope.PropertyId && r.OperatingDay == day && r.Window == window.Window)
            .OrderBy(r => r.At).FirstOrDefaultAsync(cancellationToken);
        var tasks = await db.Tasks.Where(t => t.PropertyId == scope.PropertyId && t.OperatingDay == day && t.Window == window.Window).ToListAsync(cancellationToken);

        var changes = run is null
            ? []
            : await db.Observations.Where(o => o.PropertyId == scope.PropertyId && o.RecordedAt > run.At).OrderBy(o => o.RecordedAt).ToListAsync(cancellationToken);
        var states = await db.RoomStates.Where(r => r.PropertyId == scope.PropertyId).ToDictionaryAsync(r => r.RoomId, cancellationToken);
        var size = Math.Max(1, PageSize);
        var shown = changes.Skip(Math.Max(0, page) * size).Take(size)
            .Select(o => Change(snapshot, o, states.GetValueOrDefault(o.RoomId), tasks.FirstOrDefault(t => t.RoomId == o.RoomId), policy, window.Window, day))
            .ToList();
        var byName = run?.ById is { } by ? (await house.NamesAsync([by], cancellationToken)).GetValueOrDefault(by) : null;

        return new PrepareView(
            window.Window, now.InstantOf(day, window.Starts).ToString("o"), closes.ToString("o"),
            now.Instant >= opens && now.Instant < closes,
            day.ToString("yyyy-MM-dd"),
            At(firstRun?.At),
            byName,
            tasks.Count,
            changes.Select(c => c.RoomId).Distinct().Count(),
            policy.TriggerMode,
            now.Instant.ToString("o"),
            shown,
            new Views.Paging(Math.Max(0, page), size, changes.Count),
            await ProposalAsync(snapshot, tasks, policy, scope.PropertyId, cancellationToken));
    }

    private static ChangeView Change(
        HouseSnapshot snapshot, RoomObservation seen, RoomState? state, RoomTask? task, PropertyPolicy policy, string window, DateOnly day)
    {
        var what = seen.Source switch
        {
            ObservationSource.Manual => $"entered by hand · {Words(seen)}",
            _ => $"{seen.Source.ToLowerInvariant()} · {Words(seen)}",
        };
        var decided = DayDecision.Decide(new DecisionFacts(state, policy, window, day, WindowTimes.DayEnds(day, snapshot.Now)));
        var next = (decided, task) switch
        {
            (null, null) => "nothing — the room needs no service",
            (null, _) => "left as it is",
            (_, null) => $"new {decided.Service.ToLowerInvariant().Replace('_', ' ')}{(decided.Pending ? " · pending" : string.Empty)}, priority {DayDecision.Rank(decided.Band, policy) + 1}, unassigned",
            (_, { } t) when !RoomTaskStatus.Unstarted.Contains(t.Status) => "started — the attendant's at the door; not touched",
            (_, { } t) when t.Priority != decided.Band => $"priority {DayDecision.Rank(t.Priority, policy) + 1} → {DayDecision.Rank(decided.Band, policy) + 1}; same attendant",
            _ => "unchanged",
        };
        return new ChangeView(seen.RecordedAt.ToString("o"), seen.RoomId.ToString(), snapshot.Number(seen.RoomId), seen.Source, what, next);
    }

    private static string Words(RoomObservation seen) => string.Join(" · ", new[]
    {
        seen.Condition?.ToLowerInvariant(),
        seen.Occupancy?.ToLowerInvariant(),
        seen.StayStatuses is { Count: > 0 } s ? string.Join("/", s.Select(x => x.ToLowerInvariant().Replace('_', ' '))) : null,
        seen.NextSoldAt is { } sold ? $"sold {sold:o}" : null,
    }.Where(x => x is not null));

    private async Task<ProposalView> ProposalAsync(
        HouseSnapshot snapshot, IReadOnlyList<RoomTask> tasks, PropertyPolicy policy, Guid propertyId, CancellationToken cancellationToken)
    {
        var ids = tasks.Select(t => t.Id).ToList();
        var rows = await db.Assignments.Where(a => ids.Contains(a.TaskId) && a.EndedAt == null).ToListAsync(cancellationToken);
        var candidates = await db.Postings.CountAsync(p => p.PropertyId == propertyId && p.EndedAt == null && p.DepartmentCode == policy.DepartmentCode, cancellationToken);
        var names = await house.NamesAsync(rows.Select(r => r.UserId).Distinct().ToList(), cancellationToken);
        var people = rows.GroupBy(r => r.UserId).Select(g =>
        {
            var mine = tasks.Where(t => g.Any(r => r.TaskId == t.Id)).OrderBy(t => t.PriorityRank).ToList();
            return new ProposedPersonView(g.Key.ToString(), names.GetValueOrDefault(g.Key) ?? "a person with no staff record", mine.Count,
                mine.Select(t => snapshot.Number(t.RoomId)).ToList(), mine.Sum(t => t.MinutesExpected + t.ExtraMinutes),
                g.All(r => r.Mode != AssignmentMode.Proposed));
        }).OrderBy(p => p.Name).ToList();
        var nobody = tasks.Where(t => t.Status == RoomTaskStatus.Planned && t.RoomId != null && rows.All(r => r.TaskId != t.Id))
            .OrderBy(t => t.PriorityRank)
            .Select(t => new UnassignedRoomView(t.Id.ToString(), t.Version, snapshot.Number(t.RoomId), t.Service, DayDecision.Rank(t.Priority, policy) + 1, null))
            .ToList();
        return new ProposalView(policy.AssignmentStrategy, people, nobody, rows.Count(r => r.Mode == AssignmentMode.Proposed), candidates, ZoneOnPosting: false);
    }
}
