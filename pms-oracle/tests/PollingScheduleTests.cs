using PmsOracle.Authentication;
using PmsOracle.Normalisation;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The two-tier poll frame 3 draws — three hours ordinarily, fifteen minutes
/// around check-in, in the property's own zone.
/// </summary>
/// <remarks>
/// The values are written out rather than read from the constants under test.
/// A test asserting on the constant its subject imports is a tautology, and
/// this one guards a number that was wrong by a factor of 360 until this round.
/// </remarks>
public sealed class PollingScheduleTests
{
    private static PropertyClock Kochi() =>
        PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!;

    private static Dictionary<string, string> Configured() => new()
    {
        ["pollNormalSeconds"] = "10800",
        ["pollTightSeconds"] = "900",
        ["pollTightFrom"] = "14:00",
        ["pollTightUntil"] = "16:00",
    };

    /// <summary>An unconfigured property polls at the drawn defaults.</summary>
    /// <remarks>
    /// **The number that acts, asserted where it acts.** A form hint is a
    /// placeholder; this is what a property that never touched the field
    /// actually runs at, and the previous constant was thirty seconds.
    /// </remarks>
    [Fact]
    public void Nothing_configured_polls_every_three_hours()
    {
        var schedule = OhipPollingSchedule.Read(new Dictionary<string, string>());

        Assert.Equal(TimeSpan.FromHours(3), schedule.Normal);
        Assert.Equal(TimeSpan.FromMinutes(15), schedule.Tight);

        // No window configured means no tighter tier — never a window that
        // happens to be empty, which would poll tight at midnight.
        Assert.False(schedule.Covers(new TimeOnly(14, 30)));
    }

    [Theory]
    [InlineData(13, 59, false)]
    [InlineData(14, 0, true)]
    [InlineData(15, 30, true)]
    [InlineData(16, 0, false)]
    public void The_window_includes_its_start_and_excludes_its_end(
        int hour, int minute, bool tighter)
    {
        Assert.Equal(
            tighter,
            OhipPollingSchedule.Read(Configured()).Covers(new TimeOnly(hour, minute)));
    }

    /// <summary>
    /// A window that wraps midnight is honoured, not silently emptied.
    /// </summary>
    /// <remarks>
    /// 22:00–02:00 is a real arrival pattern for a property near an airport. A
    /// naive `from &lt;= t &amp;&amp; t &lt; until` treats it as covering
    /// nothing, so the connector would poll slowly through exactly the hours it
    /// was told to watch — and nothing would look wrong.
    /// </remarks>
    [Theory]
    [InlineData(21, 59, false)]
    [InlineData(22, 0, true)]
    [InlineData(23, 30, true)]
    [InlineData(0, 30, true)]
    [InlineData(1, 59, true)]
    [InlineData(2, 0, false)]
    public void A_window_across_midnight_covers_both_sides(int hour, int minute, bool tighter)
    {
        var settings = Configured();
        settings["pollTightFrom"] = "22:00";
        settings["pollTightUntil"] = "02:00";

        Assert.Equal(
            tighter,
            OhipPollingSchedule.Read(settings).Covers(new TimeOnly(hour, minute)));
    }

    /// <summary>The window is read in the property's zone, never the server's.</summary>
    /// <remarks>
    /// The instant below is 09:00 UTC, which is 14:30 in Kochi — inside the
    /// window. A schedule that compared UTC would poll at three hours through
    /// the property's whole arrival peak, and would be wrong by a different
    /// amount in every property. R16.
    /// </remarks>
    [Fact]
    public void The_window_is_read_in_the_propertys_zone()
    {
        var at = new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.Zero);

        Assert.Equal(
            TimeSpan.FromMinutes(15),
            OhipPollingSchedule.Read(Configured()).Wait(Kochi(), at));
    }

    [Fact]
    public void Outside_the_window_the_normal_interval_applies()
    {
        // 01:00 UTC is 06:30 in Kochi — long before check-in.
        var at = new DateTimeOffset(2026, 9, 2, 1, 0, 0, TimeSpan.Zero);

        Assert.Equal(
            TimeSpan.FromHours(3),
            OhipPollingSchedule.Read(Configured()).Wait(Kochi(), at));
    }

    /// <summary>A typo polls at the default rather than stopping the connector.</summary>
    /// <remarks>
    /// A schedule is not a credential. An unreadable interval should poll at
    /// the default and be visible on the next screen; refusing would stop a
    /// hotel's reservations arriving over a mistyped number.
    /// </remarks>
    [Theory]
    [InlineData("")]
    [InlineData("every three hours")]
    [InlineData("0")]
    [InlineData("-60")]
    public void An_unreadable_interval_falls_back(string raw)
    {
        var settings = Configured();
        settings["pollNormalSeconds"] = raw;

        Assert.Equal(TimeSpan.FromHours(3), OhipPollingSchedule.Read(settings).Normal);
    }

    [Fact]
    public void A_configured_interval_is_used()
    {
        var settings = Configured();
        settings["pollNormalSeconds"] = "1800";

        // The negative control for the fallback tests above: without it they
        // pass on a schedule that ignores configuration entirely.
        Assert.Equal(TimeSpan.FromMinutes(30), OhipPollingSchedule.Read(settings).Normal);
    }
}
