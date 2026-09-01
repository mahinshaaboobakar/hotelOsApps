using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Rota;

/// <summary>
/// The rota — who is on which shift, on which day.
/// </summary>
/// <remarks>
/// <para>
/// Direct manipulation, per the owner's own refusal of templates and rotation
/// engines: a cell is clicked and filled, a week is copied into empty cells, two
/// cells are exchanged. <b>Simple beats clever</b> was the founding direction and
/// it is the whole design.
/// </para>
/// <para>
/// <b>Every cell references the catalogue entry</b>, never a set of hours — see
/// <see cref="ShiftAssignment"/>. Planned hours are computed from the revision in
/// force on the cell's own date, which is what makes rescheduling a shift leave
/// last month alone.
/// </para>
/// </remarks>
public class RotaService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    TimeProvider clock)
{
    /// <summary>Put somebody on a shift for a day.</summary>
    public async Task<ShiftAssignment> AssignAsync(
        RequestScope scope, AssignShiftCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ShiftDefine, "property", scope.PropertyId, cancellationToken);

        var code = Normalise(command.DepartmentCode);

        if (code.Length == 0)
        {
            throw new InvalidRequestException("department_code is required");
        }

        ValidateOverride(command);
        await RequireOfferedAsync(scope.PropertyId, command.CatalogueEntryId, cancellationToken);

        var now = clock.GetUtcNow();
        var existing = await FindAsync(
            scope.PropertyId, command.StaffId, command.Date, cancellationToken);

        // Replacing is what clicking a filled cell and choosing another shift
        // means. It is deliberate here and deliberately absent from CopyWeek.
        if (existing is not null)
        {
            existing.CatalogueEntryId = command.CatalogueEntryId;
            existing.DepartmentCode = code;
            existing.OverrideStartsAt = command.OverrideStartsAt;
            existing.OverrideEndsAt = command.OverrideEndsAt;
            existing.UpdatedAt = now;
            existing.Version += 1;

            await db.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var assignment = new ShiftAssignment
        {
            Id = Uuid7.NewUuid7(),
            PropertyId = scope.PropertyId,
            StaffId = command.StaffId,
            Date = command.Date,
            CatalogueEntryId = command.CatalogueEntryId,
            DepartmentCode = code,
            OverrideStartsAt = command.OverrideStartsAt,
            OverrideEndsAt = command.OverrideEndsAt,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        db.ShiftAssignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        return assignment;
    }

    /// <summary>Empty a cell.</summary>
    public async Task ClearAsync(
        RequestScope scope, ClearShiftCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ShiftDefine, "property", scope.PropertyId, cancellationToken);

        var assignment = await FindAsync(
            scope.PropertyId, command.StaffId, command.Date, cancellationToken);

        // Clearing an already-empty cell is what the caller asked for, so it is
        // not an error. A refusal here would make a manager's double-click a
        // failure dialog.
        if (assignment is null)
        {
            return;
        }

        db.ShiftAssignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Copy a week forward, filling empty cells only.</summary>
    /// <returns>How many cells were filled.</returns>
    public async Task<int> CopyWeekAsync(
        RequestScope scope, CopyWeekCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ShiftDefine, "property", scope.PropertyId, cancellationToken);

        var offset = command.To.DayNumber - command.From.DayNumber;

        if (offset <= 0)
        {
            throw new InvalidRequestException("a week is copied forward, not backward or onto itself");
        }

        var source = await ReadAsync(
            scope,
            new RotaQuery
            {
                From = command.From,
                To = command.From.AddDays(6),
                DepartmentCode = command.DepartmentCode,
            },
            cancellationToken);

        var target = await ReadAsync(
            scope,
            new RotaQuery
            {
                From = command.To,
                To = command.To.AddDays(6),
                DepartmentCode = command.DepartmentCode,
            },
            cancellationToken);

        var taken = target.Select(a => (a.StaffId, a.Date)).ToHashSet();
        var now = clock.GetUtcNow();
        var filled = 0;

        foreach (var cell in source)
        {
            var date = cell.Date.AddDays(offset);

            // **Empty cells only.** Overwriting would silently undo a decision
            // somebody had already made about the new week — and a manager who
            // wanted that would clear the cell first.
            if (taken.Contains((cell.StaffId, date)))
            {
                continue;
            }

            db.ShiftAssignments.Add(new ShiftAssignment
            {
                Id = Uuid7.NewUuid7(),
                PropertyId = scope.PropertyId,
                StaffId = cell.StaffId,
                Date = date,
                CatalogueEntryId = cell.CatalogueEntryId,
                DepartmentCode = cell.DepartmentCode,

                // The override is **not** copied. It was a one-off for one day —
                // that is what made it an override rather than a change to the
                // shift — and carrying it forward would make a single exception
                // permanent without anybody deciding so.
                CreatedAt = now,
                UpdatedAt = now,
                Version = 1,
            });

            filled += 1;
        }

        await db.SaveChangesAsync(cancellationToken);
        return filled;
    }

