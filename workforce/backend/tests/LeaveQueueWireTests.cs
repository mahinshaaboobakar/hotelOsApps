using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The approval queue sends the leave type and the number of days — never the
/// sentence "Casual · 3 days".
/// </summary>
/// <remarks>
/// <c>LeaveView.Queue</c> composed <c>what = type + " · " + days + " days"</c>,
/// so a number, a separator and an English plural were written here in the
/// service's culture (NUM-Q1, ADR 0174). The screen writes the sentence now.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class LeaveQueueWireTests(WorkforceFixture fixture)
{
    [Fact]
    public async Task A_waiting_leave_row_sends_its_type_and_days_as_values()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        // The department head approves — the queue is what waits on ONE person.
        var head = await Post(harness, scope, "Priya Thomas", head: true);
        var staff = await Post(harness, scope, "Anjali Menon", head: false);
        harness.Directory.WithLogin(staff, scope.UserId!.Value);

        var type = await harness.Service<LeaveTypeService>().SetAsync(
            scope,
            new SetLeaveTypeCommand { Code = "CL", Name = "Casual", AccrualPerMonth = 2m },
            default);

        // Three days, so the count is not the 1 a single day would make it.
        await harness.CallAsync(LeaveView.Request, scope, "raise", new
        {
            typeId = type.Id, from = "2026-09-14", to = "2026-09-16",
        });

        var board = await harness.CallAsync(LeaveView.Board, scope, "leave", new { staffId = head });
        var row = Assert.Single(board.GetProperty("waiting").EnumerateArray());

        Assert.Equal("Casual", row.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Number, row.GetProperty("days").ValueKind);
        Assert.Equal(3m, row.GetProperty("days").GetDecimal());
        Assert.False(row.TryGetProperty("what", out _),
            "the composed sentence is gone — the screen writes it");
    }

    private static async Task<Guid> Post(
        ModuleHarness harness, RequestScope scope, string name, bool head)
    {
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, name);

        await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff, department = "FO", role = head ? "Supervisor" : "Receptionist",
            head, from = "2026-01-01",
        });

        return staff;
    }
}
