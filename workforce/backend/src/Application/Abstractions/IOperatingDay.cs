using HotelOS.Platform;

namespace HotelOS.Workforce.Application.Abstractions;

/// <summary>
/// What day it is at the property — asked, never worked out here.
/// </summary>
/// <remarks>
/// <para>
/// <b>ADR 0211 (<c>WF-Q21</c>): the operating day is the Context Service's.</b>
/// Attendance, leave, postings, shift boards and every day count take the
/// property's operating day from Context — not the UTC calendar day, and not a
/// calendar day this application derives for itself. A day is a hotel's own
/// fact: a property whose day rolls at 04:00 has one answer, and nothing here
/// can compute it from a clock and a zone.
/// </para>
/// <para>
/// <b>Null means the day could not be read.</b> It never means today, never
/// means the UTC day, and never means a calendar day standing in for one: a
/// guessed day is wrong by up to a day, looks exactly like a right one, and
/// every count drawn from it looks like a fact. Callers answer with a failure
/// the screen can show, which is <see cref="UnavailableException"/>'s job.
/// </para>
/// <para>
/// <b>Two implementations, deliberately</b> — <c>ContextOperatingDay</c>, which
/// is ADR 0211's answer, and <c>CalendarOperatingDay</c>, which is what this
/// application did before it. Both are kept and both are tested until the
/// architect gives the word to switch, which waits on the live proof that an
/// installed application's Context call is admitted at all. The word changes
/// one registration in <c>Program.cs</c> and nothing else.
/// </para>
/// </remarks>
public interface IOperatingDay
{
    /// <summary>The property's operating day, or null when it cannot be read.</summary>
    /// <param name="scope">The request's scope — the property is bound to the
    /// authenticated caller (<c>AUTHZ-Q18b</c>), so no property id is sent.</param>
    /// <param name="cancellationToken">The request's token.</param>
    /// <returns>The day at the property, or null.</returns>
    Task<DateOnly?> TodayAsync(RequestScope scope, CancellationToken cancellationToken);
}

/// <summary>What a caller does when the day could not be read.</summary>
/// <remarks>
/// One place, so no screen invents its own sentence and no caller quietly
/// substitutes a day. The message names what could not be read and says
/// nothing about the person.
/// </remarks>
public static class OperatingDay
{
    /// <summary>The day, or a failure the screen can show.</summary>
    /// <param name="day">What <see cref="IOperatingDay.TodayAsync"/> answered.</param>
    /// <returns>The day.</returns>
    /// <exception cref="UnavailableException">The day could not be read.</exception>
    public static DateOnly OrUnavailable(DateOnly? day)
        => day ?? throw new UnavailableException(
            "the property's operating day could not be read");
}
