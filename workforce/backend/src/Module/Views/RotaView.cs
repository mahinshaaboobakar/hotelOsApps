using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
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
        var clock = call.Service<TimeProvider>();

        var anchor = call.Optional("week") is { } named
            ? DateOnly.Parse(named.GetString()!)
            : DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

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
            new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new DateTimeOffset(sunday.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            cancellationToken);

        return new
        {
            department,
            label = monday.ToString("d") + " – " + sunday.ToString("d MMM") + " Week",
            month = monday.ToString("MMMM yyyy"),
            days = Enumerable.Range(0, 7)
                .Select(offset => monday.AddDays(offset).ToString("ddd d").ToUpperInvariant())
                .ToList(),
            duty = spans.Select(one => Span(one, monday, names)).ToList(),
            people = people
                .Select(group => Person(group, monday, cells, approved, shifts, hours, names))
                .ToList(),
            catalogue = shifts.Select(one => Shift(one, hours)).ToList(),
            overtime = warnings.Select(one => new
            {
                who = names.TryGetValue(one.StaffId, out var name) ? name : null,
                planned = one.PlannedHours.ToString("0.#") + " h planned",
                // The threshold is the property's, not the warning's: the check
                // returns what was planned and which days exceeded, and the
                // number it was measured against is read from policy once.
                threshold = one.ExceedsWeekly
                    ? "over the weekly threshold"
                    : one.DailyExceedances.Count + " day over",
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
            zone = (string?)null,
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
            @override = assigned?.IsOverridden == true
                ? assigned.OverrideStartsAt!.Value.ToString("HH:mm")
                  + "–" + assigned.OverrideEndsAt!.Value.ToString("HH:mm")
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
                ? window.StartsAt!.Value.ToString("HH:mm")
                  + "–" + window.EndsAt!.Value.ToString("HH:mm")
                : null,
        };
    }

    /// <summary>One duty span, positioned across the week's seven columns.</summary>
    private static object Span(
        DutyAssignment duty, DateOnly monday, IReadOnlyDictionary<Guid, string> names)
    {
        var start = DateOnly.FromDateTime(duty.StartsAt.UtcDateTime);
        var from = start.DayNumber - monday.DayNumber;
        var end = DateOnly.FromDateTime(duty.EndsAt.UtcDateTime);

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
