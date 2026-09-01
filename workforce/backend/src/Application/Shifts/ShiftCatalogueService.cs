using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Shifts;

/// <summary>
/// The property's shift catalogue, and the hours each shift has had.
/// </summary>
/// <remarks>
/// <para>
/// <c>WF-Q11</c>: the catalogue is property-created and free-form.
/// <c>WF-Q15</c>: editing the hours is <b>effective-forward from a
/// manager-chosen date</b>, and never rewrites history.
/// </para>
/// <para>
/// <b>The ruling is kept by the model rather than by this service.</b> An
/// assignment references the catalogue entry, not a set of hours, so resolving
/// what was worked on a date reads the revision in force on that date. There is
/// no code path here that could rewrite a past rota — which is stronger than a
/// service that carefully avoids doing so.
/// </para>
/// </remarks>
public class ShiftCatalogueService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    TimeProvider clock)
{
    /// <summary>Add a shift to the catalogue.</summary>
    public async Task<ShiftCatalogueEntry> CreateAsync(
        RequestScope scope, CreateShiftCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PolicyManage, "property", scope.PropertyId, cancellationToken);

        var name = Require(command.Name, "name");
        var shortCode = Require(command.ShortCode, "short_code");
        var colour = Require(command.Colour, "colour");

        Validate(command.Hours);

        // A short code is what a rota cell shows and what a photocopy keeps, so
        // two entries sharing one would be two shifts that look identical on
        // paper — the exact failure the typed-not-derived rule exists to
        // prevent, arriving by a different route.
        await RefuseDuplicateCodeAsync(scope.PropertyId, shortCode, null, cancellationToken);

        var now = clock.GetUtcNow();
        var entry = new ShiftCatalogueEntry
        {
            Id = Uuid7.NewUuid7(),
            PropertyId = scope.PropertyId,
            Name = name,
            ShortCode = shortCode,
            Colour = colour,
            Active = true,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        db.ShiftCatalogue.Add(entry);
        db.ShiftHours.Add(HoursFrom(entry, command.Hours, command.EffectiveFrom, now));

        await db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    /// <summary>Change how a shift reads. Its hours are untouched.</summary>
    public async Task<ShiftCatalogueEntry> RenameAsync(
        RequestScope scope, RenameShiftCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PolicyManage, "property", scope.PropertyId, cancellationToken);

        var entry = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(entry, command.ExpectedVersion);

        if (command.Name is { } name)
        {
            entry.Name = Require(name, "name");
        }

        if (command.ShortCode is { } shortCode)
        {
            var code = Require(shortCode, "short_code");
            await RefuseDuplicateCodeAsync(scope.PropertyId, code, entry.Id, cancellationToken);
            entry.ShortCode = code;
        }

        if (command.Colour is { } colour)
        {
            entry.Colour = Require(colour, "colour");
        }

        entry.UpdatedAt = clock.GetUtcNow();
        entry.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    /// <summary>Change a shift's hours, forward from a chosen date.</summary>
    /// <remarks>
    /// The successor is created and the current revision is closed <b>the day
    /// before it starts</b>, so the series can have neither a gap nor an
    /// overlap: every date from the catalogue entry's first day onward resolves
    /// to exactly one set of hours.
    /// </remarks>
    public async Task<ShiftHours> RescheduleAsync(
        RequestScope scope, RescheduleShiftCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PolicyManage, "property", scope.PropertyId, cancellationToken);

        var entry = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(entry, command.ExpectedVersion);

        Validate(command.Hours);

        var current = await CurrentHoursAsync(entry.Id, cancellationToken);

        // Backdating before the hours currently in force began would leave the
        // series with two rows claiming one date. Refused rather than resolved:
        // the caller means either "correct the current hours" (which is not this
        // operation) or "start a new period", and guessing between them is how a
        // rota quietly changes under somebody.
        if (current is not null && command.EffectiveFrom <= current.EffectiveFrom)
        {
            throw new InvalidRequestException(
                "effective_from must be after the hours currently in force began "
                + $"({current.EffectiveFrom:O})");
        }

        var now = clock.GetUtcNow();

        if (current is not null)
        {
            current.EffectiveTo = command.EffectiveFrom.AddDays(-1);
        }

        var hours = HoursFrom(entry, command.Hours, command.EffectiveFrom, now);
        db.ShiftHours.Add(hours);

        entry.UpdatedAt = now;
        entry.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return hours;
    }

    /// <summary>Stop offering a shift. Every rota it was worked under survives.</summary>
    public async Task<ShiftCatalogueEntry> RetireAsync(
        RequestScope scope, RetireShiftCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PolicyManage, "property", scope.PropertyId, cancellationToken);

        var entry = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(entry, command.ExpectedVersion);

        entry.Active = false;
        entry.UpdatedAt = clock.GetUtcNow();
        entry.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    /// <summary>The catalogue, as a rota picker shows it.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="includeRetired">Retired entries too, for a historical view.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The entries.</returns>
    public async Task<IReadOnlyList<ShiftCatalogueEntry>> ListAsync(
        RequestScope scope, bool includeRetired, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        var entries = db.ShiftCatalogue.Where(e => e.PropertyId == scope.PropertyId);

        if (!includeRetired)
        {
            entries = entries.Where(e => e.Active);
        }

        return await entries.OrderBy(e => e.ShortCode).ToListAsync(cancellationToken);
    }

