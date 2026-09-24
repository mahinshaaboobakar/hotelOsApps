using HotelOS.GuestOps.Domain;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// When a stay may be offered as one that never arrived — the owner's N2
/// caption, ruled 2026-09-24.
/// </summary>
/// <remarks>
/// <b>This decides whether a button that forfeits a night appears.</b> It is
/// pure, so it is tested over values rather than through a screen, and
/// exhaustively enough that the boundary is argued with rather than
/// remembered.
/// </remarks>
public sealed class NoShowRuleTests
{
    /// <summary>
    /// The day after the arrival, the action appears.
    /// </summary>
    [Fact]
    public void An_arrival_day_that_has_passed_may_be_offered()
        => Assert.True(NoShowRule.DayHasPassed(
            arrival: new DateOnly(2026, 8, 31), today: new DateOnly(2026, 9, 1)));

    /// <summary>
    /// A stay arriving today is not a no-show at four in the afternoon.
    /// </summary>
    /// <remarks>
    /// <b>The owner's own sentence, and the whole rule.</b> The guest has
    /// until the day is over. An action offered on today's row is one somebody
    /// presses by mistake, and the mistake writes a forfeited night onto a
    /// guest who is about to walk in.
    /// </remarks>
    [Fact]
    public void A_stay_arriving_today_may_not_be()
        => Assert.False(NoShowRule.DayHasPassed(
            arrival: new DateOnly(2026, 9, 1), today: new DateOnly(2026, 9, 1)));

    /// <summary>A stay arriving tomorrow certainly may not.</summary>
    [Fact]
    public void A_stay_arriving_later_may_not_be()
        => Assert.False(NoShowRule.DayHasPassed(
            arrival: new DateOnly(2026, 9, 3), today: new DateOnly(2026, 9, 1)));

    /// <summary>
    /// Neither absence is read as today, and they are different absences.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An arrival with no established date is a stay nobody can call late; an
    /// absent business day is a property whose operating day the Context
    /// Service could not derive. <b>Neither is an argument for offering the
    /// action</b>, and a default to today would be a third claim nobody made —
    /// the claim being <i>this guest did not come</i>.
    /// </para>
    /// <para>
    /// Both directions are asserted rather than one, because a rule that
    /// short-circuits on the first null would pass a test that only supplied
    /// the first.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(null, "2026-09-01")]
    [InlineData("2026-08-31", null)]
    [InlineData(null, null)]
    public void An_unestablished_date_is_never_read_as_today(string? arrival, string? today)
        => Assert.False(NoShowRule.DayHasPassed(Day(arrival), Day(today)));

    /// <summary>
    /// The dates are chosen so the two candidate rules disagree.
    /// </summary>
    /// <remarks>
    /// <b>The arrival anchor and the departure anchor give different answers
    /// here, which is the point.</b> A one-night stay on the 31st departing on
    /// 1 September is PAST by its arrival and NOT past by its departure, so a
    /// fixture using it can tell the two rules apart — and the drawing's own
    /// caption says the arrival day is the anchor. The frames disagreed about
    /// this until 2026-09-24, when one row measured from the arrival and
    /// another from the departure.
    /// </remarks>
    [Fact]
    public void The_anchor_is_the_arrival_and_not_the_departure()
    {
        var arrival = new DateOnly(2026, 8, 31);
        var departure = new DateOnly(2026, 9, 1);
        var today = new DateOnly(2026, 9, 1);

        Assert.True(NoShowRule.DayHasPassed(arrival, today));

        // The same stay, asked the other way round, answers differently — so a
        // green above is evidence about the anchor and not a coincidence.
        Assert.False(NoShowRule.DayHasPassed(departure, today));
    }

    private static DateOnly? Day(string? text)
        => text is null ? null : DateOnly.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
}
