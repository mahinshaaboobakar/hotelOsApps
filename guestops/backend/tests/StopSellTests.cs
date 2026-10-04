using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Domain;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The seller's control — ADR 0377, the owner's ruling of 2026-10-04.
/// </summary>
/// <remarks>
/// <para>
/// <b>Availability subtracted stop-sells from the first migration and nothing
/// could create one</b>, so one of Setup's five tabs was readable and
/// unwritable while the approved page drew its control live.
/// </para>
/// <para>
/// <b>And the arithmetic was wrong before the ruling arrived.</b> The subtraction
/// was a COUNT OF ROWS, so one hold on a three-room type withheld ONE room —
/// against an approved page drawing <c>Suite … Stop-sell 4</c> over the caption
/// <i>"four suites are physically fine, unsold, and not for sale"</i>. The first
/// test below is the discriminator: it fails under the old arithmetic and passes
/// under ADR 0377's <i>"room type alone — the whole type is held for the
/// dates"</i>.
/// </para>
/// <para>
/// <b>The fixture is three rooms of one type</b>, so a type-wide hold (3) and a
/// per-room hold (1) give different answers. One room would have made the two
/// rules agree, and the test could not have told them apart.
/// </para>
/// </remarks>
public sealed class StopSellTests
{
    private static readonly Guid One = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Two = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Three = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
    private static readonly Guid OtherType = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid OfOtherType = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000009");

    private static readonly DateOnly From = new(2026, 9, 3);
    private static readonly DateOnly To = new(2026, 9, 5);

    /// <summary>A type held whole withholds every room of it — not one.</summary>
    /// <remarks>
    /// <b>The discriminator.</b> Under the pre-ruling arithmetic this returns 1,
    /// which is what the approved page contradicts.
    /// </remarks>
    [Fact]
    public async Task A_type_held_whole_withholds_every_room_of_it()
    {
        await using var harness = await Ready();

        await Set(harness, room: null);

        Assert.Equal(3, await StopSoldOn(harness, From));
    }

    [Fact]
    public async Task One_named_room_withholds_one()
    {
        await using var harness = await Ready();

        await Set(harness, room: Two);

        Assert.Equal(1, await StopSoldOn(harness, From));
    }

    /// <summary>Two holds on one room withhold one room.</summary>
    /// <remarks>
    /// Counting rows would withhold inventory the property has not got — and
    /// <c>Free</c>'s clamp would hide it at zero while the figure on the screen
    /// stayed wrong, which is the shape a clamp makes invisible.
    /// </remarks>
    [Fact]
    public async Task Two_holds_on_one_room_withhold_one_room()
    {
        await using var harness = await Ready();

        await Set(harness, room: Two);
        await Set(harness, room: Two, reason: "and again, for the same week");

        Assert.Equal(1, await StopSoldOn(harness, From));
    }

    /// <summary>A type-wide hold absorbs a per-room one rather than adding to it.</summary>
    [Fact]
    public async Task A_type_wide_hold_absorbs_a_per_room_hold()
    {
        await using var harness = await Ready();

        await Set(harness, room: Two);
        await Set(harness, room: null, reason: "the whole wing, for the wedding party");

        Assert.Equal(3, await StopSoldOn(harness, From));
    }

    /// <summary>Outside the range it withholds nothing.</summary>
    [Fact]
    public async Task A_night_outside_the_range_is_untouched()
    {
        await using var harness = await Ready();

        await Set(harness, room: null);

        Assert.Equal(0, await StopSoldOn(harness, To.AddDays(1)));
    }

    /// <summary>A room of another type is refused, naming both.</summary>
    /// <remarks>
    /// ADR 0377's own distinction from <i>type OR room</i>: a stop-sell names a
    /// room WITHIN a type, so a room of a different type is not a narrower hold
    /// but a different one — and accepting it would withhold a room the
    /// subtraction attributes to the type that was named.
    /// </remarks>
    [Fact]
    public async Task A_room_of_another_type_is_refused_by_name()
    {
        await using var harness = await Ready();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Set(harness, room: OfOtherType));

