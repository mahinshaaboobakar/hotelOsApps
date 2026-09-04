using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The four lists, and the paging a numbered pager needs — <c>CORE-Q13</c>.
/// </summary>
/// <remarks>
/// <para>
/// The pair is what makes the pager honest: a page bounded by its size, and a
/// total that counts the whole list. Asserting only the page would pass against
/// a service that ignored the page number entirely, which is exactly the defect
/// CORE-Q13 found in Master Data — <i>"a caller asking for page two gets page
/// one"</i>, silent and indistinguishable from a correct answer on a short list.
/// </para>
/// <para>
/// The business day is a double, not a derivation. This application computes no
/// operating day, so a test that computed one would be asserting against a
/// second implementation of the thing the port exists to keep out.
/// </para>
/// </remarks>
public class StayListTests
{
    private static readonly DateOnly Day = new(2026, 8, 31);

    private static StayListService Service(DeskHarness harness, DayBounds? bounds = null)
        => new(harness.Db, harness.Authorizer, new StubBusinessDay(Day, bounds));

    /// <summary>The booking every stay in these tests belongs to.</summary>
    /// <remarks>
    /// A real row rather than an id: a stay's booking is a foreign key, and a
    /// test that invented one would fail on the constraint rather than on the
    /// thing it is about. One booking is enough — none of these tests is about
    /// how stays are grouped.
    /// </remarks>
    private static async Task<Guid> BookingAsync(DeskHarness harness)
    {
        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            PropertyId = DeskHarness.Property,
            Origin = RecordOrigin.Staff,
            CreatedAt = harness.Clock.GetUtcNow(),
        };

        harness.Db.Bookings.Add(booking);
        await harness.Db.SaveChangesAsync();

