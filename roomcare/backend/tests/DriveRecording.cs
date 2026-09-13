using System.Text.Json;
using HotelOS.RoomCare.Application.Assignment;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Application.Work;
using HotelOS.RoomCare.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>Coral Cove's Saturday morning, driven through the real services, and every screen's answer recorded as the wire carries it.</summary>
/// <remarks>
/// <para>
/// The frames draw this property, these people and these times (mockup 01,
/// 1a–5); the UI's tests and its capture harness read what this writes to
/// <c>ui/preview/recorded/</c>. So the fixture is the service's own answer to the
/// frames' morning — never a hand-made shape a screen could agree with while the
/// backend said something else (page 64 §8: the harness holds the fixture the
/// frame draws).
/// </para>
/// <para>
/// Reached through the service layer as each person, then read over HTTP as the
/// supervisor, exactly as the Shell forwards a module call.
/// </para>
/// </remarks>
[Collection(RoomCareCollection.Name)]
public sealed class DriveRecording(RoomCareFixture fixture)
{
    private static readonly string Recorded = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ui", "preview", "recorded"));

    [Fact]
    public async Task Coral_Coves_morning_is_recorded_for_the_screens()
    {
        await using var surface = await ModuleSurface.StartAsync(fixture);
        var h = surface.Data;
        var meera = surface.Person;
        h.House.Names[meera] = "Meera Krishnan";
        var rekha = Named(h, "Rekha Sinha");
        var anita = await h.PostAsync("Anita Pillai");
        var priya = await h.PostAsync("Priya Das");
        var farhan = await h.PostAsync("Farhan Ali");
        var rohan = await h.PostAsync("Rohan Desai");

        var g = Enumerable.Range(1, 12).ToDictionary(i => i, i => h.House.Room($"G{i:00}"));
        var l = new[] { 1, 2, 3, 9, 14 }.ToDictionary(i => i, i => h.House.Room($"L{i:00}", i == 14 ? HouseDouble.Suite : null));
        await SeedHouseAsync(h, g, l, rekha, meera);

        At(h, 3, 2);
        await ObserveAsync(h, g[1], Condition.Dirty, Occupancy.Vacant, [StayStatus.CheckedOut], soldAt: (15, 0));
        At(h, 7, 48);
        await ObserveAsync(h, l[3], Condition.Dirty, Occupancy.Vacant, [StayStatus.CheckedOut], soldAt: (12, 0));

        At(h, 8, 0);
        await h.Get<PrepareService>().PressAsync(h.As(rekha), null, default);
        At(h, 8, 1);
        foreach (var (room, person) in new[] { (g[1], anita), (g[2], anita), (g[3], anita), (g[7], anita), (g[5], priya), (g[10], priya), (g[12], priya), (l[1], farhan), (l[2], farhan), (l[3], rohan), (l[9], rohan) })
        {
            var task = await TaskOfAsync(h, room);
            await h.Get<AssignmentService>().AssignAsync(h.As(meera), task.Id, task.Version, person, default);
        }

        await WorkAsync(h, anita, g[3], (8, 6), (8, 41), AttemptFound.Done, linen: true);
        await WorkAsync(h, farhan, l[1], (8, 2), (8, 20), AttemptFound.Done);
        await WorkAsync(h, priya, g[12], (8, 10), (8, 33), AttemptFound.Partial, parts: [PartialPart.Bathroom]);
        At(h, 8, 40);
        await h.Get<AttendantWork>().AttemptAsync(h.As(priya), new AttemptCommand((await TaskOfAsync(h, g[5])).Id, AttemptFound.Dnd), default);
        await StartAsync(h, rohan, l[3], (8, 51));
        await StartAsync(h, anita, g[1], (9, 4));
        await StartAsync(h, farhan, l[2], (9, 6));
        await WorkAsync(h, rohan, l[9], (8, 55), (9, 10), AttemptFound.Done);

        At(h, 8, 20);
        await ObserveAsync(h, g[11], Condition.Dirty, Occupancy.Vacant, [StayStatus.CheckedOut], soldAt: (16, 30));
        At(h, 9, 11);
        await ObserveAsync(h, l[9], Condition.Dirty, Occupancy.Occupied, [StayStatus.CheckedIn]);

        At(h, 9, 12);
        var g7 = await TaskOfAsync(h, g[7]);
        await h.Get<AmendService>().DeferAsync(h.As(meera), g7.Id, g7.Version, RoomCareHarness.Saturday(12, 0), "the guest asked at the desk", default);
        var g10 = await TaskOfAsync(h, g[10]);
        await h.Get<AmendService>().ReduceAsync(h.As(meera), g10.Id, g10.Version, "light — no bed", "the guest's wish", default);

        Directory.CreateDirectory(Recorded);
        var reads = new (string File, string Method, object? Body)[]
        {
            ("me", "me", null), ("board", "board", null), ("prepare", "prepare", null), ("attendants", "attendants", null), ("states", "states", null),
            ("supervision", "supervision", null), ("deep-cleans", "deepCleans", null), ("setup", "setup", null), ("services", "services", null),
            ("zones", "zones", null), ("areas", "areas", null), ("deep-clean-plan", "deepCleanPlan", null), ("grants", "grants", null),
            ("room-l09", "room", new { roomId = l[9] }), ("room-g03", "room", new { roomId = g[3] }),
            ("widget-rooms-ready", "widgetRoomsReady", null), ("widget-arrivals", "widgetArrivals", null), ("widget-attention", "widgetAttention", null),
            ("widget-attendants", "widgetAttendants", null), ("widget-pending", "widgetPending", null),
        };
        foreach (var (file, method, body) in reads)
        {
            await WriteAsync(file, await surface.CallAsync("roomcare.read", method, body));
        }

        await WriteAsync("my-rooms", await surface.CallAsAsync(anita, "roomcare.read", "myRooms"));
        await WriteAsync("door-g01", await surface.CallAsAsync(anita, "roomcare.read", "door", new { taskId = (await TaskOfAsync(h, g[1])).Id }));
        Assert.True(File.Exists(Path.Combine(Recorded, "board.json")));
    }

