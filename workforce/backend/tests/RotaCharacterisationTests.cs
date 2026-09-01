using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Domain;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The rota: filling cells, copying a week, swapping two, and the overtime
/// warning that never blocks.
/// </summary>
/// <remarks>
/// Direct manipulation only — the owner refused templates and rotation engines,
/// so what is characterised here is a cell, a copy and an exchange.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class RotaCharacterisationTests(WorkforceFixture fixture)
{
    private static int week = -1;

    /// <summary>A short code no other test in this suite is using.</summary>
    /// <remarks>
    /// A counter, not <c>Random</c>. The catalogue refuses two live shifts
    /// sharing a code, so a random suffix collides eventually — and it did: this
    /// suite passed in isolation and failed in the full run, which is the worst
    /// failure mode a test can have. Determinism here is not tidiness; it is the
    /// difference between a suite you can believe and one you re-run.
    /// </remarks>
    private static int code = -1;

    /// <summary>A Monday nobody else in this suite is using.</summary>
    /// <remarks>
    /// Cells are unique per person and day, so unlike the duty register this
    /// suite could isolate by staff alone — but a week of its own keeps the
    /// copy-forward tests from reaching into a neighbour's dates, which is the
    /// one place they would collide.
    /// </remarks>
    private static DateOnly Monday() =>
        new DateOnly(2026, 1, 5).AddDays(Interlocked.Increment(ref week) * 28);

    [Fact]
    public async Task A_cell_is_filled_and_reads_back()
    {
        var (rota, shifts, _, _) = Build();
        var scope = fixture.Scope();
        var monday = Monday();
        var morning = await Shift(shifts, scope, "M", 7, 15);
        var staff = Uuid7.NewUuid7();

        var cell = await rota.AssignAsync(scope, Assign(staff, monday, morning), default);

        Assert.Equal(monday, cell.Date);
        Assert.Equal(morning, cell.CatalogueEntryId);
        Assert.Equal("FO", cell.DepartmentCode);
    }

    [Fact]
    public async Task Assigning_over_a_filled_cell_replaces_it()
    {
        var (rota, shifts, _, _) = Build();
        var scope = fixture.Scope();
        var monday = Monday();
        var morning = await Shift(shifts, scope, "M", 7, 15);
        var afternoon = await Shift(shifts, scope, "A", 15, 23);
        var staff = Uuid7.NewUuid7();

        await rota.AssignAsync(scope, Assign(staff, monday, morning), default);
        var replaced = await rota.AssignAsync(scope, Assign(staff, monday, afternoon), default);

        // Clicking a filled cell and choosing another shift is what a manager
        // does all morning. Refusing it would make the rota unusable.
        Assert.Equal(afternoon, replaced.CatalogueEntryId);

        var cells = await rota.ReadAsync(
            scope, new RotaQuery { From = monday, To = monday, StaffId = staff }, default);

        Assert.Single(cells);
    }

    [Fact]
    public async Task Copying_a_week_fills_empty_cells_only()
    {
        var (rota, shifts, _, _) = Build();
        var scope = fixture.Scope();
        var monday = Monday();
        var next = monday.AddDays(7);
        var morning = await Shift(shifts, scope, "M", 7, 15);
        var night = await Shift(shifts, scope, "N", 23, 7);
        var staff = Uuid7.NewUuid7();

        await rota.AssignAsync(scope, Assign(staff, monday, morning), default);
        await rota.AssignAsync(scope, Assign(staff, monday.AddDays(1), morning), default);

        // A decision already made about the new week.
        await rota.AssignAsync(scope, Assign(staff, next.AddDays(1), night), default);

        var filled = await rota.CopyWeekAsync(
            scope, new CopyWeekCommand { From = monday, To = next }, default);

        var copied = await rota.ReadAsync(
            scope,
            new RotaQuery { From = next, To = next.AddDays(6), StaffId = staff },
            default);

        // One cell filled, not two: the Tuesday was already decided and copying
        // over it would silently undo somebody's work.
        Assert.Equal(1, filled);
        Assert.Equal(morning, copied.Single(c => c.Date == next).CatalogueEntryId);
        Assert.Equal(night, copied.Single(c => c.Date == next.AddDays(1)).CatalogueEntryId);
    }

    [Fact]
    public async Task A_one_off_span_is_not_carried_forward_by_a_copy()
    {
        var (rota, shifts, _, _) = Build();
        var scope = fixture.Scope();
        var monday = Monday();
        var morning = await Shift(shifts, scope, "M", 7, 15);
        var staff = Uuid7.NewUuid7();

        await rota.AssignAsync(
            scope,
            Assign(staff, monday, morning) with
            {
                OverrideStartsAt = new TimeOnly(5, 0),
                OverrideEndsAt = new TimeOnly(13, 0),
            },
            default);

        await rota.CopyWeekAsync(
            scope, new CopyWeekCommand { From = monday, To = monday.AddDays(7) }, default);

        var copied = await rota.ReadAsync(
            scope,
            new RotaQuery { From = monday.AddDays(7), To = monday.AddDays(7), StaffId = staff },
            default);

        // It was a one-off for one day — that is what made it an override rather
        // than a change to the shift — and carrying it forward would make a
        // single exception permanent without anybody deciding so.
        Assert.False(copied.Single().IsOverridden);
    }

    [Fact]
    public async Task Copying_backwards_or_onto_itself_is_refused()
    {
        var (rota, _, _, _) = Build();
        var scope = fixture.Scope();
        var monday = Monday();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => rota.CopyWeekAsync(
                scope, new CopyWeekCommand { From = monday, To = monday }, default));

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => rota.CopyWeekAsync(
                scope, new CopyWeekCommand { From = monday, To = monday.AddDays(-7) }, default));
    }

    [Fact]
    public async Task A_swap_exchanges_the_shifts_and_keeps_the_owners()
    {
        var (rota, shifts, _, _) = Build();
        var scope = fixture.Scope();
        var monday = Monday();
        var morning = await Shift(shifts, scope, "M", 7, 15);
        var afternoon = await Shift(shifts, scope, "A", 15, 23);
        var anjali = Uuid7.NewUuid7();
        var sneha = Uuid7.NewUuid7();

        var hers = await rota.AssignAsync(scope, Assign(anjali, monday, afternoon), default);
        var his = await rota.AssignAsync(scope, Assign(sneha, monday, morning), default);

        await rota.SwapAsync(
            scope,
            new SwapShiftsCommand { FirstAssignmentId = hers.Id, SecondAssignmentId = his.Id },
            default);

        var cells = await rota.ReadAsync(
            scope, new RotaQuery { From = monday, To = monday }, default);

        // The *shift* moves; the owner and the day do not. Exchanging the people
        // instead would move a shift onto a day that person may already work.
        Assert.Equal(morning, cells.Single(c => c.StaffId == anjali).CatalogueEntryId);
        Assert.Equal(afternoon, cells.Single(c => c.StaffId == sneha).CatalogueEntryId);
    }

    [Fact]
    public async Task A_shift_cannot_be_swapped_with_itself()
    {
        var (rota, shifts, _, _) = Build();
        var scope = fixture.Scope();
        var monday = Monday();
        var morning = await Shift(shifts, scope, "M", 7, 15);

        var cell = await rota.AssignAsync(
            scope, Assign(Uuid7.NewUuid7(), monday, morning), default);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => rota.SwapAsync(
                scope,
                new SwapShiftsCommand { FirstAssignmentId = cell.Id, SecondAssignmentId = cell.Id },
                default));
    }

    [Fact]
    public async Task A_retired_shift_cannot_be_assigned_and_stays_readable_where_it_was()
    {
        var (rota, shifts, _, _) = Build();
        var scope = fixture.Scope();
        var monday = Monday();
        var old = await Shift(shifts, scope, "OLD", 9, 17);
        var staff = Uuid7.NewUuid7();

        var worked = await rota.AssignAsync(scope, Assign(staff, monday, old), default);

        var entry = (await shifts.ListAsync(scope, includeRetired: false, default))
            .Single(e => e.Id == old);

        await shifts.RetireAsync(
            scope, new RetireShiftCommand { Id = old, ExpectedVersion = entry.Version }, default);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => rota.AssignAsync(scope, Assign(staff, monday.AddDays(1), old), default));

        // The catalogue is the picker, and a retired entry has left it — but the
        // rota it was worked under is untouched.
        var cells = await rota.ReadAsync(
            scope, new RotaQuery { From = monday, To = monday, StaffId = staff }, default);

        Assert.Equal(worked.Id, cells.Single().Id);
    }

    [Fact]
    public async Task Clearing_an_empty_cell_is_what_the_caller_asked_for()
    {
        var (rota, _, _, _) = Build();

        // Not an error: a manager's double-click must not be a failure dialog.
        await rota.ClearAsync(
            fixture.Scope(),
            new ClearShiftCommand { StaffId = Uuid7.NewUuid7(), Date = Monday() },
            default);
    }

    [Fact]
    public void A_night_shift_counts_eight_hours_and_not_minus_sixteen()
    {
        // The single most likely arithmetic mistake in a rota, made once in
        // WorkedHours rather than at every call site.
        Assert.Equal(8m, WorkedHours.Of(new TimeOnly(23, 0), new TimeOnly(7, 0)));
        Assert.Equal(8m, WorkedHours.Of(new TimeOnly(7, 0), new TimeOnly(15, 0)));
        // WF-Q17: equal instants are ZERO, not twenty-four. The same arithmetic
        // serves attendance, where an identical clock-in and clock-out is zero
        // worked — and twenty-four would put a day's pay behind a typo.
        Assert.Equal(0m, WorkedHours.Of(new TimeOnly(9, 0), new TimeOnly(9, 0)));
    }

    [Fact]
    public async Task Overtime_warns_with_the_number_and_never_blocks()
    {
        var (rota, shifts, overtime, policy) = Build();
        var scope = fixture.Scope();
        var monday = Monday();
        var longDay = await Shift(shifts, scope, "LNG", 6, 18);
        var staff = Uuid7.NewUuid7();

        await policy.SetOvertimeAsync(
            scope, new SetOvertimeThresholdCommand { DailyHours = 9m, WeeklyHours = 48m }, default);

        for (var day = 0; day < 5; day++)
        {
            await rota.AssignAsync(scope, Assign(staff, monday.AddDays(day), longDay), default);
        }

        var warnings = await overtime.CheckAsync(
            scope,
            new RotaQuery { From = monday, To = monday.AddDays(6), StaffId = staff },
            default);

        var warning = Assert.Single(warnings);

        // Five twelve-hour days: over daily every day, and over weekly. The
        // warning carries the number, because "Vishnu is over" tells a manager
        // nothing they can act on.
        Assert.Equal(60m, warning.PlannedHours);
        Assert.Equal(5, warning.DailyExceedances.Count);
        Assert.True(warning.ExceedsWeekly);

        // And the rota is unchanged: warn-never-block. WF-Q14 and WF-Q16.
        var cells = await rota.ReadAsync(
            scope,
            new RotaQuery { From = monday, To = monday.AddDays(6), StaffId = staff },
            default);

        Assert.Equal(5, cells.Count);
    }

    [Fact]
    public async Task A_property_with_no_threshold_is_warned_about_nothing()
    {
        var (rota, shifts, overtime, _) = Build();
        var scope = fixture.OtherPropertyScope();
        var monday = Monday();
        var longDay = await Shift(shifts, scope, "LNG2", 6, 22);

        await rota.AssignAsync(
            scope, Assign(Uuid7.NewUuid7(), monday, longDay), default);

        var warnings = await overtime.CheckAsync(
            scope, new RotaQuery { From = monday, To = monday.AddDays(6) }, default);

        // Not zero and not a default of eight: a property that has never opened
        // the policy screen must not have every rota flagged by a labour rule
        // this application invented.
        Assert.Empty(warnings);
    }

    [Fact]
    public async Task Rescheduling_a_shift_changes_next_month_and_not_last()
    {
        var (rota, shifts, overtime, policy) = Build();
        var scope = fixture.Scope();
        var monday = Monday();
        var later = monday.AddDays(14);
        var shift = await Shift(shifts, scope, "RS", 8, 16);
        var staff = Uuid7.NewUuid7();

        await policy.SetOvertimeAsync(
            scope, new SetOvertimeThresholdCommand { DailyHours = 9m }, default);

        await rota.AssignAsync(scope, Assign(staff, monday, shift), default);
        await rota.AssignAsync(scope, Assign(staff, later, shift), default);

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
                    EndsAt = new TimeOnly(20, 0),
                },
                EffectiveFrom = later,
            },
            default);

        var before = await overtime.CheckAsync(
            scope, new RotaQuery { From = monday, To = monday, StaffId = staff }, default);
        var after = await overtime.CheckAsync(
            scope, new RotaQuery { From = later, To = later, StaffId = staff }, default);

        // WF-Q15 reaching all the way through to a computed number: the same
        // cell, the same shift, and eight hours before the change and fourteen
        // after it.
        Assert.Empty(before);
        Assert.Equal(14m, Assert.Single(after).PlannedHours);
    }

    [Fact]
    public async Task A_threshold_of_zero_is_refused()
    {
        var (_, _, _, policy) = Build();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => policy.SetOvertimeAsync(
                fixture.Scope(), new SetOvertimeThresholdCommand { DailyHours = 0m }, default));
    }

    [Fact]
    public async Task Writing_the_rota_asks_for_shift_manage()
    {
        var (_, shifts, _, _) = Build();
        var scope = fixture.Scope();
        var morning = await Shift(shifts, scope, "PRM2", 7, 15);

        // A second service over a fresh recorder, so what is asserted is this
        // call's permission and not the catalogue write that set the scene.
        var authorizer = new RecordingAuthorizer();
        var rota = new RotaService(fixture.Context(), authorizer, TimeProvider.System);

        await rota.AssignAsync(
            scope, Assign(Uuid7.NewUuid7(), Monday(), morning), default);

        Assert.Equal("shift.define", Assert.Single(authorizer.Checks).Permission);
    }

    private static AssignShiftCommand Assign(Guid staff, DateOnly date, Guid entry) =>
        new()
        {
            StaffId = staff,
            Date = date,
            CatalogueEntryId = entry,
            DepartmentCode = "FO",
        };

    private static async Task<Guid> Shift(
        ShiftCatalogueService shifts, RequestScope scope, string code, int from, int to)
    {
        var entry = await shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = $"Shift {code}",
                ShortCode = $"{code}{Interlocked.Increment(ref RotaCharacterisationTests.code)}",
                Colour = "cyan",
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(from, 0),
                    EndsAt = new TimeOnly(to, 0),
                },
                EffectiveFrom = new DateOnly(2025, 1, 1),
            },
            default);

        return entry.Id;
    }

    private (RotaService Rota, ShiftCatalogueService Shifts, OvertimeCheck Overtime,
        PolicyService Policy) Build() => Build(out _);

    private (RotaService Rota, ShiftCatalogueService Shifts, OvertimeCheck Overtime,
        PolicyService Policy) Build(out RecordingAuthorizer authorizer)
    {
        authorizer = new RecordingAuthorizer();
        var db = fixture.Context();

        return (
            new RotaService(db, authorizer, TimeProvider.System),
            new ShiftCatalogueService(db, authorizer, TimeProvider.System),
            new OvertimeCheck(db, authorizer),
            new PolicyService(db, authorizer, TimeProvider.System));
    }
}
