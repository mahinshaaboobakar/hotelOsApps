using System.Globalization;
using System.Text.Json;
using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Today at the Desk's read sends what the widget draws.
/// </summary>
/// <remarks>
/// The widget read <c>today</c> and expected five fields that view never sent;
/// only the harness fixture had them. These assert the widget's own names on the
/// serialised answer, because JSON is what crosses the envelope. Stays hold no
/// room here: the scratch database has no <c>masterdata</c> schema, and
/// <c>StayLabels</c> sends no room query for a set with no rooms.
/// </remarks>
public sealed class DeskViewTests
{
    private static readonly DateOnly Day = new(2026, 9, 1);

    private static readonly DateTimeOffset Start = new(2026, 9, 1, 6, 0, 0, TimeSpan.Zero);

    private static JsonElement Wire(object? answer) => JsonSerializer.SerializeToElement(answer);

    private static async Task SeedAsync(
        DeskHarness harness,
        StayLifecycle lifecycle,
        DateOnly? businessDate,
        DateTimeOffset? arrival = null,
        DateTimeOffset? departure = null)
    {
        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            PropertyId = DeskHarness.Property,
            Origin = RecordOrigin.Staff,
            CreatedAt = harness.Clock.GetUtcNow(),
            Version = 1,
        };

        harness.Db.Bookings.Add(booking);
        harness.Db.Stays.Add(new RoomStay
        {
            Id = Guid.CreateVersion7(),
            BookingId = booking.Id,
            PropertyId = DeskHarness.Property,
            RoomTypeId = DeskHarness.RoomType,
            Lifecycle = lifecycle,
            BusinessDate = businessDate,
            ArrivalAt = arrival is { } a ? StayTime.Observed(a) : StayTime.None,
            DepartureAt = departure is { } d ? StayTime.Observed(d) : StayTime.None,
            Origin = RecordOrigin.Staff,
            CreatedAt = harness.Clock.GetUtcNow(),
            Version = 1,
        });

        await harness.Db.SaveChangesAsync();
    }

    private static DeskView View(DeskHarness harness, DateOnly? day, DayBounds? bounds)
    {
        var business = new StubBusinessDay(day, bounds);
        return new DeskView(
            new StayListService(harness.Db, harness.Authorizer, business),
            new StayLabels(harness.Db),
            business);
    }

    [Fact]
    public async Task Counts_are_disjoint_and_the_next_arrivals_are_the_ones_still_due()
    {
        await using var harness = await DeskHarness.CreateAsync();

        // Today's arrivals: two still due, one arrived, one arrived and gone.
        await SeedAsync(harness, StayLifecycle.Booked, Day, arrival: Start.AddHours(9));
        await SeedAsync(harness, StayLifecycle.Booked, Day, arrival: Start.AddHours(8));
        await SeedAsync(harness, StayLifecycle.InHouse, Day, arrival: Start.AddHours(2));
        await SeedAsync(harness, StayLifecycle.Departed, Day, arrival: Start.AddHours(1),
            departure: Start.AddHours(3));

        // A departure today by a guest who arrived three days ago: still in house.
        await SeedAsync(harness, StayLifecycle.InHouse, new DateOnly(2026, 8, 29),
            arrival: Start.AddDays(-3), departure: Start.AddHours(5));

        var answer = Wire(await View(harness, Day, new DayBounds(Start, Start.AddDays(1)))
            .AnswerAsync(harness.Scope(), CancellationToken.None));

        Assert.Equal(2, answer.GetProperty("dueIn").GetInt32());
        Assert.Equal(2, answer.GetProperty("arrived").GetInt32());
        // One: the stay that arrived today has no departure time, so it is not
        // today's departure — only the one leaving at Start+5h is.
        Assert.Equal(1, answer.GetProperty("dueOut").GetInt32());
        Assert.Equal(1, answer.GetProperty("departed").GetInt32());

        var arrivals = answer.GetProperty("arrivals").EnumerateArray().ToList();
        Assert.Equal(2, arrivals.Count);
        Assert.Equal(
            Start.AddHours(8),
            DateTimeOffset.ParseExact(arrivals[0].GetProperty("at").GetString()!, "O",
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        Assert.Equal(StayLabels.Unnamed, arrivals[0].GetProperty("guest").GetString());
        Assert.Equal(JsonValueKind.Null, arrivals[0].GetProperty("room").ValueKind);
    }

    [Fact]
    public async Task Without_the_days_bounds_departures_are_unknown_not_zero()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await SeedAsync(harness, StayLifecycle.Booked, Day, arrival: Start.AddHours(9));

        var answer = Wire(await View(harness, Day, bounds: null)
            .AnswerAsync(harness.Scope(), CancellationToken.None));

        Assert.Equal(1, answer.GetProperty("dueIn").GetInt32());
        Assert.Equal(JsonValueKind.Null, answer.GetProperty("dueOut").ValueKind);
        Assert.Equal(JsonValueKind.Null, answer.GetProperty("departed").ValueKind);
    }

    [Fact]
    public async Task Without_a_business_day_nothing_is_counted()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await SeedAsync(harness, StayLifecycle.Booked, Day, arrival: Start.AddHours(9));

        var answer = Wire(await View(harness, day: null, bounds: null)
            .AnswerAsync(harness.Scope(), CancellationToken.None));

        foreach (var field in new[] { "dueIn", "arrived", "dueOut", "departed" })
        {
            Assert.Equal(JsonValueKind.Null, answer.GetProperty(field).ValueKind);
        }

        Assert.Empty(answer.GetProperty("arrivals").EnumerateArray());
    }
}
