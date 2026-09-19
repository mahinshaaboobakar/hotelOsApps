using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Calendar;
using HotelOS.Workforce.Application.Duties;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// One person's month — the staff schedule, and the calendar it is drawn on.
/// </summary>
/// <remarks>
/// <para>
/// Its own file rather than the rota's, because it is its own screen. The team
/// rota answers "who is working this week in this department" and this answers
/// "what does this person's month look like": one grid is people by day, the
/// other is days of one month, and they share a service and not a purpose.
/// </para>
/// <para>
/// <b>The grid is padded to start on a Monday.</b> A month opening mid-week
/// would otherwise put its first day under whichever column came first, and the
/// padding days are drawn faint rather than omitted so the columns stay true.
/// </para>
/// </remarks>
public static class ScheduleView
{
    /// <summary>One person's month.</summary>
    public static async Task<object?> Month(ModuleCall call, CancellationToken cancellationToken)
    {
        var rota = call.Service<RotaService>();
        var catalogue = call.Service<ShiftCatalogueService>();
        var duties = call.Service<DutyService>();
        var leave = call.Service<LeaveService>();
        var directory = call.Service<IStaffDirectory>();
        var clock = call.Service<TimeProvider>();

        var staffId = call.Id("staffId");

        // Days at the property, never UTC days — see PropertyCalendar.
        var calendar = await PropertyCalendar.ForAsync(
            directory, call.Scope.PropertyId, cancellationToken);

        var anchor = call.Optional("month") is { } named
            ? DateOnly.Parse(named.GetString()!)
            : calendar.DayOf(clock.GetUtcNow());

        var first = new DateOnly(anchor.Year, anchor.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);

        var cells = await rota.ReadAsync(
            call.Scope,
            new RotaQuery { From = first, To = last, StaffId = staffId },
            cancellationToken);

        var shifts = await catalogue.ListAsync(call.Scope, true, cancellationToken);
        var byId = shifts.ToDictionary(one => one.Id);

        var approved = await leave.ApprovedBetweenAsync(call.Scope, first, last, cancellationToken);
        var mine = approved.Where(one => one.StaffId == staffId).ToList();

        // The month from the property's first midnight to the one after its last
        // day — UTC midnights would drop a duty early on the 1st and admit one
        // from the next month's first hours.
        var spans = await duties.ListAsync(
            call.Scope,
            calendar.StartOf(first),
            calendar.StartOf(last.AddDays(1)),
            cancellationToken);

        var held = spans.Where(one => one.StaffId == staffId).ToList();

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, [staffId], cancellationToken);

        var name = names.TryGetValue(staffId, out var found) ? found : null;

        return new
        {
            who = name,
            initials = name is null ? "" : Wording.Initials(name),
            // The month as a DAY on the wire, formatted by the surface - ADR
            // 0175. "MMMM yyyy" through the machine's culture put one language's
            // month name on every property's screen.
            month = Wire.Day(first),
            shifts = cells.Count,
            leaveDays = (int)mine.Sum(one => one.Days),
            // The count is a fact; the instant is the screen's to say. A
            // sentence composed here would carry this server's clock into a
            // header a property reads.
            duty = held.Count,
            dutyFrom = held.Count == 0 ? null : held[0].StartsAt.ToString("O"),
            dutyTo = held.Count == 0 ? null : held[0].EndsAt.ToString("O"),
            // The balance sentence belongs to Leave and is read there. Absent
            // rather than recomputed here: two answers to "how much casual is
            // left" would eventually disagree, and this is the one nobody would
            // check.
            balance = (string?)null,
            days = Calendar(first, last, cells, mine, held, byId, calendar),
        };
    }

    /// <summary>The month grid, padded to whole weeks.</summary>
    private static List<object> Calendar(
        DateOnly first,
        DateOnly last,
        IReadOnlyList<ShiftAssignment> cells,
        IReadOnlyList<LeaveRequest> away,
        IReadOnlyList<DutyAssignment> duties,
        IReadOnlyDictionary<Guid, ShiftCatalogueEntry> shifts,
        PropertyCalendar calendar)
    {
        var days = new List<object>();
        var lead = ((int)first.DayOfWeek + 6) % 7;

        // The leading days belong to the previous month and are drawn faint
        // rather than omitted: a grid that started mid-row would put Monday's
        // column over a Thursday.
        for (var back = lead; back > 0; back -= 1)
        {
            days.Add(new
            {
                date = first.AddDays(-back).Day,
                mark = (string?)null,
                tone = (string?)null,
                duty = (string?)null,
            });
        }

        for (var day = first; day <= last; day = day.AddDays(1))
        {
            var assigned = cells.FirstOrDefault(one => one.Date == day);
            var leave = away.FirstOrDefault(one => one.From <= day && day <= one.To);
            // The day the duty starts AT THE PROPERTY. This compared UTC dates,
            // so a 02:00 duty at +05:30 sat on the day before.
            var duty = duties.FirstOrDefault(one => calendar.DayOf(one.StartsAt) == day);

            var entry = assigned is not null
                        && shifts.TryGetValue(assigned.CatalogueEntryId, out var found)
                ? found
                : null;

            days.Add(new
            {
                date = day.Day,
                mark = leave is not null ? "Leave" : entry?.ShortCode,
                tone = leave is not null
                    ? "leave"
                    : entry is null ? null : Wording.Tone(entry.Colour),
                dutyFrom = duty?.StartsAt.ToString("O"),
                dutyTo = duty?.EndsAt.ToString("O"),
            });
        }

        return days;
    }
}
