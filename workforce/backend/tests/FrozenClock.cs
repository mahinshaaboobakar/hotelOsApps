namespace HotelOS.Workforce.Tests;

/// <summary>A clock that says what a test tells it to.</summary>
/// <remarks>
/// <para>
/// The summaries answer <i>now</i> — who is on shift, how long a request has
/// been waiting, what the next seven days hold — and a suite that used the real
/// clock would be asserting against the moment it happened to run. Two of these
/// tests are about six o'clock in the morning specifically, and there is no
/// arranging for that.
/// </para>
/// <para>
/// Deliberately minimal: <see cref="TimeProvider"/> is abstract and one method
/// is the whole of what this application asks a clock. A testing package would
/// bring timers, time zones and a scheduler for a question none of these
/// services asks.
/// </para>
/// </remarks>
/// <param name="now">The moment this clock reports.</param>
public sealed class FrozenClock(DateTimeOffset now) : TimeProvider
{
    /// <summary>Move the clock. Nothing recomputes; the next read sees it.</summary>
    public DateTimeOffset Now { get; set; } = now;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => Now;
}
