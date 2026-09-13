using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Supervision;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Day;

/// <summary>The assignment proposal — rooms matched to the people posted to Housekeeping, by the property's strategy.</summary>
/// <remarks>
/// <para>
/// <b>A proposal is only a proposal</b> (the note to the architect §1): each
/// match is a <c>task_assignment</c> row in mode <c>PROPOSED</c>, and the task
/// is not the attendant's until a supervisor accepts it. A reconcile proposes
/// for unassigned work only — it never moves a room someone already has.
/// </para>
/// <para>
/// <b>Who is here is Workforce's.</b> The candidates are the people Workforce
/// announced as posted to the property's Housekeeping department. The zone on
/// the posting and minutes on shift are Workforce's to add (chapter 03 §9); until
/// they are readable the proposal groups by department, balances by expected
/// minutes, and says so on the Prepare screen. "Nobody available" is an outcome
/// a supervisor sees — for a room sold tonight it opens the supervision lane.
/// </para>
/// </remarks>
public sealed class ProposalService(RoomCareDbContext db, SupervisionLane lane, TimeProvider clock)
{
    /// <summary>Propose for every planned, unassigned room task of the window; answers how many found nobody.</summary>
    public async Task<int> ProposeAsync(
        RequestScope scope, DateOnly day, string window, PropertyPolicy policy, CancellationToken cancellationToken)
    {
        var candidates = await db.Postings
            .Where(p => p.PropertyId == scope.PropertyId && p.EndedAt == null && p.DepartmentCode == policy.DepartmentCode)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var open = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId && t.OperatingDay == day && t.Window == window)
            .Where(t => t.Status == RoomTaskStatus.Planned && t.AssignedToUserId == null && t.RoomId != null)
            .Where(t => !db.Assignments.Any(a => a.TaskId == t.Id && a.EndedAt == null))
            .OrderBy(t => t.PriorityRank)
            .ToListAsync(cancellationToken);

        if (open.Count == 0)
        {
            return 0;
        }

        if (candidates.Count == 0)
        {
            foreach (var task in open.Where(t => t.Priority == PriorityBand.SoldTonight))
            {
                await lane.OpenAsync(scope, task.RoomId!.Value, day, SupervisionReason.NobodyAvailable, cancellationToken);
            }

            return open.Count;
        }

        var load = await LoadAsync(scope.PropertyId, day, window, candidates, cancellationToken);
        var yesterday = policy.AssignmentStrategy == AssignmentStrategy.Continuity
            ? await YesterdayAsync(scope.PropertyId, day.AddDays(-1), candidates, cancellationToken)
            : [];
        var zones = await db.ZoneAssignments
            .Where(z => z.PropertyId == scope.PropertyId && z.EffectiveUntil == null)
            .ToDictionaryAsync(z => z.RoomId, z => z.ZoneId, cancellationToken);
        var zoneHolder = new Dictionary<Guid, Guid>();
        var now = clock.GetUtcNow();

        foreach (var task in open)
        {
            var room = task.RoomId!.Value;
            var person = Pick(policy.AssignmentStrategy, room, candidates, load, yesterday, zones, zoneHolder);
            load[person] += task.MinutesExpected;
            if (zones.TryGetValue(room, out var zone))
            {
                zoneHolder.TryAdd(zone, person);
            }

            db.Assignments.Add(new TaskAssignment
            {
                Id = Guid.CreateVersion7(),
                TaskId = task.Id,
                PropertyId = task.PropertyId,
                UserId = person,
                AssignedByKind = ActorKind.System,
                AssignedAt = now,
                Mode = AssignmentMode.Proposed,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return 0;
    }

    private static Guid Pick(
        string strategy,
        Guid room,
        IReadOnlyList<Guid> candidates,
        Dictionary<Guid, int> load,
        Dictionary<Guid, Guid> yesterday,
        Dictionary<Guid, Guid> zones,
        Dictionary<Guid, Guid> zoneHolder)
    {
        if (strategy == AssignmentStrategy.Continuity && yesterday.TryGetValue(room, out var had) && candidates.Contains(had))
        {
            return had;
        }

        if (strategy == AssignmentStrategy.SameZone && zones.TryGetValue(room, out var zone) && zoneHolder.TryGetValue(zone, out var holder))
        {
            var lightest = load.Values.Min();
            if (load[holder] <= lightest + 120)
            {
                return holder;
            }
        }

        return candidates.OrderBy(c => load[c]).First();
    }

    private async Task<Dictionary<Guid, int>> LoadAsync(
        Guid propertyId, DateOnly day, string window, IReadOnlyList<Guid> people, CancellationToken cancellationToken)
    {
        var taken = await db.Assignments
            .Where(a => a.PropertyId == propertyId && a.EndedAt == null && people.Contains(a.UserId))
            .Join(db.Tasks.Where(t => t.OperatingDay == day && t.Window == window), a => a.TaskId, t => t.Id, (a, t) => new { a.UserId, t.MinutesExpected })
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Minutes = g.Sum(x => x.MinutesExpected) })
            .ToDictionaryAsync(x => x.UserId, x => x.Minutes, cancellationToken);
        return people.ToDictionary(p => p, p => taken.GetValueOrDefault(p));
    }

    private async Task<Dictionary<Guid, Guid>> YesterdayAsync(
        Guid propertyId, DateOnly day, IReadOnlyList<Guid> people, CancellationToken cancellationToken) =>
        await db.Tasks
            .Where(t => t.PropertyId == propertyId && t.OperatingDay == day && t.RoomId != null && t.AssignedToUserId != null)
            .Where(t => people.Contains(t.AssignedToUserId!.Value))
            .GroupBy(t => t.RoomId!.Value)
            .Select(g => new { Room = g.Key, Person = g.Select(t => t.AssignedToUserId!.Value).First() })
            .ToDictionaryAsync(x => x.Room, x => x.Person, cancellationToken);
}
