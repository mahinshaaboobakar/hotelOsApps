using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;

namespace HotelOS.RoomCare.Application.Day;

/// <summary>When a property's service windows open and close for a business day — the one place window instants are made.</summary>
/// <remarks>
/// A window belongs to the business day whose local calendar date it starts on;
/// one that ends at or before it starts ends on the next calendar date (the
/// 22:00–02:00 night shape the reference could never match, survey F2).
/// </remarks>
public static class WindowTimes
{
    /// <summary>The window's opening and closing instants on a business day.</summary>
    public static (DateTimeOffset Opens, DateTimeOffset Closes) Of(ServiceWindow window, DateOnly day, PropertyNow now)
    {
        var opens = now.InstantOf(day, window.Starts);
        var closes = window.Starts < window.Ends
            ? now.InstantOf(day, window.Ends)
            : now.InstantOf(day.AddDays(1), window.Ends);
        return (opens, closes);
    }

    /// <summary>The window open right now, if any, with the business day it belongs to.</summary>
    public static (ServiceWindow Window, DateOnly Day)? Open(IReadOnlyList<ServiceWindow> windows, PropertyNow now)
    {
        foreach (var window in windows.Where(w => w.Enabled))
        {
            foreach (var day in new[] { now.Day, now.Day.AddDays(-1), now.Day.AddDays(1) })
            {
                var (opens, closes) = Of(window, day, now);
                if (now.Instant >= opens && now.Instant < closes)
                {
                    return (window, day);
                }
            }
        }

        return null;
    }

    /// <summary>The window a press prepares: the one open now, else the next to open.</summary>
    public static (ServiceWindow Window, DateOnly Day) Target(IReadOnlyList<ServiceWindow> windows, PropertyNow now, string? asked)
    {
        var enabled = windows.Where(w => w.Enabled && (asked is null || w.Window == asked)).ToList();
        if (enabled.Count == 0)
        {
            throw new HotelOS.Platform.InvalidRequestException(asked is null
                ? "this property has no service window enabled; set one in Setup"
                : $"the {asked.ToLowerInvariant()} window is not enabled at this property");
        }

        if (Open(enabled, now) is { } open)
        {
            return open;
        }

        return enabled
            .SelectMany(w => new[] { now.Day, now.Day.AddDays(1) }.Select(d => (Window: w, Day: d, Opens: Of(w, d, now).Opens)))
            .Where(x => x.Opens > now.Instant)
            .OrderBy(x => x.Opens)
            .Select(x => (x.Window, x.Day))
            .First();
    }

    /// <summary>The instant a business day ends — the next boundary — which "sold tonight" is measured against.</summary>
    public static DateTimeOffset DayEnds(DateOnly day, PropertyNow now) =>
        now.InstantOf(day.AddDays(1), now.Settings.Boundary);
}
