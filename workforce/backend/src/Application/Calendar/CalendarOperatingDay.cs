using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;

namespace HotelOS.Workforce.Application.Calendar;

/// <summary>
/// The day as this application worked it out before ADR 0211 — the calendar day
/// in the property's own zone.
/// </summary>
/// <remarks>
/// <para>
/// <b>Kept, and not the default, until the switch is given.</b> ADR 0211 makes
/// the operating day Context's. Until an installed application's Context call
/// is proved live, both paths exist and both are tested, and the registration
/// in <c>Program.cs</c> says which one is in use. This one is here so that
/// switching is a decision somebody makes rather than an outage they discover.
/// </para>
/// <para>
/// <b>It is a calendar day, and that is the whole difference.</b> A property
/// whose day rolls at 04:00 is on yesterday's operating day at 02:00, and this
/// answers today. That is why it is not the answer ADR 0211 wanted — but it is
/// the property's own zone, never UTC, so it is right about the zone and only
/// wrong about the boundary.
/// </para>
/// </remarks>
/// <param name="directory">Master Data, for the property's zone.</param>
/// <param name="clock">The platform clock.</param>
public sealed class CalendarOperatingDay(IStaffDirectory directory, TimeProvider clock)
    : IOperatingDay
{
    /// <inheritdoc />
    /// <remarks>
    /// Null when Master Data has no zone for this property: an unreadable zone
    /// is an unreadable day here too, never UTC. <see cref="PropertyCalendar"/>
    /// refuses by name, and that refusal becomes this null.
    /// </remarks>
    public async Task<DateOnly?> TodayAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var calendar = await PropertyCalendar.ForAsync(
                directory, scope.PropertyId, cancellationToken);

            return calendar.DayOf(clock.GetUtcNow());
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
