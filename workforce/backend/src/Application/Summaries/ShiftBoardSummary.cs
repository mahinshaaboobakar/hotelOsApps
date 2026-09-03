using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Summaries;

/// <summary>One department's people on shift right now, and the shift they are on.</summary>
/// <param name="DepartmentCode">Which department.</param>
/// <param name="StartsAt">When their shift began.</param>
/// <param name="EndsAt">When it finishes.</param>
/// <param name="OnNow">How many of them.</param>
/// <remarks>
/// A row is a <b>department and a shift</b>, not a department. Two shifts can
/// overlap in one department — a night handing over to a morning, a split
/// shift's second half — and collapsing them would need a rule for which times
/// to show, which is a rule nobody has made. Two rows say what is true.
/// </remarks>
public sealed record DepartmentOnShift(
    string DepartmentCode, TimeOnly StartsAt, TimeOnly EndsAt, int OnNow);

/// <summary>The next moment the people on shift change, and by how many.</summary>
/// <param name="At">When.</param>
/// <param name="On">How many start being covered.</param>
/// <param name="Off">How many stop.</param>
/// <remarks>
/// Counted in <b>people</b>, not spans: a split shift's second half starting is
/// not somebody arriving if they were already on, and counting spans would say
/// it was.
/// </remarks>
public sealed record Changeover(DateTimeOffset At, int On, int Off);

/// <summary>Is the property covered at this moment — the Shift Board's answer.</summary>
/// <param name="OnNow">People on shift, property-wide.</param>
/// <param name="Departments">How many departments have somebody on.</param>
/// <param name="Rows">A row per department and shift, busiest first.</param>
/// <param name="NextChange">
/// The next changeover, or <c>null</c> when nothing more changes in the window.
/// </param>
/// <remarks>
/// <b><see cref="OnNow"/> counts the property and <see cref="Rows"/> may not.</b>
/// A caller draws what its surface holds — the widget's card fits four — and the
/// figure still answers the question. That is the size guarantee working rather
/// than a truncation defect, and it only works because the count is computed
/// here rather than by counting the rows.
/// </remarks>
public sealed record ShiftBoardView(
    int OnNow,
    int Departments,
    IReadOnlyList<DepartmentOnShift> Rows,
    Changeover? NextChange);

/// <summary>
/// Who is on shift now — the read behind the Shift Board.
/// </summary>
/// <remarks>
/// <para>
/// <b>Its own file rather than a method on <see cref="Rota.RotaService"/>.</b>
/// Filling a cell and asking what the whole property is doing at 14:47 are two
/// purposes (ADR 0038), and this one needs the catalogue's effective-dated hours
/// and a clock — collaborators the rota service has no other reason to hold.
/// </para>
/// <para>
/// <b>Yesterday's cells are read too, and that is the whole difficulty.</b> A
/// night shift belongs to the date it starts on, so at 06:00 the people working
/// are on yesterday's rota. <see cref="ShiftCoverage.DatesCovering"/> says which
/// dates can answer for a moment, and every judgment about midnight and split
/// shifts lives in <see cref="ShiftCoverage"/> rather than here.
/// </para>
/// <para>
/// <b>The window is one day forward.</b> The next changeover is looked for
/// within it, so a property whose last shift has started reports
/// <c>null</c> — absent rather than a placeholder time, because a dash there
/// would read as a figure this failed to fetch instead of one that does not
/// exist.
/// </para>
/// </remarks>
public class ShiftBoardSummary(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    TimeProvider clock)
{
    /// <summary>What the property is working, at this moment.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The board.</returns>
    public async Task<ShiftBoardView> ReadAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var dates = ShiftCoverage.DatesCovering(today);
        var from = dates[0];
        var to = today.AddDays(1);

        var cells = await db.ShiftAssignments
            .Where(a => a.PropertyId == scope.PropertyId && a.Date >= from && a.Date <= to)
            .ToListAsync(cancellationToken);

        if (cells.Count == 0)
        {
            return new ShiftBoardView(0, 0, [], null);
        }

        // The hours in force on each cell's own date, loaded once for the
        // window. A query per cell would be a hundred round trips to answer one
        // glance — CLAUDE.md's per-round-trip review of a hot path, and this one
        // is read every time a popover opens.
        var entryIds = cells.Select(c => c.CatalogueEntryId).Distinct().ToList();

