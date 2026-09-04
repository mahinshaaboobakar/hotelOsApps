using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Attendance;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Application.Summaries;
using HotelOS.Workforce.Application.Swaps;
using HotelOS.Workforce.Application.Teams;
using HotelOS.Workforce.Domain;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The five reads behind the dock widgets — and the four questions they turned
/// out to be about.
/// </summary>
/// <remarks>
/// <para>
/// These assert the rules that were <b>found by trying to draw them</b>: that a
/// night shift is worked on yesterday's cell, that a split shift has a gap in
/// it, that "present" means the same thing here as on the Attendance screen,
/// and that a name is read once for a whole card rather than once per row.
/// </para>
/// <para>
/// The clock is frozen, because every one of these answers <i>now</i>. Two of
/// them are about six in the morning specifically, and there is no arranging
/// for that with the real one.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class SummaryCharacterisationTests(WorkforceFixture fixture)
{
    private static int slot = -1;
    private static int code = -1;

    /// <summary>A distinct fortnight per test, so two never share a day.</summary>
    private static DateOnly SomeDay() =>
        new DateOnly(2031, 1, 6).AddDays(Interlocked.Increment(ref slot) * 14);

    /// <summary>A department code no other test in this suite posts into.</summary>
    /// <remarks>
    /// The suite shares one property in one scratch database, so a count of who
    /// is posted to <c>HK</c> is a count of how many other tests ran first. A
    /// test that asserts a department's size needs one of its own.
    /// </remarks>
    private static string Department() => $"D{Interlocked.Increment(ref code)}";

    [Fact]
    public async Task At_six_in_the_morning_the_night_shift_is_yesterdays_cell()
    {
        var day = SomeDay();
        var clock = At(day, 6, 0);
        var (board, rota, shifts, _) = Board(clock);
        var scope = fixture.Scope();

        var night = await Shift(shifts, scope, "N", 23, 7);
        var staff = Guid.CreateVersion7();

        // Yesterday's rota, because a night shift belongs to the date it starts.
        await rota.AssignAsync(scope, Assign(staff, day.AddDays(-1), night), default);

        var view = await board.ReadAsync(scope, default);

        // The whole reason `ShiftCoverage` exists. A window of one day answers
        // "nobody is on" every night, for eight hours, and looks correct.
        Assert.Equal(1, view.OnNow);
        Assert.Equal(1, view.Departments);
    }

    [Fact]
    public async Task A_split_shift_leaves_a_gap_and_the_gap_is_not_covered()
    {
        var day = SomeDay();
        var (board, rota, shifts, _) = Board(At(day, 16, 0));
        var scope = fixture.Scope();

        var split = await Split(shifts, scope);
        await rota.AssignAsync(scope, Assign(Guid.CreateVersion7(), day, split), default);

        // 10–14 and 18–22: at four o'clock nobody on this shift is working, and
        // `start <= now < end` over the outer bounds would say otherwise.
        Assert.Equal(0, (await board.ReadAsync(scope, default)).OnNow);
    }

    [Fact]
    public async Task The_split_shifts_second_half_is_not_somebody_arriving()
    {
        var day = SomeDay();
        var (board, rota, shifts, _) = Board(At(day, 12, 0));
        var scope = fixture.Scope();

        var split = await Split(shifts, scope);
        await rota.AssignAsync(scope, Assign(Guid.CreateVersion7(), day, split), default);

        var change = (await board.ReadAsync(scope, default)).NextChange;

        // The next boundary is 14:00, where they go off. Counting spans rather
        // than people would report the 18:00 restart as an arrival and the same
        // person as two.
        Assert.NotNull(change);
        Assert.Equal(0, change.On);
        Assert.Equal(1, change.Off);
    }

    [Fact]
    public async Task Nothing_more_today_is_no_changeover_rather_than_a_placeholder()
    {
        var day = SomeDay();
        var (board, rota, shifts, _) = Board(At(day, 10, 0));
        var scope = fixture.Scope();

        // Nobody is rostered at all, so there is no boundary to find.
        Assert.Null((await board.ReadAsync(scope, default)).NextChange);

        var morning = await Shift(shifts, scope, "M", 7, 15);
        await rota.AssignAsync(scope, Assign(Guid.CreateVersion7(), day, morning), default);

        // And when there is one, it is the end of the shift being worked.
        var change = (await board.ReadAsync(scope, default)).NextChange;
        Assert.NotNull(change);
        Assert.Equal(new TimeOnly(15, 0), TimeOnly.FromDateTime(change.At.UtcDateTime));
    }

    [Fact]
    public async Task Present_means_rostered_and_present_which_is_what_the_screen_shows()
    {
        var day = SomeDay();
        var clock = At(day, 12, 0);
        var (attendance, rota, shifts, marks, directory) = Attendance(clock);
        var scope = fixture.Scope();

        var morning = await Shift(shifts, scope, "M", 7, 15);
        var rostered = Guid.CreateVersion7();
        var unplanned = Guid.CreateVersion7();

        await rota.AssignAsync(scope, Assign(rostered, day, morning), default);
        await marks.RecordAsync(scope, Mark(rostered, day, 7, 15), default);

        // Somebody who came and was not on the rota — real, and the reason
        // `DayComparison` is a union rather than a join.
        await marks.RecordAsync(scope, Mark(unplanned, day, 9, 17), default);

        var view = await attendance.ReadAsync(scope, default);

        // One rostered, one present. The unplanned arrival is not counted in
        // either — which is the Attendance screen's own reading, and the ruling
        // was that there is one answer rather than one per surface.
        Assert.Equal(1, view.Rostered);
        Assert.Equal(1, view.Present);
        Assert.Equal(0, view.Absent);
    }

    [Fact]
    public async Task A_late_arrival_is_named_once_for_the_whole_card()
    {
        var day = SomeDay();
        var (attendance, rota, shifts, marks, directory) = Attendance(At(day, 12, 0));
        var scope = fixture.Scope();

        var morning = await Shift(shifts, scope, "M", 7, 15);
        var known = Guid.CreateVersion7();
        var stranger = Guid.CreateVersion7();

        directory.WithName(known, "S. Kumar");

        foreach (var person in new[] { known, stranger })
        {
            await rota.AssignAsync(scope, Assign(person, day, morning), default);
            await marks.RecordAsync(scope, Mark(person, day, 7, 15) with
            {
                InAt = new TimeOnly(7, 22),
            }, default);
        }

        var view = await attendance.ReadAsync(scope, default);

        Assert.Equal(2, view.Late);

        // One call for both — the port takes a set precisely so a card does not
        // cost one round trip per row.
        Assert.Single(directory.NameLookups);
        Assert.Equal(2, directory.NameLookups[0].Count);

        // And a name this directory does not know stays null. "Unknown" would be
        // this application deciding what somebody is called.
        Assert.Equal("S. Kumar", view.LateIn.Single(row => row.Person.StaffId == known).Person.Name);
        Assert.Null(view.LateIn.Single(row => row.Person.StaffId == stranger).Person.Name);
    }

    [Fact]
    public async Task Pending_is_the_whole_property_and_the_oldest_is_first()
    {
        var day = SomeDay();
        var clock = At(day, 9, 0);
        var (pending, leave, types, swaps, rota, shifts, postings) = Pending(clock);
        var scope = fixture.Scope();

        var casual = await Type(types, scope, $"CL{Interlocked.Increment(ref code)}");
        var raiser = Guid.CreateVersion7();
        await postings.CreateAsync(scope, Post(raiser, "HK"), default);

        // Raised six days ago, and nobody has answered.
        clock.Now = At(day.AddDays(-6), 9, 0).Now;
        await leave.RaiseAsync(scope, Raise(raiser, casual, day.AddDays(20), day.AddDays(21)), default);

        clock.Now = At(day.AddDays(-2), 9, 0).Now;
        var second = Guid.CreateVersion7();
        await postings.CreateAsync(scope, Post(second, "KIT"), default);
        await leave.RaiseAsync(scope, Raise(second, casual, day.AddDays(30), day.AddDays(30)), default);

        clock.Now = At(day, 9, 0).Now;

        var view = await pending.ReadAsync(scope, default);

        // **Asserted over this test's own two rows, not over the total.** The
        // read is property-wide by design and the suite shares one property, so
        // a count here would be a count of how many other tests left something
        // waiting — the read would be right and the test would fail.
        var mine = view.Rows
            .Where(row => row.Raiser.StaffId == raiser || row.Raiser.StaffId == second)
            .ToList();

        // Property-wide: neither of these is waiting on the caller, and the
        // per-approver queues that existed before could not see them at all.
        Assert.Equal(2, mine.Count);

        // Oldest first, and the age is time waiting rather than time until the
        // day off — the second request's leave falls later and it is still second.
        Assert.Equal(raiser, mine[0].Raiser.StaffId);
        Assert.Equal(6, mine[0].WaitingDays);
        Assert.Equal(2, mine[1].WaitingDays);

        // The department is the posting's; neither request carries one.
        Assert.Equal("HK", mine[0].DepartmentCode);
        Assert.Equal("KIT", mine[1].DepartmentCode);
    }

    [Fact]
    public async Task Two_away_from_one_department_is_an_overlap_and_one_is_not()
    {
        var day = SomeDay();
        var clock = At(day, 9, 0);
        var (coming, leave, types, postings, _) = Coming(clock);
        var scope = fixture.Scope();

        var casual = await Type(types, scope, $"CU{Interlocked.Increment(ref code)}");
        var team = new[] { Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7() };

        // **A department this test owns.** `Posted` counts everybody posted
        // there, and the suite shares one property in one scratch database — so
        // asserting a size against a department other tests also post into is
        // asserting against how many of them ran first.
        var department = Department();

        // Posted at all, because the department comes from the posting and
        // somebody with none is left out rather than filed under a blank one.
        foreach (var person in team)
        {
            await postings.CreateAsync(scope, Post(person, department), default);
        }

        var overlapping = day.AddDays(3);

        // Two of the three away on one day.
        await leave.RaiseAsync(scope, Raise(team[0], casual, overlapping, overlapping), default);
        await leave.RaiseAsync(scope, Raise(team[1], casual, overlapping, overlapping), default);

        // And one away on another, which is not an overlap.
        await leave.RaiseAsync(
            scope, Raise(team[2], casual, day.AddDays(5), day.AddDays(5)), default);

        var view = await coming.ReadAsync(scope, default);

        var overlap = Assert.Single(view.Overlaps, row => row.DepartmentCode == department);
        Assert.Equal(overlapping, overlap.On);
        Assert.Equal(2, overlap.Away);

        // Both numbers, never a ratio: two away is a different fact in a
        // department of three than in one of thirty.
        Assert.Equal(3, overlap.Posted);
    }

    [Fact]
    public async Task On_leave_counts_people_and_not_days_and_only_what_was_approved()
    {
        var day = SomeDay();
        var clock = At(day, 9, 0);
        var (onLeave, leave, types, postings, directory) = Away(clock);
        var scope = fixture.Scope();

        var casual = await Type(types, scope, $"OL{Interlocked.Increment(ref code)}");
        var granted = Guid.CreateVersion7();
        var waiting = Guid.CreateVersion7();

        await postings.CreateAsync(scope, Post(granted, "HK"), default);
        await postings.CreateAsync(scope, Post(waiting, "HK"), default);
        directory.WithName(granted, "P. Das");

        var approved = await leave.RaiseAsync(
            scope, Raise(granted, casual, day, day.AddDays(2)), default);
        await leave.ApproveAsync(
            scope,
            new DecideLeaveCommand { Id = approved.Id, ExpectedVersion = approved.Version },
            default);

        // Requested and not decided: this person is at work.
        await leave.RaiseAsync(scope, Raise(waiting, casual, day, day), default);

        var view = await onLeave.ReadAsync(scope, default);

        // One person away for three days is one person, not three absences.
        Assert.Equal(1, view.AwayToday);
        Assert.Equal(1, view.AwayThisWeek);

        var department = Assert.Single(view.Today);
        Assert.Equal("HK", department.DepartmentCode);
        Assert.Equal("P. Das", Assert.Single(department.People).Name);
    }

    private static FrozenClock At(DateOnly day, int hour, int minute) =>
        new(new DateTimeOffset(day.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero));

    private static AssignShiftCommand Assign(Guid staff, DateOnly date, Guid entry) =>
        new()
        {
            StaffId = staff,
            Date = date,
            CatalogueEntryId = entry,
            DepartmentCode = "FO",
        };

    private static RecordAttendanceCommand Mark(Guid staff, DateOnly day, int inAt, int outAt) =>
        new()
        {
            StaffId = staff,
            BusinessDate = day,
            InAt = new TimeOnly(inAt, 0),
            OutAt = new TimeOnly(outAt, 0),
        };

    private static RaiseLeaveCommand Raise(Guid staff, Guid type, DateOnly from, DateOnly to) =>
        new() { StaffId = staff, LeaveTypeId = type, From = from, To = to };

    private static CreatePostingCommand Post(Guid staff, string department) =>
        new()
        {
            StaffId = staff,
            DepartmentCode = department,
            JobRole = "Attendant",
            EffectiveFrom = new DateOnly(2030, 1, 1),
        };

    private static async Task<Guid> Type(LeaveTypeService types, RequestScope scope, string code)
    {
        var type = await types.SetAsync(
            scope,
            new SetLeaveTypeCommand { Code = code, Name = $"Leave {code}", AccrualPerMonth = 2m },
            default);

        return type.Id;
    }

    private static async Task<Guid> Shift(
        ShiftCatalogueService shifts, RequestScope scope, string prefix, int from, int to)
    {
        var entry = await shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = $"Shift {prefix}",
                // Prefixed for this class, and numbered from the counter every
                // helper here shares. The suite runs one property in one
                // database, so two classes minting "M0" is a duplicate-code
                // refusal in whichever ran second — which is a real rule
                // failing a test that was not about it.
                ShortCode = $"Z{prefix}{Interlocked.Increment(ref code)}",
                Colour = "cyan",
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(from, 0),
                    EndsAt = new TimeOnly(to, 0),
                },
                EffectiveFrom = new DateOnly(2030, 1, 1),
            },
            default);

        return entry.Id;
    }

    /// <summary>10–14 and 18–22 — the property's own split shift.</summary>
    private static async Task<Guid> Split(ShiftCatalogueService shifts, RequestScope scope)
    {
        var entry = await shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = "Split — Banquet",
                ShortCode = $"ZS{Interlocked.Increment(ref code)}",
                Colour = "amber",
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(10, 0),
                    EndsAt = new TimeOnly(14, 0),
                    SecondStartsAt = new TimeOnly(18, 0),
                    SecondEndsAt = new TimeOnly(22, 0),
                },
                EffectiveFrom = new DateOnly(2030, 1, 1),
            },
            default);

        return entry.Id;
    }

    private (ShiftBoardSummary Board, RotaService Rota, ShiftCatalogueService Shifts,
        RecordingAuthorizer Authorizer) Board(TimeProvider clock)
    {
        var authorizer = new RecordingAuthorizer();
        var db = fixture.Context();

        return (
            new ShiftBoardSummary(db, authorizer, clock),
            new RotaService(db, authorizer, clock),
            new ShiftCatalogueService(db, authorizer, clock),
            authorizer);
    }

    private (AttendanceTodaySummary Summary, RotaService Rota, ShiftCatalogueService Shifts,
        AttendanceService Marks, StaffDirectoryDouble Directory) Attendance(TimeProvider clock)
    {
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();
        var db = fixture.Context();

        return (
            new AttendanceTodaySummary(new DayComparison(db, authorizer), directory, clock),
            new RotaService(db, authorizer, clock),
            new ShiftCatalogueService(db, authorizer, clock),
            new AttendanceService(db, authorizer, clock),
            directory);
    }

    private (PendingRequestsSummary Summary, LeaveService Leave, LeaveTypeService Types,
        SwapProposalService Swaps, RotaService Rota, ShiftCatalogueService Shifts,
        PostingService Postings) Pending(TimeProvider clock)
    {
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();
        var db = fixture.Context();

        var leave = new LeaveService(db, authorizer, new ApproverResolver(db), clock);
        var swaps = new SwapProposalService(db, authorizer, new ApproverResolver(db), clock);

        return (
            new PendingRequestsSummary(db, swaps, leave, directory, clock),
            leave,
            new LeaveTypeService(db, authorizer, directory, clock),
            swaps,
            new RotaService(db, authorizer, clock),
            new ShiftCatalogueService(db, authorizer, clock),
            Postings(db, authorizer, directory, clock));
    }

    private (ComingUpSummary Summary, LeaveService Leave, LeaveTypeService Types,
        PostingService Postings, StaffDirectoryDouble Directory) Coming(TimeProvider clock)
    {
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();
        var db = fixture.Context();

        return (
            new ComingUpSummary(
                db, new CapabilityService(db, authorizer, clock), authorizer, directory, clock),
            new LeaveService(db, authorizer, new ApproverResolver(db), clock),
            new LeaveTypeService(db, authorizer, directory, clock),
            Postings(db, authorizer, directory, clock),
            directory);
    }

    private (OnLeaveSummary Summary, LeaveService Leave, LeaveTypeService Types,
        PostingService Postings, StaffDirectoryDouble Directory) Away(TimeProvider clock)
    {
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();
        var db = fixture.Context();

        return (
            new OnLeaveSummary(db, authorizer, directory, clock),
            new LeaveService(db, authorizer, new ApproverResolver(db), clock),
            new LeaveTypeService(db, authorizer, directory, clock),
            Postings(db, authorizer, directory, clock),
            directory);
    }

    private static PostingService Postings(
        Infrastructure.WorkforceDbContext db,
        IKernelAuthorizer authorizer,
        StaffDirectoryDouble directory,
        TimeProvider clock) =>
        new(db, authorizer, directory,
            new PostingAnnouncer(new RecordingEventAppender(), directory),
            new TeamService(db, authorizer, directory, clock), clock);
}
