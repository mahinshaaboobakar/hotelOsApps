using HotelOS.GuestOps.Domain;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Whether a stay must be filed, and by when — S19b.
/// </summary>
public class ReportingRuleTests
{
    /// <summary>A property with no obligation never sees the flag.</summary>
    /// <remarks>
    /// <b>Off is a real answer.</b> The obligation is a property policy, never a
    /// country's law compiled into the product, so a property that files with
    /// nobody must be able to say so — and a build that assumed otherwise would
    /// put an unclearable task on every stay it sold.
    /// </remarks>
    [Theory]
    [InlineData("IN")]
    [InlineData("GB")]
    [InlineData(null)]
    public void Reporting_switched_off_needs_nothing_of_anyone(string? nationality)
    {
        var settings = Settings(required: false);

        Assert.Equal(
            ReportingState.NotRequired, ReportingRule.StateFor(settings, nationality));
    }

    /// <summary>The from-outside policy follows the property's home country.</summary>
    [Theory]
    [InlineData("IN", ReportingState.NotRequired)]
    [InlineData("GB", ReportingState.Needed)]
    public void A_from_outside_policy_covers_only_visitors(
        string nationality, ReportingState expected)
        => Assert.Equal(expected, ReportingRule.StateFor(Settings(), nationality));

    /// <summary>An every-guest policy covers the property's own nationals too.</summary>
    [Theory]
    [InlineData("IN")]
    [InlineData("GB")]
    public void An_every_guest_policy_covers_everyone(string nationality)
    {
        var settings = Settings();
        settings.ReportingAppliesTo = ReportingScope.EveryGuest;

        Assert.Equal(ReportingState.Needed, ReportingRule.StateFor(settings, nationality));
    }

    /// <summary>
    /// An uncaptured nationality does not create an obligation under a
    /// from-outside policy.
    /// </summary>
    /// <remarks>
    /// It is the same reading as the registration rule's: a blank field is an
    /// incomplete card, not a foreign guest. The obligation appears when the
    /// nationality does — which is why the service recomputes on every capture.
    /// </remarks>
    [Fact]
    public void An_unknown_nationality_raises_no_obligation_yet()
        => Assert.Equal(
            ReportingState.NotRequired, ReportingRule.StateFor(Settings(), nationality: null));

    /// <summary>The deadline is the offset applied to the arrival — R18.</summary>
    [Fact]
    public void The_deadline_is_computed_from_the_arrival_and_the_offset()
    {
        var arrival = new StayTime(
            new DateTimeOffset(2026, 9, 1, 22, 0, 0, TimeSpan.Zero), TimeBasis.Observed);

        Assert.Equal(new DateOnly(2026, 9, 2), ReportingRule.DueBy(arrival, 24));
    }

    /// <summary>
    /// Moving the arrival moves the deadline — which a stored date could not do.
    /// </summary>
    /// <remarks>
    /// R18's whole reason. *"Within 24 hours of arrival"* survives the arrival
    /// changing; a date captured at booking keeps pointing at the old one, and
    /// it does so silently.
    /// </remarks>
    [Fact]
    public void The_deadline_follows_the_arrival_when_it_moves()
    {
        var first = new StayTime(
            new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero), TimeBasis.Derived);

        var moved = new StayTime(
            new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero), TimeBasis.Observed);

        Assert.Equal(new DateOnly(2026, 9, 2), ReportingRule.DueBy(first, 24));
        Assert.Equal(new DateOnly(2026, 9, 4), ReportingRule.DueBy(moved, 24));
    }

    /// <summary>No arrival means no deadline, and never today's date — R25.</summary>
    /// <remarks>
    /// A fabricated deadline would put a stay on the overdue list for a night
    /// that has not happened. An absence is neither dropped nor invented.
    /// </remarks>
    [Fact]
    public void An_unknown_arrival_produces_no_deadline()
        => Assert.Null(ReportingRule.DueBy(StayTime.None, 24));

    /// <summary>A property's own offset is used, not a default.</summary>
    [Theory]
    [InlineData(12, "2026-09-01")]
    [InlineData(24, "2026-09-02")]
    [InlineData(72, "2026-09-04")]
    public void The_offset_is_the_propertys(int hours, string expected)
    {
        var arrival = new StayTime(
            new DateTimeOffset(2026, 9, 1, 6, 0, 0, TimeSpan.Zero), TimeBasis.Observed);

        Assert.Equal(DateOnly.Parse(expected), ReportingRule.DueBy(arrival, hours));
    }

    private static GuestOpsSettings Settings(bool required = true) => new()
    {
        HomeCountry = "IN",
        ReportingRequired = required,
        ReportingAppliesTo = ReportingScope.FromOutside,
        ReportingDueHours = 24,
    };
}
