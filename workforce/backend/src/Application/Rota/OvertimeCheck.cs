using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Rota;

/// <summary>What a planned rota would cost somebody in hours, and where it warns.</summary>
/// <param name="StaffId">Whose week.</param>
/// <param name="PlannedHours">Hours the rota plans for them in the window.</param>
/// <param name="DailyExceedances">The days over the daily threshold, with their hours.</param>
/// <param name="ExceedsWeekly">Whether the window's total is over the weekly threshold.</param>
/// <remarks>
/// A warning carries <b>the number</b>, not just the fact. <i>"Vishnu is over"</i>
/// tells a manager nothing they can act on; <i>"Vishnu, 54.0 against 48"</i> tells
/// them how much to move.
/// </remarks>
public sealed record OvertimeWarning(
    Guid StaffId,
    decimal PlannedHours,
    IReadOnlyList<(DateOnly Date, decimal Hours)> DailyExceedances,
    bool ExceedsWeekly);

/// <summary>
/// The overtime warning — at planning time, on planned hours, and never blocking.
/// </summary>
/// <remarks>
/// <para>
/// <c>WF-Q14</c>, owner 2026-08-31: overtime <b>warns while the rota is being
/// built</b>, on planned hours against the property's threshold, warn-never-block
/// per <c>WF-Q5</c>; actuals arrive at month-end, and live mid-week alerting is
/// deferred while attendance is manual.
/// </para>
/// <para>
/// <b>Its own file, not a method on <see cref="RotaService"/>.</b> Filling a
/// cell and judging a week are two purposes (ADR 0038), and this one reads the
/// catalogue's effective-dated hours and the property's policy — collaborators
/// the rota service has no other reason to hold.
/// </para>
/// <para>
/// <b>It returns warnings; it never throws.</b> A refusal here would be this
/// application deciding a labour question for a hotel, which <c>WF-Q16</c> puts
/// on the judgment side of the line: a person can physically work the hours, and
/// whether they should is the hotel's call.
/// </para>
/// </remarks>
public class OvertimeCheck(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer)
{
    /// <summary>Check a window's planned hours against the property's thresholds.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="query">Which cells to judge.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>
    /// One warning per person who crosses a threshold. <b>Empty means nothing to
    /// say</b> — including when the property has set no threshold at all, which
    /// is not the same as everybody being within it and is why the caller gets a
    /// list rather than a verdict.
    /// </returns>
    public async Task<IReadOnlyList<OvertimeWarning>> CheckAsync(
        RequestScope scope, RotaQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var policy = await db.Policies.FirstOrDefaultAsync(
            p => p.PropertyId == scope.PropertyId, cancellationToken);

        // No threshold, nothing to warn about. Not zero, and not a default of
        // eight — a property that has never opened the policy screen must not
        // have every rota flagged by a labour rule this application invented.
        if (policy is null
            || (policy.OvertimeDailyHours is null && policy.OvertimeWeeklyHours is null))
        {
            return [];
        }

        var cells = await Cells(scope, query).ToListAsync(cancellationToken);

        if (cells.Count == 0)
        {
            return [];
        }

        // The hours in force **on each cell's own date** — the whole point of the
        // effective-dated catalogue. Loaded once for the window rather than per
        // cell, because a week's rota is a hundred cells over half a dozen
        // shifts and a query each would be a hundred round trips (CLAUDE.md's
        // per-round-trip review of a hot path).
        var entryIds = cells.Select(c => c.CatalogueEntryId).Distinct().ToList();

        var revisions = await db.ShiftHours
            .Where(h => h.PropertyId == scope.PropertyId
                        && entryIds.Contains(h.CatalogueEntryId)
                        && h.EffectiveFrom <= query.To
                        && (h.EffectiveTo == null || h.EffectiveTo >= query.From))
            .ToListAsync(cancellationToken);

        var warnings = new List<OvertimeWarning>();

        foreach (var person in cells.GroupBy(c => c.StaffId))
        {
            var total = 0m;
            var overDaily = new List<(DateOnly, decimal)>();

            foreach (var cell in person)
            {
                var hours = revisions.FirstOrDefault(
                    h => h.CatalogueEntryId == cell.CatalogueEntryId && h.InForceOn(cell.Date));

                var planned = WorkedHours.Planned(cell, hours);
                total += planned;

                if (policy.OvertimeDailyHours is { } daily && planned > daily)
                {
                    overDaily.Add((cell.Date, planned));
                }
            }

            var overWeekly = policy.OvertimeWeeklyHours is { } weekly && total > weekly;

            if (overDaily.Count > 0 || overWeekly)
            {
                warnings.Add(new OvertimeWarning(person.Key, total, overDaily, overWeekly));
            }
        }

        return warnings;
    }

    private IQueryable<ShiftAssignment> Cells(RequestScope scope, RotaQuery query)
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

        return query.StaffId is { } staffId
            ? cells.Where(a => a.StaffId == staffId)
            : cells;
    }
}
