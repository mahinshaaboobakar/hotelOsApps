using HotelOS.GuestOps.Domain;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// A stay's day is the property's day, in Kolkata and in Guatemala — ADR 0174.
/// </summary>
/// <remarks>
/// <para>
/// <b>The instants are at offset zero, as PostgreSQL returns them.</b> That is
/// the case the old <c>StayTime.Date</c> got wrong: it read the instant's own
/// offset, which after a round trip is UTC's. A test built with the property's
/// offset in memory would have passed against it — the fixture-agreement shape.
/// </para>
/// <para>
/// Kolkata is 05:30 ahead and Guatemala 06:00 behind, so each catches the
/// direction the other cannot: an arrival just after midnight in Kolkata is the
/// previous UTC day; an arrival after 18:00 in Guatemala is the next.
/// </para>
/// </remarks>
public sealed class PropertyClockTests
{
    private static TimeZoneInfo Zone(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);

    [Theory]
    // 00:30 on 1 Sep in Kolkata is 19:00 UTC on 31 Aug.
    [InlineData("Asia/Kolkata", "2026-08-31T19:00:00Z", "2026-09-01")]
    // 19:00 on 1 Sep in Guatemala is 01:00 UTC on 2 Sep.
    [InlineData("America/Guatemala", "2026-09-02T01:00:00Z", "2026-09-01")]
    public void A_stored_instant_falls_on_the_propertys_day(string zone, string storedUtc, string day)
    {
        var stored = DateTimeOffset.Parse(storedUtc, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(TimeSpan.Zero, stored.Offset);

        Assert.Equal(DateOnly.Parse(day), new StayTime(stored, TimeBasis.Observed).DateIn(Zone(zone)));
        Assert.Equal(DateOnly.Parse(day), PropertyClock.Day(stored, Zone(zone)));
    }

    [Theory]
    [InlineData("Asia/Kolkata", "2026-09-01T08:30:00Z")]
    [InlineData("America/Guatemala", "2026-09-01T20:00:00Z")]
    public void A_wall_clock_time_is_an_instant_through_the_zone(string zone, string instant)
        => Assert.Equal(
            DateTimeOffset.Parse(instant, System.Globalization.CultureInfo.InvariantCulture),
            PropertyClock.Instant(Zone(zone), new DateOnly(2026, 9, 1), new TimeOnly(14, 0)));

    [Fact]
    public void No_zone_is_no_day_never_a_utc_one()
        => Assert.Null(new StayTime(DateTimeOffset.UnixEpoch, TimeBasis.Observed).DateIn(null));
}
