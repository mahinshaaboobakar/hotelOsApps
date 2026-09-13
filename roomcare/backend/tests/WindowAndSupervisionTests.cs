using HotelOS.Platform;
using HotelOS.RoomCare.Application.Supervision;
using HotelOS.RoomCare.Application.Tick;
using HotelOS.RoomCare.Application.Work;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>§6.1 — a window closes on every room with an outcome; S5 c9 — the supervisor's threshold, and a decision that is final.</summary>
[Collection(RoomCareCollection.Name)]
public sealed class WindowAndSupervisionTests(RoomCareFixture fixture)
{
    [Theory]
    [InlineData("22:00", "06:00", "23:30", true)]
    [InlineData("22:00", "06:00", "05:59", true)]
    [InlineData("22:00", "06:00", "06:00", false)]
    [InlineData("08:00", "15:00", "07:59", false)]
    public void A_window_may_cross_midnight(string starts, string ends, string at, bool inside)
    {
        var window = new ServiceWindow { Starts = TimeOnly.Parse(starts), Ends = TimeOnly.Parse(ends) };

        Assert.Equal(inside, window.Contains(TimeOnly.Parse(at)));
    }

    [Fact]
    public async Task When_the_window_closes_a_DND_room_ends_DND_and_an_unreached_room_ends_not_reached()
    {
        var (h, anita, dnd) = await TheDoorTests.AssignedAsync(Service.DailyService, fixture);
        await h.Get<AttendantWork>().AttemptAsync(h.As(anita), new AttemptCommand(dnd.Id, AttemptFound.Dnd), default);

        h.Clock.Set(RoomCareHarness.Saturday(16, 5));
        await h.Get<TickPass>().RunAsync(h.Tick, default);

        await using var db = h.Db();
        var closed = await db.Tasks.SingleAsync(t => t.Id == dnd.Id);
        Assert.Equal((RoomTaskStatus.ClosedByPolicy, TaskOutcome.Dnd), (closed.Status, closed.Outcome));
    }

    [Fact]
    public async Task Two_days_without_service_make_the_room_the_supervisors_and_the_decision_is_final()
    {
        var h = new RoomCareHarness(fixture, RoomCareHarness.Saturday(9, 12));
        var room = h.House.Room("312");
        await h.SeedStateAsync(room, s => { s.Condition = Condition.Dirty; s.DaysWithoutService = 1; });
        await using (var db = h.Db())
        {
            var at = h.Clock.GetUtcNow().AddDays(-1);
            db.Tasks.Add(new RoomTask
            {
                Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, LocationId = room, RoomId = room, OperatingDay = new DateOnly(2026, 9, 4),
                Status = RoomTaskStatus.ClosedByPolicy, Outcome = TaskOutcome.Dnd, CreatedAt = at, UpdatedAt = at,
            });
            await db.SaveChangesAsync();
        }

        await h.Get<TickPass>().RunAsync(h.Tick, default);

        RoomSupervision lane;
        await using (var db = h.Db())
        {
            var state = await db.RoomStates.SingleAsync(r => r.RoomId == room);
            Assert.Equal((2, new DateOnly(2026, 9, 5)), (state.DaysWithoutService, state.SupervisedSince!.Value));
            lane = await db.Supervision.SingleAsync(s => s.RoomId == room);
            Assert.Equal(SupervisionReason.DaysWithoutService, lane.Reason);
            Assert.Contains(await h.EventsAsync(room), e => e.Type == EventTypes.ServiceMissed && e.Payload.GetProperty("days").GetInt32() == 2);
        }

        await OrderingClauseTests.SeedTaskAsync(h, room);
        var meera = Guid.CreateVersion7();
        var decided = await h.Get<SupervisionService>().DecideAsync(h.As(meera), lane.Id, SupervisionDecision.DndApproved, null, default);

        Assert.Equal((SupervisionDecision.DndApproved, meera), (decided.Decision, decided.DecidedByUserId!.Value));
        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            h.Get<SupervisionService>().DecideAsync(h.As(meera), lane.Id, SupervisionDecision.Clean, null, default));
        await using var read = h.Db();
        Assert.Equal(TaskOutcome.SupervisorDndApproved, (await read.Tasks.SingleAsync(t => t.RoomId == room && t.OperatingDay == new DateOnly(2026, 9, 5))).Outcome);
    }

    [Fact]
    public async Task A_room_waiting_on_the_supervisor_is_not_closed_by_the_window()
    {
        var (h, anita, task) = await TheDoorTests.AssignedAsync(Service.DailyService, fixture);
        await using (var db = h.Db())
        {
            db.Supervision.Add(new RoomSupervision
            {
                Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, RoomId = task.RoomId!.Value, OperatingDay = task.OperatingDay,
                Reason = SupervisionReason.DaysWithoutService, OpenedAt = h.Clock.GetUtcNow(),
            });
            await db.SaveChangesAsync();
        }

        h.Clock.Set(RoomCareHarness.Saturday(16, 5));
        await h.Get<TickPass>().RunAsync(h.Tick, default);

        await using var read = h.Db();
        Assert.True((await read.Tasks.SingleAsync(t => t.Id == task.Id)).IsOpen);
    }
}
