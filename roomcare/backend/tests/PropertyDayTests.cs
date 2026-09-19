using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Module.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>
/// "Vacant for N days" counts the property's days, not UTC days (owner instruction, 2026-09-19: every app works in
/// the property's time zone). The day a room's condition was set is its business day at the property — the same rule
/// as the day it is counted from — never the UTC date of the instant.
/// </summary>
/// <remarks>
/// Every instant here is chosen so its UTC date and its business day at the property differ, in both directions:
/// Kolkata (+05:30) where the UTC date is a day earlier and the count came out one too many, and Guatemala (−06:00,
/// no daylight saving) where the UTC date is a day later and it came out one too few. The refresh rule is the
/// property default, three days; each count sits on either side of it, so a wrong day changes the decision and not
/// only a number. Workforce's b5c5ffc is the reference for the shape.
/// </remarks>
[Collection(RoomCareCollection.Name)]
public sealed class PropertyDayTests(RoomCareFixture fixture)
{
    private static readonly TimeSpan Kolkata = TimeSpan.FromHours(5.5);
    private static readonly TimeSpan Guatemala = TimeSpan.FromHours(-6);

    [Fact]
    public async Task A_room_clean_since_the_18th_at_Kolkata_is_vacant_two_days_on_the_20th_and_is_not_refreshed()
    {
        // Set at 05:00 on the 18th at the property — 23:30 on the 17th in UTC.
        var (count, refreshed) = await DecideAsync("Asia/Kolkata", new(2026, 9, 20, 10, 0, 0, Kolkata), new(2026, 9, 18, 5, 0, 0, Kolkata));
        Assert.Equal((2, false), (count, refreshed));
    }

    [Fact]
    public async Task A_room_clean_since_the_17th_at_Guatemala_is_vacant_three_days_on_the_20th_and_is_refreshed()
    {
        // Set at 20:00 on the 17th at the property — 02:00 on the 18th in UTC.
        var (count, refreshed) = await DecideAsync("America/Guatemala", new(2026, 9, 20, 10, 0, 0, Guatemala), new(2026, 9, 17, 20, 0, 0, Guatemala));
        Assert.Equal((3, true), (count, refreshed));
    }

    /// <summary>The board's "vacant · N days", and whether the first press decides a refresh, for one clean vacant room.</summary>
    private async Task<(int? Count, bool Refreshed)> DecideAsync(string timezone, DateTimeOffset now, DateTimeOffset conditionSetAt)
    {
        var h = new RoomCareHarness(fixture, now);
        h.House.Settings = h.House.Settings! with { Timezone = timezone };
        var room = h.House.Room("G03");
        await h.SeedStateAsync(room, s =>
        {
            s.Condition = Condition.Clean; s.Occupancy = Occupancy.Vacant; s.StayStatuses = []; s.NextSoldAt = null;
            s.ConditionSetAt = conditionSetAt;
        });

        var board = await h.InScopeAsync(p => p.GetRequiredService<BoardProjection>().BoardAsync(h.As(Guid.CreateVersion7()), default));
        var count = board.Zones.SelectMany(z => z.Rooms).Single(r => r.Number == "G03").VacantDays;

        await h.Get<PrepareService>().PressAsync(h.As(Guid.CreateVersion7()), null, default);
        await using var db = h.Db();
        var refreshed = await db.Tasks.AnyAsync(t => t.PropertyId == h.PropertyId && t.RoomId == room && t.Service == Service.Refresh);
        return (count, refreshed);
    }
}
