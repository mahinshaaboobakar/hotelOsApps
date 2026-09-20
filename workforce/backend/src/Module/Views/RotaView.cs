using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Calendar;
using HotelOS.Workforce.Application.Duties;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// The rota — one department's week, one person's month, and the writes.
/// </summary>
/// <remarks>
/// <para>
/// <b>A cell is what the service says it is.</b> After a write the screen
/// re-reads rather than patching what it drew: an assignment can move a
/// coverage warning, close a gap or trip the overtime threshold, and a cell
/// updated from memory would show the change without its consequences.
/// </para>
/// <para>
/// <b>The catalogue travels with the week.</b> The picker's list is the
/// property's own shifts as they stood on that week — a rota read for August
/// must not offer a shift defined in September, and must still render one
/// retired since.
/// </para>
/// </remarks>
public static class RotaView
{
    /// <summary>One department's week.</summary>
    public static async Task<object?> Week(ModuleCall call, CancellationToken cancellationToken)
    {
        var rota = call.Service<RotaService>();
        var catalogue = call.Service<ShiftCatalogueService>();
        var postings = call.Service<PostingService>();
        var duties = call.Service<DutyService>();
        var leave = call.Service<LeaveService>();
        var overtime = call.Service<OvertimeCheck>();
        var directory = call.Service<IStaffDirectory>();

        // Days at the property, never UTC days — see PropertyCalendar.
        var calendar = await PropertyCalendar.ForAsync(
            directory, call.Scope.PropertyId, cancellationToken);

        // Which week opens when nobody names one is *what day is it* — Context's
        // answer, ADR 0211, and the same correction `DutyView` needed. The
        // property's wall clock answers *has 07:00 passed*, which is a different
        // question; two views reading a day from two places is one property with
        // two answers to it.
        var anchor = call.Optional("week") is { } named
            ? DateOnly.Parse(named.GetString()!)
            : await PropertyDay.TodayAsync(call, cancellationToken);

        var monday = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
        var sunday = monday.AddDays(6);
        var department = call.Optional("department")?.GetString();

        var cells = await rota.ReadAsync(
            call.Scope,
            new RotaQuery { From = monday, To = sunday, DepartmentCode = department },
            cancellationToken);

        var shifts = await catalogue.ListAsync(call.Scope, true, cancellationToken);
        var hours = new Dictionary<Guid, ShiftHours?>();

        foreach (var shift in shifts)
        {
            hours[shift.Id] = await catalogue.HoursOnAsync(
                call.Scope, shift.Id, monday, cancellationToken);
        }

        var held = await postings.ListAsync(
            call.Scope,
            new ListPostingsQuery { DepartmentCode = department },
            cancellationToken);

        var people = held.GroupBy(one => one.StaffId).ToList();

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, people.Select(group => group.Key).ToList(), cancellationToken);

        var approved = await leave.ApprovedBetweenAsync(
            call.Scope, monday, sunday, cancellationToken);

        var warnings = await overtime.CheckAsync(
            call.Scope,
            new RotaQuery { From = monday, To = sunday, DepartmentCode = department },
            cancellationToken);

        var spans = await duties.ListAsync(
            call.Scope,
            calendar.StartOf(monday),
            calendar.StartOf(sunday.AddDays(1)),
            cancellationToken);

