using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Standard;

/// <summary>Setup's house tabs — zones for rooms, the areas' schedules, the deep-clean plan per room type (frames 7d–7f).</summary>
public sealed class HouseSetupService(RoomCareDbContext db, Gate gate, IHouse house, PropertyClock clock)
{
    /// <summary>Put rooms in a Master Data zone — ADR 0044's assignment, Room Care's; the previous membership ends today.</summary>
    public async Task<int> AssignZoneAsync(
        RequestScope scope, IReadOnlyList<Guid> roomIds, Guid zoneId, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        if (roomIds.Count == 0)
        {
            throw new InvalidRequestException("choose at least one room");
        }

        if ((await house.ZonesAsync(scope.PropertyId, cancellationToken)).All(z => z.Id != zoneId))
        {
            throw new InvalidRequestException("that zone is not one of this property's zones in Master Data");
        }

        var person = Actor.PersonOf(scope, "assigning rooms to a zone");
        var today = (await clock.AtAsync(scope.PropertyId, cancellationToken)).Day;
        var standing = await db.ZoneAssignments
            .Where(z => z.PropertyId == scope.PropertyId && roomIds.Contains(z.RoomId) && z.EffectiveUntil == null)
            .ToListAsync(cancellationToken);
        foreach (var row in standing)
        {
            row.EffectiveUntil = today;
        }

        await db.SaveChangesAsync(cancellationToken);
        foreach (var room in roomIds.Distinct())
        {
            if (standing.Any(s => s.RoomId == room && s.ZoneId == zoneId && s.EffectiveFrom == today))
            {
                continue;
            }

            db.ZoneAssignments.Add(new RoomZoneAssignment
            {
                Id = Guid.CreateVersion7(), PropertyId = scope.PropertyId, RoomId = room, ZoneId = zoneId, EffectiveFrom = today, AssignedBy = person,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return roomIds.Count;
    }

    /// <summary>When a public area is cleaned — Master Data's node, Room Care's routine (S3).</summary>
    public async Task<AreaSchedule> SaveAreaAsync(
        RequestScope scope, Guid locationId, IReadOnlyList<TimeOnly> times, int minutes, bool enabled, long expectedVersion,
        CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        if ((await house.AreasAsync(scope.PropertyId, cancellationToken)).All(a => a.Id != locationId))
        {
            throw new InvalidRequestException("that place is not one of this property's public areas in Master Data");
        }

        if (times.Count > 24 || minutes is < 1 or > 480)
        {
            throw new InvalidRequestException("an area has at most 24 times a day, each between 1 and 480 minutes");
        }

        var saved = await db.AreaSchedules.FirstOrDefaultAsync(a => a.PropertyId == scope.PropertyId && a.LocationId == locationId, cancellationToken);
        if (saved is null)
        {
            saved = new AreaSchedule { Id = Guid.CreateVersion7(), PropertyId = scope.PropertyId, LocationId = locationId };
            db.AreaSchedules.Add(saved);
        }

        if (saved.Version != expectedVersion)
        {
            throw new ConcurrencyException("area_schedule", saved.Id, expectedVersion);
        }

        saved.Times = times.Distinct().Order().ToList();
        saved.Minutes = minutes;
        saved.Enabled = enabled;
        saved.Version += 1;
        await db.SaveChangesAsync(cancellationToken);
        return saved;
    }

    /// <summary>How often a room type is deep cleaned (S0).</summary>
    public async Task<DeepCleanPlan> SaveDeepCleanPlanAsync(
        RequestScope scope, Guid roomTypeId, int everyMonths, long expectedVersion, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        if (everyMonths is < 1 or > 60)
        {
            throw new InvalidRequestException("a deep clean comes round every 1 to 60 months");
        }

        var saved = await db.DeepCleanPlans.FirstOrDefaultAsync(p => p.PropertyId == scope.PropertyId && p.RoomTypeId == roomTypeId, cancellationToken);
        if (saved is null)
        {
            saved = new DeepCleanPlan { Id = Guid.CreateVersion7(), PropertyId = scope.PropertyId, RoomTypeId = roomTypeId };
            db.DeepCleanPlans.Add(saved);
        }

        if (saved.Version != expectedVersion)
        {
            throw new ConcurrencyException("deep_clean_plan", saved.Id, expectedVersion);
        }

        saved.EveryMonths = everyMonths;
        saved.Version += 1;
        await db.SaveChangesAsync(cancellationToken);
        return saved;
    }
}
