using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Events;
using Xunit;
using Wire = HotelOS.Contracts.Integration.V1;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Reading the Integration Hub's contract at this application's edge.
/// </summary>
/// <remarks>
/// The mapper is the only place that knows the wire type, so it is the only
/// place these rules can be asserted. Each test names the rule rather than the
/// field: what matters is that a guessed room type is refused and a due-out
/// guest is in house, not that property 11 copies to property 9.
/// </remarks>
public class RoomStayFactMapperTests
{
    private static readonly Guid Property = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RoomType = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>A fact carrying the minimum the contract says it must.</summary>
    private static Wire.RoomStayFact Fact() => new()
    {
        Header = new Wire.FactHeader
        {
            PropertyId = Property.ToString(),
            BusinessDate = "2026-09-01",
            Provenance = new Wire.Provenance { IntegrationId = "opera-ohip" },
        },
        Lifecycle = Wire.StayLifecycle.Booked,
        RoomTypeId = RoomType.ToString(),
    };

    [Fact]
    public void The_header_carries_the_property_the_integration_and_the_business_date()
    {
        var read = RoomStayFactMapper.Read(Fact());

        Assert.Equal(Property, read.PropertyId);
        Assert.Equal("opera-ohip", read.IntegrationId);
        Assert.Equal(new DateOnly(2026, 9, 1), read.BusinessDate);
    }

    /// <summary>
    /// A due-out guest is in house.
    /// </summary>
    /// <remarks>
    /// The wire keeps <c>DUE_OUT</c> as its own value because Room Care works
    /// from it. This domain has six lifecycle states and should not gain a
    /// seventh to hold another application's planning signal.
    /// </remarks>
    [Theory]
    [InlineData(Wire.StayLifecycle.Booked, StayLifecycle.Booked)]
    [InlineData(Wire.StayLifecycle.CheckedIn, StayLifecycle.InHouse)]
    [InlineData(Wire.StayLifecycle.DueOut, StayLifecycle.InHouse)]
    [InlineData(Wire.StayLifecycle.CheckedOut, StayLifecycle.Departed)]
    [InlineData(Wire.StayLifecycle.Cancelled, StayLifecycle.Cancelled)]
    [InlineData(Wire.StayLifecycle.NoShow, StayLifecycle.NoShow)]
    public void Every_wire_lifecycle_reads_as_a_domain_one(
        Wire.StayLifecycle sent, StayLifecycle expected)
    {
        var fact = Fact();
        fact.Lifecycle = sent;

        Assert.Equal(expected, RoomStayFactMapper.Read(fact).Lifecycle);
    }

    /// <summary>
    /// An unsent lifecycle is refused rather than read as booked.
    /// </summary>
    /// <remarks>
    /// Proto3's zero means "never sent". Defaulting it would create a stay the
    /// source never described — and it would be created silently.
    /// </remarks>
    [Fact]
    public void A_fact_with_no_lifecycle_is_refused()
    {
        var fact = Fact();
        fact.Lifecycle = Wire.StayLifecycle.Unspecified;

        Assert.Throws<ArgumentException>(() => RoomStayFactMapper.Read(fact));
    }