        return booking.Id;
    }

    /// <summary>A stay expected on the day, with an arrival instant of its own.</summary>
    private static RoomStay Arriving(
        Guid bookingId, int hour, StayLifecycle lifecycle = StayLifecycle.Booked)
        => new()
        {
            Id = Guid.CreateVersion7(),
            BookingId = bookingId,
            PropertyId = DeskHarness.Property,
            RoomTypeId = DeskHarness.RoomType,
            Lifecycle = lifecycle,
            BusinessDate = Day,
            ArrivalAt = StayTime.Observed(
                new DateTimeOffset(Day.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero)),
        };

    [Fact]
    public async Task A_page_is_bounded_and_the_total_is_not()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var booking = await BookingAsync(harness);
        harness.Db.Stays.AddRange(
            Arriving(booking, 9), Arriving(booking, 10), Arriving(booking, 11));
        await harness.Db.SaveChangesAsync();

        var page = await Service(harness).ListAsync(
            harness.Scope(),
            new StayQuery(StayView.Arrivals, Day, new Paging.Window(0, 2)),
            CancellationToken.None);

        Assert.Equal(2, page.Rows.Count);
        Assert.Equal(3, page.Total);
    }

    /// <summary>The defect CORE-Q13 named, asserted directly.</summary>
    /// <remarks>
    /// Page two must be different stays, not the same ones again. Compared by
    /// id rather than by count, because two pages of the same size are equally
    /// plausible whether or not the page number was read.
    /// </remarks>
    [Fact]
    public async Task The_second_page_is_not_the_first_page()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var booking = await BookingAsync(harness);
        harness.Db.Stays.AddRange(
            Arriving(booking, 9), Arriving(booking, 10),
            Arriving(booking, 11), Arriving(booking, 12));
        await harness.Db.SaveChangesAsync();

        var service = Service(harness);
        var scope = harness.Scope();

        var first = await service.ListAsync(
            scope, new StayQuery(StayView.Arrivals, Day, new Paging.Window(0, 2)),
            CancellationToken.None);

        var second = await service.ListAsync(
            scope, new StayQuery(StayView.Arrivals, Day, new Paging.Window(1, 2)),
            CancellationToken.None);

        Assert.Empty(first.Rows.Select(s => s.Id).Intersect(second.Rows.Select(s => s.Id)));
        Assert.Equal(4, first.Total);
        Assert.Equal(first.Total, second.Total);
    }

    /// <summary>Reading a list is asked of the property.</summary>
    [Fact]
    public async Task Listing_is_asked_of_the_property()
    {
        await using var harness = await DeskHarness.CreateAsync();

        await Service(harness).ListAsync(
            harness.Scope(),
            new StayQuery(StayView.Arrivals, Day, new Paging.Window(0, 25)),
            CancellationToken.None);

        // The recorder keeps permission names and not the object they were
        // asked about, so this asserts the permission and says nothing about
        // the resource — an assertion the double cannot support would be a
        // test that only looked like coverage.
        Assert.Equal(
            Permissions.ReservationRead,
            Assert.Single(harness.Authorizer.Permissions));
    }

    /// <summary>Cancelled and no-show stays stay in the arrivals list.</summary>
    /// <remarks>
    /// They were expected on the day and the desk is accountable for both — a
    /// cancelled reservation exists and a no-show is reportable (ADR 0062).
    /// Filtering them out would make the list disagree with the count beside it.
    /// </remarks>
    [Fact]
    public async Task Arrivals_keep_the_cancelled_and_the_no_show()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var booking = await BookingAsync(harness);
        harness.Db.Stays.AddRange(
            Arriving(booking, 9, StayLifecycle.Booked),
            Arriving(booking, 10, StayLifecycle.Cancelled),
            Arriving(booking, 11, StayLifecycle.NoShow));
        await harness.Db.SaveChangesAsync();

        var page = await Service(harness).ListAsync(
            harness.Scope(),
            new StayQuery(StayView.Arrivals, Day, new Paging.Window(0, 25)),
            CancellationToken.None);

        Assert.Equal(3, page.Total);
    }

    /// <summary>A property whose boundary Context cannot answer gets nothing.</summary>
    /// <remarks>
    /// Not a whole-day guess. A departure is stored only as an instant, so
    /// without the operating day's bounds there is no honest filter — and a
    /// guessed window is wrong by a day near midnight while looking like
    /// correct data, which is the failure <c>IBusinessDay</c> warns about.
    /// </remarks>
    [Fact]
    public async Task Departures_without_a_known_boundary_are_empty_rather_than_guessed()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var booking = await BookingAsync(harness);
        harness.Db.Stays.Add(Arriving(booking, 9, StayLifecycle.InHouse));
        await harness.Db.SaveChangesAsync();

        var page = await Service(harness, bounds: null).ListAsync(
            harness.Scope(),
            new StayQuery(StayView.Departures, Day, new Paging.Window(0, 25)),
            CancellationToken.None);

        Assert.Empty(page.Rows);
        Assert.Equal(0, page.Total);
    }

    /// <summary>With bounds, a departure inside the day is listed.</summary>
    [Fact]
    public async Task Departures_inside_the_day_are_listed()
    {
        await using var harness = await DeskHarness.CreateAsync();

        var start = new DateTimeOffset(Day.ToDateTime(new TimeOnly(4, 0)), TimeSpan.Zero);
        var bounds = new DayBounds(start, start.AddDays(1));

        var booking = await BookingAsync(harness);

        var leaving = Arriving(booking, 9, StayLifecycle.Departed);
        leaving.DepartureAt = StayTime.Observed(start.AddHours(6));

        var staying = Arriving(booking, 10, StayLifecycle.InHouse);
        staying.DepartureAt = StayTime.Observed(start.AddDays(3));

        harness.Db.Stays.AddRange(leaving, staying);
        await harness.Db.SaveChangesAsync();

        var page = await Service(harness, bounds).ListAsync(
            harness.Scope(),
            new StayQuery(StayView.Departures, Day, new Paging.Window(0, 25)),
            CancellationToken.None);

        Assert.Equal(leaving.Id, Assert.Single(page.Rows).Id);
    }
}
