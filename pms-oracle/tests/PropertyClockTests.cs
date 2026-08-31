using PmsOracle.Normalisation;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The property clock: a zone that must be a real zone, and dates completed
/// with the property's own times.
/// </summary>
public sealed class PropertyClockTests
{
    private static readonly TimeOnly CheckIn = new(14, 0);
    private static readonly TimeOnly CheckOut = new(12, 0);

    private static PropertyClock Kochi() =>
        PropertyClock.For("Asia/Kolkata", CheckIn, CheckOut)!;

    [Fact]
    public void an_arrival_date_is_completed_with_the_property_check_in_time()
    {
        var arrival = Kochi().ArrivalOn(new DateOnly(2026, 8, 31));

        Assert.Equal(new DateTime(2026, 8, 31, 14, 0, 0), arrival.DateTime);
        Assert.Equal(TimeSpan.FromHours(5.5), arrival.Offset);
    }

    [Fact]
    public void a_departure_date_is_completed_with_the_property_check_out_time()
    {
        var departure = Kochi().DepartureOn(new DateOnly(2026, 9, 2));

        Assert.Equal(new DateTime(2026, 9, 2, 12, 0, 0), departure.DateTime);
    }

    /// <summary>
    /// R16. A UTC offset cannot express daylight saving, so accepting one would
    /// be wrong for half the year in any property that observes it.
    /// </summary>
    [Theory]
    [InlineData("+05:30")]
    [InlineData("-08:00")]
    public void a_utc_offset_is_not_a_zone_and_is_refused(string offset)
    {
        Assert.Null(PropertyClock.For(offset, CheckIn, CheckOut));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Not/AZone")]
    public void an_absent_or_unknown_zone_yields_no_clock(string zone)
    {
        Assert.Null(PropertyClock.For(zone, CheckIn, CheckOut));
    }

    /// <summary>
    /// The reason the zone is required rather than defaulted: two properties
    /// with the same local check-in time are different moments, and the
    /// reference's silent Asia/Kolkata fallback made every one of them look
    /// like the Indian one.
    /// </summary>
    [Fact]
    public void the_same_date_in_two_properties_is_two_different_moments()
    {
        var kochi = PropertyClock.For("Asia/Kolkata", CheckIn, CheckOut)!;
        var newYork = PropertyClock.For("America/New_York", CheckIn, CheckOut)!;

        var date = new DateOnly(2026, 8, 31);

        Assert.NotEqual(
            kochi.ArrivalOn(date).ToUniversalTime(),
            newYork.ArrivalOn(date).ToUniversalTime());
    }

    /// <summary>
    /// The offset comes from the zone's rules on that date, not from today —
    /// which is the whole reason an IANA zone is required.
    /// </summary>
    [Fact]
    public void a_zone_that_observes_daylight_saving_gives_the_offset_of_the_day()
    {
        var newYork = PropertyClock.For("America/New_York", CheckIn, CheckOut)!;

        var summer = newYork.ArrivalOn(new DateOnly(2026, 7, 1));
        var winter = newYork.ArrivalOn(new DateOnly(2026, 12, 1));

        Assert.Equal(TimeSpan.FromHours(-4), summer.Offset);
        Assert.Equal(TimeSpan.FromHours(-5), winter.Offset);
        Assert.NotEqual(summer.Offset, winter.Offset);
    }
}
