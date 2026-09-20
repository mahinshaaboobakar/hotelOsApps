using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Calendar;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Shifts;

/// <summary>
/// Announcing shift boundaries as they fall — Workforce's fan-out.
/// </summary>
/// <remarks>
/// <para>
/// Ruled 2026-09-04 on Jobs' <c>S5-D13</c>. Jobs keeps a
/// <c>department_presence</c> row per department and asked for the two events
/// that maintain it; every other application that cares whether a department is
/// staffed subscribes to the same pair.
/// </para>
/// <para>
/// <b>A sweep, not a schedule per boundary.</b> The rota row already holds the
/// boundary instant, so a schedule per boundary is the per-entity-timer shape
/// wearing a rota: two schedules per cell per day, and every rota edit — assign,
/// clear, swap, copy-week, override, reschedule — cancelling and rebuilding
/// them. Temporal holds the recurring tick instead, and the future that is the
/// thing is <i>every minute, look</i>. This class is the looking; the host that
/// calls it is the platform's.
/// </para>
/// <para>
/// <b>It is idempotent, and that is what makes the tick ordinary.</b> Every
/// announcement writes a <see cref="ShiftBoundary"/> row under a unique key in
/// the same transaction as the event, so a tick that overlaps, retries or
/// restarts announces nothing twice. The trigger needs to be at-least-once and
/// nothing more.
/// </para>
/// <para>
/// <b>It catches up rather than skipping.</b> An outage does not lose
/// boundaries: the next tick announces everything inside the lookback that has
/// no row yet. Beyond the lookback they are gone, which is stated rather than
/// hidden — a consumer recovering from longer than that reads the current state
/// instead, which is why the state-by-read half of the ruling exists.
/// </para>
/// </remarks>
public class ShiftBoundaryAnnouncer(
    WorkforceDbContext db,
    IEventAppender events,
    IStaffDirectory directory,
    TimeProvider clock)
{
    /// <summary>
    /// How far back a tick will look for boundaries nobody announced.
    /// </summary>
    /// <remarks>
    /// One day, which is what the coverage question already loads: a night shift
    /// belongs to the day it starts on, so yesterday's cells are read anyway.
    /// Making the catch-up window and the coverage window the same span means
    /// there is one number rather than two that must agree.
    /// </remarks>
    private const int LookbackDays = 1;

    /// <summary>Announce every boundary that has fallen and has not been announced.</summary>
    /// <param name="scope">
    /// The application's own service identity — the worker acts as the package,
    /// not as a user. Passed in rather than constructed here: the SDK's
    /// constructor for it is the platform's, and this service is testable
    /// without waiting for it.
    /// </param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>How many announcements were appended.</returns>
    /// <remarks>
    /// <b>It saves once.</b> Every row and every event in this tick commit
    /// together, so a crash halfway leaves the whole tick unannounced and the
    /// next one repeats it — rather than leaving half the departments told and
    /// no way to know which.
    /// </remarks>
    public async Task<int> AnnounceDueAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        // **The property's wall clock, not the operating day.** A shift's hours
        // are wall-clock times there, so "has 07:00 passed" is a question about
        // the property's own clock — ADR 0174, and the same distinction the
        // shift board needed. ADR 0211's operating day is the answer to *which
        // day is it*, which is not what a boundary is compared against.
        //
        // Both sides were UTC, so at +05:30 a 07:00 start was announced as due
        // at 12:30 local, and every consumer of `shift.started` was five and a
        // half hours late.
        var calendar = await PropertyCalendar.ForAsync(
            directory, scope.PropertyId, cancellationToken);

        var now = clock.GetUtcNow();
        var local = calendar.LocalAt(now);
        var today = DateOnly.FromDateTime(local);
        var from = today.AddDays(-LookbackDays);

        var cells = await db.ShiftAssignments
            .Where(a => a.PropertyId == scope.PropertyId && a.Date >= from && a.Date <= today)
            .ToListAsync(cancellationToken);

        if (cells.Count == 0)
        {
            return 0;
        }

        var revisions = await Revisions(scope.PropertyId, from, today, cells, cancellationToken);

        var worked = cells
            .Select(cell => (Cell: cell, Hours: InForce(revisions, cell)))
            .ToList();

        var announced = await db.ShiftBoundaries
            .Where(b => b.PropertyId == scope.PropertyId && b.BusinessDate >= from)
            .Select(b => new { b.DepartmentCode, b.CatalogueEntryId, b.BusinessDate, b.Kind })
            .ToListAsync(cancellationToken);

        var already = announced
            .Select(b => (b.DepartmentCode, b.CatalogueEntryId, b.BusinessDate, b.Kind))
            .ToHashSet();

        var appended = 0;

        foreach (var due in Due(worked, calendar, local))
        {
            var key = (due.DepartmentCode, due.CatalogueEntryId, due.BusinessDate, due.Kind);

            if (!already.Add(key))
            {
                continue;
            }

            Announce(scope, due, worked, calendar, now);
            appended++;
        }

        if (appended > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return appended;
    }

    /// <summary>Every boundary in the window that has already fallen.</summary>
    /// <remarks>
    /// <b>One per department and shift, not one per person.</b> Nine attendants
    /// coming on at seven is one fact about Housekeeping, and nine events would
    /// be nine consumers doing the same aggregation the wrong way.
    /// </remarks>
    /// <remarks>
    /// <b>The comparison is wall clock against wall clock</b>, and the instant
    /// it yields is that moment converted through the property's zone. It used
    /// to build each boundary at <c>TimeSpan.Zero</c> and compare it against a
    /// UTC now, which at +05:30 made every announcement five and a half hours
    /// late and stamped each event with an instant the property never saw.
    /// </remarks>
    private static IEnumerable<DueBoundary> Due(
        IReadOnlyList<(ShiftAssignment Cell, ShiftHours? Hours)> worked,
        PropertyCalendar calendar,
        DateTime local)
    {
        var seen = new HashSet<(string, Guid, DateOnly, ShiftBoundaryKind)>();

        foreach (var (cell, hours) in worked)
        {
            var moments = ShiftCoverage.Boundaries(cell, hours).ToList();

            for (var i = 0; i < moments.Count; i++)
            {
                // Boundaries come out in span order — start, end, start, end —
                // so an even index is a start.
                var kind = i % 2 == 0 ? ShiftBoundaryKind.Started : ShiftBoundaryKind.Ended;

                if (moments[i] > local)
                {
                    continue;
                }

                // The instant that wall-clock moment IS at the property.
                var at = calendar.At(
                    DateOnly.FromDateTime(moments[i]), TimeOnly.FromDateTime(moments[i]));

                var key = (cell.DepartmentCode, cell.CatalogueEntryId, cell.Date, kind);

                if (seen.Add(key))
                {
                    yield return new DueBoundary(
                        cell.DepartmentCode, cell.CatalogueEntryId, cell.Date, kind, at);
                }
            }
        }
    }

    /// <summary>Write the row and append the event, in one transaction.</summary>
    private void Announce(
        RequestScope scope,
        DueBoundary due,
        IReadOnlyList<(ShiftAssignment Cell, ShiftHours? Hours)> worked,
        PropertyCalendar calendar,
        DateTimeOffset now)
    {
        // Coverage **immediately after** the boundary, which is what a consumer
        // sets its presence from. At a handover the ended and started events
        // both carry this number, so the boolean is right whichever arrives
        // last.
        // **Read at the property's own clock.** `due.At` is an instant now, and
        // a shift's hours are wall-clock times: asking who is covered used the
        // instant's UTC face, which is a different time of day everywhere but
        // Greenwich.
        var when = calendar.LocalAt(due.At);

        var onNowAfter = worked
            .Where(w => w.Cell.DepartmentCode == due.DepartmentCode)
            .Where(w => ShiftCoverage.Covers(
                w.Cell,
                w.Hours,
                DateOnly.FromDateTime(when),
                TimeOnly.FromDateTime(when)))
            .Select(w => w.Cell.StaffId)
            .Distinct()
            .Count();

        var boundary = new ShiftBoundary
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            DepartmentCode = due.DepartmentCode,
            CatalogueEntryId = due.CatalogueEntryId,
            BusinessDate = due.BusinessDate,
            Kind = due.Kind,
            At = due.At,
            OnNowAfter = onNowAfter,
            AnnouncedAt = now,
        };

        db.ShiftBoundaries.Add(boundary);

        events.Append(
            scope,
            due.Kind == ShiftBoundaryKind.Started
                ? ShiftAnnouncements.Started
                : ShiftAnnouncements.Ended,
            ShiftAnnouncements.Aggregate,
            boundary.Id,

            // Always 1. The announcement row is written once and never updated,
            // so its version is not a sequence — it is the statement that this
            // aggregate has exactly one fact.
            1,
            new ShiftBoundaryAnnouncement
            {
                PropertyId = boundary.PropertyId,
                DepartmentCode = boundary.DepartmentCode,
                ShiftId = boundary.CatalogueEntryId,
                BusinessDate = boundary.BusinessDate.ToString("yyyy-MM-dd"),
                At = boundary.At,
                OnNowAfter = boundary.OnNowAfter,
            });
    }

    private async Task<List<ShiftHours>> Revisions(
        Guid propertyId,
        DateOnly from,
        DateOnly to,
        IReadOnlyList<ShiftAssignment> cells,
        CancellationToken cancellationToken)
    {
        var entryIds = cells.Select(c => c.CatalogueEntryId).Distinct().ToList();

        return await db.ShiftHours
            .Where(h => h.PropertyId == propertyId
                        && entryIds.Contains(h.CatalogueEntryId)
                        && h.EffectiveFrom <= to
                        && (h.EffectiveTo == null || h.EffectiveTo >= from))
            .ToListAsync(cancellationToken);
    }

    private static ShiftHours? InForce(IReadOnlyList<ShiftHours> revisions, ShiftAssignment cell) =>
        revisions.FirstOrDefault(
            h => h.CatalogueEntryId == cell.CatalogueEntryId && h.InForceOn(cell.Date));

    /// <summary>A boundary that has fallen and may not have been announced.</summary>
    private sealed record DueBoundary(
        string DepartmentCode,
        Guid CatalogueEntryId,
        DateOnly BusinessDate,
        ShiftBoundaryKind Kind,
        DateTimeOffset At);
}
