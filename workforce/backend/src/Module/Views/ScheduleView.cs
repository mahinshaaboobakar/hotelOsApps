using HotelOS.Workforce.Application.Abstractions;
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

        var anchor = call.Optional("month") is { } named
            ? DateOnly.Parse(named.GetString()!)
            : DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

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

        var spans = await duties.ListAsync(
            call.Scope,
            new DateTimeOffset(first.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new DateTimeOffset(last.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            cancellationToken);

        var held = spans.Where(one => one.StaffId == staffId).ToList();

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, [staffId], cancellationToken);

        var name = names.TryGetValue(staffId, out var found) ? found : null;

        return new
        {
            who = name,
            initials = name is null ? "" : Wording.Initials(name),
            month = first.ToString("MMMM yyyy"),
            shifts = cells.Count,
            leaveDays = (int)mine.Sum(one => one.Days),
            duty = held.Count == 0
                ? "—"
                : held.Count + " MOD duty · " + held[0].StartsAt.ToString("ddd d, HH:mm"),
            // The balance sentence belongs to Leave and is read there. Absent
            // rather than recomputed here: two answers to "how much casual is
            // left" would eventually disagree, and this is the one nobody would
            // check.
            balance = (string?)null,
            days = Calendar(first, last, cells, mine, held, byId),
        };
    }

    /// <summary>The month grid, padded to whole weeks.</summary>
    private static List<object> Calendar(
        DateOnly first,
        DateOnly last,
        IReadOnlyList<ShiftAssignment> cells,
        IReadOnlyList<LeaveRequest> away,
        IReadOnlyList<DutyAssignment> duties,
        IReadOnlyDictionary<Guid, ShiftCatalogueEntry> shifts)
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
            var duty = duties.FirstOrDefault(
                one => DateOnly.FromDateTime(one.StartsAt.UtcDateTime) == day);

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
                duty = duty is null
                    ? null
                    : "MOD " + duty.StartsAt.ToString("HH:mm")
                      + "→" + duty.EndsAt.ToString("HH:mm"),
            });
        }

        return days;
    }
}
