using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Every write this application serves, executed, and seen on the next read.
/// </summary>
/// <remarks>
/// <para>
/// The primary part, <c>ModuleSurfaceTests.cs</c>, carries this suite's contract
/// and the arrangement these rows are built on. Each test here changes the
/// database and then reads it back: a write asserted only against its own
/// response would pass over a service that answered correctly and persisted
/// nothing.
/// </para>
/// </remarks>
public partial class ModuleSurfaceTests
{
    [Fact]
    public async Task Posting_somebody_puts_them_on_the_next_read()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, "Sneha Iyer");

        var written = await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff,
            department = "FO",
            role = "Receptionist",
            from = September.ToString("yyyy-MM-dd"),
        });

        Assert.Equal(1, written.GetProperty("version").GetInt64());

        var answer = await harness.CallAsync(PeopleView.Page, scope, "people");
        Assert.Equal("Sneha Iyer", answer.GetProperty("postings")[0].GetProperty("who").GetString());
    }

    [Fact]
    public async Task Ending_a_posting_closes_the_memberships_it_supported()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Rajan Pillai", "KIT", "Sous chef");
        var team = await Form(harness, scope, "KIT", "Banquet Service");

        await harness.CallAsync(TeamsView.Write, scope, "addMember", new
        {
            teamId = team,
            staffId = staff,
            on = September.ToString("yyyy-MM-dd"),
        });

        var posting = await Posting(harness, scope, staff);

        await harness.CallAsync(PeopleView.Write, scope, "end", new
        {
            id = posting.Id,
            version = posting.Version,
            lastDay = September.AddDays(3).ToString("yyyy-MM-dd"),
        });

        var after = await harness.CallAsync(
            TeamsView.List, scope, "teams",
            new { on = September.AddDays(5).ToString("yyyy-MM-dd") });

        // One commit, and this is what the consequence panel promised — the
        // membership goes with the posting rather than outliving it.
        Assert.Equal(0, after.GetProperty("teams")[0].GetProperty("members").GetInt32());
    }

    [Fact]
    public async Task A_stale_version_is_a_conflict_rather_than_a_silent_overwrite()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Rahul Nair", "SEC", "Security officer");
        var posting = await Posting(harness, scope, staff);

        // **Captured as a value, not read off the entity.** The harness holds
        // one DbContext, so `posting` is a TRACKED instance and its Version
        // follows the write — reading it after the first end would hand the
        // second call a version that is fresh, and this test would pass without
        // ever presenting a stale one. Over HTTP each call has its own context
        // and a screen genuinely holds the number it read.
        var read = posting.Version;
        var id = posting.Id;

        await harness.CallAsync(PeopleView.Write, scope, "end", new
        {
            id,
            version = read,
            lastDay = September.AddDays(3).ToString("yyyy-MM-dd"),
        });

        // The second edit carries the version the first one replaced. It must
        // not win quietly: the platform maps this to 409, which reaches the
        // bundle as `rejected` and the screen renders.
        await Assert.ThrowsAsync<ConcurrencyException>(
            () => harness.CallAsync(PeopleView.Write, scope, "end", new
            {
                id,
                version = read,
                lastDay = September.AddDays(4).ToString("yyyy-MM-dd"),
            }));
    }

    [Fact]
    public async Task A_write_naming_no_id_is_invalid_rather_than_acting_on_a_default()
    {
        var harness = new ModuleHarness(fixture);

        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(
            () => harness.CallAsync(
                PeopleView.Write, ModuleHarness.Property(), "end", new { version = 1 }));

        Assert.Contains("'id' is required", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_write_method_is_refused()
    {
        var harness = new ModuleHarness(fixture);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => harness.CallAsync(PeopleView.Write, ModuleHarness.Property(), "delete"));
    }

    [Fact]
    public async Task Standing_a_team_down_and_back_up_flips_it_both_ways()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var team = await Form(harness, scope, "KIT", "Pool Bar");

        var down = await harness.CallAsync(TeamsView.Write, scope, "standing", new
        {
            id = team,
            version = 1L,
            active = false,
            keepMembers = true,
        });

        Assert.False(down.GetProperty("active").GetBoolean());

        var up = await harness.CallAsync(TeamsView.Write, scope, "standing", new
        {
            id = team,
            version = down.GetProperty("version").GetInt64(),
            active = true,
        });

        // Reactivation is required, not optional: a Deactivate with no
        // counterpart states a capability in the schema and withholds it from
        // the service — ADR 0062.
        Assert.True(up.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Renaming_a_team_to_one_that_exists_is_refused_by_the_service()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        await Form(harness, scope, "HK", "Morning Crew");
        var second = await Form(harness, scope, "HK", "Tower Block");

        // The duplicate rule is the service's. A screen that checked first would
        // be a second copy of it, and the copy is the one that goes stale.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => harness.CallAsync(TeamsView.Write, scope, "rename", new
            {
                id = second,
                version = 1L,
                name = "Morning Crew",
            }));
    }

    [Fact]
    public async Task Clearing_a_cell_puts_the_gap_back()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Priya Thomas", "FO", "Supervisor");
        var shift = await DefineShift(harness, scope, "Morning", "M");
        var monday = new DateOnly(2026, 8, 24);

        await harness.CallAsync(RotaView.Write, scope, "assign", new
        {
            staffId = staff,
            date = monday.ToString("yyyy-MM-dd"),
            shiftId = shift,
            department = "FO",
        });

        await harness.CallAsync(RotaView.Write, scope, "clear", new
        {
            staffId = staff,
            date = monday.ToString("yyyy-MM-dd"),
        });

        var answer = await harness.CallAsync(
            RotaView.Week, scope, "week", new { week = monday.ToString("yyyy-MM-dd") });

        var cell = answer.GetProperty("people")[0].GetProperty("week")[0];
        Assert.Equal(JsonValueKind.Null, cell.GetProperty("shift").ValueKind);
        Assert.True(cell.GetProperty("gap").GetBoolean());
    }

    [Fact]
    public async Task Copying_a_week_forward_writes_the_next_one()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Priya Thomas", "FO", "Supervisor");
        var shift = await DefineShift(harness, scope, "Morning", "M");
        var monday = new DateOnly(2026, 8, 24);

        await harness.CallAsync(RotaView.Write, scope, "assign", new
        {
            staffId = staff,
            date = monday.ToString("yyyy-MM-dd"),
            shiftId = shift,
            department = "FO",
        });

        var copied = await harness.CallAsync(RotaView.Write, scope, "copyWeek", new
        {
            from = monday.ToString("yyyy-MM-dd"),
            to = monday.AddDays(7).ToString("yyyy-MM-dd"),
        });

        Assert.Equal(1, copied.GetProperty("copied").GetInt32());

        var next = await harness.CallAsync(
            RotaView.Week, scope, "week",
            new { week = monday.AddDays(7).ToString("yyyy-MM-dd") });

        Assert.Equal(
            "M",
            next.GetProperty("people")[0].GetProperty("week")[0]
                .GetProperty("shift").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Amending_attendance_changes_the_record_and_bumps_its_version()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Sneha Iyer", "FO", "Receptionist");
        var day = new DateOnly(2026, 8, 28);

        var written = await harness.CallAsync(AttendanceView.Record, scope, "record", new
        {
            staffId = staff,
            on = day.ToString("yyyy-MM-dd"),
            @in = "15:38",
        });

        var amended = await harness.CallAsync(AttendanceView.Amend, scope, "amend", new
        {
            id = written.GetProperty("id").GetGuid(),
            version = written.GetProperty("version").GetInt64(),
            @in = "15:30",
            @out = "23:02",
        });

        Assert.True(
            amended.GetProperty("version").GetInt64()
            > written.GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task Setting_the_overtime_threshold_makes_the_policy_read_it_back()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        await harness.CallAsync(
            PolicyView.Write, scope, "setOvertime", new { daily = 9m, weekly = 48m });

        var answer = await harness.CallAsync(PolicyView.Read, scope, "policy");

        // **Numbers, and the sentence is the surface's** — ADR 0174, rewritten
        // rather than deleted (ADR 0034). These asserted "9 h / day", which was
        // this service composing a unit, a separator and an English word for
        // the period, in one locale's decimal notation.
        Assert.Equal(9m, answer.GetProperty("overtimeDaily").GetDecimal());
        Assert.Equal(48m, answer.GetProperty("overtimeWeekly").GetDecimal());
    }

    [Fact]
    public async Task Withdrawing_duty_leaves_the_night_uncovered_again()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Rahul Nair", "SEC", "Security officer");
        var monday = new DateOnly(2026, 8, 24);

        var duty = await harness.CallAsync(DutyView.Write, scope, "assign", new
        {
            staffId = staff,
            from = monday.ToString("yyyy-MM-dd") + "T08:00:00Z",
            to = monday.ToString("yyyy-MM-dd") + "T20:00:00Z",
        });

        await harness.CallAsync(DutyView.Write, scope, "withdraw", new
        {
            id = duty.GetProperty("id").GetGuid(),
            version = duty.GetProperty("version").GetInt64(),
        });

        var answer = await harness.CallAsync(
            DutyView.Register, scope, "register",
            new { week = monday.ToString("yyyy-MM-dd") });

        Assert.Equal(
            JsonValueKind.Null,
            answer.GetProperty("duties")[0].GetProperty("who").ValueKind);
    }
}
