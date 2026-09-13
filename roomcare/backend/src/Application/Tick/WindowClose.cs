using HotelOS.Platform;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Application.Supervision;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Tick;

/// <summary>When a window closes, every room still open in it gets its outcome — never just "not done" (S5 c1; §6.1).</summary>
/// <remarks>
/// A last attempt of DND stands as DND; a room nobody reached is NOT_REACHED;
/// either is recorded as closed by policy, never as done. A room waiting on a
/// supervisor's decision stays open — the window does not close on it (S5 c9).
/// Pending-policy tasks close too: the day's decision that held them is the
/// record of why.
/// </remarks>
public sealed class WindowClose(RoomCareDbContext db, TaskWriter writer, SupervisionLane lane)
{
    public async Task<int> RunAsync(
        RequestScope scope, IReadOnlyList<ServiceWindow> windows, PropertyNow now, CancellationToken cancellationToken)
    {
        var open = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId && RoomTaskStatus.Open.Contains(t.Status) && t.OperatingDay <= now.Day)
            .ToListAsync(cancellationToken);

        var closed = 0;
        foreach (var task in open)
        {
            var window = windows.FirstOrDefault(w => w.Window == task.Window);
            if (window is null || now.Instant < WindowTimes.Of(window, task.OperatingDay, now).Closes)
            {
                continue;
            }

            if (task.RoomId is { } room && await lane.AwaitingAsync(task.PropertyId, room, task.OperatingDay, cancellationToken))
            {
                continue;
            }

            var lastFound = await db.Attempts
                .Where(a => a.TaskId == task.Id)
                .OrderByDescending(a => a.At)
                .Select(a => a.Found)
                .FirstOrDefaultAsync(cancellationToken);
            task.Outcome = lastFound == AttemptFound.Dnd ? TaskOutcome.Dnd : TaskOutcome.NotReached;
            if (await writer.RunningAsync(task.Id, cancellationToken) is { } running)
            {
                running.Stop(writer.Now, SessionEnd.End);
            }

            writer.Move(scope, task, RoomTaskStatus.ClosedByPolicy, EventTypes.TaskEnded, new TaskNote
            {
                Reason = $"the {task.Window.ToLowerInvariant()} window closed",
            });
            closed++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return closed;
    }
}
