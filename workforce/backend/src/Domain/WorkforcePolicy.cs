namespace HotelOS.Workforce.Domain;

/// <summary>
/// What one property configures about how its workforce is run.
/// </summary>
/// <remarks>
/// <para>
/// One row per property. Slice 3b puts the <b>overtime threshold</b> here;
/// slice 4 adds the leave policy beside it, which is why this is a policy rather
/// than an <c>OvertimeThreshold</c> table — the screen it serves is one screen,
/// and a second table would arrive with the second field.
/// </para>
/// <para>
/// <b>The holiday calendar is not here.</b> <c>WF-Q16</c>: the administrator
/// establishes the property's holidays in Core Administration exactly as they
/// establish <c>check_in_time</c>, and this application <i>reads</i> them.
/// Being used by an application does not make an attribute owned by it — what
/// decides is who establishes the value (ADR 0052).
/// </para>
/// </remarks>
public class WorkforcePolicy
{
    /// <summary>The property this policy is for, and the row's identity.</summary>
    /// <remarks>
    /// The property id <i>is</i> the key: one property has one policy, and a
    /// surrogate id would admit a second row that nothing could choose between.
    /// </remarks>
    public Guid PropertyId { get; set; }

    /// <summary>Hours in a day after which the rota warns, or null.</summary>
    /// <remarks>
    /// <para>
    /// <c>WF-Q14</c>: overtime <b>warns at planning time</b>, on planned hours,
    /// and <b>never blocks</b>. The owner's example is <i>"OT after 9h/day or
    /// 48h/week"</i>.
    /// </para>
    /// <para>
    /// Nullable, and null means the property has not set one — not zero. A
    /// property that has never opened this screen must not have every rota
    /// flagged, and a default of eight would be this application inventing a
    /// labour rule for a hotel it has never seen.
    /// </para>
    /// </remarks>
    public decimal? OvertimeDailyHours { get; set; }

    /// <summary>Hours in a week after which the rota warns, or null.</summary>
    public decimal? OvertimeWeeklyHours { get; set; }

    /// <summary>When the policy was first written.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }
}