    /// <summary>Exchange two cells, atomically.</summary>
    public async Task SwapAsync(
        RequestScope scope, SwapShiftsCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ShiftDefine, "property", scope.PropertyId, cancellationToken);

        if (command.FirstAssignmentId == command.SecondAssignmentId)
        {
            throw new InvalidRequestException("a shift cannot be swapped with itself");
        }

        var first = await LoadAsync(scope, command.FirstAssignmentId, cancellationToken);
        var second = await LoadAsync(scope, command.SecondAssignmentId, cancellationToken);

        // The same exchange an approved staff proposal performs — one
        // implementation, so a manager's rearrangement and an approved swap
        // cannot produce different rotas. What moves is the shift; the owner and
        // the day do not.
        ShiftExchange.Apply(first, second, clock.GetUtcNow());

        // One SaveChanges, one transaction: both cells change or neither does.
        // A half-applied swap leaves one person covering two shifts and the
        // other none.
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The cells in a window.</summary>
    public async Task<IReadOnlyList<ShiftAssignment>> ReadAsync(
        RequestScope scope, RotaQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var cells = db.ShiftAssignments.Where(
            a => a.PropertyId == scope.PropertyId
                 && a.Date >= query.From
                 && a.Date <= query.To);

        if (!string.IsNullOrWhiteSpace(query.DepartmentCode))
        {
            var code = Normalise(query.DepartmentCode);
            cells = cells.Where(a => a.DepartmentCode == code);
        }

        if (query.StaffId is { } staffId)
        {
            cells = cells.Where(a => a.StaffId == staffId);
        }

        return await cells
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StaffId)
            .ToListAsync(cancellationToken);
    }

    private async Task RequireOfferedAsync(
        Guid propertyId, Guid catalogueEntryId, CancellationToken cancellationToken)
    {
        var offered = await db.ShiftCatalogue.AnyAsync(
            e => e.Id == catalogueEntryId && e.PropertyId == propertyId && e.Active,
            cancellationToken);

        if (!offered)
        {
            // A retired shift is still readable on the rotas it was worked
            // under; what it may no longer be is *assigned*. The catalogue is
            // the picker, and a retired entry has left it.
            throw new InvalidRequestException(
                "that shift is not in this property's catalogue, or has been retired");
        }
    }

    private static void ValidateOverride(AssignShiftCommand command)
    {
        var stated = command.OverrideStartsAt is not null || command.OverrideEndsAt is not null;

        if (stated && (command.OverrideStartsAt is null || command.OverrideEndsAt is null))
        {
            throw new InvalidRequestException(
                "a one-off span states both a start and an end");
        }
    }

    private async Task<ShiftAssignment?> FindAsync(
        Guid propertyId, Guid staffId, DateOnly date, CancellationToken cancellationToken) =>
        await db.ShiftAssignments.FirstOrDefaultAsync(
            a => a.PropertyId == propertyId && a.StaffId == staffId && a.Date == date,
            cancellationToken);

    private async Task<ShiftAssignment> LoadAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken)
    {
        var assignment = await db.ShiftAssignments.FirstOrDefaultAsync(
            a => a.Id == id && a.PropertyId == scope.PropertyId, cancellationToken);

        return assignment ?? throw new NotFoundException("shift assignment", id);
    }

    private static string Normalise(string? code) =>
        code?.Trim().ToUpperInvariant() ?? string.Empty;
}
