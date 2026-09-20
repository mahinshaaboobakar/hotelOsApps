using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// What day it is at the property, for a view that was asked for no day.
/// </summary>
/// <remarks>
/// Every read here takes an explicit day and falls back to "today" — the rota's
/// week, the month a report covers, the day a team is read on. **One place
/// answers it**, so no view works a day out for itself: six of them used to,
/// each from <c>clock.GetUtcNow().UtcDateTime</c>, which is the UTC calendar day
/// and is wrong at every property in the world for part of every day.
/// </remarks>
internal static class PropertyDay
{
    /// <summary>The property's operating day, or a failure the screen shows.</summary>
    /// <param name="call">The request.</param>
    /// <param name="cancellationToken">The request's token.</param>
    /// <returns>The day at the property.</returns>
    /// <exception cref="UnavailableException">The day could not be read.</exception>
    public static async Task<DateOnly> TodayAsync(
        ModuleCall call, CancellationToken cancellationToken)
        => OperatingDay.OrUnavailable(await MaybeTodayAsync(call, cancellationToken));

    /// <summary>The same day, where a view can still answer without it.</summary>
    /// <param name="call">The request.</param>
    /// <param name="cancellationToken">The request's token.</param>
    /// <returns>The day at the property, or null where Context could not say.</returns>
    /// <remarks>
    /// <b>Two forms, because a missing day means two different things.</b> A
    /// view whose whole subject is today — attendance, the shift board — cannot
    /// answer without one, and fails with a sentence naming what could not be
    /// read. A view asked for an explicit period can still answer: the Staff
    /// Schedule draws the month it was given and simply marks no cell as today.
    /// <para>
    /// Marking one anyway is the defect this exists to prevent: a day nobody
    /// established, drawn as a fact, on a grid a supervisor plans from.
    /// </para>
    /// </remarks>
    public static Task<DateOnly?> MaybeTodayAsync(
        ModuleCall call, CancellationToken cancellationToken)
        => call.Service<IOperatingDay>().TodayAsync(call.Scope, cancellationToken);
}
