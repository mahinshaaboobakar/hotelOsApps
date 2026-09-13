using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>Setup's seven tabs, read — the standard with its live versions (frames 7a–7g).</summary>
/// <remarks>Reading the standard is <c>roomcare.configure</c>'s, like changing it: Setup is one screen for the one role.</remarks>
public sealed class SetupProjection(RoomCareDbContext db, IHouse house, StandardReader standard, Gate gate, Application.Days.PropertyClock clock)
{
    public const int AreaPageSize = 12;

    public async Task<SetupView> SetupAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        var p = await standard.PolicyAsync(scope.PropertyId, cancellationToken);
        var windows = await standard.WindowsAsync(scope.PropertyId, cancellationToken);
        var by = p.ChangedBy is { } id ? (await house.NamesAsync([id], cancellationToken)).GetValueOrDefault(id) : null;
        return new SetupView(
            new PolicyView(p.TriggerMode, p.WhoLeads, p.StaySource, p.BoardDefaultView, p.StatesDefaultView, p.OnDepartureCondition, p.LinenRuleKind,
                p.LinenEveryDays, p.Towels, p.TurndownEnabled, p.RefreshAfterDays, p.DndRecheckMinutes, p.SupervisorAfterDays, p.PriorityLadder,
                p.AssignmentStrategy, p.UnsoldDeparture, p.Version, p.Version == 0 ? null : p.ChangedAt.ToString("o")),
            windows.Select(w => new WindowView(w.Window, w.Enabled, w.Starts.ToString("HH:mm"), w.Ends.ToString("HH:mm"), w.AllowAssignmentOutside, w.Version)).ToList(),
            by);
    }

    public async Task<ServicesView> ServicesAsync(RequestScope scope, Guid? roomTypeId, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        var types = await house.RoomTypesAsync(scope.PropertyId, cancellationToken);
        var rooms = await house.RoomsAsync(scope.PropertyId, cancellationToken);
        var chosen = roomTypeId ?? types.FirstOrDefault()?.Id;
        var rows = new List<ServiceRowView>();
        foreach (var service in new[] { Service.DepartureClean, Service.DailyService, Service.Turndown, Service.Refresh })
        {
            var s = await standard.StandardAsync(scope.PropertyId, chosen, service, cancellationToken);
            rows.Add(new ServiceRowView(service, s.Minutes, s.Credits, s.Phases, s.InspectionRule, s.ChecklistRef, s.Version, s.Id != Guid.Empty));
        }

        return new ServicesView(
            types.Select(t => new RoomTypeView(t.Id.ToString(), t.Code, t.Name, rooms.Count(r => r.RoomTypeId == t.Id))).ToList(),
            chosen?.ToString(), rows, InspectionApplicationInstalled: false);
    }

    public async Task<ZonesView> ZonesAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        var policy = await standard.PolicyAsync(scope.PropertyId, cancellationToken);
        var zones = await house.ZonesAsync(scope.PropertyId, cancellationToken);
        var rooms = await house.RoomsAsync(scope.PropertyId, cancellationToken);
        var members = await db.ZoneAssignments.Where(z => z.PropertyId == scope.PropertyId && z.EffectiveUntil == null).ToListAsync(cancellationToken);
        var rows = zones.Select(z =>
        {
            var inZone = rooms.Where(r => members.Any(m => m.RoomId == r.Id && m.ZoneId == z.Id)).ToList();
            var since = members.Where(m => m.ZoneId == z.Id).Select(m => (DateOnly?)m.EffectiveFrom).Max();
            return new ZoneRowView(z.Id.ToString(), z.Code, z.Name, inZone.Count, inZone.FirstOrDefault()?.Number, inZone.LastOrDefault()?.Number, since?.ToString("yyyy-MM-dd"));
        }).ToList();
        var zoned = members.Select(m => m.RoomId).ToHashSet();
        return new ZonesView(policy.AssignmentStrategy, rows, rooms.Count, rooms.Count(r => !zoned.Contains(r.Id)),
            rooms.Select(r => new ZoneRoomView(r.Id.ToString(), r.Number, members.FirstOrDefault(m => m.RoomId == r.Id)?.ZoneId.ToString())).ToList());
    }

    public async Task<AreasView> AreasAsync(RequestScope scope, int page, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        var areas = await house.AreasAsync(scope.PropertyId, cancellationToken);
        var schedules = await db.AreaSchedules.Where(a => a.PropertyId == scope.PropertyId).ToDictionaryAsync(a => a.LocationId, cancellationToken);
        var rows = areas.Select(a => schedules.TryGetValue(a.Id, out var s)
                ? new AreaRowView(a.Id.ToString(), a.Name, a.LocationType, s.Times.Select(t => t.ToString("HH:mm")).ToList(), s.Minutes, s.Enabled, s.Version)
                : new AreaRowView(a.Id.ToString(), a.Name, a.LocationType, [], null, false, 0))
            .ToList();
        return new AreasView(areas.Count, rows.Count(r => r.Enabled && r.Times.Count > 0),
            rows.Skip(Math.Max(0, page) * AreaPageSize).Take(AreaPageSize).ToList(), new Views.Paging(Math.Max(0, page), AreaPageSize, rows.Count));
    }

    public async Task<DeepCleanPlanView> PlanAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        var types = await house.RoomTypesAsync(scope.PropertyId, cancellationToken);
        var rooms = await house.RoomsAsync(scope.PropertyId, cancellationToken);
        var plans = await db.DeepCleanPlans.Where(p => p.PropertyId == scope.PropertyId).ToDictionaryAsync(p => p.RoomTypeId, cancellationToken);
        var done = await db.DeepCleans.Where(d => d.PropertyId == scope.PropertyId && d.DoneOn != null)
            .GroupBy(d => d.RoomId).Select(g => new { Room = g.Key, Last = g.Max(d => d.DoneOn!.Value) }).ToDictionaryAsync(x => x.Room, x => x.Last, cancellationToken);
        var quarterEnd = (await clock.AtAsync(scope.PropertyId, cancellationToken)).Day.AddMonths(3);
        return new DeepCleanPlanView(types.Select(t =>
        {
            plans.TryGetValue(t.Id, out var plan);
            var ofType = rooms.Where(r => r.RoomTypeId == t.Id).ToList();
            var due = plan is null ? 0 : ofType.Count(r => !done.TryGetValue(r.Id, out var last) || last.AddMonths(plan.EveryMonths) <= quarterEnd);
            return new PlanRowView(t.Id.ToString(), t.Name, plan?.EveryMonths, ofType.Count, due, plan?.Version ?? 0);
        }).ToList());
    }

    public async Task<GrantsView> GrantsAsync(RequestScope scope, ManagerGrants grants, CancellationToken cancellationToken)
    {
        var live = await grants.LiveAsync(scope, cancellationToken);
        var names = await house.NamesAsync(live.SelectMany(g => new[] { g.UserId, g.GrantedBy }).Distinct().ToList(), cancellationToken);
        return new GrantsView(live.Select(g => new GrantRowView(g.UserId.ToString(), names.GetValueOrDefault(g.UserId) ?? g.UserId.ToString(),
            g.GrantedAt.ToString("o"), names.GetValueOrDefault(g.GrantedBy))).ToList());
    }
}
