using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Application.Teams;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Every capability and method this application serves to its own UI, executed.
/// </summary>
/// <remarks>
/// <para>
/// This is the wired ledger's application half. Each test is one row: a read
/// that answers with the shape a screen destructures, or a write that changes
/// the database and can be seen to have changed it on the next read.
/// </para>
/// <para>
/// <b>Every assertion is against the JSON, not the object.</b> What reaches a
/// bundle is text — a field named in the wrong case, or one whose type
/// serialises to nothing, is invisible to an assertion on the anonymous object
/// and plain against the parsed result.
/// </para>
/// <para>
/// <b>What is deliberately absent.</b> Nothing here touches the envelope: the
/// token, the capability guard and the status mapping are the platform's, and
/// they cannot execute in any application today (see <see cref="ModuleHarness"/>).
/// A test that stood those up itself would be testing a second implementation
/// of them.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class ModuleSurfaceTests(WorkforceFixture fixture)
{
    private static readonly DateOnly September = new(2026, 9, 1);

    // ── roster.read ────────────────────────────────────────────────────────

    [Fact]
    public async Task People_answers_the_page_the_screen_draws()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var staff = await Post(harness, scope, "Anjali Menon", "FO", "Receptionist");

        var answer = await harness.CallAsync(PeopleView.Page, scope, "people");

        var row = answer.GetProperty("postings")[0];
        Assert.Equal("Anjali Menon", row.GetProperty("who").GetString());
        Assert.Equal("Receptionist", row.GetProperty("role").GetString());
        Assert.Equal("FO", row.GetProperty("departments")[0].GetString());

        // The zone is a Room Care name this application cannot resolve, so it is
        // null and the screen draws an em-dash — never "Zone 1" from an index
        // nobody assigned.
        Assert.Equal(JsonValueKind.Null, row.GetProperty("zone").ValueKind);

        var paging = answer.GetProperty("paging");
        Assert.Equal(0, paging.GetProperty("page").GetInt32());
        Assert.Equal(25, paging.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, paging.GetProperty("total").GetInt32());
        Assert.NotEqual(Guid.Empty, staff);
    }

    [Fact]
    public async Task People_pages_and_the_second_page_holds_what_the_first_did_not()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        for (var index = 0; index < 7; index += 1)
        {
            await Post(harness, scope, "Person " + index, "FO", "Receptionist");
        }

        var first = await harness.CallAsync(
            PeopleView.Page, scope, "people", new { page = 0, pageSize = 5 });
        var second = await harness.CallAsync(
            PeopleView.Page, scope, "people", new { page = 1, pageSize = 5 });

        Assert.Equal(5, first.GetProperty("postings").GetArrayLength());
        Assert.Equal(2, second.GetProperty("postings").GetArrayLength());
        Assert.Equal(7, second.GetProperty("paging").GetProperty("total").GetInt32());

        // Two windows on one ordering, never two queries: a name on both pages,
        // or on neither, is the defect nothing on the screen could reveal.
        var names = Names(first).Concat(Names(second)).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public async Task An_unknown_read_is_refused_rather_than_answered_emptily()
    {
        var harness = new ModuleHarness(fixture);

        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(
            () => harness.CallAsync(ReadViews.Answer, ModuleHarness.Property(), "nonsense"));

        // `invalid` reaches the bundle and the screen says so. A null would have
        // it draw its recorded fixture and report itself live.
        Assert.Contains("nonsense", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Teams_answers_the_list_with_a_live_count_for_the_day()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        harness.Directory.WithDepartmentName("HK", "Housekeeping");

        var staff = await Post(harness, scope, "Deepa Menon", "HK", "Supervisor");
        var team = await Form(harness, scope, "HK", "Morning Crew");

        await harness.CallAsync(TeamsView.Write, scope, "addMember", new
        {
            teamId = team,
            staffId = staff,
            on = September.ToString("yyyy-MM-dd"),
        });

        var answer = await harness.CallAsync(
            TeamsView.List, scope, "teams", new { on = September.ToString("yyyy-MM-dd") });

        var row = answer.GetProperty("teams")[0];
        Assert.Equal("Morning Crew", row.GetProperty("name").GetString());
        Assert.Equal("Housekeeping", row.GetProperty("departmentName").GetString());
        Assert.Equal(1, row.GetProperty("members").GetInt32());
    }

    [Fact]
    public async Task A_team_read_the_day_before_it_was_joined_has_nobody_in_it()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Arun Kumar", "HK", "Room attendant");
        var team = await Form(harness, scope, "HK", "Tower Block");

        await harness.CallAsync(TeamsView.Write, scope, "addMember", new
        {
            teamId = team,
            staffId = staff,
            on = September.ToString("yyyy-MM-dd"),
        });

        var before = await harness.CallAsync(
            TeamsView.List, scope, "teams",
            new { on = September.AddDays(-1).ToString("yyyy-MM-dd") });

        // Membership is effective-dated, so the day the screen is asking about
        // changes the number — a count taken without a date is right once.
        Assert.Equal(0, before.GetProperty("teams")[0].GetProperty("members").GetInt32());
    }

    [Fact]
    public async Task The_candidate_who_is_not_posted_here_carries_the_services_refusal()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        await Post(harness, scope, "Joseph Kurian", "FO", "Bell captain");
        var team = await Form(harness, scope, "HK", "Morning Crew");

        var answer = await harness.CallAsync(TeamsView.List, scope, "teams", new
        {
            on = September.ToString("yyyy-MM-dd"),
            team = team.ToString(),
        });

        var candidate = answer.GetProperty("detail").GetProperty("candidates")[0];

        // The sentence is computed from the postings, not typed into the screen.
        Assert.Equal("Not posted here", candidate.GetProperty("refused").GetString());
    }

    [Fact]
    public async Task Policy_answers_the_catalogue_with_its_assignments_counted()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        await harness.CallAsync(PolicyView.Write, scope, "defineShift", new
        {
            name = "Morning",
            code = "M",
            colour = "Cyan",
            startsAt = "07:00",
            endsAt = "15:00",
            from = September.ToString("yyyy-MM-dd"),
        });

        var answer = await harness.CallAsync(PolicyView.Read, scope, "policy");
        var shift = answer.GetProperty("catalogue")[0];

        Assert.Equal("Morning", shift.GetProperty("name").GetString());
        Assert.Equal("M", shift.GetProperty("code").GetString());
        Assert.Equal("working", shift.GetProperty("kind").GetString());
        Assert.Equal("0 assignments", shift.GetProperty("inUse").GetString());
    }

    [Fact]
    public async Task An_unset_overtime_threshold_reads_as_an_em_dash_and_not_as_zero()
    {
        var harness = new ModuleHarness(fixture);

        var answer = await harness.CallAsync(
            PolicyView.Read, ModuleHarness.Property(), "policy");

        // A property that has set no threshold and one that set zero are
        // different facts, and zero is the one that reads as "never warn".
        Assert.Equal("—", answer.GetProperty("overtimeDaily").GetString());
        Assert.Equal(JsonValueKind.Null, answer.GetProperty("holidays").ValueKind);
    }

    [Fact]
    public async Task The_rota_answers_a_week_with_its_catalogue_and_its_gaps()
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

        var answer = await harness.CallAsync(
            RotaView.Week, scope, "week", new { week = monday.ToString("yyyy-MM-dd") });

        var person = answer.GetProperty("people")[0];
        Assert.Equal("Priya Thomas", person.GetProperty("name").GetString());

        var week = person.GetProperty("week");
        Assert.Equal(7, week.GetArrayLength());
        Assert.Equal("M", week[0].GetProperty("shift").GetProperty("code").GetString());
        Assert.False(week[0].GetProperty("gap").GetBoolean());

        // A day with neither a shift nor leave on it is the unfinished rota a
        // supervisor scans for, and it is stated rather than left blank.
        Assert.True(week[1].GetProperty("gap").GetBoolean());
    }

    [Fact]
    public async Task A_schedule_answers_one_persons_month()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Vishnu Das", "FO", "Night auditor");
        var shift = await DefineShift(harness, scope, "Night", "N");

        await harness.CallAsync(RotaView.Write, scope, "assign", new
        {
            staffId = staff,
            date = "2026-08-24",
            shiftId = shift,
            department = "FO",
        });

        var answer = await harness.CallAsync(
            ScheduleView.Month, scope, "schedule",
            new { staffId = staff, month = "2026-08-01" });

        Assert.Equal("Vishnu Das", answer.GetProperty("who").GetString());
        Assert.Equal("VD", answer.GetProperty("initials").GetString());
        Assert.Equal("August 2026", answer.GetProperty("month").GetString());
        Assert.Equal(1, answer.GetProperty("shifts").GetInt32());

        // The grid is padded to start on a Monday, so its length depends on
        // which day the month opens — asserting a number would be asserting
        // August 2026's calendar. What must hold is that the month is COMPLETE
        // and in order after the padding, however long the padding is.
        var days = answer.GetProperty("days").EnumerateArray().ToList();
        var dates = days.TakeLast(31).Select(one => one.GetProperty("date").GetInt32());

        Assert.Equal(Enumerable.Range(1, 31), dates);
        Assert.InRange(days.Count - 31, 0, 6);
    }

    [Fact]
    public async Task Attendance_answers_the_day_and_measures_the_lateness()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Anjali Menon", "FO", "Receptionist");
        var shift = await DefineShift(harness, scope, "Morning", "M");
        var day = new DateOnly(2026, 8, 28);

        await harness.CallAsync(RotaView.Write, scope, "assign", new
        {
            staffId = staff,
            date = day.ToString("yyyy-MM-dd"),
            shiftId = shift,
            department = "FO",
        });

        await harness.CallAsync(AttendanceView.Record, scope, "record", new
        {
            staffId = staff,
            on = day.ToString("yyyy-MM-dd"),
            @in = "07:20",
            @out = "15:10",
        });

        var answer = await harness.CallAsync(
            AttendanceView.Day, scope, "day",
            new { date = day.ToString("yyyy-MM-dd"), department = "FO" });

        var row = answer.GetProperty("rows")[0];
        Assert.Equal("Anjali Menon", row.GetProperty("who").GetString());
        Assert.Equal("07:20", row.GetProperty("in").GetString());
        Assert.Equal("15:10", row.GetProperty("out").GetString());
        Assert.Equal("Late 20 min", row.GetProperty("against").GetString());
        Assert.Equal("warn", row.GetProperty("tone").GetString());
        Assert.Equal("manual", row.GetProperty("source").GetString());
    }

    [Fact]
    public async Task An_absent_person_has_no_source_on_their_row()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Rani Rajan", "FO", "Guest relations");
        var shift = await DefineShift(harness, scope, "Afternoon", "A");
        var day = new DateOnly(2026, 8, 28);

        await harness.CallAsync(RotaView.Write, scope, "assign", new
        {
            staffId = staff,
            date = day.ToString("yyyy-MM-dd"),
            shiftId = shift,
            department = "FO",
        });

        var answer = await harness.CallAsync(
            AttendanceView.Day, scope, "day",
            new { date = day.ToString("yyyy-MM-dd"), department = "FO" });

        var row = answer.GetProperty("rows")[0];
        Assert.Equal("Absent", row.GetProperty("against").GetString());

        // Nobody entered an absence, so nothing wrote it — "manual" here would
        // attribute a record to a person who made none.
        Assert.Equal(JsonValueKind.Null, row.GetProperty("source").ValueKind);
    }

    [Fact]
    public async Task The_duty_register_answers_fourteen_bands_and_names_the_uncovered_ones()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Priya Thomas", "FO", "Supervisor");
        var monday = new DateOnly(2026, 8, 24);

        await harness.CallAsync(DutyView.Write, scope, "assign", new
        {
            staffId = staff,
            from = monday.ToString("yyyy-MM-dd") + "T08:00:00Z",
            to = monday.ToString("yyyy-MM-dd") + "T20:00:00Z",
        });

        var answer = await harness.CallAsync(
            DutyView.Register, scope, "register",
            new { week = monday.ToString("yyyy-MM-dd") });

        var bands = answer.GetProperty("duties");
        Assert.Equal(14, bands.GetArrayLength());
        Assert.Equal("Priya Thomas", bands[0].GetProperty("who").GetString());

        // The night nobody covers is a row with a null holder, never an omitted
        // row: an absent band reads as a night somebody has not got to yet.
        Assert.Equal(JsonValueKind.Null, bands[1].GetProperty("who").ValueKind);
        Assert.Equal("—", bands[1].GetProperty("hours").GetString());
    }

    [Fact]
    public async Task Reports_answers_the_month_and_leaves_holidays_absent()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var staff = await Post(harness, scope, "Priya Thomas", "FO", "Supervisor");
        var shift = await DefineShift(harness, scope, "Morning", "M");

        await harness.CallAsync(RotaView.Write, scope, "assign", new
        {
            staffId = staff,
            date = "2026-08-24",
            shiftId = shift,
            department = "FO",
        });

        var answer = await harness.CallAsync(
            ReportsView.Month, scope, "month", new { month = "2026-08-01" });

        var row = answer.GetProperty("rows")[0];
        Assert.Equal("Priya Thomas", row.GetProperty("who").GetString());
        Assert.Equal(1, row.GetProperty("posted").GetInt32());

        // Nothing here knows which days a property declared. Null, and the
        // screen draws an em-dash; a zero would be a measurement nobody took.
        Assert.Equal(JsonValueKind.Null, row.GetProperty("holidays").ValueKind);
    }

    [Fact]
    public async Task Leave_answers_the_board_and_the_queue_is_empty_without_a_viewer()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        var answer = await harness.CallAsync(LeaveView.Board, scope, "leave");

        // The queue is what is waiting on ONE person, and the scope carries a
        // user rather than a staff id. Empty rather than everybody's: a
        // supervisor's list shown to somebody else is a decision handed to the
        // wrong person.
        Assert.Empty(answer.GetProperty("waiting").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, answer.GetProperty("swap").ValueKind);
    }

    // ── the writes ─────────────────────────────────────────────────────────

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

        Assert.Equal("9 h / day", answer.GetProperty("overtimeDaily").GetString());
        Assert.Equal("48 h / week", answer.GetProperty("overtimeWeekly").GetString());
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

    // ── the fixtures these rows are built on ───────────────────────────────

    private static IEnumerable<string?> Names(JsonElement page)
        => page.GetProperty("postings").EnumerateArray()
            .Select(one => one.GetProperty("who").GetString());

    private async Task<Guid> Post(
        ModuleHarness harness, RequestScope scope, string name, string department, string role)
    {
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, name);

        await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff,
            department,
            role,
            from = September.ToString("yyyy-MM-dd"),
        });

        return staff;
    }

    private async Task<Guid> Form(
        ModuleHarness harness, RequestScope scope, string department, string name)
    {
        var formed = await harness.CallAsync(
            TeamsView.Write, scope, "form", new { department, name });

        return formed.GetProperty("id").GetGuid();
    }

    private async Task<Guid> DefineShift(
        ModuleHarness harness, RequestScope scope, string name, string code)
    {
        var shift = await harness.CallAsync(PolicyView.Write, scope, "defineShift", new
        {
            name,
            code,
            colour = "Cyan",
            startsAt = "07:00",
            endsAt = "15:00",
            from = new DateOnly(2026, 1, 1).ToString("yyyy-MM-dd"),
        });

        return shift.GetProperty("id").GetGuid();
    }

    private async Task<Posting> Posting(
        ModuleHarness harness, RequestScope scope, Guid staffId)
    {
        var service = harness.Service<PostingService>();

        var held = await service.ListAsync(
            scope, new ListPostingsQuery { StaffId = staffId }, default);

        return held[0];
    }
}
