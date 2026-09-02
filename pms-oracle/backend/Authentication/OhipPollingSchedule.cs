using PmsOracle.Normalisation;

namespace PmsOracle.Authentication;

/// <summary>
/// How often to ask OHIP, which is not one number — frame 3's two tiers.
/// </summary>
/// <remarks>
/// <para>
/// <b>A normal interval and a tighter one around check-in.</b> The queue is
/// emptied by reading, so a long interval makes a backlog rather than saving
/// work — but that argues for polling harder <i>when there is traffic</i>, not
/// permanently. Arrivals cluster around the property's check-in time, and the
/// rest of the day is mostly empty reads against an API that rate-limits.
/// </para>
/// <para>
/// <b>The window is in the property's own zone.</b> A window expressed in UTC
/// would drift away from check-in twice a year in any property that observes
/// daylight saving, and would be wrong by hours in one that does not share the
/// server's offset. <see cref="PropertyClock"/> carries the IANA zone, which is
/// requirement R16's whole point: an offset cannot express the rule, only one
/// of its answers.
/// </para>
/// <para>
/// <b>Defaults, and why they are here rather than in the form.</b> A form's
/// hint is a placeholder — it shows what to type and governs nothing. These
/// govern: they are what an unconfigured property actually polls at. This round
/// corrected a 30-second hint and left a 30-second constant behind, so the
/// numbers that act now live in one place with the reasoning beside them.
/// </para>
/// </remarks>
public sealed record OhipPollingSchedule
{
    /// <summary>How often to ask outside the tighter window.</summary>
    public const string NormalSecondsSetting = "pollNormalSeconds";

    /// <summary>How often to ask inside it.</summary>
    public const string TightSecondsSetting = "pollTightSeconds";

    /// <summary>When the tighter window opens, <c>HH:MM</c> in the property's zone.</summary>
    public const string TightFromSetting = "pollTightFrom";

    /// <summary>When it closes, <c>HH:MM</c> in the property's zone.</summary>
    public const string TightUntilSetting = "pollTightUntil";

    /// <summary>Three hours, as frame 3 draws it.</summary>
    public static readonly TimeSpan DefaultNormal = TimeSpan.FromHours(3);

    /// <summary>Fifteen minutes, as frame 3 draws it.</summary>
    public static readonly TimeSpan DefaultTight = TimeSpan.FromMinutes(15);

    private OhipPollingSchedule(
        TimeSpan normal, TimeSpan tight, TimeOnly? from, TimeOnly? until)
    {
        Normal = normal;
        Tight = tight;
        From = from;
        Until = until;
    }

    /// <summary>The ordinary interval.</summary>
    public TimeSpan Normal { get; }

    /// <summary>The interval inside the window.</summary>
    public TimeSpan Tight { get; }

    /// <summary>When the window opens, or <c>null</c> when there is none.</summary>
    public TimeOnly? From { get; }

    /// <summary>When it closes, or <c>null</c> when there is none.</summary>
    public TimeOnly? Until { get; }

    /// <summary>Read a schedule from what the property configured.</summary>
    /// <param name="settings">The stored settings, in this connector's vocabulary.</param>
    /// <returns>The schedule, with defaults for whatever is absent.</returns>
    /// <remarks>
    /// <b>An unreadable value falls back rather than throwing.</b> A schedule is
    /// not a credential: a typo in an interval should poll at the default and
    /// be visible on the next screen, not stop a hotel's reservations arriving.
    /// The credential set takes the opposite view for the opposite reason.
    /// </remarks>
    public static OhipPollingSchedule Read(IReadOnlyDictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new OhipPollingSchedule(
            Seconds(settings, NormalSecondsSetting, DefaultNormal),
            Seconds(settings, TightSecondsSetting, DefaultTight),
            Clock(settings, TightFromSetting),
            Clock(settings, TightUntilSetting));
    }

    /// <summary>How long to wait, from a moment.</summary>
    /// <param name="clock">The property's zone.</param>
    /// <param name="now">The moment being scheduled from.</param>
    /// <returns>The tighter interval inside the window, the normal one outside.</returns>
    public TimeSpan Wait(PropertyClock clock, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var local = TimeZoneInfo.ConvertTime(now, clock.Zone);

        return Covers(TimeOnly.FromDateTime(local.DateTime)) ? Tight : Normal;
    }

    /// <summary>Whether a local time of day is inside the tighter window.</summary>
    /// <param name="local">The time of day, in the property's zone.</param>
    /// <returns><c>true</c> when the tighter interval applies.</returns>
    /// <remarks>
    /// <b>A window that wraps midnight is honoured rather than ignored.</b>
    /// 22:00–02:00 is a real arrival pattern for a property near an airport,
    /// and a naive <c>from &lt;= t &amp;&amp; t &lt; until</c> would silently
    /// treat it as empty — the connector polling slowly through exactly the
    /// hours it was told to watch.
    /// </remarks>
    public bool Covers(TimeOnly local)
    {
        if (From is not { } from || Until is not { } until || from == until)
        {
            return false;
        }

        return from < until
            ? local >= from && local < until
            : local >= from || local < until;
    }

    private static TimeSpan Seconds(
        IReadOnlyDictionary<string, string> settings, string name, TimeSpan fallback) =>
        settings.TryGetValue(name, out var raw)
        && int.TryParse(raw, out var seconds)
        && seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : fallback;

    private static TimeOnly? Clock(
        IReadOnlyDictionary<string, string> settings, string name) =>
        settings.TryGetValue(name, out var raw) && TimeOnly.TryParse(raw, out var at)
            ? at
            : null;
}
