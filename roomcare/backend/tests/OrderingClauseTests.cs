using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>S4's ordering clause, on the walkthrough's own worked example — 214 at 11:02, 12:40, 12:41 and 12:45.</summary>
[Collection(RoomCareCollection.Name)]
public sealed class OrderingClauseTests(RoomCareFixture fixture)
{
    [Fact]
    public async Task A_checkout_on_a_room_nobody_touched_since_is_applied_and_announced()
    {
        var h = new RoomCareHarness(fixture, RoomCareHarness.Saturday(11, 2));
        var room = h.House.Room("214");
        await h.SeedStateAsync(room, s => { s.Condition = Condition.Clean; s.ConditionSource = ConditionSource.Attendant; });

        var seen = await ObserveAsync(h, room, RoomCareHarness.Saturday(11, 2), Condition.Dirty, Occupancy.Vacant, [StayStatus.CheckedOut]);

        Assert.Equal(ObservationOutcome.Applied, seen!.Outcome);
        await using var db = h.Db();
        var state = await db.RoomStates.SingleAsync(r => r.RoomId == room);
        Assert.Equal((Condition.Dirty, ConditionSource.Pms, Occupancy.Vacant), (state.Condition, state.ConditionSource, state.Occupancy));
        var events = await h.EventsAsync(room);
        Assert.Contains(events, e => e.Type == EventTypes.RoomConditionChanged && e.Payload.GetProperty("to").GetString() == Condition.Dirty);
    }

    [Fact]
    public async Task A_late_message_older_than_the_attendants_act_is_history_and_raises_no_flag()
    {
        var h = new RoomCareHarness(fixture, RoomCareHarness.Saturday(12, 41));
        var room = h.House.Room("214");
        await h.SeedStateAsync(room, s =>
        {
            s.Condition = Condition.Clean;
            s.ConditionSource = ConditionSource.Attendant;
            s.ConditionSetAt = RoomCareHarness.Saturday(12, 40);
            s.Occupancy = Occupancy.Vacant;
        });

        var seen = await ObserveAsync(h, room, RoomCareHarness.Saturday(11, 2), Condition.Dirty, Occupancy.Vacant, [StayStatus.CheckedOut]);

        Assert.Equal(ObservationOutcome.OlderThanAct, seen!.Outcome);
        Assert.False(seen.Applied);
        await using var db = h.Db();
        var state = await db.RoomStates.SingleAsync(r => r.RoomId == room);
        Assert.Equal(Condition.Clean, state.Condition);
        Assert.False(state.HasDisagreement);
    }

    [Fact]
    public async Task The_desk_contradicting_a_newer_act_is_a_disagreement_when_Room_Care_leads()
    {
        var h = new RoomCareHarness(fixture, RoomCareHarness.Saturday(12, 45));
        var room = h.House.Room("214");
        await h.SeedStateAsync(room, s =>
        {
            s.Condition = Condition.Clean;
            s.ConditionSource = ConditionSource.Attendant;
            s.ConditionSetAt = RoomCareHarness.Saturday(12, 40);
            s.Occupancy = Occupancy.Vacant;
            s.StayStatuses = [StayStatus.CheckedOut];
        });

        var seen = await ObserveAsync(h, room, RoomCareHarness.Saturday(12, 45), Condition.Dirty, Occupancy.Vacant, [StayStatus.CheckedOut]);

        Assert.Equal(ObservationOutcome.DisagreementFlagged, seen!.Outcome);
        await using var db = h.Db();
        var state = await db.RoomStates.SingleAsync(r => r.RoomId == room);
        Assert.Equal(Condition.Clean, state.Condition);
        Assert.True(state.HasDisagreement);
        Assert.Contains(await h.EventsAsync(room), e => e.Type == EventTypes.DisagreementFlagged);
    }

