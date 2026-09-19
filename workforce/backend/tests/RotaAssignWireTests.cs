using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// An assignment made from the rota the way the screen makes it: from what the
/// week read sent, with no department ever named.
/// </summary>
/// <remarks>
/// The screen has never named a department — its picker was drawn and not
/// wired — so on an installed property the week read answered
/// <c>departmentCode = ""</c>, the cell picker sent that back, and
/// <c>RotaService.AssignAsync</c> refused every assignment with
/// <i>"department_code is required"</i>. The harness fixture carries
/// <c>"FO"</c>, which is why no rendering ever showed it (found building the
/// capability ledger, 2026-09-19).
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class RotaAssignWireTests(WorkforceFixture fixture)
{
    private static readonly DateOnly Monday = new(2026, 8, 24);

    [Fact]
    public async Task A_cell_is_assigned_from_what_an_unfiltered_week_sent()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        // Housekeeping, not the fixture's Front Office — a code nothing on the
        // screen could have supplied except the person's own posting.
        var staff = await Post(harness, scope, "HK");
        var shift = await Morning(harness, scope);

        var week = await harness.CallAsync(
            RotaView.Week, scope, "week", new { week = Monday.ToString("yyyy-MM-dd") });

        var person = week.GetProperty("people").EnumerateArray()
            .Single(one => one.GetProperty("id").GetGuid() == staff);

        // What the picker sends: the row's department code.
        var department = person.TryGetProperty("departmentCode", out var code)
            ? code.GetString()
            : week.GetProperty("departmentCode").GetString();

        // Assigned before the code is checked, so the red is the service's own
        // refusal — the failure the property meets — and not this test's opinion.
        await harness.CallAsync(RotaView.Write, scope, "assign", new
        {
            staffId = staff,
            date = Monday.ToString("yyyy-MM-dd"),
            shiftId = shift,
            department,
        });

        Assert.Equal("HK", department);

        var after = await harness.CallAsync(
            RotaView.Week, scope, "week", new { week = Monday.ToString("yyyy-MM-dd") });

        var cell = after.GetProperty("people").EnumerateArray()
            .Single(one => one.GetProperty("id").GetGuid() == staff)
            .GetProperty("week")[0];

        Assert.NotEqual(JsonValueKind.Null, cell.GetProperty("shift").ValueKind);
    }

    private static async Task<Guid> Post(ModuleHarness harness, RequestScope scope, string department)
    {
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, "Meera Pillai");

        await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff,
            department,
            role = "Room attendant",
            from = new DateOnly(2026, 1, 1).ToString("yyyy-MM-dd"),
        });

        return staff;
    }

    private static async Task<Guid> Morning(ModuleHarness harness, RequestScope scope)
    {
        var shift = await harness.CallAsync(PolicyView.Write, scope, "defineShift", new
        {
            name = "Morning",
            code = "M",
            colour = "Cyan",
            startsAt = "07:00",
            endsAt = "15:00",
            from = new DateOnly(2026, 1, 1).ToString("yyyy-MM-dd"),
        });

        return shift.GetProperty("id").GetGuid();
    }
}