        return new
        {
            department,
            // The week's own anchor, machine-readable, beside the strings that
            // render it. Every write on this screen names a DATE — `assign`,
            // `clear`, `copyWeek` all take one — and `days` is seven display
            // headings ("THU 27") that no client can turn back into one. A
            // surface that had to parse its own heading to say which day it
            // meant is the defect ADR 0175 exists to prevent, arriving through
            // the write instead of the render.
            monday = monday.ToString("O")[..10],

            // The code, because `assign` requires `DepartmentCode` and
            // `department` below is the name a person reads. A name is not a
            // key: the read sent one and the write needs the other, and nothing
            // on the screen could have bridged them.
            departmentCode = department ?? string.Empty,

            // **Three rendered strings become two dates and seven days** - ADR
            // 0175. `label` was `monday.ToString("d")`, which is the LOCALE'S
            // FULL SHORT-DATE PATTERN: 24/08/2026 in en-GB, 8/24/2026 in en-US,
            // 24.08.2026 in de-DE - rendered in the machine's culture and shipped
            // to every property. It was also the worst site in this file and no
            // census of mine ever saw it, because it shares a line with another
            // ToString and a per-line count keeps only the last.
            //
            // And it appended " Week" while the surface wrote "Week" too, so a
            // real property read "24/08/2026 – 30 Aug Week  Week". The word
            // belongs to whichever side says it once; it is the surface's.
            //
            // `days` was "MON 24", uppercased HERE - and case is a script's
            // property, not a style: a locale whose weekday names have no case
            // distinction gets the same string back, and one whose uppercase
            // rules differ from the invariant gets the wrong letters.
            sunday = Wire.Day(sunday),
            days = Enumerable.Range(0, 7)
                .Select(offset => Wire.Day(monday.AddDays(offset)))
                .ToList(),
            duty = spans.Select(one => Span(one, monday, names, calendar)).ToList(),
            people = people
                .Select(group => Person(group, monday, cells, approved, shifts, hours, names))
                .ToList(),
            catalogue = shifts.Select(one => Shift(one, hours)).ToList(),
            overtime = warnings.Select(one => new
            {
                who = names.TryGetValue(one.StaffId, out var name) ? name : null,
                // The number alone. This carried " h planned" and the surface
                // wrote "is planned {planned} hours against {threshold}", so the
                // sentence rendered "is planned 9 h planned hours against 8" -
                // two words composed here and the rest there, stuttering where
                // they met.
                planned = one.PlannedHours,

                // **Numbers, and the screen writes the sentence** (NUM-Q1, ADR
                // 0174). This sent `threshold` as English composed here — "over
                // the weekly threshold" or "2 day over" — into a screen that
                // wrote "against {threshold}", so a property read "is planned 60
                // hours against over the weekly threshold". The comment beside it
                // said the threshold was "read from policy once"; nothing read
                // it. The thresholds come from the check, which held the policy
                // it measured with.
                weekly = one.ExceedsWeekly,
                weeklyHours = one.WeeklyThreshold,
                daysOver = one.DailyExceedances.Count,
                dailyHours = one.DailyThreshold,
            }).ToList(),
        };
    }
    /// <summary>Fill a cell, clear one, copy a week, or exchange two.</summary>
    public static Task<object?> Write(ModuleCall call, CancellationToken cancellationToken)
        => call.Method switch
        {
            "assign" => Assign(call, cancellationToken),
            "clear" => Clear(call, cancellationToken),
            "copyWeek" => CopyWeek(call, cancellationToken),
            "swap" => Swap(call, cancellationToken),
            _ => throw new InvalidRequestException(call.Method + " is not a rota method"),
        };

    private static async Task<object?> Assign(ModuleCall call, CancellationToken cancellationToken)
    {
        var cell = await call.Service<RotaService>().AssignAsync(
            call.Scope,
            new AssignShiftCommand
            {
                StaffId = call.Id("staffId"),
                Date = call.Date("date"),
                CatalogueEntryId = call.Id("shiftId"),
                DepartmentCode = call.Text("department"),
                OverrideStartsAt = Time(call, "startsAt"),
                OverrideEndsAt = Time(call, "endsAt"),
            },
            cancellationToken);

        return new { id = cell.Id, version = cell.Version };
    }

    private static async Task<object?> Clear(ModuleCall call, CancellationToken cancellationToken)
    {
        await call.Service<RotaService>().ClearAsync(
            call.Scope,
            new ClearShiftCommand
            {
                StaffId = call.Id("staffId"),
                Date = call.Date("date"),
            },
            cancellationToken);

        return new { cleared = true };
    }

    private static async Task<object?> CopyWeek(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var written = await call.Service<RotaService>().CopyWeekAsync(
            call.Scope,
            new CopyWeekCommand
            {
                From = call.Date("from"),
                To = call.Date("to"),
                DepartmentCode = call.Optional("department")?.GetString(),
            },
            cancellationToken);

        return new { copied = written };
    }

    private static async Task<object?> Swap(ModuleCall call, CancellationToken cancellationToken)
    {
        await call.Service<RotaService>().SwapAsync(
            call.Scope,
            new SwapShiftsCommand
            {
                FirstAssignmentId = call.Id("first"),
                SecondAssignmentId = call.Id("second"),
            },
            cancellationToken);

        return new { swapped = true };
    }

    /// <summary>One person's row across seven days.</summary>
    private static object Person(
        IGrouping<Guid, Posting> held,
        DateOnly monday,
        IReadOnlyList<ShiftAssignment> cells,
        IReadOnlyList<LeaveRequest> approved,
        IReadOnlyList<ShiftCatalogueEntry> shifts,
        IReadOnlyDictionary<Guid, ShiftHours?> hours,
        IReadOnlyDictionary<Guid, string> names)
    {
        var primary = held.OrderByDescending(one => one.IsPrimary).First();
        var name = names.TryGetValue(held.Key, out var found) ? found : null;
        var byId = shifts.ToDictionary(one => one.Id);

        return new
        {
            id = held.Key,
            name,
            initials = name is null ? "" : Wording.Initials(name),
            role = primary.JobRole,

            // **The department a cell on this row is assigned under.** `assign`
            // requires one, and the week-level `departmentCode` is the filter
            // the screen asked for — "" when it named none, which it never has,
            // so every assignment on a real property was refused. The row's own
            // posting is the only honest source. A person posted to two
            // departments is assigned under the PRIMARY one here — the same
            // posting `role` is read from; which is right for such a person is
            // queued for the owner, not decided in this line.
            departmentCode = primary.DepartmentCode,

            // **No zone.** This sent `null` forever while the approved frames
            // drew "Night auditor · Zone 1" under every name, so the drawing
            // promised something no property received. The owner dropped the
            // zone from this screen and from Attendance together
            // (2026-09-20, `64g` §5), so the field goes rather than staying as
            // a null nobody can fill: the posting's zone is People's, where
            // `WF-Q7` put it.
            head = primary.IsDepartmentHead,
            week = Enumerable.Range(0, 7)
                .Select(offset => Cell(held.Key, monday.AddDays(offset), cells, approved, byId,
                    hours))
                .ToList(),
        };
    }

    /// <summary>One day of one person's week.</summary>
    private static object Cell(
        Guid staffId,
        DateOnly day,
        IReadOnlyList<ShiftAssignment> cells,
        IReadOnlyList<LeaveRequest> approved,
        IReadOnlyDictionary<Guid, ShiftCatalogueEntry> shifts,
        IReadOnlyDictionary<Guid, ShiftHours?> hours)
    {
        var assigned = cells.FirstOrDefault(
            one => one.StaffId == staffId && one.Date == day);

        var away = approved.FirstOrDefault(
            one => one.StaffId == staffId && one.From <= day && day <= one.To);

        return new
        {
            shift = assigned is null || !shifts.TryGetValue(assigned.CatalogueEntryId, out var entry)
                ? null
                : Shift(entry, hours),
            // The two ends, not a joined string - ADR 0175.
            @override = assigned?.IsOverridden == true
                ? Wire.Span(assigned.OverrideStartsAt, assigned.OverrideEndsAt)
                : null,
            leave = away is null ? null : "Leave",
            // A gap is a day with neither a shift nor leave on it — an
            // unfinished rota, which is the thing a supervisor scans for.
            gap = assigned is null && away is null,
        };
    }

    /// <summary>One shift, as the picker and the cells draw it.</summary>
    private static object Shift(
        ShiftCatalogueEntry entry, IReadOnlyDictionary<Guid, ShiftHours?> hours)
    {
        hours.TryGetValue(entry.Id, out var window);

        return new
        {
            id = entry.Id,
            code = entry.ShortCode,
            name = entry.Name,
            tone = Wording.Tone(entry.Colour),
            hours = window?.IsWorking == true
                ? Wire.Span(window.StartsAt, window.EndsAt)
                : null,
        };
    }

    /// <summary>One duty span, positioned across the week's seven columns.</summary>
    private static object Span(
        DutyAssignment duty, DateOnly monday, IReadOnlyDictionary<Guid, string> names,
        PropertyCalendar calendar)
    {
        // The days AT THE PROPERTY that the duty starts and ends on. These were
        // UTC dates, so at +05:30 a Tuesday 02:00 duty drew in Monday's column.
        var start = calendar.DayOf(duty.StartsAt);
        var from = start.DayNumber - monday.DayNumber;
        var end = calendar.DayOf(duty.EndsAt);

        return new
        {
            who = names.TryGetValue(duty.StaffId, out var name) ? name : null,
            department = (string?)null,
            // Instants, like the duty register's — the ribbon and the register
            // draw the same spans, and one of them rendering UTC hours while the
            // other rendered the property's would be the same fact told two ways
            // on two screens.
            // Named for what they are: `from` on this shape is already the day
            // COLUMN the span starts in, and two different meanings under one
            // name is the collision this caught at compile time.
            startsAt = duty.StartsAt.ToString("O"),
            endsAt = duty.EndsAt.ToString("O"),
            from,
            span = Math.Max(1, end.DayNumber - start.DayNumber),
            overnight = end > start,
        };
    }

    /// <summary>An optional time on the wire.</summary>
    private static TimeOnly? Time(ModuleCall call, string field)
        => call.Optional(field) is { } value ? TimeOnly.Parse(value.GetString()!) : null;
}
