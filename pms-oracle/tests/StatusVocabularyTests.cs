using HotelOS.Contracts.Integration.V1;
using PmsOracle.Vocabularies;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The status vocabularies: what they recognise, and — the part that matters —
/// what they do with a value they do not.
/// </summary>
public sealed class StatusVocabularyTests
{
    [Theory]
    [InlineData("Reserved", StayLifecycle.Booked)]
    [InlineData("InHouse", StayLifecycle.CheckedIn)]
    [InlineData("CheckedOut", StayLifecycle.CheckedOut)]
    [InlineData("Cancelled", StayLifecycle.Cancelled)]
    [InlineData("NoShow", StayLifecycle.NoShow)]
    public void ohip_status_reads_to_its_declared_meaning(string source, StayLifecycle expected)
    {
        var reading = CloudStayStatus.Read(source);

        Assert.True(reading.TryGet(out var meaning));
        Assert.Equal(expected, meaning);
    }

    /// <summary>
    /// The defect this connector exists not to repeat: an unrecognised status
    /// keeps its value instead of becoming null.
    /// </summary>
    [Fact]
    public void an_unrecognised_ohip_status_carries_the_value_it_could_not_read()
    {
        var reading = CloudStayStatus.Read("Waitlisted");

        Assert.False(reading.Recognised);
        Assert.False(reading.TryGet(out _));
        Assert.Equal("Waitlisted", reading.UnrecognisedValue);
    }

    [Fact]
    public void an_unrecognised_on_site_status_carries_the_value_it_could_not_read()
    {
        // The exact value the mockup's Operations Center frame shows rejected.
        var reading = OnSiteStayStatus.Read("NO SHOW");

        Assert.False(reading.Recognised);
        Assert.Equal("NO SHOW", reading.UnrecognisedValue);
    }

    /// <summary>
    /// Requirement R6. The casing is the message part, so folding it would
    /// silently merge two different messages into one.
    /// </summary>
    [Fact]
    public void the_two_casings_of_checked_in_are_two_halves_of_one_check_in()
    {
        Assert.True(OnSiteStayStatus.Read("Checked In").TryGet(out var contactHalf));
        Assert.True(OnSiteStayStatus.Read("CHECKED IN").TryGet(out var roomHalf));

        Assert.Equal(StayLifecycle.CheckedIn, contactHalf.Lifecycle);
        Assert.Equal(StayLifecycle.CheckedIn, roomHalf.Lifecycle);

        Assert.Equal(OnSiteMessagePart.ContactHalf, contactHalf.Part);
        Assert.Equal(OnSiteMessagePart.RoomHalf, roomHalf.Part);
        Assert.NotEqual(contactHalf.Part, roomHalf.Part);
    }

    /// <summary>
    /// Case folding would make the two halves indistinguishable, so it must not
    /// happen anywhere in the reader.
    /// </summary>
    [Fact]
    public void on_site_reading_is_case_sensitive()
    {
        Assert.False(OnSiteStayStatus.Read("checked in").Recognised);
        Assert.False(OnSiteStayStatus.Read("Checked in").Recognised);
    }

    [Theory]
    [InlineData("Due In")]
    [InlineData("DUE IN")]
    [InlineData("OT")]
    public void the_three_spellings_of_due_in_all_mean_booked(string source)
    {
        Assert.True(OnSiteStayStatus.Read(source).TryGet(out var status));

        Assert.Equal(StayLifecycle.Booked, status.Lifecycle);
        Assert.Equal(OnSiteMessagePart.Whole, status.Part);
    }

    /// <summary>
    /// The three the reference discarded (study §5.1). They are declared here,
    /// so a stay the PMS is still deciding about is a fact rather than a gap.
    /// </summary>
    [Theory]
    [InlineData("DUE OUT", StayLifecycle.DueOut)]
    [InlineData("PENDING", StayLifecycle.Pending)]
    [InlineData("WAITLIST", StayLifecycle.Waitlisted)]
    public void the_statuses_the_reference_dropped_are_declared(string source, StayLifecycle expected)
    {
        Assert.True(OnSiteStayStatus.Read(source).TryGet(out var status));
        Assert.Equal(expected, status.Lifecycle);
    }
}
