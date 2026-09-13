using HotelOS.Platform;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Application.Supervision;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Tick;

/// <summary>One property's pass of the tick — the day roll, window closes, automatic mode and arrivals before the window (§6.1).</summary>
/// <remarks>
/// <b>No per-room timer exists.</b> Every step compares the clock with
/// timestamps already on the rows; the DND re-check is read, not scheduled. In
/// PREPARE mode the tick decides nothing: it closes windows and counts, and the
/// person presses the button.
/// </remarks>
public sealed class TickPass(
    RoomCareDbContext db,
    PropertyClock clock,
    StandardReader standard,
    PrepareService prepare,
    WindowClose windowClose,
    DayRoll roll,
    SupervisionLane lane)
{
    public async Task RunAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var now = await clock.AtAsync(scope.PropertyId, cancellationToken);
        var windows = await standard.WindowsAsync(scope.PropertyId, cancellationToken);
        var policy = await standard.PolicyAsync(scope.PropertyId, cancellationToken);

        await roll.RunAsync(scope, now, cancellationToken);
        await windowClose.RunAsync(scope, windows, now, cancellationToken);

        var open = WindowTimes.Open(windows, now);
        if (open is { } current && policy.TriggerMode == TriggerMode.Automatic)
        {
            await AutomaticAsync(scope, current.Window, current.Day, cancellationToken);
        }

        if (open is null)
        {
            await ArrivalsBeforeWindowAsync(scope, windows, now, cancellationToken);
        }
    }

    /// <summary>The first tick in a window builds the day; later ticks reconcile only when something changed.</summary>
    private async Task AutomaticAsync(RequestScope scope, ServiceWindow window, DateOnly day, CancellationToken cancellationToken)
    {
        var last = await db.PrepareRuns
            .Where(r => r.PropertyId == scope.PropertyId && r.OperatingDay == day && r.Window == window.Window)
            .OrderByDescending(r => r.At)
            .Select(r => (DateTimeOffset?)r.At)
            .FirstOrDefaultAsync(cancellationToken);

        if (last is null || await prepare.ChangesSinceAsync(scope.PropertyId, last.Value, cancellationToken) > 0)
        {
            await prepare.PrepareAsync(scope, window.Window, cancellationToken);
        }
    }

    /// <summary>A room sold before the next window opens and not yet ready — the supervisor is told (S0 midnight case).</summary>
    private async Task ArrivalsBeforeWindowAsync(
        RequestScope scope, IReadOnlyList<ServiceWindow> windows, PropertyNow now, CancellationToken cancellationToken)
    {
        var (next, day) = WindowTimes.Target(windows, now, null);
        var opens = WindowTimes.Of(next, day, now).Opens;
        var early = await db.RoomStates
            .Where(r => r.PropertyId == scope.PropertyId && r.Condition == Condition.Dirty && !r.IsPseudoRoom)
            .Where(r => r.NextSoldAt != null && r.NextSoldAt < opens && r.NextSoldAt > now.Instant)
            .Select(r => r.RoomId)
            .ToListAsync(cancellationToken);

        foreach (var room in early)
        {
            await lane.OpenAsync(scope, room, day, SupervisionReason.ArrivalBeforeWindow, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