        Assert.Contains("is not a room of type", refused.Message);
        Assert.Equal(0, await StopSoldOn(harness, From));
    }

    /// <summary>A type this property has no rooms of is refused.</summary>
    /// <remarks>
    /// It would withhold nothing while reading on the screen as though it
    /// withheld everything, and nothing downstream would ever contradict it.
    /// </remarks>
    [Fact]
    public async Task A_type_this_property_has_no_rooms_of_is_refused()
    {
        await using var harness = await Ready();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Service(harness).SetAsync(
                harness.Scope(),
                new StopSellEdit(Guid.NewGuid(), null, From, To, "a type nobody has"),
                CancellationToken.None));

        Assert.Contains("has no rooms of type", refused.Message);
    }

    [Fact]
    public async Task An_empty_reason_is_refused()
    {
        // ADR 0377's schema makes the reason required. A hold nobody can explain
        // is one nobody can lift with confidence.
        await using var harness = await Ready();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Set(harness, room: null, reason: "   "));

        Assert.Contains("needs a reason", refused.Message);
    }

    [Fact]
    public async Task A_range_that_ends_before_it_starts_is_refused()
    {
        await using var harness = await Ready();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Service(harness).SetAsync(
                harness.Scope(),
                new StopSellEdit(DeskHarness.RoomType, null, To, From, "backwards"),
                CancellationToken.None));

        Assert.Contains("the last date is before the first", refused.Message);
    }

    /// <summary>The operator and the moment are recorded.</summary>
    [Fact]
    public async Task The_operator_and_the_moment_are_recorded()
    {
        await using var harness = await Ready();
        var scope = harness.Scope();

        var row = await Service(harness).SetAsync(
            scope, new StopSellEdit(DeskHarness.RoomType, null, From, To, "renovation"),
            CancellationToken.None);

        Assert.Equal(scope.UserId, row.SetBy);
        Assert.Equal(harness.Clock.GetUtcNow(), row.SetAt);
        Assert.Equal("renovation", row.Reason);
    }

    private static StopSellService Service(DeskHarness harness)
        => new(harness.Db, harness.Authorizer, harness.Clock);

    private static Task<StopSell> Set(
        DeskHarness harness, Guid? room, string reason = "block for the wedding party")
        => Service(harness).SetAsync(
            harness.Scope(),
            new StopSellEdit(DeskHarness.RoomType, room, From, To, reason),
            CancellationToken.None);

    /// <summary>What availability says is withheld on one night.</summary>
    /// <remarks>
    /// Read through <c>AvailabilityService</c> rather than from the rows, because
    /// the arithmetic is the thing under test and a test that counted the rows
    /// itself would assert its own copy of the rule.
    /// </remarks>
    private static async Task<int> StopSoldOn(DeskHarness harness, DateOnly date)
    {
        var availability = new AvailabilityService(
            harness.Db,
            harness.Authorizer,
            new Infrastructure.ReadModels.RoomInventory(harness.Db),
            new StubBusinessDay(date));

        var answer = await availability.GetAsync(
            harness.Scope(), date, date, [DeskHarness.RoomType], CancellationToken.None);

        return answer.Single().StopSold;
    }

    /// <summary>Three rooms of one type, and one room of another.</summary>
    /// <remarks>
    /// The fourth room exists so <c>A_room_of_another_type_is_refused_by_name</c>
    /// names a room this property really has — a refusal on a room that does not
    /// exist would pass for the wrong reason.
    /// </remarks>
    private static async Task<DeskHarness> Ready()
    {
        var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();

        await harness.MasterDataRoomsAsync(
        [
            Room(One, "301", DeskHarness.RoomType),
            Room(Two, "302", DeskHarness.RoomType),
            Room(Three, "303", DeskHarness.RoomType),
            Room(OfOtherType, "401", OtherType),
        ]);

        return harness;
    }

    private static MasterDataRoomSource.Row Room(Guid id, string number, Guid type) => new()
    {
        Id = id,
        PropertyId = DeskHarness.Property,
        RoomTypeId = type,
        RoomNumber = number,
        Active = true,
    };
}