        var revisions = await db.ShiftHours
            .Where(h => h.PropertyId == scope.PropertyId
                        && entryIds.Contains(h.CatalogueEntryId)
                        && h.EffectiveFrom <= to
                        && (h.EffectiveTo == null || h.EffectiveTo >= from))
            .ToListAsync(cancellationToken);

        var worked = cells
            .Select(cell => (Cell: cell, Hours: InForce(revisions, cell)))
            .ToList();

        var time = TimeOnly.FromDateTime(now.UtcDateTime);

        var onNow = worked
            .Where(w => ShiftCoverage.Covers(w.Cell, w.Hours, today, time))
            .ToList();

        var rows = onNow
            .GroupBy(w => (w.Cell.DepartmentCode, w.Cell.CatalogueEntryId))
            .Select(group => Row(group.Key.DepartmentCode, group.ToList()))
            .OfType<DepartmentOnShift>()
            .OrderByDescending(row => row.OnNow)
            .ThenBy(row => row.DepartmentCode, StringComparer.Ordinal)
            .ToList();

        return new ShiftBoardView(
            OnNow: onNow.Select(w => w.Cell.StaffId).Distinct().Count(),
            Departments: onNow.Select(w => w.Cell.DepartmentCode).Distinct().Count(),
            Rows: rows,
            NextChange: NextChange(worked, now, today, time));
    }

    /// <summary>One department-and-shift row, or nothing when it has no times.</summary>
    /// <remarks>
    /// An off shift covers no instant, so it never reaches here; a cell whose
    /// catalogue hours have gone missing has no times to show and is left out
    /// rather than drawn with blanks.
    /// </remarks>
    private static DepartmentOnShift? Row(
        string department, IReadOnlyList<(ShiftAssignment Cell, ShiftHours? Hours)> group)
    {
        var first = group[0];
        var span = ShiftCoverage.Spans(first.Cell, first.Hours);

        if (span.Count == 0)
        {
            return null;
        }

        var midnight = first.Cell.Date.ToDateTime(TimeOnly.MinValue);

        return new DepartmentOnShift(
            department,
            TimeOnly.FromDateTime(midnight.AddMinutes(span[0].Starts)),
            TimeOnly.FromDateTime(midnight.AddMinutes(span[^1].Ends)),
            group.Select(w => w.Cell.StaffId).Distinct().Count());
    }

    /// <summary>The next moment somebody's coverage changes, and by how many.</summary>
    /// <remarks>
    /// Counted by comparing who is covered a moment before and a moment at each
    /// boundary, which is why a split shift's gap does not read as two people
    /// arriving. The alternative — counting spans that start and end — is one
    /// line shorter and wrong for exactly the shift this property runs.
    /// </remarks>
    private static Changeover? NextChange(
        IReadOnlyList<(ShiftAssignment Cell, ShiftHours? Hours)> worked,
        DateTimeOffset now,
        DateOnly today,
        TimeOnly time)
    {
        var horizon = now.UtcDateTime.AddDays(1);

        var next = worked
            .SelectMany(w => ShiftCoverage.Boundaries(w.Cell, w.Hours))
            .Where(moment => moment > now.UtcDateTime && moment <= horizon)
            .DefaultIfEmpty()
            .Min();

        if (next == default)
        {
            return null;
        }

        var before = Covered(worked, today, time);
        var after = Covered(worked, DateOnly.FromDateTime(next), TimeOnly.FromDateTime(next));

        return new Changeover(
            new DateTimeOffset(next, TimeSpan.Zero),
            On: after.Except(before).Count(),
            Off: before.Except(after).Count());
    }

    /// <summary>Who is covered at one moment.</summary>
    private static HashSet<Guid> Covered(
        IReadOnlyList<(ShiftAssignment Cell, ShiftHours? Hours)> worked,
        DateOnly onDate,
        TimeOnly atTime) =>
        [.. worked
            .Where(w => ShiftCoverage.Covers(w.Cell, w.Hours, onDate, atTime))
            .Select(w => w.Cell.StaffId)];

    /// <summary>The catalogue hours in force on a cell's own date.</summary>
    private static ShiftHours? InForce(IReadOnlyList<ShiftHours> revisions, ShiftAssignment cell) =>
        revisions.FirstOrDefault(
            h => h.CatalogueEntryId == cell.CatalogueEntryId && h.InForceOn(cell.Date));
}