    /// <summary>
    /// A room type the Hub failed to resolve is refused, loudly.
    /// </summary>
    /// <remarks>
    /// The anchor is the room type (GUEST-Q2's addendum) and Enrich resolves it
    /// to Master Data. A code arriving here means Enrich did not, and a stay
    /// anchored on a guess is worse than a fact that did not arrive.
    /// </remarks>
    [Fact]
    public void An_unresolved_room_type_is_refused()
    {
        var fact = Fact();
        fact.RoomTypeId = "DLX";

        var refusal = Assert.Throws<ArgumentException>(() => RoomStayFactMapper.Read(fact));
        Assert.Contains("room_type_id", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>A stay with no room yet is the ordinary case, not a defect.</summary>
    [Fact]
    public void An_absent_room_is_read_as_absent()
        => Assert.Null(RoomStayFactMapper.Read(Fact()).RoomId);

    /// <summary>But a room id that will not parse is the Hub contradicting itself.</summary>
    [Fact]
    public void A_room_that_is_not_an_identifier_is_refused()
    {
        var fact = Fact();
        fact.RoomId = "214";

        Assert.Throws<ArgumentException>(() => RoomStayFactMapper.Read(fact));
    }

    /// <summary>A time arrives with its basis, or it does not arrive.</summary>
    /// <remarks>
    /// R12/R13. An absent <c>FactTime</c> reads as <see cref="StayTime.None"/>
    /// rather than a zero instant: "not sent" and "midnight" are different.
    /// </remarks>
    [Fact]
    public void A_time_keeps_the_basis_the_source_gave_it()
    {
        var fact = Fact();
        fact.Arrival = new Wire.FactTime
        {
            At = Timestamp.FromDateTimeOffset(new DateTimeOffset(2026, 9, 1, 14, 10, 0, TimeSpan.Zero)),
            Basis = Wire.TimeBasis.Observed,
        };

        var read = RoomStayFactMapper.Read(fact);

        Assert.Equal(TimeBasis.Observed, read.Arrival.Basis);
        Assert.Equal(StayTime.None, read.Departure);
    }

    /// <summary>
    /// A group of no rooms is not something a source means.
    /// </summary>
    /// <remarks>
    /// R9's <c>noOfRooms</c>. Proto3 cannot distinguish an unsent int from
    /// zero, and "expected zero stays" would make a group the desk must
    /// complete out of a group nobody described.
    /// </remarks>
    [Fact]
    public void An_unstated_group_size_is_unstated_rather_than_zero()
    {
        var fact = Fact();
        fact.BookingGroup = new Wire.BookingGroup { IsComplete = false };

        var read = RoomStayFactMapper.Read(fact);

        Assert.Null(read.ExpectedStayCount);
        Assert.False(read.IsComplete);
    }

    /// <summary>
    /// No group at all is not the same as a group claiming to be incomplete.
    /// </summary>
    [Fact]
    public void A_fact_with_no_group_makes_no_claim_about_one()
        => Assert.Null(RoomStayFactMapper.Read(Fact()).IsComplete);

    /// <summary>What the source did not supply, and why, is carried — R25.</summary>
    [Fact]
    public void An_absence_the_source_explained_survives_the_boundary()
    {
        var fact = Fact();
        fact.Header.Absences.Add(new Wire.Absence
        {
            Field = "arrival",
            Reason = Wire.Absence.Types.Reason.NotAvailableFromSource,
            RawValue = "",
        });

        var absence = Assert.Single(RoomStayFactMapper.Read(fact).Absences);

        Assert.Equal("arrival", absence.Field);
        Assert.Equal(AbsenceReason.NotAvailableFromSource, absence.Reason);
    }

    /// <summary>The party is forwarded for this domain to resolve or create.</summary>
    [Fact]
    public void A_guest_keeps_the_name_as_the_source_gave_it()
    {
        var fact = Fact();
        var guest = new Wire.StayGuest
        {
            Name = new Wire.GuestName { AsGiven = "PILLAI/RAJESH MR", Family = "Pillai" },
            IsPrimary = true,
        };
        guest.Contacts.Add(new Wire.ContactPoint
        {
            Kind = Wire.ContactPoint.Types.Kind.Phone,
            Value = "+91 98470 12345",
        });
        fact.Guests.Add(guest);

        var read = Assert.Single(RoomStayFactMapper.Read(fact).Guests);

        Assert.Equal("PILLAI/RAJESH MR", read.NameAsGiven);
        Assert.Equal("Pillai", read.NameFamily);
        Assert.Null(read.NameGiven);
        Assert.Equal("+91 98470 12345", read.Phone);
        Assert.True(read.IsPrimary);
    }

    /// <summary>Money arrives with its currency and basis, or not at all — R19.</summary>
    [Fact]
    public void An_amount_without_a_currency_is_not_read_as_money()
    {
        var fact = Fact();
        fact.TotalAmount = new Wire.Money { MinorUnits = 1000 };

        Assert.Null(RoomStayFactMapper.Read(fact).Terms?.Amount);
    }

    /// <summary>
    /// With matching options it round-trips exactly, which is what makes
    /// reading the contract — rather than restating it — the right call.
    /// </summary>
    [Fact]
    public void The_wire_fact_survives_a_round_trip_that_uses_one_naming_policy()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        var written = JsonSerializer.SerializeToDocument(Fact(), options);
        var read = written.RootElement.Deserialize<Wire.RoomStayFact>(options);

        Assert.NotNull(read);
        Assert.Equal(Property.ToString(), read.Header.PropertyId);
        Assert.Equal(RoomType.ToString(), read.RoomTypeId);
        Assert.Equal(Wire.StayLifecycle.Booked, read.Lifecycle);
    }
}
