using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using HotelOS.Contracts.Common.V1;
using HotelOS.Contracts.Integration.V1;
using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>The contracts at Room Care's edges — the manifest against the code, the Hub's message, the module surface.</summary>
[Collection(RoomCareCollection.Name)]
public sealed class WireTests(RoomCareFixture fixture)
{
    private static readonly string Manifest = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "manifest.yaml"));

    [Fact]
    public void The_manifest_declares_exactly_what_the_code_publishes_consumes_and_asks_for()
    {
        Assert.Equal(EventTypes.Published, Section("publishes"));
        Assert.Equal(EventTypes.Subscribed, Section("subscribes"));
        Assert.Equal(Permissions.All, Manifest.Split('\n').Where(l => l.TrimStart().StartsWith("- id: ") && !l.Contains("-ready") && l.Contains('.'))
            .Select(l => l.Trim()[6..]).ToList());
        Assert.Contains("aggregate: roomcare_manager_grant", Manifest);
    }

    [Fact]
    public async Task The_Hubs_RoomStateFact_as_its_appender_writes_it_reaches_the_room()
    {
        var h = new RoomCareHarness(fixture, RoomCareHarness.Saturday(11, 3));
        var room = h.House.Room("214");
        var fact = new RoomStateFact
        {
            Header = new FactHeader
            {
                PropertyId = h.PropertyId.ToString(),
                OccurredAt = new FactTime { At = Timestamp.FromDateTimeOffset(RoomCareHarness.Saturday(11, 2)), Basis = TimeBasis.Observed },
                BusinessDate = "2026-09-05",
            },
            State = new Contracts.Integration.V1.RoomState
            {
                RoomId = room.ToString(),
                Occupancy = Contracts.Integration.V1.Occupancy.Vacant,
                Condition = RoomCondition.Dirty,
                NextSoldAt = Timestamp.FromDateTimeOffset(RoomCareHarness.Saturday(15, 0)),
            },
        };
        fact.State.StayStatuses.Add(StayLifecycle.CheckedOut);

        // Exactly the bytes the Hub's EventAppender stores, then exactly the read the consumer makes.
        var wire = JsonSerializer.SerializeToElement(fact, EventAppender.PayloadOptions);
        var payload = wire.Deserialize<RoomStateObserved>(EventAppender.PayloadOptions)!;
        await h.InScopeAsync(async s =>
        {
            await ActivatorUtilities.CreateInstance<RoomStateObservedHandler>(s).HandleAsync(h.Tick, payload,
                new EventEnvelope { EventId = Guid.CreateVersion7(), EventType = EventTypes.RoomStateObserved, OccurredAt = h.Clock.GetUtcNow() }, default);
            return 0;
        });

        await using var db = h.Db();
        var state = await db.RoomStates.SingleAsync(r => r.RoomId == room);
        Assert.Equal((Condition.Dirty, Domain.Occupancy.Vacant, ConditionSource.Pms), (state.Condition, state.Occupancy, state.ConditionSource));
        Assert.Equal([StayStatus.CheckedOut], state.StayStatuses);
        Assert.Equal(RoomCareHarness.Saturday(15, 0).ToUniversalTime(), state.NextSoldAt);
        Assert.Equal(new DateOnly(2026, 9, 5), (await db.Observations.SingleAsync(o => o.RoomId == room)).OperatingDay);
    }

    [Fact]
    public async Task The_board_answers_over_the_module_surface_and_a_call_without_a_token_is_refused()
    {
        await using var surface = await ModuleSurface.StartAsync(fixture);
        var room = surface.Data.House.Room("G01");
        await surface.Data.SeedStateAsync(room, s => s.Condition = Condition.Dirty);

        var (status, body) = await surface.CallAsync("roomcare.read", "board");
        var (refused, _) = await surface.CallAsync("roomcare.read", "board", withToken: false);

        Assert.Equal(200, status);
        var zones = body!.Value.GetProperty("zones");
        Assert.Equal("G01", zones[0].GetProperty("rooms")[0].GetProperty("number").GetString());
        Assert.Equal(1, body.Value.GetProperty("strip").GetProperty("dirty").GetInt32());
        Assert.Equal(401, refused);
    }

    [Fact]
    public async Task Every_screen_and_widget_read_answers_on_a_prepared_day()
    {
        await using var surface = await ModuleSurface.StartAsync(fixture);
        var h = surface.Data;
        var anita = await h.PostAsync("Anita Pillai");
        var departed = h.House.Room("G01");
        var occupied = h.House.Room("G02");
        await h.SeedStateAsync(departed, s =>
        {
            s.Condition = Condition.Dirty; s.Occupancy = Domain.Occupancy.Vacant; s.StayStatuses = [StayStatus.CheckedOut];
            s.NextSoldAt = RoomCareHarness.Saturday(15, 0);
        });
        await h.SeedStateAsync(occupied, s => s.Condition = Condition.Dirty);
        Assert.Equal(200, (await surface.CallAsync("roomcare.assign", "prepare")).Status);

        string[] reads = ["me", "board", "prepare", "attendants", "states", "supervision", "deepCleans", "myRooms", "setup", "services", "zones", "areas",
            "deepCleanPlan", "grants", "widgetRoomsReady", "widgetArrivals", "widgetAttention", "widgetAttendants", "widgetPending"];
        foreach (var method in reads)
        {
            var (status, body) = await surface.CallAsync("roomcare.read", method);
            Assert.True(status == 200, $"{method} answered {status}: {body}");
        }

        var (roomStatus, room) = await surface.CallAsync("roomcare.read", "room", new { roomId = departed });
        Assert.Equal(200, roomStatus);
        Assert.Equal("DEPARTURE_CLEAN", room!.Value.GetProperty("line").GetProperty("service").GetString());
        var (_, prepared) = await surface.CallAsync("roomcare.read", "prepare");
        Assert.Equal(anita.ToString(), prepared!.Value.GetProperty("proposal").GetProperty("people")[0].GetProperty("userId").GetString());
    }

    private static List<string> Section(string name) =>
        Manifest.Split('\n')
            .SkipWhile(l => l.Trim() != $"{name}:")
            .Skip(1)
            .TakeWhile(l => l.TrimStart().StartsWith("- ") || l.TrimStart().StartsWith('#'))
            .Where(l => l.TrimStart().StartsWith("- "))
            .Select(l => l.Trim()[2..])
            .ToList();
}
