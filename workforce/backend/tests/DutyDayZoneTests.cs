using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// A duty belongs to the day it falls on at the PROPERTY — never the UTC day.
/// </summary>
/// <remarks>
/// <para>
/// The schedule, the rota's ribbon and the duty register each turned an instant
/// into a day with <c>DateOnly.FromDateTime(instant.UtcDateTime)</c>, and the
/// register probed its bands at UTC noon and 23:00. At +05:30 a 02:00 duty
/// landed on the day before; at a negative offset an evening duty landed on the
/// day after. The zone is Master Data's (<c>Property.Timezone</c>).
/// </para>
/// <para>
/// Each instant here is chosen so its UTC date and its local date DIFFER — a
/// fixture where they agree would pass under either rule and prove neither.
/// Guatemala is the negative offset because it keeps no daylight saving, so the
/// test says the same thing in every month.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class DutyDayZoneTests(WorkforceFixture fixture)
{
    [Fact]
    public async Task A_2am_duty_at_plus_0530_is_on_its_own_day_in_the_schedule()
    {
        // 2 Oct 02:00 in Kochi is 1 Oct 20:30 UTC.
        var answer = await Schedule("Asia/Kolkata",
            from: "2026-10-01T20:30:00Z", to: "2026-10-02T02:30:00Z");

        Assert.True(HasDuty(answer, 2), "the duty is on the 2nd, where it happens");
        Assert.False(HasDuty(answer, 1), "and not on the 1st, which is only its UTC date");
    }

    [Fact]
    public async Task A_9pm_duty_at_minus_0600_is_on_its_own_day_in_the_schedule()
    {
        // 1 Oct 21:00 in Guatemala is 2 Oct 03:00 UTC.
        var answer = await Schedule("America/Guatemala",
            from: "2026-10-02T03:00:00Z", to: "2026-10-02T09:00:00Z");

        Assert.True(HasDuty(answer, 1), "the duty is on the 1st, where it happens");
        Assert.False(HasDuty(answer, 2), "and not on the 2nd, which is only its UTC date");
    }

    [Fact]
    public async Task The_rota_ribbon_starts_a_duty_in_its_own_days_column()
    {
        var harness = new ModuleHarness(fixture) ;
        harness.Directory.Zone = "Asia/Kolkata";
        var scope = ModuleHarness.Property();
        var staff = await Post(harness, scope);

        // Tuesday 6 Oct 02:00 in Kochi is Monday 5 Oct 20:30 UTC.
        await Assign(harness, scope, staff, "2026-10-05T20:30:00Z", "2026-10-06T02:30:00Z");

        var week = await harness.CallAsync(RotaView.Week, scope, "week", new { week = "2026-10-05" });
        var span = Assert.Single(week.GetProperty("duty").EnumerateArray());

        Assert.Equal(1, span.GetProperty("from").GetInt32());
    }

    [Fact]
    public async Task The_register_finds_a_night_duty_in_the_property_night()
    {
        var harness = new ModuleHarness(fixture);
        harness.Directory.Zone = "Asia/Kolkata";
        var scope = ModuleHarness.Property();
        var staff = await Post(harness, scope);

        // Monday 5 Oct 21:00 → Tuesday 02:00 in Kochi: 15:30 → 20:30 UTC. The
        // UTC probe at 23:00 is 04:30 Tuesday in Kochi, after it has ended.
        await Assign(harness, scope, staff, "2026-10-05T15:30:00Z", "2026-10-05T20:30:00Z");

        var register = await harness.CallAsync(
            DutyView.Register, scope, "register", new { week = "2026-10-05" });

        var monday = register.GetProperty("duties").EnumerateArray()
            .Where(one => one.GetProperty("day").GetInt32() == 0)
            .ToDictionary(one => one.GetProperty("band").GetString()!);

        Assert.Equal("Rahul Nair", monday["night"].GetProperty("who").GetString());
        Assert.Equal(JsonValueKind.Null, monday["day"].GetProperty("who").ValueKind);
    }

    private async Task<JsonElement> Schedule(string zone, string from, string to)
    {
        var harness = new ModuleHarness(fixture);
        harness.Directory.Zone = zone;
        var scope = ModuleHarness.Property();
        var staff = await Post(harness, scope);

        await Assign(harness, scope, staff, from, to);

        return await harness.CallAsync(
            ScheduleView.Month, scope, "schedule", new { staffId = staff, month = "2026-10-01" });
    }

    /// <summary>Whether the month's cell for this date carries a duty.</summary>
    private static bool HasDuty(JsonElement month, int date)
    {
        // October 2026 opens on a Thursday, so the padding is 28–30 September:
        // the 1st and 2nd appear once each.
        var cell = month.GetProperty("days").EnumerateArray()
            .Single(one => one.GetProperty("date").GetInt32() == date);

        return cell.TryGetProperty("dutyFrom", out var start)
               && start.ValueKind == JsonValueKind.String;
    }

    private static async Task<Guid> Post(ModuleHarness harness, RequestScope scope)
    {
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, "Rahul Nair");

        await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff, department = "SEC", role = "Security officer", from = "2026-01-01",
        });

        return staff;
    }

    private static Task Assign(
        ModuleHarness harness, RequestScope scope, Guid staff, string from, string to)
        => harness.CallAsync(DutyView.Write, scope, "assign", new { staffId = staff, from, to });
}
