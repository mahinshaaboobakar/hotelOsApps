using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Attendance;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Periods;
using HotelOS.Workforce.Application.Postings;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// Reports — the month's figures, and nothing that resembles pay.
/// </summary>
/// <remarks>
/// <para>
/// <b>Workforce produces inputs and stops there.</b> Pay differs by country
/// (WPS, PF, ESI) and by hotel, and getting it wrong is a salary dispute rather
/// than a bug. So there is no rate here, no total, and no currency — the
/// accountant or the payroll system takes this file.
/// </para>
/// <para>
/// <b>Holidays worked is absent rather than zero.</b> Nothing in this
/// application knows which days a property declared, so the column carries null
/// and the screen draws an em-dash. A zero would be a measurement nobody took,
/// read as "nobody worked a holiday".
/// </para>
/// </remarks>
public static class ReportsView
{
    /// <summary>One month, one department.</summary>
    public static async Task<object?> Month(ModuleCall call, CancellationToken cancellationToken)
    {
        var periods = call.Service<PeriodService>();
        var directory = call.Service<IStaffDirectory>();
        var postings = call.Service<PostingService>();
        var types = call.Service<LeaveTypeService>();
        var clock = call.Service<TimeProvider>();

        var anchor = call.Optional("month") is { } named
            ? DateOnly.Parse(named.GetString()!)
            : DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var from = new DateOnly(anchor.Year, anchor.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var department = call.Optional("department")?.GetString();

        var computed = await periods.ComputeAsync(
            call.Scope,
            new AttendanceQuery { From = from, To = to, DepartmentCode = department },
            cancellationToken);

        var held = await postings.ListAsync(
            call.Scope,
            new ListPostingsQuery { DepartmentCode = department },
            cancellationToken);

        var role = held.GroupBy(one => one.StaffId)
            .ToDictionary(group => group.Key, group => group.First().JobRole);

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, computed.Select(one => one.StaffId).ToList(), cancellationToken);

        // Leave is counted by TYPE, and the columns are named ones — so the
        // month's totals are looked up by the property's own codes rather than
        // by position in a list, which would silently re-label every column the
        // day a property added a type.
        var leaveTypes = await types.ListAsync(call.Scope, false, cancellationToken);
        var byCode = leaveTypes.ToDictionary(one => one.Id, one => one.Code.ToUpperInvariant());

        return new
        {
            label = from.ToString("MMMM yyyy")
                    + (department is null ? "" : " · " + department)
                    + " · " + from.ToString("d MMM") + " – " + to.ToString("d MMM"),
            department,
            rows = computed.Select(one => Row(one, names, role, byCode)).ToList(),
        };
    }

    /// <summary>One person's month.</summary>
    private static object Row(
        WorkforcePeriod period,
        IReadOnlyDictionary<Guid, string> names,
        IReadOnlyDictionary<Guid, string> roles,
        IReadOnlyDictionary<Guid, string> leaveCodes)
        => new
        {
            who = names.TryGetValue(period.StaffId, out var name) ? name : null,
            role = roles.TryGetValue(period.StaffId, out var job) ? job : null,
            posted = period.DaysPosted,
            present = period.DaysPresent,
            late = period.LateCount,
            casual = Taken(period, leaveCodes, "CL"),
            sick = Taken(period, leaveCodes, "SL"),
            earned = Taken(period, leaveCodes, "EL"),
            comp = Taken(period, leaveCodes, "CO"),
            holidays = (int?)null,
            hours = period.HoursWorked.ToString("0.0"),
            overtime = period.OvertimeHours == 0
                ? "0"
                : period.OvertimeHours.ToString("0.0"),
        };

    /// <summary>Days of one leave code, or zero when the property has no such type.</summary>
    private static decimal Taken(
        WorkforcePeriod period, IReadOnlyDictionary<Guid, string> codes, string code)
    {
        var matching = codes.Where(one => one.Value == code).Select(one => one.Key);

        return matching.Sum(id =>
            period.LeaveTakenByType.TryGetValue(id, out var days) ? days : 0m);
    }
}
