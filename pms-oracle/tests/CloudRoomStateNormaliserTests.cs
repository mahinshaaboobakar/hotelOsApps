using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.Cloud;
using PmsOracle.Normalisation;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// OHIP's housekeeping view: the same four axes in OHIP's words, plus the two
/// things only this source supplies.
/// </summary>
public sealed class CloudRoomStateNormaliserTests
{
    private static CloudRoomStateNormaliser Kochi() =>
        new(new IntegrationSettings(
            IntegrationId: "oracle-cloud",
            PropertyId: "prop-kochi",
            PropertyCode: "KOCHI01",
            Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
            Currency: "INR",
            AmountTaxBasis: TaxBasis.Net));

    private static OhipHousekeepingRoom Room(
        string? condition = "Dirty",
        string? department = "Inspected",
        bool pseudo = false,
        params string[] stays) => new(
        RoomId: "417",
        RoomType: new OhipRoomType("DLX", "DELUXE", pseudo),
        Housekeeping: new OhipHousekeepingStatus(
            ReservationStatusList: stays.Length == 0 ? ["Arrived"] : stays,
            FrontOfficeStatus: "Occupied",
            HousekeepingRoomStatus: condition,
            HousekeepingStatus: department));

    private static RoomState StateFrom(OhipHousekeepingRoom room) =>
        Assert.IsType<NormalisationOutcome.RoomStateNormalised>(Kochi().Normalise(room)).Fact.State;

    [Fact]
    public void the_four_axes_are_read_into_four_fields()
    {
        var state = StateFrom(Room());

        Assert.Equal(Occupancy.Occupied, state.Occupancy);
        Assert.Equal(RoomCondition.Dirty, state.Condition);
        Assert.Equal(RoomCondition.Inspected, state.HousekeepingStatus);
        Assert.Equal([StayLifecycle.CheckedIn], state.ReservationStatuses);
    }

    /// <summary>
    /// R5, as amended. OHIP sends the empty string for a room needing a light
    /// tidy — a real state the floor works from, and the one the reference's
    /// <c>default: return null</c> would have erased.
    /// </summary>
    [Fact]
    public void an_empty_condition_means_pick_up()
    {
        Assert.Equal(RoomCondition.PickUp, StateFrom(Room(condition: string.Empty)).Condition);
    }

    /// <summary>
    /// And the distinction that makes it meaningful: absent is not empty. A
    /// null axis was never sent; an empty one was sent and says pick-up.
    /// </summary>
    [Fact]
    public void an_absent_condition_is_not_a_pick_up()
    {
        Assert.Equal(RoomCondition.Unspecified, StateFrom(Room(condition: null)).Condition);
    }

    [Fact]
    public void an_absent_department_status_stays_unspecified()
    {
        Assert.Equal(RoomCondition.Unspecified, StateFrom(Room(department: null)).HousekeepingStatus);
    }

    /// <summary>
    /// R4. House accounts and group masters arrive room-shaped; mapping one
    /// onto a canonical room is a permanent error, so the flag is carried and
    /// the record is marked rather than dropped.
    /// </summary>
    [Fact]
    public void a_pseudo_room_is_marked_and_still_carried()
    {
        var state = StateFrom(Room(pseudo: true));

        Assert.True(state.IsPseudoRoom);
        Assert.Single(state.ExternalRefs);
    }

    [Fact]
    public void a_physical_room_is_not_marked()
    {
        Assert.False(StateFrom(Room()).IsPseudoRoom);
    }

    /// <summary>
    /// R2 on the cloud side, where the list arrives as a real array. The
    /// reference reduced it to its last element.
    /// </summary>
    [Fact]
    public void every_stay_in_the_list_is_read()
    {
        var state = StateFrom(Room(stays: ["NotReserved", "Departed", "StayOver", "Arrived"]));

        Assert.Equal(
            new[] { StayLifecycle.CheckedOut, StayLifecycle.CheckedIn, StayLifecycle.CheckedIn },
            state.ReservationStatuses);
    }

    /// <summary>
    /// OHIP puts occupancy words in the housekeeping field. They are read as
    /// occupancy rather than mapped to a wrong condition — a source mixing two
    /// axes into one field does not make them one axis.
    /// </summary>
    [Theory]
    [InlineData("Vacant", Occupancy.Vacant)]
    [InlineData("Occupied", Occupancy.Occupied)]
    public void the_front_office_words_are_read_as_occupancy(string word, Occupancy expected)
    {
        var room = Room() with
        {
            Housekeeping = Room().Housekeeping! with { FrontOfficeStatus = word },
        };

        Assert.Equal(expected, StateFrom(room).Occupancy);
    }

    /// <summary>
    /// And a condition word in the occupancy field is refused rather than
    /// silently accepted, because it would answer the wrong question.
    /// </summary>
    [Fact]
    public void a_condition_word_in_the_occupancy_field_is_rejected()
    {
        var room = Room() with
        {
            Housekeeping = Room().Housekeeping! with { FrontOfficeStatus = "Dirty" },
        };

        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(Kochi().Normalise(room));

        Assert.Equal(RejectionReason.UnknownStatus, rejected.Reason);
        Assert.Equal("frontOfficeStatus", rejected.Field);
        Assert.Equal("Dirty", rejected.RawValue);
    }

    [Fact]
    public void a_room_with_no_status_block_still_carries_its_identity()
    {
        var state = StateFrom(Room() with { Housekeeping = null });

        Assert.Single(state.ExternalRefs);
        Assert.Equal(Occupancy.Unspecified, state.Occupancy);
        Assert.Equal(RoomCondition.Unspecified, state.Condition);
        Assert.Empty(state.ReservationStatuses);
    }

    [Fact]
    public void a_room_without_a_number_is_rejected()
    {
        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(
            Kochi().Normalise(Room() with { RoomId = null }));

        Assert.Equal(RejectionReason.MissingRequiredField, rejected.Reason);
        Assert.Equal("roomId", rejected.Field);
    }

    [Fact]
    public void an_unknown_condition_is_rejected_carrying_the_value()
    {
        var rejected = Assert.IsType<NormalisationOutcome.Rejected>(
            Kochi().Normalise(Room(condition: "Fumigated")));

        Assert.Equal("housekeepingRoomStatus", rejected.Field);
        Assert.Equal("Fumigated", rejected.RawValue);
    }

    /// <summary>
    /// Both flavours declare the same identifier kind for a room, because a
    /// room is a master entity and Enrich resolves it the same way whichever
    /// integration reported it.
    /// </summary>
    [Fact]
    public void the_room_number_is_forwarded_under_the_same_kind_as_the_on_site_flavour()
    {
        var reference = Assert.Single(StateFrom(Room()).ExternalRefs);

        Assert.Equal("oracle-cloud", reference.IntegrationId);
        Assert.Equal(RoomStateNormaliser.RoomNumberKind, reference.IdentifierKind);
    }
}
