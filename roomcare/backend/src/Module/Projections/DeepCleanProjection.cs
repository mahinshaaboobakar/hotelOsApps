using HotelOS.Contracts.Common.V1;
using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using HotelOS.RoomCare.Module.Views;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Module.HouseSnapshot;

namespace HotelOS.RoomCare.Module.Projections;

/// <summary>The Deep clean tab — every open project, then every room due by the plan, soonest first (frame 6).</summary>
/// <remarks>
/// "Due" comes from the room type's plan and the room's last recorded deep
/// clean. A room with none recorded is due now and says "never recorded" — the
/// honest reading, never an invented date.
/// </remarks>
public sealed class DeepCleanProjection(RoomCareDbContext db, IHouse house, PropertyClock clock)
{
    public const int PageSize = 12;

    public async Task<DeepCleanPageView> PageAsync(RequestScope scope, int page, CancellationToken cancellationToken)
    {
        var snapshot = await LoadAsync(scope.PropertyId, house, clock, db, cancellationToken);
        var today = snapshot.Now.Day;
        var plans = await db.DeepCleanPlans.Where(p => p.PropertyId == scope.PropertyId).ToDictionaryAsync(p => p.RoomTypeId, cancellationToken);
        var projects = await db.DeepCleans.Where(d => d.PropertyId == scope.PropertyId).ToListAsync(cancellationToken);
        var lastDone = projects.Where(p => p.DoneOn is not null).GroupBy(p => p.RoomId).ToDictionary(g => g.Key, g => g.Max(p => p.DoneOn!.Value));

        var rows = new List<(DateOnly Due, DeepCleanRowView Row)>();
        foreach (var project in projects.Where(p => DeepCleanStatus.Open.Contains(p.Status) && p.WindowFrom is not null))
        {
            rows.Add((project.DueOn, new DeepCleanRowView(
                project.Id.ToString(), project.Version, project.RoomId.ToString(), snapshot.Number(project.RoomId), snapshot.TypeName(project.RoomId),
                lastDone.TryGetValue(project.RoomId, out var done) ? done.ToString("yyyy-MM-dd") : null,
                project.DueOn.ToString("yyyy-MM-dd"), project.WindowFrom?.ToString("yyyy-MM-dd"), project.WindowTo?.ToString("yyyy-MM-dd"),
                At(project.BlockRequestedAt), At(project.BlockAppliedAt), project.JobId?.ToString(), project.JobStatusSeen, project.Status)));
        }

        var planned = projects.Where(p => DeepCleanStatus.Open.Contains(p.Status) && p.WindowFrom is not null).Select(p => p.RoomId).ToHashSet();
        var horizon = today.AddMonths(1);
        foreach (var room in snapshot.Rooms.Where(r => plans.ContainsKey(r.RoomTypeId) && !planned.Contains(r.Id)))
        {
            var last = lastDone.TryGetValue(room.Id, out var d) ? d : (DateOnly?)null;
            var due = last?.AddMonths(plans[room.RoomTypeId].EveryMonths) ?? today;
            if (due > horizon)
            {
                continue;
            }

            rows.Add((due, new DeepCleanRowView(null, null, room.Id.ToString(), room.Number, snapshot.TypeName(room.Id),
                last?.ToString("yyyy-MM-dd"), due.ToString("yyyy-MM-dd"), null, null, null, null, null, null, "DUE")));
        }

        var ordered = rows.OrderBy(r => r.Row.State == "DUE" ? 1 : 0).ThenBy(r => r.Due).Select(r => r.Row).ToList();
        var monthEnd = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        var slice = HotelOS.Platform.Paging.Of(new PagedRequest { Page = page, PageSize = PageSize });
        return new DeepCleanPageView(
            rows.Count(r => r.Row.State == "DUE" && r.Due < monthEnd),
            ordered.Count(r => r.State is DeepCleanStatus.BlockRequested or DeepCleanStatus.Blocked or DeepCleanStatus.Planned),
            ordered.Count(r => r.State is DeepCleanStatus.InProgress or DeepCleanStatus.Returning),
            plans.Values.Select(p => new PlanLineView(snapshot.Types.TryGetValue(p.RoomTypeId, out var t) ? t.Name : "—", p.EveryMonths)).ToList(),
            ordered.Skip(slice.Skip).Take(slice.PageSize).ToList(),
            new Views.Paging(slice.Page, slice.PageSize, ordered.Count));
    }
}
