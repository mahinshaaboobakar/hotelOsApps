using System.Text.Json;
using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Module;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// What a room type's row puts on the wire — the id that books it, and what it
/// sleeps.
/// </summary>
/// <remarks>
/// <para>
/// <b>Availability had no test of any kind until 2026-09-22.</b> The view has
/// been served since it was written, and two fields were added to it this week
/// — the room type's id, without which a desk can choose a type it cannot then
/// book, and the occupancy this asserts. Both would have shipped unasserted.
/// </para>
/// <para>
/// <b>The occupancy is Master Data's and this only carries it</b> — ADR 0215,
/// which withdrew the claim that the platform held no such attribute. The
/// fixture therefore sets values that are NOT the domain default of 2, because
/// a row of twos cannot tell "read from Master Data" from "left at whatever the
/// column happened to be".
/// </para>
/// <para>
/// <b>A room's effective occupancy is not asserted here and cannot be.</b>
/// <c>masterdata.rooms.max_occupancy</c> overrides the type's, and Context
/// resolves it; this view answers "which TYPE can take this party", and the
/// number for an assigned room is a different question with a different owner.
/// </para>
/// </remarks>
public sealed class AvailabilityWireTests
{
    private static readonly DateOnly Day = new(2026, 9, 3);

    /// <summary>Rooms per type, as Master Data would answer.</summary>
    private sealed class Inventory(int rooms) : IRoomInventory
    {
        public Task<IReadOnlyDictionary<Guid, int>> CountByTypeAsync(
            Guid propertyId,
            IReadOnlyCollection<Guid> roomTypeIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, int>>(
                new Dictionary<Guid, int> { [DeskHarness.RoomType] = rooms });
    }

    private static JsonElement Wire(object? answer) => JsonSerializer.SerializeToElement(answer);

    private static AvailabilityView View(DeskHarness harness)
        => new(
            harness.Db,
            new AvailabilityService(
                harness.Db,
                harness.Authorizer,
                new Inventory(4),
                new StubBusinessDay(Day)));

    [Fact]
    public async Task A_room_type_row_carries_the_id_that_books_it()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.MasterDataRoomTypesAsync([Suite()]);

        var answer = Wire(await View(harness)
            .AnswerAsync(harness.Scope(), Day, Day, CancellationToken.None));

        var row = Rows(answer).Single();

        Assert.Equal(DeskHarness.RoomType.ToString(), row.GetProperty("roomTypeId").GetString());
        Assert.Equal("Executive Suite", row.GetProperty("roomType").GetString());
    }

    [Fact]
    public async Task A_room_type_row_carries_what_the_type_sleeps()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.MasterDataRoomTypesAsync([Suite()]);

        var answer = Wire(await View(harness)
            .AnswerAsync(harness.Scope(), Day, Day, CancellationToken.None));

        var sleeps = Rows(answer).Single().GetProperty("sleeps");

        Assert.Equal(3, sleeps.GetProperty("included").GetInt32());
        Assert.Equal(5, sleeps.GetProperty("most").GetInt32());
        Assert.Equal(4, sleeps.GetProperty("adults").GetInt32());
        Assert.Equal(2, sleeps.GetProperty("children").GetInt32());
        Assert.True(sleeps.GetProperty("extraBed").GetBoolean());
        Assert.Equal(1, sleeps.GetProperty("extraBeds").GetInt32());
    }

    [Fact]
    public async Task A_type_Master_Data_has_no_row_for_sleeps_nothing_rather_than_a_guess()
    {
        // No room type seeded at all: the inventory says the property has four
        // rooms of this type and Master Data holds nothing about it. A number
        // invented here is one a desk would book a family against.
        await using var harness = await DeskHarness.CreateAsync();
        await harness.MasterDataRoomTypesAsync([]);

        var answer = Wire(await View(harness)
            .AnswerAsync(harness.Scope(), Day, Day, CancellationToken.None));

        var row = Rows(answer).Single();

        Assert.Equal(JsonValueKind.Null, row.GetProperty("sleeps").ValueKind);
        Assert.Equal(JsonValueKind.Null, row.GetProperty("roomType").ValueKind);

        // The id is the property's own fact and survives Master Data's silence.
        Assert.Equal(DeskHarness.RoomType.ToString(), row.GetProperty("roomTypeId").GetString());
    }

    /// <summary>Values that are not the domain default, so the read is visible.</summary>
    private static MasterDataRoomTypeSource.Row Suite()
        => new()
        {
            Id = DeskHarness.RoomType,
            Name = "Executive Suite",
            BaseOccupancy = 3,
            MaxOccupancy = 5,
            MaxAdults = 4,
            MaxChildren = 2,
            ExtraBedAllowed = true,
            MaxExtraBeds = 1,
        };

    /// <summary>The room types, which the answer sends whole and unpaged.</summary>
    private static IEnumerable<JsonElement> Rows(JsonElement answer)
        => answer.GetProperty("types").EnumerateArray();
}