    [Fact]
    public async Task The_same_contradiction_is_applied_with_the_overwrite_recorded_when_the_PMS_leads()
    {
        var h = new RoomCareHarness(fixture, RoomCareHarness.Saturday(12, 45));
        var room = h.House.Room("214");
        await using (var db = h.Db())
        {
            db.Policies.Add(new PropertyPolicy { PropertyId = h.PropertyId, WhoLeads = WhoLeads.Pms, Version = 1 });
            await db.SaveChangesAsync();
        }

        await h.SeedStateAsync(room, s =>
        {
            s.Condition = Condition.Clean;
            s.ConditionSource = ConditionSource.Attendant;
            s.ConditionSetAt = RoomCareHarness.Saturday(12, 40);
            s.Occupancy = Occupancy.Vacant;
            s.StayStatuses = [StayStatus.CheckedOut];
        });

        var seen = await ObserveAsync(h, room, RoomCareHarness.Saturday(12, 45), Condition.Dirty, Occupancy.Vacant, [StayStatus.CheckedOut]);

        Assert.Equal(ObservationOutcome.AppliedPmsLeads, seen!.Outcome);
        var changed = (await h.EventsAsync(room)).Single(e => e.Type == EventTypes.RoomConditionChanged);
        Assert.Contains("PMS leads", changed.Payload.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task A_redelivered_event_changes_nothing()
    {
        var h = new RoomCareHarness(fixture, RoomCareHarness.Saturday(11, 2));
        var room = h.House.Room("214");
        var eventId = Guid.CreateVersion7();

        await ObserveAsync(h, room, RoomCareHarness.Saturday(11, 2), Condition.Dirty, Occupancy.Vacant, [StayStatus.CheckedOut], eventId);
        var again = await ObserveAsync(h, room, RoomCareHarness.Saturday(11, 2), Condition.Dirty, Occupancy.Vacant, [StayStatus.CheckedOut], eventId);

        Assert.Null(again);
        await using var db = h.Db();
        Assert.Equal(1, await db.Observations.CountAsync(o => o.RoomId == room));
    }

    [Fact]
    public async Task Clearing_by_taking_theirs_sets_the_condition_as_the_supervisors_and_says_which_side_won()
    {
        var h = new RoomCareHarness(fixture, RoomCareHarness.Saturday(12, 50));
        var room = h.House.Room("214");
        var supervisor = Guid.CreateVersion7();
        await h.SeedStateAsync(room, s =>
        {
            s.Condition = Condition.Clean;
            s.ConditionSource = ConditionSource.Attendant;
            s.DisagreementObservedCondition = Condition.Dirty;
            s.DisagreementObservedAt = RoomCareHarness.Saturday(12, 45);
            s.DisagreementSource = ObservationSource.Pms;
        });
        await SeedTaskAsync(h, room);

        var cleared = await h.Get<DisagreementService>().ClearAsync(h.As(supervisor), room, 1, DisagreementKept.Theirs, default);

        Assert.Equal((Condition.Dirty, ConditionSource.Supervisor, DisagreementKept.Theirs), (cleared.Condition, cleared.ConditionSource, cleared.DisagreementClearedKept));
        Assert.Contains(h.Authorizer.Checks, c => c.Permission == "roomcare.amend" && c.ObjectType == "room_task");
        var events = await h.EventsAsync(room);
        Assert.Equal([EventTypes.RoomConditionChanged, EventTypes.DisagreementCleared], events.Select(e => e.Type));
        Assert.Equal(events.Select(e => e.Version).Distinct().Count(), events.Count);
    }

    internal static async Task SeedTaskAsync(RoomCareHarness h, Guid room)
    {
        await using var db = h.Db();
        var at = h.Clock.GetUtcNow();
        db.Tasks.Add(new RoomTask
        {
            Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, LocationId = room, RoomId = room, OperatingDay = new DateOnly(2026, 9, 5),
            CreatedAt = at, UpdatedAt = at,
        });
        await db.SaveChangesAsync();
    }

    private static Task<RoomObservation?> ObserveAsync(
        RoomCareHarness h, Guid room, DateTimeOffset occurred, string condition, string occupancy, string[] stays, Guid? eventId = null) =>
        h.InScopeAsync(async services =>
        {
            var observed = await services.GetRequiredServiceFor<ObservationService>().ObserveAsync(h.Tick, new ObservedFact(room, ObservationSource.Pms, occurred, h.Clock.GetUtcNow())
            {
                Condition = condition,
                Occupancy = occupancy,
                StayStatuses = stays,
                EventId = eventId ?? Guid.CreateVersion7(),
            }, default);
            await services.GetRequiredServiceFor<Infrastructure.RoomCareDbContext>().SaveChangesAsync();
            return observed;
        });
}

/// <summary>Resolving a service in a test's scope without importing the container's namespace everywhere.</summary>
internal static class ServiceProviderReading
{
    public static T GetRequiredServiceFor<T>(this IServiceProvider services)
        where T : notnull => (T)(services.GetService(typeof(T)) ?? throw new InvalidOperationException($"{typeof(T).Name} is not registered"));
}