    /// <summary>The hours in force for a shift on a given day.</summary>
    /// <remarks>
    /// <b>The whole of <c>WF-Q15</c>, in one query.</b> A rota for last March
    /// asks for March and gets March's hours; an edit made in November changed
    /// nothing about that answer, because it created a revision that starts in
    /// November.
    /// </remarks>
    /// <param name="scope">The caller.</param>
    /// <param name="catalogueEntryId">The shift.</param>
    /// <param name="on">The day.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The hours in force, or null when the shift did not exist then.</returns>
    public async Task<ShiftHours?> HoursOnAsync(
        RequestScope scope,
        Guid catalogueEntryId,
        DateOnly on,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        return await db.ShiftHours
            .Where(h => h.PropertyId == scope.PropertyId
                        && h.CatalogueEntryId == catalogueEntryId
                        && h.EffectiveFrom <= on
                        && (h.EffectiveTo == null || h.EffectiveTo >= on))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>Every set of hours a shift has had, oldest first.</summary>
    public async Task<IReadOnlyList<ShiftHours>> HistoryAsync(
        RequestScope scope, Guid catalogueEntryId, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        return await db.ShiftHours
            .Where(h => h.PropertyId == scope.PropertyId && h.CatalogueEntryId == catalogueEntryId)
            .OrderBy(h => h.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    private async Task<ShiftHours?> CurrentHoursAsync(
        Guid catalogueEntryId, CancellationToken cancellationToken) =>
        await db.ShiftHours
            .Where(h => h.CatalogueEntryId == catalogueEntryId && h.EffectiveTo == null)
            .SingleOrDefaultAsync(cancellationToken);

    private static ShiftHours HoursFrom(
        ShiftCatalogueEntry entry,
        ShiftHoursCommand hours,
        DateOnly from,
        DateTimeOffset now) => new()
    {
        Id = Uuid7.NewUuid7(),
        PropertyId = entry.PropertyId,
        CatalogueEntryId = entry.Id,
        StartsAt = hours.StartsAt,
        EndsAt = hours.EndsAt,
        SecondStartsAt = hours.SecondStartsAt,
        SecondEndsAt = hours.SecondEndsAt,
        EffectiveFrom = from,
        CreatedAt = now,
    };

    /// <summary>What a set of hours may and may not be.</summary>
    /// <remarks>
    /// Every refusal here is a record that cannot be true rather than a judgment
    /// — <c>WF-Q16</c>. A half-stated span and a second span without a first are
    /// both self-contradicting; a span that ends before it starts is <b>not</b>,
    /// because that is how a night shift is written.
    /// </remarks>
    private static void Validate(ShiftHoursCommand hours)
    {
        var firstStated = hours.StartsAt is not null || hours.EndsAt is not null;

        if (firstStated && (hours.StartsAt is null || hours.EndsAt is null))
        {
            throw new InvalidRequestException(
                "a shift states both a start and an end, or neither — neither makes it an off shift");
        }

        // A shift that ends where it starts is zero hours long — WF-Q17. It is
        // not a midnight-crossing round-the-clock shift, and it is not something
        // a property can have meant, so the catalogue never carries one and the
        // rota never has to ask.
        if (hours.StartsAt is { } from && hours.EndsAt is { } to && from == to)
        {
            throw new InvalidRequestException(
                "a shift cannot end at the moment it starts — that is zero hours long");
        }

        var secondStated = hours.SecondStartsAt is not null || hours.SecondEndsAt is not null;

        if (secondStated && (hours.SecondStartsAt is null || hours.SecondEndsAt is null))
        {
            throw new InvalidRequestException(
                "a split shift's second span states both a start and an end");
        }

        if (hours.SecondStartsAt is { } secondFrom && hours.SecondEndsAt is { } secondTo
            && secondFrom == secondTo)
        {
            throw new InvalidRequestException(
                "a split shift's second span cannot end at the moment it starts");
        }

        if (secondStated && !firstStated)
        {
            throw new InvalidRequestException(
                "a shift cannot have a second span and no first");
        }
    }

    private async Task RefuseDuplicateCodeAsync(
        Guid propertyId, string shortCode, Guid? excluding, CancellationToken cancellationToken)
    {
        var taken = await db.ShiftCatalogue.AnyAsync(
            e => e.PropertyId == propertyId
                 && e.Active
                 && e.ShortCode == shortCode
                 && (excluding == null || e.Id != excluding),
            cancellationToken);

        if (taken)
        {
            throw new InvalidRequestException(
                $"another shift already uses the code '{shortCode}' at this property");
        }
    }

    private async Task<ShiftCatalogueEntry> LoadAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken)
    {
        var entry = await db.ShiftCatalogue.FirstOrDefaultAsync(
            e => e.Id == id && e.PropertyId == scope.PropertyId, cancellationToken);

        return entry ?? throw new NotFoundException("shift", id);
    }

    private static void RequireVersion(ShiftCatalogueEntry entry, long expected)
    {
        if (entry.Version != expected)
        {
            throw new ConcurrencyException("shift", entry.Id, expected);
        }
    }

    private static string Require(string? value, string field)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        return trimmed.Length > 0
            ? trimmed
            : throw new InvalidRequestException($"{field} is required");
    }
}
