using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Application.Supervision;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Tick;

/// <summary>What the tick does once a business day has fully closed, and at the start of the next (§6.1 DAILY).</summary>
/// <remarks>
/// <para>
/// <b>Days without service</b> (S5 c9): a room whose every task on a closed day
/// ended declined, DND or skipped by the guest counts one more day; any service
/// resets it. Reaching the property's threshold sets the day the supervisor must
/// go, announces <c>roomcare.service_missed</c> and opens the lane — once; the
/// room stays the supervisor's while the stay continues.
/// </para>
/// <para>
/// <b>Areas</b> (S3): each enabled area's times for today become tasks, once.
/// </para>
/// </remarks>
public sealed class DayRoll(
    RoomCareDbContext db, IHouse house, TaskMaker factory, SupervisionLane lane, StandardReader standard, IEventAppender events)
{
    public async Task RunAsync(RequestScope scope, PropertyNow now, CancellationToken cancellationToken)
    {
        await CountDaysWithoutServiceAsync(scope, now, cancellationToken);
        await AreasAsync(scope, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CountDaysWithoutServiceAsync(RequestScope scope, PropertyNow now, CancellationToken cancellationToken)
    {
        var policy = await standard.PolicyAsync(scope.PropertyId, cancellationToken);
        var day = now.Day.AddDays(-1);
        var ended = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId && t.OperatingDay == day && t.RoomId != null)
            .GroupBy(t => t.RoomId!.Value)
            .Select(g => new
            {
                Room = g.Key,
                StillOpen = g.Any(t => RoomTaskStatus.Open.Contains(t.Status)),
                WithoutService = g.All(t => t.Outcome != null && TaskOutcome.WithoutService.Contains(t.Outcome)),
            })
            .ToListAsync(cancellationToken);

        foreach (var result in ended.Where(r => !r.StillOpen))
        {
            var room = await db.RoomStates.FirstOrDefaultAsync(r => r.RoomId == result.Room, cancellationToken);
            if (room is null || room.DaysCountedThrough >= day)
            {
                continue;
            }

            room.DaysCountedThrough = day;
            room.DaysWithoutService = result.WithoutService ? room.DaysWithoutService + 1 : 0;
            if (room.DaysWithoutService < policy.SupervisorAfterDays || room.SupervisedSince is not null)
            {
                continue;
            }

            room.SupervisedSince = now.Day;
            room.Touch(now.Instant);
            events.Append(scope, EventTypes.ServiceMissed, EventTypes.RoomAggregate, room.RoomId, room.Version, new SupervisionAnnouncement
            {
                SupervisionId = Guid.Empty,
                RoomId = room.RoomId,
                PropertyId = room.PropertyId,
                OperatingDay = now.Day.ToString("yyyy-MM-dd"),
                Reason = SupervisionReason.DaysWithoutService,
                Days = room.DaysWithoutService,
                OccurredAt = now.Instant,
            });
            await lane.OpenAsync(scope, room.RoomId, now.Day, SupervisionReason.DaysWithoutService, cancellationToken, room.DaysWithoutService);
        }
    }

    private async Task AreasAsync(RequestScope scope, PropertyNow now, CancellationToken cancellationToken)
    {
        var schedules = await db.AreaSchedules.Where(a => a.PropertyId == scope.PropertyId && a.Enabled).ToListAsync(cancellationToken);
        if (schedules.Count == 0)
        {
            return;
        }

        var policy = await standard.PolicyAsync(scope.PropertyId, cancellationToken);
        var department = await house.DepartmentIdAsync(scope.PropertyId, policy.DepartmentCode, cancellationToken);
        var windows = await standard.WindowsAsync(scope.PropertyId, cancellationToken);
        var made = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId && t.OperatingDay == now.Day && t.Service == Service.AreaClean)
            .Select(t => new { t.LocationId, t.DueAt })
            .ToListAsync(cancellationToken);

        foreach (var schedule in schedules)
        {
            foreach (var time in schedule.Times)
            {
                var due = now.InstantOf(now.Day, time);
                if (made.Any(m => m.LocationId == schedule.LocationId && m.DueAt == due))
                {
                    continue;
                }

                var window = windows.FirstOrDefault(w => w.Contains(time))?.Window ?? ServiceWindowName.Morning;
                factory.CreateArea(scope, schedule, now.Day, window, due, department);
            }
        }
    }
}
