using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Attendance;

namespace HotelOS.Workforce.Application.Summaries;

/// <summary>One department's absences against its rota, today.</summary>
/// <param name="DepartmentCode">Which department.</param>
/// <param name="Absent">How many were rostered and did not come.</param>
/// <param name="Rostered">How many the rota planned for.</param>
public sealed record DepartmentAbsence(string DepartmentCode, int Absent, int Rostered);

/// <summary>Somebody who arrived after the rota expected them.</summary>
/// <param name="Person">Who.</param>
/// <param name="DepartmentCode">Where they were rostered.</param>
/// <param name="ExpectedAt">When the rota expected them.</param>
/// <param name="LateBy">By how much — derived, and stored nowhere.</param>
public sealed record LateArrival(
    NamedPerson Person, string DepartmentCode, TimeOnly ExpectedAt, TimeSpan LateBy);

/// <summary>The rota against who came — Attendance Today's answer.</summary>
/// <param name="Rostered">People the rota planned for.</param>
/// <param name="Present">
/// People the rota planned for <b>who came</b>. See the service's note: this
/// follows the Attendance screen and is not a second reading of the word.
/// </param>
/// <param name="Absent">Rostered, and nobody recorded them arriving.</param>
/// <param name="Late">People who arrived after the rota expected them.</param>
/// <param name="ByDepartment">Absences, by department, worst first.</param>
/// <param name="LateIn">Who was late, latest first.</param>
public sealed record AttendanceTodayView(
    int Rostered,
    int Present,
    int Absent,
    int Late,
    IReadOnlyList<DepartmentAbsence> ByDepartment,
    IReadOnlyList<LateArrival> LateIn);

/// <summary>
/// Today's rota against today's attendance — the read behind Attendance Today.
/// </summary>
/// <remarks>
/// <para>
/// <b>It composes <see cref="DayComparison"/> rather than re-querying.</b> The
/// union of rostered and attended, and the lateness derived from the hours in
/// force on the date, are decided once — a second implementation here would
/// drift from the screen's, and the two would disagree about the same day on
/// two surfaces of one application.
/// </para>
/// <para>
/// <b><c>Present</c> means rostered and present</b>, which is what the
/// Attendance screen already computes and shows as <i>present against posted</i>.
/// <see cref="DayRow.Attended"/> is true for anybody who came, the unrostered
/// included, so the two readings differ by exactly the person nobody planned
/// for — and they were ruled to be one answer rather than two (2026-09-03):
/// this follows the screen. The unrostered arrival is not lost; it is
/// <see cref="DayRow.Unplanned"/>, which the screen draws and this card's frame
/// does not ask for.
/// </para>
/// <para>
/// <b>Names are read at answer time.</b> One call for everybody late, never one
/// per row — the port takes a set for that reason.
/// </para>
/// </remarks>
public class AttendanceTodaySummary(
    DayComparison comparison,
    IStaffDirectory directory,
    TimeProvider clock)
{
    /// <summary>How today's rota and today's attendance compare.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The day.</returns>
    public async Task<AttendanceTodayView> ReadAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        // `DayComparison` authorizes with `roster.read`, so this does not ask a
        // second time: two checks for one read is one place for them to disagree.
        var rows = await comparison.CompareAsync(
            scope, new AttendanceQuery { From = today, To = today }, cancellationToken);

        var rostered = rows.Where(row => row.Rostered).ToList();
        var late = rows.Where(row => row.LateBy is not null).ToList();

        var names = await directory.FindNamesAsync(
            scope.PropertyId,
            [.. late.Select(row => row.StaffId).Distinct()],
            cancellationToken);

        var byDepartment = rostered
            .GroupBy(row => row.DepartmentCode)
            .Select(group => new DepartmentAbsence(
                group.Key, group.Count(row => row.Absent), group.Count()))
            .Where(department => department.Absent > 0)
            .OrderByDescending(department => department.Absent)
            .ThenBy(department => department.DepartmentCode, StringComparer.Ordinal)
            .ToList();

        var lateIn = late
            .OrderByDescending(row => row.LateBy)
            .Select(row => new LateArrival(
                new NamedPerson(row.StaffId, names.GetValueOrDefault(row.StaffId)),
                row.DepartmentCode,
                row.ScheduledStart ?? default,
                row.LateBy ?? TimeSpan.Zero))
            .ToList();

        return new AttendanceTodayView(
            Rostered: rostered.Count,
            Present: rostered.Count(row => row.Attended),
            Absent: rostered.Count(row => row.Absent),
            Late: late.Count,
            ByDepartment: byDepartment,
            LateIn: lateIn);
    }
}
