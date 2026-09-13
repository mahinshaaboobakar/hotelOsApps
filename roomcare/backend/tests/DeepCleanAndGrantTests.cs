using HotelOS.Platform;
using HotelOS.RoomCare.Application.DeepCleans;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>S0's deep clean as a project, the Room states save, the GM's grant, and Setup's refusals.</summary>
[Collection(RoomCareCollection.Name)]
public sealed class DeepCleanAndGrantTests(RoomCareFixture fixture)
{
    [Fact]
    public async Task Planning_requests_the_block_and_the_job_and_the_room_comes_back_through_a_departure_clean()
    {
        var h = new RoomCareHarness(fixture);
        var rekha = Guid.CreateVersion7();
        var room = h.House.Room("0902");
        await h.SeedStateAsync(room, s => { s.Condition = Condition.Clean; s.Occupancy = Occupancy.Vacant; s.StayStatuses = []; });

        var project = await h.Get<DeepCleanService>().PlanAsync(h.As(rekha), room, new DateOnly(2026, 9, 8), new DateOnly(2026, 9, 12), default);

        var events = await h.EventsAsync(project.Id);
        Assert.Equal([EventTypes.BlockRequested, EventTypes.DeepCleanDue], events.Select(e => e.Type));
        Assert.Equal([project.BlockCorrelationId, project.JobCorrelationId], events.Select(e => e.CorrelationId));
        Assert.Contains(h.Authorizer.Checks, c => c.Permission == "roomcare.plan" && c.ObjectType == "property");

        var job = Guid.CreateVersion7();
        await h.InScopeAsync(async s =>
        {
            await ActivatorUtilities.CreateInstance<JobCreatedHandler>(s).HandleAsync(h.Tick, new JobAnnounced(job, "J-1171", room, null, null, null),
                new EventEnvelope { EventId = Guid.CreateVersion7(), CorrelationId = project.JobCorrelationId!, OccurredAt = h.Clock.GetUtcNow() }, default);
            await ActivatorUtilities.CreateInstance<JobClosedHandler>(s).HandleAsync(h.Tick, new JobAnnounced(job, "J-1171", room, "deep clean", null, null),
                new EventEnvelope { EventId = Guid.CreateVersion7(), OccurredAt = h.Clock.GetUtcNow() }, default);
            return job;
        });

        await using var db = h.Db();
        var returning = await db.DeepCleans.SingleAsync(d => d.Id == project.Id);
        Assert.Equal((DeepCleanStatus.Returning, job), (returning.Status, returning.JobId!.Value));
        Assert.Equal(Condition.Dirty, (await db.RoomStates.SingleAsync(r => r.RoomId == room)).Condition);
        Assert.Equal(1, await db.JobTouches.CountAsync(t => t.RoomId == room));
    }

    [Fact]
    public async Task One_Save_writes_many_rooms_as_manual_acts_and_hands_back_the_one_the_PMS_moved()
    {
        var h = new RoomCareHarness(fixture);
        var meera = Guid.CreateVersion7();
        var g03 = h.House.Room("G03");
        var g06 = h.House.Room("G06");
        await h.SeedStateAsync(g03, s => s.Condition = Condition.Clean);
        await h.SeedStateAsync(g06, s => { s.Condition = Condition.Dirty; s.Version = 4; });
        await OrderingClauseTests.SeedTaskAsync(h, g03);
        await OrderingClauseTests.SeedTaskAsync(h, g06);

        var saved = await h.Get<RoomStatesService>().SaveAsync(h.As(meera),
        [
            new RoomStateEdit(g03, 1) { Condition = Condition.Dirty, Stay = "DEPARTED", SoldAt = new TimeOnly(13, 0) },
            new RoomStateEdit(g06, 3) { Stay = "DEPARTED" },
        ], default);

        Assert.Equal(1, saved.Saved);
        Assert.Equal(g06, saved.Conflicts.Single().RoomId);
        await using var db = h.Db();
        var state = await db.RoomStates.SingleAsync(r => r.RoomId == g03);
        Assert.Equal((Condition.Dirty, ConditionSource.Manual, Occupancy.Vacant), (state.Condition, state.ConditionSource, state.Occupancy));
        Assert.Equal(RoomCareHarness.Saturday(13, 0).ToUniversalTime(), state.NextSoldAt);
        Assert.Equal(meera, (await db.Observations.SingleAsync(o => o.RoomId == g03)).ByUserId);
    }

    [Fact]
    public async Task The_grant_announces_on_its_own_row_and_names_the_person_granted_not_the_manager_who_decided()
    {
        var h = new RoomCareHarness(fixture);
        var gm = Guid.CreateVersion7();
        var arjun = Guid.CreateVersion7();

        var grant = await h.Get<ManagerGrants>().GrantAsync(h.As(gm), arjun, default);
        await h.Get<ManagerGrants>().RevokeAsync(h.As(gm), arjun, default);

        var events = await h.EventsAsync(grant.Id);
        Assert.Equal([(EventTypes.ManagerGranted, 1L), (EventTypes.ManagerRevoked, 2L)], events.Select(e => (e.Type, e.Version)));
        Assert.All(events, e => Assert.Equal(EventTypes.ManagerGrantAggregate, e.Aggregate));
        Assert.All(events, e => Assert.Equal(arjun.ToString(), e.Payload.GetProperty("user_id").GetString()));
    }

    [Fact]
    public async Task Setup_refuses_an_inspection_rule_while_no_inspection_application_is_installed()
    {
        var h = new RoomCareHarness(fixture);

        var refused = await Assert.ThrowsAsync<InvalidRequestException>(() => h.Get<StandardService>().SaveServiceAsync(h.As(Guid.CreateVersion7()),
            new ServiceEdit(HouseDouble.Standard, Service.DepartureClean, 35, 1m, InspectionRule.Arrivals, [Phase.Strip, Phase.Clean, Phase.MakeUp, Phase.Done]),
            0, default));

        Assert.Equal(StandardService.NoInspectionApplication, refused.Message);
    }
}
