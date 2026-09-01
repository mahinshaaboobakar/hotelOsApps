using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Attendance;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Domain;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Attendance: source-agnostic, provenance mandatory, and everything derived
/// that can be.
/// </summary>
/// <remarks>
/// <c>WF-Q13</c> makes v1 manual and devices later, so the shape must not change
/// when they arrive. <c>WF-Q17</c>'s equal-instants rule is here for its real
/// reason: an identical clock-in and clock-out is zero worked.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class AttendanceCharacterisationTests(WorkforceFixture fixture)
{
    private static int slot = -1;

    private static DateOnly SomeDay() =>
        new DateOnly(2029, 1, 1).AddDays(Interlocked.Increment(ref slot) * 3);

    [Fact]
    public async Task A_manual_record_names_the_account_that_entered_it()
    {
        var (attendance, _, _, _) = Build();
        var scope = fixture.Scope();

        var record = await attendance.RecordAsync(
            scope, Day(Uuid7.NewUuid7(), SomeDay(), 7, 15), default);

        // The provenance obligation's fourth surface, and the one where it
        // matters most: this record is what a wage is eventually computed from.
        Assert.Equal(AttendanceSource.Manual, record.Source);
        Assert.Equal(scope.UserId, record.RecordedByUserId);
    }

    [Fact]
    public async Task A_device_record_carries_its_reference_and_names_no_person()
    {
        var (attendance, _, _, _) = Build();
        var scope = fixture.Scope();

        var record = await attendance.RecordAsync(
            scope,
            Day(Uuid7.NewUuid7(), SomeDay(), 7, 15) with
            {
                Source = AttendanceSource.Device,
                ExternalReference = "turnstile-3/8891",
            },
            default);

        // Naming a person would attribute a machine reading to whoever happened
        // to be signed in. The reference is the provenance.
        Assert.Null(record.RecordedByUserId);
        Assert.Equal("turnstile-3/8891", record.ExternalReference);
    }

    [Fact]
    public async Task A_device_record_with_no_reference_is_refused()
    {
        var (attendance, _, _, _) = Build();

        // A reading nobody can trace cannot be reconciled when two machines
        // disagree — which is the whole reason the shape does not change when
        // devices arrive.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => attendance.RecordAsync(
                fixture.Scope(),
                Day(Uuid7.NewUuid7(), SomeDay(), 7, 15) with
                {
                    Source = AttendanceSource.Device,
                },
                default));
    }

    [Fact]
    public async Task A_night_shift_counts_eight_hours()
    {
        var (attendance, _, _, _) = Build();

        var record = await attendance.RecordAsync(
            fixture.Scope(), Day(Uuid7.NewUuid7(), SomeDay(), 23, 7), default);

        // The same arithmetic the rota plans with, reused exactly as WF-Q17
        // anticipated.
        Assert.Equal(8m, record.Worked);
    }

    [Fact]
    public async Task An_identical_in_and_out_is_zero_worked()
    {
        var (attendance, _, _, _) = Build();

        var record = await attendance.RecordAsync(
            fixture.Scope(), Day(Uuid7.NewUuid7(), SomeDay(), 9, 9), default);

        // WF-Q17, and this aggregate is the reason for it: twenty-four would put
        // a day of pay behind a typo.
        Assert.Equal(0m, record.Worked);
    }

    [Fact]
    public async Task An_open_shift_has_no_worked_hours_and_is_still_in()
    {
        var (attendance, _, _, _) = Build();
        var scope = fixture.Scope();
        var day = SomeDay();

        var record = await attendance.RecordAsync(
            scope,
            Day(Uuid7.NewUuid7(), day, 7, 15) with { OutAt = null },
            default);

        Assert.True(record.StillIn);
        Assert.Null(record.Worked);

        var open = await attendance.StillInAsync(scope, day, default);
        Assert.Contains(open, r => r.Id == record.Id);
    }

    [Fact]
    public async Task An_absence_is_a_record_with_no_arrival()
    {
        var (attendance, _, _, _) = Build();

        var record = await attendance.RecordAsync(
            fixture.Scope(),
            Day(Uuid7.NewUuid7(), SomeDay(), 7, 15) with { InAt = null, OutAt = null },
            default);

        // Somebody looked, and they were not there. Deleting the record would say
        // only that nobody looked, and those are different answers to a payroll
        // question.
        Assert.False(record.Attended);
    }

    [Fact]
    public async Task A_departure_with_no_arrival_is_refused()
    {
        var (attendance, _, _, _) = Build();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => attendance.RecordAsync(
                fixture.Scope(),
                Day(Uuid7.NewUuid7(), SomeDay(), 7, 15) with { InAt = null },
                default));
    }

    [Fact]
    public async Task Recording_twice_for_one_day_replaces_rather_than_duplicates()
    {
        var (attendance, _, _, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();
        var day = SomeDay();

        await attendance.RecordAsync(scope, Day(staff, day, 7, 15), default);
        await attendance.RecordAsync(scope, Day(staff, day, 7, 19), default);

        var records = await attendance.ReadAsync(
            scope, new AttendanceQuery { From = day, To = day, StaffId = staff }, default);

        // One answer to "what did they do that day".
        var only = Assert.Single(records);
        Assert.Equal(12m, only.Worked);
    }

    [Fact]
    public async Task Clearing_an_arrival_needs_its_own_instruction()
    {
        var (attendance, _, _, _) = Build();
        var scope = fixture.Scope();

        var record = await attendance.RecordAsync(
            scope, Day(Uuid7.NewUuid7(), SomeDay(), 7, 15), default);

        // Null already means "leave it alone" on the two time fields, so without
        // an explicit clear a mistaken arrival could never be undone except by
        // deleting the record — which loses the trail.
        var amended = await attendance.AmendAsync(
            scope,
            new AmendAttendanceCommand
            {
                Id = record.Id,
                ExpectedVersion = record.Version,
                ClearIn = true,
                ClearOut = true,
                Note = "Marked present in error",
            },
            default);

        Assert.False(amended.Attended);
        Assert.Equal("Marked present in error", amended.Note);
    }

    [Fact]
    public async Task Correcting_a_record_re_attributes_it_to_the_corrector()
    {
        var (attendance, _, _, _) = Build();
        var scope = fixture.Scope();

        var record = await attendance.RecordAsync(
            scope, Day(Uuid7.NewUuid7(), SomeDay(), 7, 15), default);

        var amended = await attendance.AmendAsync(
            scope,
            new AmendAttendanceCommand
            {
                Id = record.Id,
                ExpectedVersion = record.Version,
                OutAt = new TimeOnly(17, 30),
            },
            default);

        // A correction is still an entry, and the account that made it is the one
        // now answerable.
        Assert.Equal(scope.UserId, amended.RecordedByUserId);
        Assert.Equal(10.5m, amended.Worked);
    }

    [Fact]
    public async Task Posted_against_present_shows_the_absence_and_the_unplanned_arrival()
    {
        var (attendance, comparison, rota, shifts) = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var morning = await Shift(shifts, scope, 7, 15);
        var rostered = Uuid7.NewUuid7();
        var walkedIn = Uuid7.NewUuid7();

        await rota.AssignAsync(scope, Cell(rostered, day, morning), default);
        await attendance.RecordAsync(
            scope, Day(walkedIn, day, 7, 15), default);

        var rows = await comparison.CompareAsync(
            scope, new AttendanceQuery { From = day, To = day }, default);

        // The union, not the intersection: joining on the rota would hide the
        // person who turned up unrostered, and joining on attendance would hide
        // the absence. Those are the two rows anybody opens this screen for.
        var absent = rows.Single(r => r.StaffId == rostered);
        var unplanned = rows.Single(r => r.StaffId == walkedIn);

        Assert.True(absent.Absent);
        Assert.True(unplanned.Unplanned);
    }

    [Fact]
    public async Task Lateness_is_the_arrival_against_the_hours_in_force_that_day()
    {
        var (attendance, comparison, rota, shifts) = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var morning = await Shift(shifts, scope, 7, 15);
        var staff = Uuid7.NewUuid7();

        await rota.AssignAsync(scope, Cell(staff, day, morning), default);
        await attendance.RecordAsync(
            scope,
            Day(staff, day, 7, 15) with { InAt = new TimeOnly(7, 18) },
            default);

        var row = Assert.Single(
            await comparison.CompareAsync(
                scope, new AttendanceQuery { From = day, To = day, StaffId = staff }, default));

        Assert.Equal(TimeSpan.FromMinutes(18), row.LateBy);
        Assert.Equal(new TimeOnly(7, 0), row.ScheduledStart);
        Assert.Equal(8m, row.PlannedHours);
    }

    [Fact]
    public async Task Arriving_early_is_not_negative_lateness()
    {
        var (attendance, comparison, rota, shifts) = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var morning = await Shift(shifts, scope, 7, 15);
        var staff = Uuid7.NewUuid7();

        await rota.AssignAsync(scope, Cell(staff, day, morning), default);
        await attendance.RecordAsync(
            scope,
            Day(staff, day, 7, 15) with { InAt = new TimeOnly(6, 50) },
            default);

        var row = Assert.Single(
            await comparison.CompareAsync(
                scope, new AttendanceQuery { From = day, To = day, StaffId = staff }, default));

        // Ten minutes early is not lateness of minus ten. A signed number here
        // would eventually be summed into a figure that means nothing.
        Assert.Null(row.LateBy);
    }

    [Fact]
    public async Task A_one_off_span_is_what_lateness_is_measured_against()
    {
        var (attendance, comparison, rota, shifts) = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var morning = await Shift(shifts, scope, 7, 15);
        var staff = Uuid7.NewUuid7();

        await rota.AssignAsync(
            scope,
            Cell(staff, day, morning) with
            {
                OverrideStartsAt = new TimeOnly(5, 0),
                OverrideEndsAt = new TimeOnly(13, 0),
            },
            default);

        await attendance.RecordAsync(
            scope,
            Day(staff, day, 5, 13) with { InAt = new TimeOnly(5, 10) },
            default);

        var row = Assert.Single(
            await comparison.CompareAsync(
                scope, new AttendanceQuery { From = day, To = day, StaffId = staff }, default));

        // The override replaced the catalogue's hours for that day, so it is what
        // the person was expected against. Measuring from 07:00 would report them
        // early by two hours when they were ten minutes late.
        Assert.Equal(TimeSpan.FromMinutes(10), row.LateBy);
    }

    [Fact]
    public async Task Rescheduling_a_shift_does_not_make_somebody_late_last_month()
    {
        var (attendance, comparison, rota, shifts) = Build();
        var scope = fixture.Scope();
        var day = SomeDay();
        var later = day.AddDays(60);
        var shift = await Shift(shifts, scope, 8, 16);
        var staff = Uuid7.NewUuid7();

        await rota.AssignAsync(scope, Cell(staff, day, shift), default);
        await attendance.RecordAsync(
            scope, Day(staff, day, 8, 16) with { InAt = new TimeOnly(8, 0) }, default);

        var entry = (await shifts.ListAsync(scope, includeRetired: false, default))
            .Single(e => e.Id == shift);

        await shifts.RescheduleAsync(
            scope,
            new RescheduleShiftCommand
            {
                Id = shift,
                ExpectedVersion = entry.Version,
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(6, 0),
                    EndsAt = new TimeOnly(14, 0),
                },
                EffectiveFrom = later,
            },
            default);

        var row = Assert.Single(
            await comparison.CompareAsync(
                scope, new AttendanceQuery { From = day, To = day, StaffId = staff }, default));

        // WF-Q15 reaching through the rota into a derived lateness: they arrived
        // at 08:00 for an 08:00 shift and were on time, and an edit made two
        // months later does not make them two hours late retrospectively.
        Assert.Null(row.LateBy);
        Assert.Equal(new TimeOnly(8, 0), row.ScheduledStart);
    }

    [Fact]
    public async Task Recording_asks_for_attendance_record_and_correcting_for_attendance_amend()
    {
        var (attendance, _, _, _) = Build(out var authorizer);
        var scope = fixture.Scope();

        var record = await attendance.RecordAsync(
            scope, Day(Uuid7.NewUuid7(), SomeDay(), 7, 15), default);
        await attendance.AmendAsync(
            scope,
            new AmendAttendanceCommand
            {
                Id = record.Id,
                ExpectedVersion = record.Version,
                OutAt = new TimeOnly(16, 0),
            },
            default);

        // The registry's design, not this service's: entering today's sheet is
        // routine, and correcting a record somebody may already have been paid
        // against is not. A property may want those in different hands.
        Assert.Equal(
            ["attendance.record", "attendance.amend"],
            authorizer.Checks.Select(c => c.Permission));
    }

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

    private static async Task<Guid> Shift(
        ShiftCatalogueService shifts, RequestScope scope, int from, int to)
    {
        var entry = await shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = "Attendance shift",
                ShortCode = $"AT{Interlocked.Increment(ref slot)}",
                Colour = "cyan",
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(from, 0),
                    EndsAt = new TimeOnly(to, 0),
                },
                EffectiveFrom = new DateOnly(2028, 1, 1),
            },
            default);

        return entry.Id;
    }

    private (AttendanceService Attendance, DayComparison Comparison, RotaService Rota,
        ShiftCatalogueService Shifts) Build() => Build(out _);

    private (AttendanceService Attendance, DayComparison Comparison, RotaService Rota,
        ShiftCatalogueService Shifts) Build(out RecordingAuthorizer authorizer)
    {
        authorizer = new RecordingAuthorizer();
        var db = fixture.Context();

        return (
            new AttendanceService(db, authorizer, TimeProvider.System),
            new DayComparison(db, authorizer),
            new RotaService(db, authorizer, TimeProvider.System),
            new ShiftCatalogueService(db, authorizer, TimeProvider.System));
    }
}
