using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Settings;

/// <summary>
/// When a department's clock runs — S7, settings frame 2: the owner's on/off,
/// whether presence follows Workforce's shifts, the service-hours fallback.
/// The shift fan-out lands here too: <see cref="ShiftStartedAsync"/> and
/// <see cref="ShiftEndedAsync"/> are the consumers' one call each.
/// </summary>
public class PresenceService(JobsDbContext db, IKernelAuthorizer authorizer, TimeProvider clock)
{
    public async Task<DepartmentPresence> SaveAsync(RequestScope scope, PresenceCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Configure, "property", scope.PropertyId, cancellationToken);
        var presence = await FindOrAddAsync(scope.PropertyId, command.DepartmentCode, cancellationToken);
        presence.Enabled = command.Enabled;
        presence.FollowShifts = command.FollowShifts;
        await db.SaveChangesAsync(cancellationToken);
        return presence;
    }

    public async Task<ServiceHours> SaveHoursAsync(RequestScope scope, ServiceHoursCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Configure, "property", scope.PropertyId, cancellationToken);
        var code = command.DepartmentCode?.Trim().ToUpperInvariant();
        var hours = await db.ServiceHours.FirstOrDefaultAsync(
            h => h.PropertyId == scope.PropertyId && h.DepartmentCode == code, cancellationToken);
        if (hours is null)
        {
            hours = new ServiceHours { Id = Uuid7.NewUuid7(), PropertyId = scope.PropertyId, DepartmentCode = code };
            db.ServiceHours.Add(hours);
        }

        hours.From = command.From;
        hours.To = command.To;
        await db.SaveChangesAsync(cancellationToken);
        return hours;
    }

    /// <summary>Workforce says the department's shift began: present, with a head-count.</summary>
    public async Task ShiftStartedAsync(Guid propertyId, string departmentCode, int onShift, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var presence = await FindOrAddAsync(propertyId, departmentCode, cancellationToken);
        if (!presence.FollowShifts) return;

        presence.Staffed = true;
        presence.Since = at;
        presence.OnShift = onShift;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Workforce says the department's shift ended: absent, unless service hours still say present.</summary>
    public async Task ShiftEndedAsync(Guid propertyId, string departmentCode, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var presence = await FindOrAddAsync(propertyId, departmentCode, cancellationToken);
        if (!presence.FollowShifts) return;

        presence.Staffed = await InsideHoursAsync(propertyId, departmentCode, at, cancellationToken);
        presence.Since = at;
        presence.OnShift = 0;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Whether service hours — the department's, else the property's — contain the moment.</summary>
    public async Task<bool> InsideHoursAsync(Guid propertyId, string departmentCode, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var hours = await db.ServiceHours
            .Where(h => h.PropertyId == propertyId && (h.DepartmentCode == departmentCode || h.DepartmentCode == null))
            .ToListAsync(cancellationToken);
        var window = hours.FirstOrDefault(h => h.DepartmentCode == departmentCode) ?? hours.FirstOrDefault(h => h.DepartmentCode == null);
        return window?.Contains(TimeOnly.FromDateTime(at.UtcDateTime)) ?? false;
    }

    private async Task<DepartmentPresence> FindOrAddAsync(Guid propertyId, string departmentCode, CancellationToken cancellationToken)
    {
        var code = departmentCode.Trim().ToUpperInvariant();
        if (code.Length == 0) throw new InvalidRequestException("department_code is required");

        var presence = await db.Presence.FirstOrDefaultAsync(
            p => p.PropertyId == propertyId && p.DepartmentCode == code, cancellationToken);
        if (presence is null)
        {
            presence = new DepartmentPresence
            {
                Id = Uuid7.NewUuid7(), PropertyId = propertyId, DepartmentCode = code, Since = clock.GetUtcNow(),
            };
            db.Presence.Add(presence);
        }

        return presence;
    }
}
