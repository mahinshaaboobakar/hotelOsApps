using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Attendance;

/// <summary>How one person's planned day and actual day compare.</summary>
/// <param name="StaffId">Whose day.</param>
/// <param name="BusinessDate">Which day.</param>
/// <param name="DepartmentCode">Which department it was worked for, when the rota says.</param>
/// <param name="Rostered">Whether the rota planned anything.</param>
/// <param name="PlannedHours">What the rota planned, in hours.</param>
/// <param name="ScheduledStart">When the rota expected them, if it did.</param>
/// <param name="Attended">Whether anybody recorded them arriving.</param>
/// <param name="ActualIn">When they arrived.</param>
/// <param name="Worked">What they actually worked, or null while the shift is open.</param>
/// <param name="LateBy">How late, derived and never stored.</param>
public sealed record DayRow(
    Guid StaffId,
    DateOnly BusinessDate,
    string DepartmentCode,
    bool Rostered,
    decimal PlannedHours,
    TimeOnly? ScheduledStart,
    bool Attended,
    TimeOnly? ActualIn,
    decimal? Worked,
    TimeSpan? LateBy)
{
    /// <summary>Rostered, and nobody recorded them arriving.</summary>
    /// <remarks>
    /// The question a supervisor opens the screen for. It is a <b>conclusion
    /// drawn from two records</b> rather than a state either of them holds, which
    /// is why it lives here and not on the attendance row.
    /// </remarks>
    public bool Absent => Rostered && !Attended;

    /// <summary>Present, and the rota did not plan for them.</summary>
    /// <remarks>
    /// Real and worth surfacing: somebody covering at short notice, or a rota
    /// nobody finished. Not an error — the attendance record is the fact, and the
    /// missing cell is the gap.
    /// </remarks>
    public bool Unplanned => Attended && !Rostered;
}

/// <summary>
/// Posted against present — what the rota planned, beside what happened.
/// </summary>
/// <remarks>
/// <para>
/// <b>A view, and its own file.</b> Recording what happened and judging it
/// against a plan are two purposes: <see cref="AttendanceService"/> deliberately
/// never reads the rota, so a record stands whether or not anybody was rostered.
/// This is where the two meet, and it writes nothing.
/// </para>
/// <para>
/// <b>Lateness is derived here and stored nowhere.</b> It is the difference
/// between the arrival and the start the rota expected — and that start comes
/// from the hours <b>in force on that date</b>, so a shift rescheduled in
/// November cannot retrospectively make somebody late last March. The sixth
/// clock-or-context-dependent value this application has refused to store.
/// </para>
/// </remarks>
public class DayComparison(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer)
{
    /// <summary>Compare planned against actual over a window.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="query">Which days, and whose.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>
    /// One row per person per day that <b>either</b> side knows about — a
    /// rostered absence and an unplanned attendance are both rows, because both
    /// are what the screen exists to show.
    /// </returns>
    public async Task<IReadOnlyList<DayRow>> CompareAsync(
        RequestScope scope, AttendanceQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var cells = await Cells(scope, query).ToListAsync(cancellationToken);
        var records = await Records(scope, query).ToListAsync(cancellationToken);

        // The hours in force across the window, loaded once. A query per cell
        // would be a hundred round trips for one week of one department.
        var entryIds = cells.Select(c => c.CatalogueEntryId).Distinct().ToList();

        var revisions = await db.ShiftHours
            .Where(h => h.PropertyId == scope.PropertyId
                        && entryIds.Contains(h.CatalogueEntryId)
                        && h.EffectiveFrom <= query.To
                        && (h.EffectiveTo == null || h.EffectiveTo >= query.From))
            .ToListAsync(cancellationToken);

        var byCell = cells.ToDictionary(c => (c.StaffId, c.Date));
        var byRecord = records.ToDictionary(r => (r.StaffId, r.BusinessDate));

        // The union, not the intersection. Joining on the rota would hide
        // everybody who turned up unrostered; joining on attendance would hide
        // every absence — and those are the two rows anybody opens this for.
        var days = byCell.Keys.Union(byRecord.Keys).OrderBy(k => k.Item2).ThenBy(k => k.Item1);
        var rows = new List<DayRow>();

        foreach (var day in days)
        {
            byCell.TryGetValue(day, out var cell);
            byRecord.TryGetValue(day, out var record);

            var hours = cell is null
                ? null
                : revisions.FirstOrDefault(
                    h => h.CatalogueEntryId == cell.CatalogueEntryId && h.InForceOn(cell.Date));

            var scheduledStart = cell?.OverrideStartsAt ?? hours?.StartsAt;

            rows.Add(new DayRow(
                StaffId: day.Item1,
                BusinessDate: day.Item2,
                DepartmentCode: cell?.DepartmentCode ?? string.Empty,
                Rostered: cell is not null,
                PlannedHours: cell is null ? 0m : WorkedHours.Planned(cell, hours),
                ScheduledStart: scheduledStart,
                Attended: record?.Attended ?? false,
                ActualIn: record?.InAt,
                Worked: record?.Worked,
                LateBy: Lateness(scheduledStart, record?.InAt)));
        }

        return rows;
    }

    /// <summary>How late somebody was, or null when the question does not arise.</summary>
    /// <remarks>
    /// <para>
    /// Null when there was no schedule to be late against, and null when nobody
    /// arrived. <b>Early is null too, not a negative</b>: arriving at 06:50 for a
    /// 07:00 shift is not lateness of minus ten minutes, and a signed number here
    /// would eventually be summed by somebody into a figure that means nothing.
    /// </para>
    /// <para>
    /// The comparison is within the day and needs no midnight rule — deliberately.
    /// A night shift starting at 23:00 with an arrival at 23:05 is five minutes
    /// late. An arrival at 07:00 the next morning is <b>not</b> eight hours late;
    /// it is somebody who missed the shift, which is a different fact this refuses
    /// to guess at. It falls out as a negative and becomes null, and the absence
    /// of a number is the honest answer.
    /// </para>
    /// </remarks>
    private static TimeSpan? Lateness(TimeOnly? scheduled, TimeOnly? arrived)
    {
        if (scheduled is not { } start || arrived is not { } actual)
        {
            return null;
        }

        var difference = actual.ToTimeSpan() - start.ToTimeSpan();

        return difference > TimeSpan.Zero ? difference : null;
    }

    private IQueryable<ShiftAssignment> Cells(RequestScope scope, AttendanceQuery query)
    {
        var cells = db.ShiftAssignments.Where(
            a => a.PropertyId == scope.PropertyId
                 && a.Date >= query.From
                 && a.Date <= query.To);

        if (!string.IsNullOrWhiteSpace(query.DepartmentCode))
        {
            var code = query.DepartmentCode.Trim().ToUpperInvariant();
            cells = cells.Where(a => a.DepartmentCode == code);
        }

        return query.StaffId is { } staffId ? cells.Where(a => a.StaffId == staffId) : cells;
    }

    private IQueryable<AttendanceRecord> Records(RequestScope scope, AttendanceQuery query)
    {
        var records = db.Attendance.Where(
            r => r.PropertyId == scope.PropertyId
                 && r.BusinessDate >= query.From
                 && r.BusinessDate <= query.To);

        return query.StaffId is { } staffId ? records.Where(r => r.StaffId == staffId) : records;
    }
}
