using HotelOS.Platform;
using HotelOS.RoomCare.Application.Work;
using HotelOS.RoomCare.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>
/// "Not before 14:30" is said in the property's time, never in UTC and never labelled UTC — in the room's history
/// and in the refusal an attendant reads at the door. Both used to format the instant in whatever offset it carried
/// and call it UTC: a deferral sent at +05:30 was recorded as "14:30 UTC", and the stored one refused as "09:00 UTC".
/// </summary>
[Collection(RoomCareCollection.Name)]
public sealed class NotBeforeWordsTests(RoomCareFixture fixture)
{
    [Fact]
    public async Task At_Kolkata_the_history_and_the_door_both_say_14_30()
    {
        var (history, refusal) = await DeferThenStartAsync(null, new DateTimeOffset(2026, 9, 5, 14, 30, 0, TimeSpan.FromHours(5.5)));
        Assert.Equal("not before 14:30", history);
        Assert.Equal("the guest asked for this room not before 14:30", refusal);
    }

    [Fact]
    public async Task At_Guatemala_an_instant_sent_in_UTC_is_said_as_14_30_there()
    {
        // 20:30 UTC is 14:30 at −06:00.
        var (history, refusal) = await DeferThenStartAsync("America/Guatemala", new DateTimeOffset(2026, 9, 5, 20, 30, 0, TimeSpan.FromHours(0)));
        Assert.Equal("not before 14:30", history);
        Assert.Equal("the guest asked for this room not before 14:30", refusal);
    }

    private async Task<(string? History, string Refusal)> DeferThenStartAsync(string? zone, DateTimeOffset notBefore)
    {
        var (h, anita, task) = await TheDoorTests.AssignedAsync(Service.DailyService, fixture);
        if (zone is not null)
        {
            h.House.Settings = h.House.Settings! with { Timezone = zone };
        }

        await h.Get<AmendService>().DeferAsync(h.As(Guid.CreateVersion7()), task.Id, task.Version, notBefore, null, default);
        var refused = await Assert.ThrowsAsync<InvalidRequestException>(() => h.Get<AttendantWork>().StartAsync(h.As(anita), task.Id, default));
        await using var db = h.Db();
        var recorded = await db.History.Where(x => x.TaskId == task.Id && x.Kind == HistoryKind.Reduction).Select(x => x.Reason).SingleAsync();
        return (recorded, refused.Message);
    }
}
