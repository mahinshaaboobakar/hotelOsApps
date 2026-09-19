using HotelOS.RoomCare.Infrastructure;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>
/// Master Data allows a staff record with no display name (<c>People.cs</c>: <c>string? DisplayName</c>), and
/// Room Care's read model declared the column required. A person with no display name must read as a person
/// whose name is not known — never as an exception that empties every screen naming staff, and never as an
/// empty string drawn as a name. Found by HH, 2026-09-19.
/// </summary>
[Collection(RoomCareCollection.Name)]
public sealed class StaffNameTests(RoomCareFixture fixture)
{
    [Fact]
    public async Task A_staff_record_with_no_display_name_reads_as_no_name_known()
    {
        var named = Guid.CreateVersion7();
        var nullName = Guid.CreateVersion7();
        var emptyName = Guid.CreateVersion7();
        var blankName = Guid.CreateVersion7();
        await fixture.MasterDataStaffAsync([(named, "Anita Pillai"), (nullName, null), (emptyName, ""), (blankName, "   ")]);

        await using var db = fixture.Context();
        var names = await new MasterDataHouseReader(db).NamesAsync([named, nullName, emptyName, blankName], CancellationToken.None);

        Assert.Equal("Anita Pillai", names[named]);
        Assert.False(names.ContainsKey(nullName), "a NULL display name is no name known");
        Assert.False(names.ContainsKey(emptyName), "an empty display name is no name known, not a name drawn empty");
        Assert.False(names.ContainsKey(blankName), "a blank display name is no name known");
    }
}
