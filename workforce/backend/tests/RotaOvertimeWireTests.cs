using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The rota's overtime warning, as it reaches the screen: numbers, never a phrase.
/// </summary>
/// <remarks>
/// <para>
/// The warning carried <c>threshold = "over the weekly threshold"</c> or
/// <c>"2 day over"</c> — English composed here, in the service's culture — and
/// the screen wrote <i>"is planned {planned} hours against {threshold}"</i>, so a
/// real property read <b>"is planned 60 hours against over the weekly
/// threshold."</b> The fixture sent <c>"48"</c>, which is why the harness never
/// showed it (found during the U1 sweep, 2026-09-19).
/// </para>
/// <para>
/// A comment beside the old field said the threshold was <i>"read from policy
/// once"</i>. Nothing read it. The check did hold the policy it measured
/// against, so the thresholds now travel with the warning from there.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class RotaOvertimeWireTests(WorkforceFixture fixture)
{
    private static readonly DateOnly Monday = new(2026, 8, 24);

    [Fact]
    public async Task A_week_over_both_thresholds_says_so_in_numbers()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var staff = await Post(harness, scope, "Vishnu Das");
        var longDay = await LongDay(harness, scope);

        await Threshold(harness, scope, daily: 9m, weekly: 48m);
        await Assign(harness, scope, staff, longDay, days: 5);

        var warning = Overtime(await Week(harness, scope));

        Assert.Equal(60m, warning.GetProperty("planned").GetDecimal());
        Assert.True(warning.GetProperty("weekly").GetBoolean());
        Assert.Equal(48m, warning.GetProperty("weeklyHours").GetDecimal());
        Assert.Equal(9m, warning.GetProperty("dailyHours").GetDecimal());
        Assert.Equal(5, warning.GetProperty("daysOver").GetInt32());
        Assert.False(warning.TryGetProperty("threshold", out _),
            "the composed phrase is gone — the screen writes the sentence");
    }

    [Fact]
    public async Task A_property_with_only_a_daily_threshold_sends_no_weekly_one()
    {
        // Values chosen so the two thresholds cannot stand in for each other:
        // no weekly figure at all, and a day count that is not the week's.
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var staff = await Post(harness, scope, "Sneha Iyer");
        var longDay = await LongDay(harness, scope);

        await Threshold(harness, scope, daily: 10m, weekly: null);
        await Assign(harness, scope, staff, longDay, days: 2);

        var warning = Overtime(await Week(harness, scope));

        Assert.False(warning.GetProperty("weekly").GetBoolean());
        Assert.Equal(JsonValueKind.Null, warning.GetProperty("weeklyHours").ValueKind);
        Assert.Equal(10m, warning.GetProperty("dailyHours").GetDecimal());
        Assert.Equal(2, warning.GetProperty("daysOver").GetInt32());
    }

    private static JsonElement Overtime(JsonElement week)
    {
        var overtime = week.GetProperty("overtime");
        Assert.Equal(1, overtime.GetArrayLength());
        return overtime[0];
    }

    private static Task<JsonElement> Week(ModuleHarness harness, RequestScope scope)
        => harness.CallAsync(RotaView.Week, scope, "week", new { week = Monday.ToString("yyyy-MM-dd") });

    private static async Task<Guid> Post(ModuleHarness harness, RequestScope scope, string name)
    {
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, name);

        await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff,
            department = "FO",
            role = "Night auditor",
            from = new DateOnly(2026, 1, 1).ToString("yyyy-MM-dd"),
        });

        return staff;
    }

    /// <summary>A twelve-hour shift, so any day of it is over a nine- or ten-hour threshold.</summary>
    private static async Task<Guid> LongDay(ModuleHarness harness, RequestScope scope)
    {
        var shift = await harness.CallAsync(PolicyView.Write, scope, "defineShift", new
        {
            name = "Long day",
            code = "LD",
            colour = "Cyan",
            startsAt = "06:00",
            endsAt = "18:00",
            from = new DateOnly(2026, 1, 1).ToString("yyyy-MM-dd"),
        });

        return shift.GetProperty("id").GetGuid();
    }

    private static Task Threshold(
        ModuleHarness harness, RequestScope scope, decimal daily, decimal? weekly)
        => harness.CallAsync(PolicyView.Write, scope, "setOvertime", new { daily, weekly });

    private static async Task Assign(
        ModuleHarness harness, RequestScope scope, Guid staff, Guid shift, int days)
    {
        for (var day = 0; day < days; day += 1)
        {
            await harness.CallAsync(RotaView.Write, scope, "assign", new
            {
                staffId = staff,
                date = Monday.AddDays(day).ToString("yyyy-MM-dd"),
                shiftId = shift,
                department = "FO",
            });
        }
    }
}