    private static Guid Named(RoomCareHarness h, string name)
    {
        var id = Guid.CreateVersion7();
        h.House.Names[id] = name;
        return id;
    }

    private static void At(RoomCareHarness h, int hour, int minute) => h.Clock.Set(RoomCareHarness.Saturday(hour, minute));

    private static async Task WriteAsync(string file, (int Status, JsonElement? Body) answer)
    {
        Assert.True(answer.Status == 200, $"{file} answered {answer.Status}: {answer.Body}");
        var text = JsonSerializer.Serialize(answer.Body, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        await File.WriteAllTextAsync(Path.Combine(Recorded, $"{file}.json"), text + "\n");
    }

    private static async Task<RoomTask> TaskOfAsync(RoomCareHarness h, Guid room)
    {
        await using var db = h.Db();
        return await db.Tasks.Where(t => t.RoomId == room && t.PropertyId == h.PropertyId).OrderByDescending(t => t.CreatedAt).FirstAsync();
    }

    private static async Task StartAsync(RoomCareHarness h, Guid person, Guid room, (int H, int M) at)
    {
        At(h, at.H, at.M);
        await h.Get<AttendantWork>().StartAsync(h.As(person), (await TaskOfAsync(h, room)).Id, default);
    }

    private static async Task WorkAsync(RoomCareHarness h, Guid person, Guid room, (int H, int M) start, (int H, int M) end, string found, bool linen = false, string[]? parts = null)
    {
        await StartAsync(h, person, room, start);
        At(h, end.H, end.M);
        await h.Get<AttendantWork>().AttemptAsync(h.As(person), new AttemptCommand((await TaskOfAsync(h, room)).Id, found) { LinenChanged = linen, PartialDone = parts ?? [] }, default);
    }

    private static Task<int> ObserveAsync(RoomCareHarness h, Guid room, string condition, string occupancy, string[] stays, (int H, int M)? soldAt = null) =>
        h.InScopeAsync(async s =>
        {
            await s.GetRequiredServiceFor<ObservationService>().ObserveAsync(h.Tick, new ObservedFact(room, ObservationSource.Pms, h.Clock.GetUtcNow(), h.Clock.GetUtcNow())
            {
                Condition = condition, Occupancy = occupancy, StayStatuses = stays, SaysNextSold = true, EventId = Guid.CreateVersion7(),
                NextSoldAt = soldAt is { } sold ? RoomCareHarness.Saturday(sold.H, sold.M) : null,
            }, default);
            return await s.GetRequiredServiceFor<Infrastructure.RoomCareDbContext>().SaveChangesAsync();
        });

    private static async Task SeedHouseAsync(RoomCareHarness h, Dictionary<int, Guid> g, Dictionary<int, Guid> l, Guid rekha, Guid meera)
    {
        await using (var db = h.Db())
        {
            var at = RoomCareHarness.Saturday(1, 0);
            db.Policies.Add(new PropertyPolicy
            {
                PropertyId = h.PropertyId, UnsoldDeparture = UnsoldDeparture.MayWait, TurndownEnabled = true, Towels = TowelRule.GreenProgramme,
                Version = 7, ChangedAt = new DateTimeOffset(2026, 9, 1, 16, 40, 0, TimeSpan.FromHours(5.5)), ChangedBy = rekha,
            });
            db.Windows.Add(new ServiceWindow { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, Window = ServiceWindowName.Morning, Starts = new TimeOnly(8, 0), Ends = new TimeOnly(15, 0), AllowAssignmentOutside = true, Version = 3 });
            db.Windows.Add(new ServiceWindow { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, Window = ServiceWindowName.Evening, Starts = new TimeOnly(18, 0), Ends = new TimeOnly(21, 0), Version = 3 });
            foreach (var room in g.Values)
            {
                db.ZoneAssignments.Add(new RoomZoneAssignment { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, RoomId = room, ZoneId = HouseDouble.ZoneOne, EffectiveFrom = new DateOnly(2026, 1, 1) });
            }

            foreach (var room in l.Values)
            {
                db.ZoneAssignments.Add(new RoomZoneAssignment { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, RoomId = room, ZoneId = HouseDouble.ZoneTwo, EffectiveFrom = new DateOnly(2026, 1, 1) });
            }

            db.DeepCleans.Add(new DeepClean
            {
                Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, RoomId = l[14], DueOn = new DateOnly(2026, 9, 1), WindowFrom = new DateOnly(2026, 9, 1), WindowTo = new DateOnly(2026, 9, 5),
                Status = DeepCleanStatus.InProgress, JobId = Guid.CreateVersion7(), JobStatusSeen = "OPEN", BlockCorrelationId = "block:l14", JobCorrelationId = "deep-clean:l14",
                BlockRequestedAt = at.AddDays(-5), CreatedAt = at.AddDays(-5), UpdatedAt = at, Version = 3,
            });
            foreach (var i in Enumerable.Range(1, 12))
            {
                // Last year's deep cleans — five of the garden villas fall due this month, the rest later.
                var done = i <= 5 ? new DateOnly(2025, 9, i + 5) : new DateOnly(2026, 3, i);
                db.DeepCleans.Add(new DeepClean { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, RoomId = g[i], DueOn = done, DoneOn = done, Status = DeepCleanStatus.Done, CreatedAt = at.AddYears(-1), UpdatedAt = at.AddYears(-1), Version = 5 });
            }

            db.DeepCleanPlans.Add(new DeepCleanPlan { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, RoomTypeId = HouseDouble.Standard, EveryMonths = 12, Version = 1 });
            db.DeepCleanPlans.Add(new DeepCleanPlan { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, RoomTypeId = HouseDouble.Suite, EveryMonths = 4, Version = 1 });
            db.AreaSchedules.Add(new AreaSchedule
            {
                Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, LocationId = HouseDouble.Lobby, Minutes = 15, Version = 2,
                Times = [new(6, 0), new(8, 0), new(10, 0), new(12, 0), new(14, 0), new(16, 0), new(18, 0), new(20, 0), new(22, 0)],
            });
            foreach (var (day, window, outcome) in new[] { (3, ServiceWindowName.Morning, TaskOutcome.Declined), (4, ServiceWindowName.Morning, TaskOutcome.Dnd), (4, ServiceWindowName.Evening, TaskOutcome.Dnd) })
            {
                db.Tasks.Add(new RoomTask
                {
                    Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, LocationId = g[6], RoomId = g[6], OperatingDay = new DateOnly(2026, 9, day), Window = window,
                    Service = window == ServiceWindowName.Morning ? Service.DailyService : Service.Turndown, Status = RoomTaskStatus.ClosedByPolicy, Outcome = outcome,
                    CreatedAt = at.AddDays(day - 5), UpdatedAt = at.AddDays(day - 5),
                });
            }

            db.Supervision.Add(new RoomSupervision { Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, RoomId = g[6], OperatingDay = new DateOnly(2026, 9, 5), Reason = SupervisionReason.DaysWithoutService, OpenedAt = at.AddHours(3) });
            await db.SaveChangesAsync();
        }

        foreach (var (i, room) in g)
        {
            await h.SeedStateAsync(room, s =>
            {
                s.Condition = i is 8 ? Condition.Clean : Condition.Dirty;
                if (i is 8 or 9) { s.Occupancy = Occupancy.Vacant; s.StayStatuses = i == 9 ? [StayStatus.CheckedOut] : []; }
                if (i == 8) { s.ConditionSetAt = RoomCareHarness.Saturday(8, 0).AddDays(-4); s.ConditionSource = ConditionSource.Attendant; s.NextSoldAt = RoomCareHarness.Saturday(13, 0); }
                if (i == 2) s.LinenLastChangedOn = new DateOnly(2026, 9, 1);
                if (i == 6) { s.DaysWithoutService = 2; s.SupervisedSince = new DateOnly(2026, 9, 5); s.LinenLastChangedOn = new DateOnly(2026, 9, 1); }
                if (i == 3) { s.Occupancy = Occupancy.Vacant; s.StayStatuses = [StayStatus.CheckedOut]; s.NextSoldAt = RoomCareHarness.Saturday(13, 0); }
                if (i == 10) { s.DaysWithoutService = 1; s.LinenLastChangedOn = new DateOnly(2026, 9, 4); }
            });
        }

        foreach (var (i, room) in l.Where(x => x.Key != 3))
        {
            await h.SeedStateAsync(room, s => { if (i == 14) { s.Occupancy = Occupancy.Vacant; s.StayStatuses = []; } s.LinenLastChangedOn = new DateOnly(2026, 9, 4); });
        }

        await h.SeedStateAsync(l[3], s => s.Condition = Condition.Dirty);
        At(h, 1, 0);
        var gm = Named(h, "Vikram Shah");
        var arjun = Named(h, "Arjun Mehta");
        h.Clock.Set(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(5.5)));
        await h.Get<ManagerGrants>().GrantAsync(h.As(gm), arjun, default);
        _ = meera;
    }
}
