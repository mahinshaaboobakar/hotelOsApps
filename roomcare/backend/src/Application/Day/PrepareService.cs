using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Day;

/// <summary>Prepare the day — the first press builds it, every later press reconciles (S0's trigger).</summary>
/// <remarks>
/// <para>
/// <b>A reconcile adds and updates; it never removes and never reshuffles.</b>
/// A new room gets a task; an unstarted task whose facts changed gets its
/// service, priority and linen brought up to date; a task already started is
/// the attendant's at the door and is not touched; nothing is taken off an
/// attendant. Automatic mode is this same call made by the tick.
/// </para>
/// </remarks>
public sealed class PrepareService(
    RoomCareDbContext db,
    Gate gate,
    IHouse house,
    PropertyClock clock,
    StandardReader standard,
    TaskMaker factory,
    ProposalService proposals,
    IEventAppender events)
{
    /// <summary>A person presses Prepare the day — or asks HosPilot to, as themselves (S7).</summary>
    /// <remarks>
    /// <c>roomcare.assign</c> is answerable on a <c>room_task</c>, and a day not
    /// yet prepared may have none: it is asked on the property's most recent
    /// task, and on the very first press — when no task exists at all — the
    /// person must hold <c>roomcare.configure</c> on the property instead. Raised
    /// with the architect alongside the room-level amend.
    /// </remarks>
    public async Task<PrepareRun> PressAsync(RequestScope scope, string? window, CancellationToken cancellationToken)
    {
        Actor.PersonOf(scope, "preparing the day");
        var latest = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is { } task)
        {
            await gate.TaskAsync(scope, Permissions.Assign, task, cancellationToken);
        }
        else
        {
            await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        }

        return await PrepareAsync(scope, window, cancellationToken);
    }

    /// <summary>Run the decision over every room for the target window and lay out the proposal.</summary>
    public async Task<PrepareRun> PrepareAsync(RequestScope scope, string? window, CancellationToken cancellationToken)
    {
        var now = await clock.AtAsync(scope.PropertyId, cancellationToken);
        var policy = await standard.PolicyAsync(scope.PropertyId, cancellationToken);
        var (target, day) = WindowTimes.Target(await standard.WindowsAsync(scope.PropertyId, cancellationToken), now, window);
        var dayEnds = WindowTimes.DayEnds(day, now);

        var previous = await db.PrepareRuns
            .Where(r => r.PropertyId == scope.PropertyId && r.OperatingDay == day && r.Window == target.Window)
            .OrderByDescending(r => r.At)
            .FirstOrDefaultAsync(cancellationToken);
        var actor = Actor.Of(scope);
        var run = new PrepareRun
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            OperatingDay = day,
            Window = target.Window,
            At = now.Instant,
            ByKind = actor.Kind,
            ById = actor.Id,
            Via = actor.Via,
            Kind = previous is null ? RunKind.First : RunKind.Reconcile,
            ChangesSincePrevious = previous is null ? 0 : await ChangesSinceAsync(scope.PropertyId, previous.At, cancellationToken),
        };

        var rooms = await house.RoomsAsync(scope.PropertyId, cancellationToken);
        var states = await db.RoomStates.Where(r => r.PropertyId == scope.PropertyId).ToDictionaryAsync(r => r.RoomId, cancellationToken);
        var existing = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId && t.OperatingDay == day && t.Window == target.Window && t.RoomId != null)
            .ToListAsync(cancellationToken);
        var blocked = await db.DeepCleans
            .Where(d => d.PropertyId == scope.PropertyId && (d.Status == DeepCleanStatus.Blocked || d.Status == DeepCleanStatus.InProgress))
            .Select(d => d.RoomId)
            .ToListAsync(cancellationToken);

        foreach (var room in rooms)
        {
            states.TryGetValue(room.Id, out var state);
            var decided = DayDecision.Decide(new DecisionFacts(state, policy, target.Window, day, dayEnds)
            {
                Blocked = blocked.Contains(room.Id),
            });
            run.RoomsConsidered++;
            if (decided is null)
            {
                continue;
            }

            var inputs = new DecisionInputs
            {
                Condition = state?.Condition,
                Occupancy = state?.Occupancy,
                StayStatuses = state?.StayStatuses ?? [],
                NextSoldAt = state?.NextSoldAt,
                Window = target.Window,
                RuleVersion = policy.Version,
                Reason = decided.Reason,
            };
            var current = existing.FirstOrDefault(t => t.RoomId == room.Id);
            var outcome = current is null
                ? await factory.CreateAsync(scope, room, state, decided, inputs, day, target.Window, run, policy, cancellationToken)
                : await factory.ReconcileAsync(scope, room, current, state, decided, inputs, run, policy, cancellationToken);
            run.TasksCreated += outcome == TaskMaker.Change.Created ? 1 : 0;
            run.TasksUpdated += outcome == TaskMaker.Change.Updated ? 1 : 0;
            run.TasksSkipped += outcome == TaskMaker.Change.Unchanged ? 1 : 0;
            run.Pending += decided.Pending ? 1 : 0;
        }

        await db.SaveChangesAsync(cancellationToken);
        run.Unassignable = await proposals.ProposeAsync(scope, day, target.Window, policy, cancellationToken);

        db.PrepareRuns.Add(run);
        events.Append(scope, EventTypes.DayPrepared, EventTypes.RunAggregate, run.Id, 1, new DayPreparedAnnouncement
        {
            RunId = run.Id,
            PropertyId = run.PropertyId,
            OperatingDay = day.ToString("yyyy-MM-dd"),
            Window = run.Window,
            Kind = run.Kind,
            RoomsConsidered = run.RoomsConsidered,
            TasksCreated = run.TasksCreated,
            TasksUpdated = run.TasksUpdated,
            Pending = run.Pending,
            Unassignable = run.Unassignable,
            OccurredAt = now.Instant,
        });
        await db.SaveChangesAsync(cancellationToken);
        return run;
    }

    /// <summary>"N changes since 08:00" — observations recorded after the last run.</summary>
    public Task<int> ChangesSinceAsync(Guid propertyId, DateTimeOffset since, CancellationToken cancellationToken) =>
        db.Observations.CountAsync(o => o.PropertyId == propertyId && o.RecordedAt > since, cancellationToken);
}
