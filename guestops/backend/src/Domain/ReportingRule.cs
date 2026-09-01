namespace HotelOS.GuestOps.Domain;

/// <summary>
/// Whether a stay must be filed, and by when — S19b.
/// </summary>
/// <remarks>
/// <para>
/// Pure, for the same reason <see cref="RegistrationRule"/> is: a property may
/// be asked years later why a stay was or was not filed, and the answer should
/// be readable in one place and testable without a database.
/// </para>
/// <para>
/// <b>This decides an obligation, never an outcome.</b> Nothing here submits
/// anything: HotelOS files with no authority, because sending guest data
/// outward is an integration and every integration is a connector (CONN-Q5's
/// deferred write-back). What this produces is a flag and a date a person acts
/// on.
/// </para>
/// </remarks>
public static class ReportingRule
{
    /// <summary>Whether this stay falls inside the property's policy.</summary>
    /// <param name="settings">The property's configuration.</param>
    /// <param name="nationality">The guest's nationality, if captured.</param>
    /// <returns>The state this stay's obligation starts in.</returns>
    /// <remarks>
    /// <b>Not-required is recorded rather than left blank.</b> A stay with no
    /// row and a stay explicitly outside the policy look identical otherwise,
    /// and only one of them is evidence that the question was asked.
    /// </remarks>
    public static ReportingState StateFor(GuestOpsSettings settings, string? nationality)
    {
        if (!settings.ReportingRequired)
        {
            return ReportingState.NotRequired;
        }

        return settings.ReportingAppliesTo switch
        {
            ReportingScope.EveryGuest => ReportingState.Needed,
            _ => RegistrationRule.IsVisitor(nationality, settings.HomeCountry)
                ? ReportingState.Needed
                : ReportingState.NotRequired,
        };
    }

    /// <summary>The date the filing is due, from the arrival and the offset.</summary>
    /// <param name="arrival">The stay's arrival, which may be unknown.</param>
    /// <param name="dueHours">The property's configured offset in hours.</param>
    /// <returns>The due date, or null when there is no arrival to count from.</returns>
    /// <remarks>
    /// <para>
    /// <b>Computed from the offset every time, never stored and reused</b> —
    /// R18. *"Within 24 hours of arrival"* survives the arrival moving; a date
    /// captured when the booking was made does not, and would keep pointing at
    /// the old arrival in the direction that matters.
    /// </para>
    /// <para>
    /// <b>No arrival means no deadline, not today's date.</b> R25: an absence is
    /// neither dropped nor invented, and a fabricated deadline would put a stay
    /// on the overdue list for a night that has not happened.
    /// </para>
    /// </remarks>
    public static DateOnly? DueBy(StayTime arrival, int dueHours)
        => arrival.At is { } at
            ? DateOnly.FromDateTime(at.AddHours(dueHours).UtcDateTime)
            : null;
}
