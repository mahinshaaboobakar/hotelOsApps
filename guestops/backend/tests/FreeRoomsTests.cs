using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Domain;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Which rooms the desk may give — gold frame 10's chooser, and the read the
/// walk-in could not be completed without.
/// </summary>
/// <remarks>
/// <para>
/// <b>`WalkInCommand` has required a `roomId` since it was written and no read
/// listed rooms.</b> Check-in needs a room (S8), so the sheet could be drawn
/// and could not be completed by any caller — an absence no comparison of
/// screens can find, because an absent control has no node to compare.
/// </para>
/// <para>
/// <b>The fixture is chosen so the candidate rules disagree.</b> Three rooms of
/// one type, one held by an in-house stay and one out of order, so a
/// calculation that forgot either would answer two where the right answer is
/// one — and a fixture with one room of each kind would pass under several
/// wrong rules.
/// </para>
/// </remarks>
public sealed class FreeRoomsTests
{
    private static readonly Guid Occupied = Guid.Parse("aaaa0000-0000-4000-8000-000000000001");
    private static readonly Guid Broken = Guid.Parse("aaaa0000-0000-4000-8000-000000000002");
    private static readonly Guid Spare = Guid.Parse("aaaa0000-0000-4000-8000-000000000003");

    private static readonly DateOnly Arrive = new(2026, 8, 31);
    private static readonly DateOnly Depart = new(2026, 9, 1);

    [Fact]
    public async Task A_room_nobody_holds_is_offered()
    {
        await using var harness = await Ready();

        var rooms = await Ask(harness);

        Assert.Equal(["305"], rooms.Free.Select(room => room.Number));
    }

    /// <summary>
    /// A room somebody is in is not offered to the next guest.
    /// </summary>
    /// <remarks>
    /// The assertion the whole read exists for: a walk-in given an occupied
    /// room is two people with one key.
    /// </remarks>
    [Fact]
    public async Task A_room_held_by_a_stay_is_not_offered()
    {
        await using var harness = await Ready();

        var rooms = await Ask(harness);

        Assert.DoesNotContain(rooms.Free, room => room.Id == Occupied);
    }

    /// <summary>
    /// An out-of-order room is not offered either, and for a different reason.
    /// </summary>
    /// <remarks>
    /// Maintenance's fact, heard as an event into a local read model. A room
    /// that is both free of stays and broken is still not one to give.
    /// </remarks>
    [Fact]
    public async Task An_out_of_order_room_is_not_offered()
    {
        await using var harness = await Ready();

        var rooms = await Ask(harness);

        Assert.DoesNotContain(rooms.Free, room => room.Id == Broken);
    }

    /// <summary>
    /// The count of the type is carried, so an empty list is not two answers.
    /// </summary>
    /// <remarks>
    /// <b>The gap rule at a contract.</b> *This property has no rooms of that
    /// type* and *every room of that type is taken* have opposite remedies —
    /// configure the property, or choose another type — and a bare empty list
    /// reports them alike.
    /// </remarks>
    [Fact]
    public async Task A_full_type_and_an_unconfigured_one_are_different_answers()
    {
        await using var harness = await Ready();

        var full = await Ask(harness);
        var unknown = await Ask(harness, Guid.NewGuid());

        Assert.Equal(3, full.Total);
        Assert.Equal(0, unknown.Total);
        Assert.Empty(unknown.Free);
    }

    /// <summary>
    /// A departed stay stops holding its room.
    /// </summary>
    /// <remarks>
    /// Nights are arrival-inclusive and departure-exclusive — the rule
    /// <c>HoldsOn</c> already keeps for the type-level count, which this read
    /// reuses rather than deciding again.
    /// </remarks>
    [Fact]
    public async Task A_room_is_free_again_for_dates_after_the_stay_leaves()
    {
        await using var harness = await Ready();

        var later = await Ask(harness, DeskHarness.RoomType,
            new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 11));

        Assert.Contains(later.Free, room => room.Id == Occupied);
    }

    /// <summary>An inactive room is not sellable — ADR 0062.</summary>
    /// <remarks>
    /// A wing closed for renovation is inactive and very much still there, so
    /// it must not be offered; it is also not deleted, which is what
    /// <c>deleted_at</c> answers. Neither appears here, and <c>Total</c> counts
    /// neither — a property's usable rooms are what the desk is choosing from.
    /// </remarks>
    [Fact]
    public async Task An_inactive_room_is_neither_offered_nor_counted()
    {
        await using var harness = await Ready(withClosedWing: true);

        var rooms = await Ask(harness);

        Assert.DoesNotContain(rooms.Free, room => room.Number == "309");
        Assert.Equal(3, rooms.Total);
    }

    /// <summary>The service's own refusal, not a silently empty list.</summary>
    [Fact]
    public async Task A_range_that_ends_before_it_starts_is_refused()
    {
        await using var harness = await Ready();

        await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Ask(harness, DeskHarness.RoomType, Depart, Arrive));
    }

    /// <summary>Ask the question the chooser asks.</summary>
    private static Task<RoomsOfType> Ask(
        DeskHarness harness,
        Guid? roomTypeId = null,
        DateOnly? from = null,
        DateOnly? to = null)
        => Service(harness).FreeRoomsAsync(
            harness.Scope(),
            roomTypeId ?? DeskHarness.RoomType,
            from ?? Arrive,
            to ?? Depart,
            CancellationToken.None);

    private static AvailabilityService Service(DeskHarness harness)
        => new(
            harness.Db,
            harness.Authorizer,
            new Infrastructure.ReadModels.RoomInventory(harness.Db),
            new StubBusinessDay(Arrive));

    /// <summary>
    /// Three rooms of one type: one occupied, one broken, one free.
    /// </summary>
    /// <remarks>
    /// The stay is seeded in house and holding <see cref="Occupied"/>, and the
    /// out-of-order row covers the dates asked about — so each exclusion has a
    /// distinct cause and a calculation that dropped either one is visible.
    /// </remarks>
    private static async Task<DeskHarness> Ready(bool withClosedWing = false)
    {
        var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();

        var rooms = new List<MasterDataRoomSource.Row>
        {
            Room(Occupied, "306"),
            Room(Broken, "307"),
            Room(Spare, "305"),
        };

        if (withClosedWing)
        {
            var closed = Room(Guid.NewGuid(), "309");
            closed.Active = false;
            rooms.Add(closed);
        }

        await harness.MasterDataRoomsAsync(rooms);

        var stay = await harness.SeedStayAsync(new DateTimeOffset(
            Arrive.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero));

        stay.CurrentRoomId = Occupied;
        stay.DepartureAt = StayTime.Observed(new DateTimeOffset(
            Depart.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero));

        harness.Db.RoomsOutOfOrder.Add(new RoomOutOfOrder
        {
            RoomId = Broken,
            PropertyId = DeskHarness.Property,
            RoomTypeId = DeskHarness.RoomType,
            FromDate = Arrive,
            ToDate = null,
            ObservedAt = harness.Clock.GetUtcNow(),
        });

        await harness.Db.SaveChangesAsync();
        return harness;
    }

    private static MasterDataRoomSource.Row Room(Guid id, string number) => new()
    {
        Id = id,
        PropertyId = DeskHarness.Property,
        RoomTypeId = DeskHarness.RoomType,
        RoomNumber = number,
        Active = true,
    };
}
