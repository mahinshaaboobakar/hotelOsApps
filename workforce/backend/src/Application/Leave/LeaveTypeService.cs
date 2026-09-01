using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Leave;

/// <summary>
/// The leave types a property offers, and the template it starts from.
/// </summary>
/// <remarks>
/// A separate service from <see cref="LeaveService"/> because configuring what
/// leave <i>is</i> and deciding one person's request are two purposes — the same
/// split the shift catalogue has from the rota, and for the same reason.
/// </remarks>
public class LeaveTypeService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    IStaffDirectory directory,
    TimeProvider clock)
{
    /// <summary>Seed the property's types from the template for where it is.</summary>
    /// <remarks>
    /// <para>
    /// <b>Keyed off the property's own setting, never a literal</b> — the
    /// country-seed ruling. The setting is <c>Property.Country</c>, which Master
    /// Data already carries; nothing new was invented to hold it, because the
    /// finding was that a literal had been written where a setting already
    /// existed.
    /// </para>
    /// <para>
    /// <b>Idempotent, and it never overwrites.</b> A property that has already
    /// configured its types has made decisions this must not undo, so seeding
    /// adds only what is missing — which also makes it safe to call again after
    /// a template gains an entry.
    /// </para>
    /// </remarks>
    /// <returns>How many types were added.</returns>
    public async Task<int> SeedAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterConfigure, "property", scope.PropertyId, cancellationToken);

        var country = await directory.FindPropertyCountryAsync(scope.PropertyId, cancellationToken);
        var template = LeaveTemplates.For(country);

        var existing = await db.LeaveTypes
            .Where(t => t.PropertyId == scope.PropertyId)
            .Select(t => t.Code)
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        var added = 0;

        foreach (var proposed in template)
        {
            if (existing.Contains(proposed.Code))
            {
                continue;
            }

            db.LeaveTypes.Add(new LeaveType
            {
                Id = Uuid7.NewUuid7(),
                PropertyId = scope.PropertyId,
                Code = proposed.Code,
                Name = proposed.Name,
                AccrualPerMonth = proposed.AccrualPerMonth,
                Active = true,
                CreatedAt = now,
                UpdatedAt = now,
                Version = 1,
            });

            added += 1;
        }

        await db.SaveChangesAsync(cancellationToken);
        return added;
    }

    /// <summary>Add or amend a type.</summary>
    public async Task<LeaveType> SetAsync(
        RequestScope scope, SetLeaveTypeCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterConfigure, "property", scope.PropertyId, cancellationToken);

        var code = Require(command.Code, "code").ToUpperInvariant();
        var name = Require(command.Name, "name");

        if (command.AccrualPerMonth is { } rate && rate < 0m)
        {
            throw new InvalidRequestException(
                "an accrual rate is zero or more, or absent when the type is granted by hand");
        }

        var now = clock.GetUtcNow();

        if (command.Id is not { } id)
        {
            var taken = await db.LeaveTypes.AnyAsync(
                t => t.PropertyId == scope.PropertyId && t.Code == code, cancellationToken);

            if (taken)
            {
                throw new InvalidRequestException(
                    $"this property already has a leave type '{code}'");
            }

            var created = new LeaveType
            {
                Id = Uuid7.NewUuid7(),
                PropertyId = scope.PropertyId,
                Code = code,
                Name = name,
                AccrualPerMonth = command.AccrualPerMonth,
                Active = true,
                CreatedAt = now,
                UpdatedAt = now,
                Version = 1,
            };

            db.LeaveTypes.Add(created);
            await db.SaveChangesAsync(cancellationToken);

            return created;
        }

        var type = await db.LeaveTypes.FirstOrDefaultAsync(
            t => t.Id == id && t.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("leave type", id);

        if (command.ExpectedVersion is { } expected && type.Version != expected)
        {
            throw new ConcurrencyException("leave type", type.Id, expected);
        }

        // The **code** does not change on an amendment. Ledger entries name the
        // type by id, but reports and exports group on the code, and a code that
        // moves takes a year of history with it.
        type.Name = name;
        type.AccrualPerMonth = command.AccrualPerMonth;
        type.UpdatedAt = now;
        type.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return type;
    }

    /// <summary>Stop offering a type. Its ledger entries survive.</summary>
    public async Task<LeaveType> RetireAsync(
        RequestScope scope, Guid id, long expectedVersion, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterConfigure, "property", scope.PropertyId, cancellationToken);

        var type = await db.LeaveTypes.FirstOrDefaultAsync(
            t => t.Id == id && t.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("leave type", id);

        if (type.Version != expectedVersion)
        {
            throw new ConcurrencyException("leave type", type.Id, expectedVersion);
        }

        type.Active = false;
        type.UpdatedAt = clock.GetUtcNow();
        type.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return type;
    }

    /// <summary>The types this property offers.</summary>
    public async Task<IReadOnlyList<LeaveType>> ListAsync(
        RequestScope scope, bool includeRetired, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var types = db.LeaveTypes.Where(t => t.PropertyId == scope.PropertyId);

        if (!includeRetired)
        {
            types = types.Where(t => t.Active);
        }

        return await types.OrderBy(t => t.Code).ToListAsync(cancellationToken);
    }

    private static string Require(string? value, string field)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        return trimmed.Length > 0
            ? trimmed
            : throw new InvalidRequestException($"{field} is required");
    }
}
