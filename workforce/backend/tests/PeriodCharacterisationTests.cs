using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Application.Attendance;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Periods;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The month-end numbers — and the slice that proves the earlier ones compose.
/// </summary>
/// <remarks>
/// <c>WorkforcePeriod</c> draws on the rota, attendance and leave at once, so
/// every figure here is a fact produced by a different slice. <b>Workforce
/// produces the numbers and never calculates pay</b> — chapter 01 §3.7 — which is
/// why nothing in this suite mentions a rate.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class PeriodCharacterisationTests(WorkforceFixture fixture)
{
    private static int slot = -1;

    private static DateOnly SomeMonth() =>
        new DateOnly(2030, 1, 1).AddDays(Interlocked.Increment(ref slot) * 40);

    [Fact]
    public async Task A_month_counts_posted_present_absent_and_late()
    {
        var w = Build();
        var scope = fixture.Scope();
        var month = SomeMonth();
        var shift = await Shift(w, scope, 7, 15);
        var staff = Uuid7.NewUuid7();

        // Four days rostered.
        for (var d = 0; d < 4; d++)
        {
            await w.Rota.AssignAsync(scope, Cell(staff, month.AddDays(d), shift), default);
        }

        // Present on three, one of them late; absent on the fourth.
        await w.Attendance.RecordAsync(scope, Day(staff, month, 7, 15), default);
        await w.Attendance.RecordAsync(
            scope, Day(staff, month.AddDays(1), 7, 15) with { InAt = new TimeOnly(7, 25) },
            default);
        await w.Attendance.RecordAsync(scope, Day(staff, month.AddDays(2), 7, 15), default);

        var period = Assert.Single(await Compute(w, scope, month, staff));

        Assert.Equal(4, period.DaysPosted);
        Assert.Equal(3, period.DaysPresent);
        Assert.Equal(1, period.DaysAbsent);
        Assert.Equal(1, period.LateCount);

        // **Not 24.** Hours worked is actual, so the late day counts the 25
        // minutes it lost — eight hours twice and seven hours thirty-five once.
        // A period that reported the rostered figure would hand payroll the rota
        // wearing attendance's name, which is the whole reason these are two
        // records.
        Assert.Equal(8m + 8m + WorkedHours.Of(new TimeOnly(7, 25), new TimeOnly(15, 0)),
            period.HoursWorked);
    }

    [Fact]
    public async Task A_week_off_is_not_a_day_posted()
    {
        var w = Build();
        var scope = fixture.Scope();
        var month = SomeMonth();
        var working = await Shift(w, scope, 7, 15);
        var off = await OffShift(w, scope);
        var staff = Uuid7.NewUuid7();

        await w.Rota.AssignAsync(scope, Cell(staff, month, working), default);
        await w.Rota.AssignAsync(scope, Cell(staff, month.AddDays(1), off), default);

        var period = Assert.Single(await Compute(w, scope, month, staff));

        // WF-Q12: Week-off is a rota marker, not a shift. Counting it would tell
        // payroll somebody was scheduled on their day off.
        Assert.Equal(1, period.DaysPosted);
    }

    [Fact]
    public async Task Overtime_is_hours_beyond_the_property_daily_threshold()
    {
        var w = Build();
        var scope = fixture.Scope();
        var month = SomeMonth();
        var shift = await Shift(w, scope, 6, 18);
        var staff = Uuid7.NewUuid7();

        await w.Policy.SetOvertimeAsync(
            scope, new SetOvertimeThresholdCommand { DailyHours = 9m }, default);

        for (var d = 0; d < 3; d++)
        {
            await w.Rota.AssignAsync(scope, Cell(staff, month.AddDays(d), shift), default);
            await w.Attendance.RecordAsync(scope, Day(staff, month.AddDays(d), 6, 18), default);
        }

        var period = Assert.Single(await Compute(w, scope, month, staff));

        // Three twelve-hour days against a nine-hour threshold. Actuals, not the
        // planning warning's planned hours — WF-Q14's second half.
        Assert.Equal(36m, period.HoursWorked);
        Assert.Equal(9m, period.OvertimeHours);
    }

    [Fact]
    public async Task A_property_with_no_threshold_reports_no_overtime()
    {
        var w = Build();
        var scope = fixture.OtherPropertyScope();
        var month = SomeMonth();
        var shift = await Shift(w, scope, 6, 22);
        var staff = Uuid7.NewUuid7();

        await w.Rota.AssignAsync(scope, Cell(staff, month, shift), default);
        await w.Attendance.RecordAsync(scope, Day(staff, month, 6, 22), default);

        var period = Assert.Single(await Compute(w, scope, month, staff));

        // A property that has never opened the policy screen has not agreed to a
        // labour rule, and a figure computed against one this application invented
        // is worse than no figure.
        Assert.Equal(16m, period.HoursWorked);
        Assert.Equal(0m, period.OvertimeHours);
    }

    [Fact]
    public async Task Leave_spanning_a_month_end_is_clipped_to_the_window()
    {
        var w = Build();
        var scope = fixture.Scope();
        var month = SomeMonth();
        var staff = Uuid7.NewUuid7();
        var type = await LeaveType(w, scope);

        // Ten days, of which four fall inside the window this report covers.
        var request = await w.Leave.RaiseAsync(
            scope,
            new RaiseLeaveCommand
            {
                StaffId = staff,
                LeaveTypeId = type,
                From = month.AddDays(27),
                To = month.AddDays(36),
            },
            default);

        await w.Leave.ApproveAsync(
            scope,
            new DecideLeaveCommand { Id = request.Id, ExpectedVersion = request.Version },
            default);

        var period = Assert.Single(await Compute(w, scope, month, staff));

        // Ten days spanning a month end are not ten days in either month. Reading
        // the ledger's single debit would have put all ten in whichever month the
        // leave began.
        Assert.Equal(4m, period.LeaveTakenByType[type]);
    }

    [Fact]
    public async Task A_day_worked_off_rota_is_surfaced_rather_than_hidden()
    {
        var w = Build();
        var scope = fixture.Scope();
        var month = SomeMonth();
        var staff = Uuid7.NewUuid7();

        await w.Attendance.RecordAsync(scope, Day(staff, month, 9, 17), default);

        var period = Assert.Single(await Compute(w, scope, month, staff));

        // Nothing was planned, and eight hours were worked. It is the row that
        // needs a human before a month is signed off, and leaving it out of the
        // summary would mean finding it only by reading every day.
        Assert.Equal(0, period.DaysPosted);
        Assert.Equal(1, period.UnplannedDays);
        Assert.Equal(8m, period.HoursWorked);
    }

    [Fact]
    public async Task Rescheduling_a_shift_does_not_change_a_closed_month()
    {
        var w = Build();
        var scope = fixture.Scope();
        var month = SomeMonth();
        var shift = await Shift(w, scope, 8, 16);
        var staff = Uuid7.NewUuid7();

        await w.Policy.SetOvertimeAsync(
            scope, new SetOvertimeThresholdCommand { DailyHours = 9m }, default);

        await w.Rota.AssignAsync(scope, Cell(staff, month, shift), default);
        await w.Attendance.RecordAsync(
            scope, Day(staff, month, 8, 16) with { InAt = new TimeOnly(8, 0) }, default);

        var before = Assert.Single(await Compute(w, scope, month, staff));

        var entry = (await w.Shifts.ListAsync(scope, includeRetired: false, default))
            .Single(e => e.Id == shift);

        await w.Shifts.RescheduleAsync(
            scope,
            new RescheduleShiftCommand
            {
                Id = shift,
                ExpectedVersion = entry.Version,
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(6, 0),
                    EndsAt = new TimeOnly(18, 0),
                },
                EffectiveFrom = month.AddDays(40),
            },
            default);

        var after = Assert.Single(await Compute(w, scope, month, staff));

        // WF-Q15 reaching all the way into a month somebody has already signed
        // off. A closed month that changes because a shift was edited is the
        // failure the effective-dated catalogue exists to prevent, and this is
        // where it would be found — by payroll, in the next dispute.
        Assert.Equal(before.LateCount, after.LateCount);
        Assert.Equal(before.HoursWorked, after.HoursWorked);
        Assert.Equal(before.DaysPosted, after.DaysPosted);
    }

    [Fact]
    public void Nothing_in_a_period_names_a_rate()
    {
        var properties = typeof(WorkforcePeriod).GetProperties().Select(p => p.Name).ToList();

        // Chapter 01 §3.7: Workforce produces the numbers and never calculates
        // pay. Pay differs by country — WPS, PF, ESI — and by hotel, and building
        // it wrong is a salary dispute. Derived from the type rather than asserted
        // in prose, so the day somebody adds a rate this fails.
        Assert.DoesNotContain(
            properties,
            name => name.Contains("Pay", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Rate", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Salary", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Amount", StringComparison.OrdinalIgnoreCase));
    }

    private static AttendanceQuery Window(DateOnly month, Guid staff) => new()
    {
        From = month,
        To = month.AddDays(30),
        StaffId = staff,
    };

    private static async Task<IReadOnlyList<WorkforcePeriod>> Compute(
        World w, RequestScope scope, DateOnly month, Guid staff) =>
        await w.Periods.ComputeAsync(scope, Window(month, staff), default);

    private static RecordAttendanceCommand Day(Guid staff, DateOnly date, int inAt, int outAt) =>
        new()
        {
            StaffId = staff,
            BusinessDate = date,
            InAt = new TimeOnly(inAt, 0),
            OutAt = new TimeOnly(outAt, 0),
        };

    private static AssignShiftCommand Cell(Guid staff, DateOnly date, Guid entry) => new()
    {
        StaffId = staff,
        Date = date,
        CatalogueEntryId = entry,
        DepartmentCode = "FO",
    };

    private static async Task<Guid> Shift(World w, RequestScope scope, int from, int to)
    {
        var entry = await w.Shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = "Period shift",
                ShortCode = $"PD{Interlocked.Increment(ref slot)}",
                Colour = "cyan",
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(from, 0),
                    EndsAt = new TimeOnly(to, 0),
                },
                EffectiveFrom = new DateOnly(2029, 1, 1),
            },
            default);

        return entry.Id;
    }

    private static async Task<Guid> OffShift(World w, RequestScope scope)
    {
        var entry = await w.Shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = "Week-off",
                ShortCode = $"OF{Interlocked.Increment(ref slot)}",
                Colour = "none",
                Hours = new ShiftHoursCommand(),
                EffectiveFrom = new DateOnly(2029, 1, 1),
            },
            default);

        return entry.Id;
    }

    private static async Task<Guid> LeaveType(World w, RequestScope scope)
    {
        var type = await w.Types.SetAsync(
            scope,
            new SetLeaveTypeCommand
            {
                Code = $"PT{Interlocked.Increment(ref slot)}",
                Name = "Period leave",
                AccrualPerMonth = 2m,
            },
            default);

        return type.Id;
    }

    private sealed record World(
        PeriodService Periods,
        RotaService Rota,
        ShiftCatalogueService Shifts,
        AttendanceService Attendance,
        LeaveService Leave,
        LeaveTypeService Types,
        PolicyService Policy);

    private World Build()
    {
        var db = fixture.Context();
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();
        var clock = TimeProvider.System;

        return new World(
            new PeriodService(db, authorizer, new DayComparison(db, authorizer)),
            new RotaService(db, authorizer, clock),
            new ShiftCatalogueService(db, authorizer, clock),
            new AttendanceService(db, authorizer, clock),
            new LeaveService(db, authorizer, new ApproverResolver(db), clock),
            new LeaveTypeService(db, authorizer, directory, clock),
            new PolicyService(db, authorizer, clock));
    }
}
