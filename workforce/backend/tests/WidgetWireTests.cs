using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The dock widgets send numbers, and the widget writes them — NUM-Q1, ADR 0174.
/// </summary>
/// <remarks>
/// <para>
/// <c>Figure.value</c> was documented as <i>"the number, already formatted. A
/// widget never computes one"</i> — so every figure, every row's value and
/// every number inside a meta (<c>"7 rostered"</c>, <c>"3 away"</c>,
/// <c>"22 min"</c>, <c>"5d"</c>) was written here, in the service's culture,
/// and reached a Gulf property in the digits of whatever account this runs
/// under (found during the U1 sweep, 2026-09-19).
/// </para>
/// <para>
/// <b>Checked by walking the wire, not by listing fields.</b> Every string leaf
/// of every widget's answer must carry no digit, except the fields that are ISO
/// values the widget formats (<c>on</c>, <c>from</c>, <c>to</c>, <c>at</c>). A
/// list of fields would check the ones somebody thought of; a walk checks the
/// one somebody adds next.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class WidgetWireTests(WorkforceFixture fixture)
{
    private static readonly string[] Widgets =
        ["shiftBoard", "attendanceToday", "pendingRequests", "comingUp", "onLeave"];

    private static readonly HashSet<string> Iso = ["on", "from", "to", "at"];

    [Fact]
    public async Task No_widget_sends_a_number_written_as_text()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        // One posting on shift today, so a widget has a row as well as figures.
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, "Priya Thomas");
        await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff, department = "FO", role = "Supervisor", from = "2026-01-01",
        });
        var shift = await harness.CallAsync(PolicyView.Write, scope, "defineShift", new
        {
            name = "All day", code = "AD", colour = "Cyan",
            startsAt = "00:00", endsAt = "23:59", from = "2026-01-01",
        });
        await harness.CallAsync(RotaView.Write, scope, "assign", new
        {
            staffId = staff,
            date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            shiftId = shift.GetProperty("id").GetGuid(),
            department = "FO",
        });

        var written = new List<string>();
        var walked = 0;

        foreach (var widget in Widgets)
        {
            var answer = await harness.CallAsync(ReadViews.Answer, scope, widget);
            walked += Walk(answer, widget, null, written);
        }

        // A walk that found nothing would pass on nothing: the five answers
        // hold at least their figures.
        Assert.True(walked > 10, $"the walk visited {walked} leaves");
        Assert.Empty(written);
    }

    [Fact]
    public async Task A_shift_board_row_sends_its_count_as_a_number()
    {
        // The positive half: a row that does exist carries a number, so the
        // walk above is not passing on rows that simply are not there.
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, "Priya Thomas");
        await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff, department = "FO", role = "Supervisor", from = "2026-01-01",
        });
        var shift = await harness.CallAsync(PolicyView.Write, scope, "defineShift", new
        {
            name = "All day", code = "AD", colour = "Cyan",
            startsAt = "00:00", endsAt = "23:59", from = "2026-01-01",
        });
        await harness.CallAsync(RotaView.Write, scope, "assign", new
        {
            staffId = staff,
            date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            shiftId = shift.GetProperty("id").GetGuid(),
            department = "FO",
        });

        var board = await harness.CallAsync(ReadViews.Answer, scope, "shiftBoard");
        var row = board.GetProperty("rows")[0];

        Assert.Equal(JsonValueKind.Number, row.GetProperty("value").ValueKind);
        Assert.Equal(1, row.GetProperty("value").GetInt32());
        Assert.Equal("count", row.GetProperty("form").GetString());
    }

    /// <summary>Every string leaf, recording those carrying a digit; returns how many leaves.</summary>
    private static int Walk(JsonElement node, string path, string? key, List<string> written)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                return node.EnumerateObject().Sum(one => Walk(one.Value, $"{path}.{one.Name}", one.Name, written));
            case JsonValueKind.Array:
                return node.EnumerateArray().Select((one, at) => Walk(one, $"{path}[{at}]", key, written)).Sum();
            case JsonValueKind.String:
                var text = node.GetString() ?? "";
                if (!Iso.Contains(key ?? "") && text.Any(char.IsAsciiDigit))
                {
                    written.Add($"{path} = \"{text}\"");
                }
                return 1;
            default:
                return 1;
        }
    }
}
