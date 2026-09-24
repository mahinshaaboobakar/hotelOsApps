namespace HotelOS.GuestOps.Domain;

/// <summary>
/// When a stay may be offered as one that never arrived.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pure, and separate from the service for the reason every <c>*Rule</c>
/// here is.</b> It is decided over values, so both surfaces that offer the
/// action — the stay page and the day's own arrivals list — reach one answer
/// rather than two derivations that drift.
/// </para>
/// <para>
/// <b>This is the DAY half only. The lifecycle half is the service's.</b>
/// <c>RecordNoShowAsync</c> refuses anything that is not still waiting to
/// arrive, in its own words, and a copy of that rule here would be a second
/// opinion at a surface — so this asks only the question the service cannot:
/// <i>has the arrival day passed?</i>
/// </para>
/// <para>
/// <b>A stay arriving today is not a no-show at four in the afternoon.</b>
/// That is the owner's approved caption (N2, 2026-09-24) and it is the whole
/// rule: the guest has until the day is over. An action offered on today's row
/// is one somebody presses by mistake, and the mistake writes a forfeited
/// night onto a guest who is about to walk in.
/// </para>
/// </remarks>
public static class NoShowRule
{
    /// <summary>
    /// Whether this arrival day is far enough past to offer the action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Both absences refuse, and they are not the same absence.</b> An
    /// arrival with no established date is a stay nobody can say is late; an
    /// unestablished business day is a property whose operating day the
    /// Context Service could not derive. Neither is an argument for offering
    /// the action, and neither is defaulted to today — a default here is a
    /// third claim nobody made, and the claim it would make is <i>this guest
    /// did not come</i>.
    /// </para>
    /// <para>
    /// Strictly before, so the action appears the day AFTER the arrival day.
    /// </para>
    /// </remarks>
    /// <param name="arrival">The arrival's date at the property, if established.</param>
    /// <param name="today">The property's current operating day, if established.</param>
    /// <returns>True only when both are known and the arrival day is past.</returns>
    public static bool DayHasPassed(DateOnly? arrival, DateOnly? today)
        => arrival is { } day && today is { } now && day < now;
}
