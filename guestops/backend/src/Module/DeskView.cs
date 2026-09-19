using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Today at the Desk — the widget's four counts and the next arrivals.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the widget has a read of its own.</b> It read <c>today</c>, the
/// screen's read, and expected <c>dueIn · arrived · dueOut · departed ·
/// arrivals</c>. <c>TodayView</c> has never sent any of those — it sends the
/// screen's lists — so on a property every count was absent and the list
/// empty. Only the harness's fixture had the widget's shape (found 2026-09-19,
/// capability ledger). A card and a screen asking different questions get
/// different answers, each shaped for its reader.
/// </para>
/// <para>
/// <b>The four counts are disjoint, and that is an implementation choice, not a
/// ruling.</b> A guest who has arrived is no longer due in: <c>dueIn</c> counts
/// today's arrivals still booked, <c>arrived</c> those in house or already
/// gone; <c>dueOut</c> counts today's departures still in house,
/// <c>departed</c> those gone.
/// </para>
/// <para>
/// <b>Unknown is null, never zero.</b> Without a business day nothing here can
/// be counted; without the day's bounds, departures cannot — and the list
/// service answers an empty list in that case, which read as a count would say
/// <i>nobody is leaving</i>.
/// </para>
/// </remarks>
public sealed class DeskView(StayListService stays, StayLabels labels, IBusinessDay businessDay)
{
    /// <summary>How many arrivals the card draws — the canvas's five.</summary>
    private const int Shown = 5;

    /// <summary>The counts and the next arrivals.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The widget's answer.</returns>
    public async Task<object?> AnswerAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var date = await businessDay.CurrentAsync(scope, cancellationToken);
        if (date is not { } day)
        {
            return new { dueIn = (int?)null, arrived = (int?)null, dueOut = (int?)null, departed = (int?)null, arrivals = Array.Empty<object>() };
        }

        var arriving = await stays.CountByLifecycleAsync(scope, StayView.Arrivals, day, cancellationToken);

        var bounds = await businessDay.BoundsAsync(scope, day, cancellationToken);
        var leaving = bounds is null
            ? null
            : await stays.CountByLifecycleAsync(scope, StayView.Departures, day, cancellationToken);

        var next = await stays.FirstAsync(
            scope, StayView.Arrivals, day, StayLifecycle.Booked, Shown, cancellationToken);
        var names = await labels.NamesAsync(next, cancellationToken);
        var rooms = await labels.RoomsAsync(scope, next, cancellationToken);

        return new
        {
            dueIn = Count(arriving, StayLifecycle.Booked),
            arrived = Count(arriving, StayLifecycle.InHouse) + Count(arriving, StayLifecycle.Departed),
            dueOut = leaving is null ? (int?)null : Count(leaving, StayLifecycle.InHouse),
            departed = leaving is null ? (int?)null : Count(leaving, StayLifecycle.Departed),

            arrivals = next.Select(stay => new
            {
                guest = names.TryGetValue(stay.Id, out var name) ? name : StayLabels.Unnamed,
                room = stay.CurrentRoomId is { } id && rooms.TryGetValue(id, out var number) ? number : null,

                // An instant, drawn by the widget in the property's form; null
                // when no arrival time was ever recorded.
                at = stay.ArrivalAt.At?.ToString("O"),
                stay = stay.Id.ToString(),
            }).ToArray(),
        };
    }

    private static int Count(IReadOnlyDictionary<StayLifecycle, int> counts, StayLifecycle lifecycle)
        => counts.TryGetValue(lifecycle, out var count) ? count : 0;
}
