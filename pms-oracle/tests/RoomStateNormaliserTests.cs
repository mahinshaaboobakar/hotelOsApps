using HotelOS.Contracts.Common.V1;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.OnSite;
using PmsOracle.Normalisation;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The four axes, the stay list, and the field that separates two rooms which
/// look identical.
/// </summary>
public sealed class RoomStateNormaliserTests
{
    private static RoomStateNormaliser Kochi() =>
        new(new IntegrationSettings(
            IntegrationId: "oracle-web",
            PropertyId: "prop-kochi",
            PropertyCode: "KOCHI01",
            Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
            Currency: "INR",
            AmountTaxBasis: TaxBasis.Net));

    private static OnSiteRoomStatusPush Push() => new()
    {
        RoomNo = "205",
        FOStatus = "OCC",
        RoomStatus = "DI",
        ReservationStatus = "Due Out",
        PropertyCode = "KOCHI01",
    };

    private static RoomState StateFrom(OnSiteRoomStatusPush push) =>
        Assert.IsType<NormalisationOutcome.RoomStateNormalised>(Kochi().Normalise(push)).Fact.State;

    /// <summary>
    /// R1. Occupancy and condition are read into separate fields, so a room can
    /// be occupied and dirty without either implying the other.
    /// </summary>
    [Fact]
    public void occupancy_and_condition_are_independent_fields()
    {
        var state = StateFrom(Push());

        Assert.Equal(Occupancy.Occupied, state.Occupancy);
        Assert.Equal(RoomCondition.Dirty, state.Condition);
    }

    [Fact]
    public void a_vacant_room_can_be_dirty_and_a_vacant_room_can_be_out_of_order()
    {
        var dirty = StateFrom(Push() with { FOStatus = "VAC", RoomStatus = "DI" });
        var outOfOrder = StateFrom(Push() with { FOStatus = "VAC", RoomStatus = "OO" });

        Assert.Equal(Occupancy.Vacant, dirty.Occupancy);
        Assert.Equal(RoomCondition.Dirty, dirty.Condition);

        Assert.Equal(Occupancy.Vacant, outOfOrder.Occupancy);
        Assert.Equal(RoomCondition.OutOfOrder, outOfOrder.Condition);
    }

    /// <summary>
    /// The on-site flavours do not send the housekeeping department's own axis,
    /// so it stays absent rather than being filled from the condition. An axis
    /// a source does not send is not an axis with a default.
    /// </summary>
    [Fact]
    public void an_axis_the_source_does_not_send_stays_unspecified()
    {
        Assert.Equal(RoomCondition.Unspecified, StateFrom(Push()).RoomCareStatus);
    }

    /// <summary>
    /// R2, and the reference's most consequential shortcut: it split this
    /// string and took element zero, so a room with a departure and an arrival
    /// on the same day had one status and the other stay was invisible.
    /// </summary>
    [Fact]
    public void every_stay_touching_the_room_is_read_not_just_the_first()
    {
        var push = Push() with { ReservationStatus = "Departed,Stayover,Arrival" };

        var state = StateFrom(push);

        Assert.Equal(3, state.StayStatuses.Count);
        Assert.Equal(
            new[] { StayLifecycle.CheckedOut, StayLifecycle.CheckedIn, StayLifecycle.Booked },
            state.StayStatuses);
    }

    /// <summary>
    /// "Nothing is reserved" contributes no entry: an empty list already says
    /// it, and an UNSPECIFIED in the middle of a list would be a value every
    /// consumer had to learn to skip.
    /// </summary>
    [Theory]
    [InlineData("Not Reserved")]
    [InlineData("NotReserved")]
    public void a_room_nobody_is_staying_in_has_an_empty_stay_list(string status)
    {
        Assert.Empty(StateFrom(Push() with { ReservationStatus = status }).StayStatuses);
    }

    [Fact]
    public void the_no_stay_value_is_skipped_among_real_ones()
    {
        var push = Push() with { ReservationStatus = "NotReserved,Departed,NotReserved,Arrival" };

        var state = StateFrom(push);

        Assert.Equal(
            new[] { StayLifecycle.CheckedOut, StayLifecycle.Booked },
            state.StayStatuses);
    }

    /// <summary>
    /// R3. Two rooms identical on all four axes, and only this field decides
    /// whether the linen is stripped or the room is made up for tonight.
    /// </summary>
    [Fact]
    public void the_next_sold_time_is_carried_and_completed_by_the_property_clock()
    {
        var push = Push() with { NextBlocked = "03-09-26" };

        var state = StateFrom(push);

        // 14:00 on 3 September 2026, Asia/Kolkata — 08:30 UTC.
        Assert.Equal(
            new DateTime(2026, 9, 3, 8, 30, 0, DateTimeKind.Utc),
            state.NextSoldAt.ToDateTime());
    }

    [Fact]
    public void two_rooms_alike_on_every_axis_differ_only_by_when_they_are_next_sold()
    {
        var soldTonight = StateFrom(Push() with { NextBlocked = "31-08-26" });
        var notSold = StateFrom(Push());

        Assert.Equal(soldTonight.Occupancy, notSold.Occupancy);
        Assert.Equal(soldTonight.Condition, notSold.Condition);
        Assert.Equal(soldTonight.StayStatuses, notSold.StayStatuses);

        Assert.NotNull(soldTonight.NextSoldAt);
        Assert.Null(notSold.NextSoldAt);
    }

    /// <summary>
    /// The room number is a master-entity reference, so it is forwarded for
    /// Enrich to resolve onto <c>masterdata.room_id</c> — GUEST-Q8(a)'s split.
    /// </summary>
    [Fact]
    public void the_room_number_is_forwarded_for_enrich_to_resolve()
    {
        var reference = Assert.Single(StateFrom(Push()).ExternalRefs);

        Assert.Equal("oracle-web", reference.IntegrationId);
        Assert.Equal(RoomStateNormaliser.RoomNumberKind, reference.IdentifierKind);
        Assert.Equal("205", reference.ExternalId);
    }

    [Fact]
    public void a_message_without_a_room_is_rejected()
    {
        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(
            Kochi().Normalise(Push() with { RoomNo = null }));

        Assert.Equal(RejectionReason.MissingRequiredField, rejected.Reason);
        Assert.Equal("RoomNo", rejected.Field);
    }

    [Theory]
    [InlineData("FOStatus", "MAYBE")]
    [InlineData("RoomStatus", "ZZ")]
    [InlineData("ReservationStatus", "Levitating")]
    public void an_unknown_code_on_any_axis_is_rejected_carrying_the_value(string field, string value)
    {
        var push = field switch
        {
            "FOStatus" => Push() with { FOStatus = value },
            "RoomStatus" => Push() with { RoomStatus = value },
            _ => Push() with { ReservationStatus = value },
        };

        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(Kochi().Normalise(push));

        Assert.Equal(RejectionReason.UnknownStatus, rejected.Reason);
        Assert.Equal(field, rejected.Field);
        Assert.Equal(value, rejected.RawValue);
    }

    [Fact]
    public void a_message_claiming_another_property_is_rejected()
    {
        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(
            Kochi().Normalise(Push() with { PropertyCode = "TRIVANDRUM01" }));

        Assert.Equal(RejectionReason.PropertyMismatch, rejected.Reason);
    }
}
