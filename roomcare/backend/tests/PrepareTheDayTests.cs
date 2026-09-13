using HotelOS.RoomCare.Application.Assignment;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>S0's trigger — the first press builds the day, a later press reconciles and never takes a room off anyone.</summary>
[Collection(RoomCareCollection.Name)]
public sealed class PrepareTheDayTests(RoomCareFixture fixture)
{
    [Fact]
    public async Task The_first_press_decides_every_room_in_the_ladders_order()
    {
        var h = new RoomCareHarness(fixture);
        var rekha = Guid.CreateVersion7();
        var soldDeparture = h.House.Room("G01");
        var occupied = h.House.Room("G02");
        var readyVacant = h.House.Room("G03");
        await h.SeedStateAsync(soldDeparture, s =>
        {
            s.Condition = Condition.Dirty; s.Occupancy = Occupancy.Vacant; s.StayStatuses = [StayStatus.CheckedOut];
            s.NextSoldAt = RoomCareHarness.Saturday(15, 0);
        });
        await h.SeedStateAsync(occupied, s => s.Condition = Condition.Dirty);
        await h.SeedStateAsync(readyVacant, s => { s.Condition = Condition.Clean; s.Occupancy = Occupancy.Vacant; s.StayStatuses = []; s.ConditionSetAt = h.Clock.GetUtcNow(); });

        var run = await h.Get<PrepareService>().PressAsync(h.As(rekha), null, default);

        Assert.Equal((RunKind.First, 3, 2), (run.Kind, run.RoomsConsidered, run.TasksCreated));
        await using var db = h.Db();
        var tasks = await db.Tasks.Where(t => t.PropertyId == h.PropertyId).OrderBy(t => t.PriorityRank).ToListAsync();
        Assert.Equal(
            [(soldDeparture, Service.DepartureClean, PriorityBand.SoldTonight, LinenDue.Must), (occupied, Service.DailyService, PriorityBand.Daily, LinenDue.Due)],
            tasks.Select(t => (t.RoomId!.Value, t.Service, t.Priority, t.LinenDue)));
        Assert.All(tasks, t => Assert.Equal(RoomTaskStatus.Planned, t.Status));
        Assert.Contains(h.Authorizer.Checks, c => c.Permission == "roomcare.configure" && c.ObjectType == "property");
        var created = (await h.EventsAsync(tasks[0].Id)).Single();
        Assert.Equal((EventTypes.TaskCreated, "room_task"), (created.Type, created.Aggregate));
        Assert.Equal(h.House.Housekeeping.ToString(), created.Payload.GetProperty("department_id").GetString());
    }

    [Fact]
    public async Task An_unsold_departure_waits_pending_when_the_property_says_it_may()
    {
        var h = new RoomCareHarness(fixture);
        var room = h.House.Room("1105");
        await using (var db = h.Db())
        {
            db.Policies.Add(new PropertyPolicy { PropertyId = h.PropertyId, UnsoldDeparture = UnsoldDeparture.MayWait, Version = 1 });
            await db.SaveChangesAsync();
        }

        await h.SeedStateAsync(room, s => { s.Condition = Condition.Dirty; s.Occupancy = Occupancy.Vacant; s.StayStatuses = [StayStatus.CheckedOut]; });

        var run = await h.Get<PrepareService>().PrepareAsync(h.Tick, null, default);

        Assert.Equal(1, run.Pending);
        await using var read = h.Db();
        Assert.Equal(RoomTaskStatus.PendingPolicy, (await read.Tasks.SingleAsync(t => t.RoomId == room)).Status);
    }

    [Fact]
    public async Task A_later_press_adds_the_new_room_and_leaves_the_accepted_one_with_its_attendant()
    {
        var h = new RoomCareHarness(fixture);
        var anita = await h.PostAsync("Anita Pillai");
        var first = h.House.Room("214");
        await h.SeedStateAsync(first, s => s.Condition = Condition.Dirty);
        await h.Get<PrepareService>().PressAsync(h.As(anita), null, default);
        await h.Get<AssignmentService>().AcceptAllAsync(h.As(anita), new DateOnly(2026, 9, 5), ServiceWindowName.Morning, default);

        var late = h.House.Room("219");
        h.Clock.Advance(TimeSpan.FromMinutes(8));
        await h.InScopeAsync(async s =>
        {
            await s.GetRequiredServiceFor<ObservationService>().ObserveAsync(h.Tick, new ObservedFact(late, ObservationSource.Pms, h.Clock.GetUtcNow(), h.Clock.GetUtcNow())
            {
                Condition = Condition.Dirty, Occupancy = Occupancy.Vacant, StayStatuses = [StayStatus.CheckedOut],
                SaysNextSold = true, NextSoldAt = RoomCareHarness.Saturday(16, 30),
            }, default);
            return await s.GetRequiredServiceFor<Infrastructure.RoomCareDbContext>().SaveChangesAsync();
        });

        var second = await h.Get<PrepareService>().PressAsync(h.As(anita), null, default);

        Assert.Equal((RunKind.Reconcile, 1, 1), (second.Kind, second.TasksCreated, second.ChangesSincePrevious));
        await using var db = h.Db();
        var kept = await db.Tasks.SingleAsync(t => t.RoomId == first);
        Assert.Equal((RoomTaskStatus.Assigned, anita), (kept.Status, kept.AssignedToUserId!.Value));
        Assert.Equal(1, await db.Assignments.CountAsync(a => a.TaskId == kept.Id));
    }

    [Fact]
    public async Task With_nobody_posted_a_room_sold_tonight_goes_to_the_supervisor_as_nobody_available()
    {
        var h = new RoomCareHarness(fixture);
        var room = h.House.Room("219");
        await h.SeedStateAsync(room, s =>
        {
            s.Condition = Condition.Dirty; s.Occupancy = Occupancy.Vacant; s.StayStatuses = [StayStatus.CheckedOut];
            s.NextSoldAt = RoomCareHarness.Saturday(16, 30);
        });

        var run = await h.Get<PrepareService>().PrepareAsync(h.Tick, null, default);

        Assert.Equal(1, run.Unassignable);
        await using var db = h.Db();
        Assert.Equal(SupervisionReason.NobodyAvailable, (await db.Supervision.SingleAsync(s => s.RoomId == room)).Reason);
    }

    [Fact]
    public async Task Two_supervisors_assigning_one_room_on_the_same_version_produce_one_row_and_one_refusal()
    {
        var h = new RoomCareHarness(fixture);
        var meera = Guid.CreateVersion7();
        var room = h.House.Room("305");
        await h.SeedStateAsync(room, s => s.Condition = Condition.Dirty);
        await h.Get<PrepareService>().PrepareAsync(h.Tick, null, default);
        Domain.RoomTask task;
        await using (var db = h.Db())
        {
            task = await db.Tasks.SingleAsync(t => t.RoomId == room);
        }

        await h.Get<AssignmentService>().AssignAsync(h.As(meera), task.Id, task.Version, Guid.CreateVersion7(), default);
        await Assert.ThrowsAsync<HotelOS.Platform.ConcurrencyException>(() =>
            h.Get<AssignmentService>().AssignAsync(h.As(meera), task.Id, task.Version, Guid.CreateVersion7(), default));

        await using var read = h.Db();
        Assert.Equal(1, await read.Assignments.CountAsync(a => a.TaskId == task.Id && a.EndedAt == null));
    }
}
