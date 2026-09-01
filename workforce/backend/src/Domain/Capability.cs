namespace HotelOS.Workforce.Domain;

/// <summary>
/// Something a person can do, and — if it lapses — until when.
/// </summary>
/// <remarks>
/// <para>
/// ADR 0063 §Q5 moved <c>skills</c> and <c>languages</c> off <c>Staff</c> to
/// this application, on the rule that *"if an attribute exists primarily to
/// determine operational assignment or workforce capability, it belongs to
/// Roster / Workforce — even when it describes a person"*. <c>mobile</c> and
/// <c>emergency_contact_name</c> stayed, because one set decides who gets
/// assigned to what and the other is who the person is.
/// </para>
/// <para>
/// <b>One optional field carries both concepts</b> — the ruling of 2026-08-31.
/// No date is an <i>ability</i>: <i>"speaks Arabic"</i>, <i>"can operate
/// boiler"</i>. A date makes it a <i>certification</i>: <i>"fire warden — valid
/// until 12 Mar 2027"</i>.
/// </para>
/// <para>
/// <b>The date is the discriminator</b>, which is why there is no <c>kind</c>
/// column. A separate discriminator can disagree with the data beside it — a row
/// marked <i>ability</i> carrying an expiry, or <i>certification</i> without one
/// — and here that state cannot be written at all. The house pattern: encode the
/// rule where violating it is inexpressible.
/// </para>
/// <para>
/// <b>Languages are not a second table.</b> Chapter 01 §3.8's own example of an
/// ability is a language, and it needs nothing this does not already carry. A
/// taxonomy separating <i>skill</i> from <i>language</i> would be invented
/// before a consumer exists — which is the objection this round already recorded
/// against <c>shift pattern</c>, and it applies to its own design too.
/// </para>
/// </remarks>
public class Capability
{
    /// <summary>This record's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary. Every query is scoped by it.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Master Data's person — never a copy of them.</summary>
    public Guid StaffId { get; set; }

    /// <summary>What they can do.</summary>
    /// <remarks>
    /// Free text, and deliberately: a hotel's capability vocabulary is its own —
    /// <i>fire warden</i>, <i>pool lifeguard</i>, <i>food handling</i>,
    /// <i>speaks Arabic</i>, <i>forklift</i> — and a closed list would be a
    /// platform release every time a property trained somebody in something new.
    /// The department canon is closed because it is the industry's vocabulary;
    /// this is not.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>The last day it is valid, or null when it does not lapse.</summary>
    /// <remarks>
    /// Null is the ordinary case and carries meaning rather than absence: this
    /// capability is an ability, so there is nothing to renew and nothing to
    /// warn about.
    /// </remarks>
    public DateOnly? ValidUntil { get; set; }

    /// <summary>Anything a certificate number or an issuer would go in.</summary>
    /// <remarks>
    /// One free-text note rather than a set of columns nobody asked for. The
    /// safety inspector's sheet wants the holder, the capability and the expiry;
    /// a hotel that also wants the certificate number has somewhere to put it,
    /// and a hotel that does not is not asked for one.
    /// </remarks>
    public string Note { get; set; } = string.Empty;

    /// <summary>When it was recorded.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it was last amended — a renewal is an amendment.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency, and the event's version.</summary>
    public long Version { get; set; }

    /// <summary>Whether this is a certification rather than an ability.</summary>
    /// <remarks>
    /// A method rather than a stored flag, for the reason the whole design rests
    /// on: the date already says it, and a second place to say it is a second
    /// place to be wrong.
    /// </remarks>
    public bool Lapses => ValidUntil is not null;

    /// <summary>How close this is to lapsing, on a given day.</summary>
    /// <remarks>
    /// <para>
    /// <b>Derived, never stored.</b> The answer depends on the clock, and a
    /// stored value that depends on the clock is wrong every day at midnight
    /// until something rewrites it — the same reason the platform derives the
    /// current business date, the duty register computes *"who is MOD now"*, and
    /// this application refuses a stored late-minutes column.
    /// </para>
    /// <para>
    /// The thresholds are the ruling's: <b>60 / 30 / 7</b>.
    /// </para>
    /// </remarks>
    /// <param name="on">The day to judge it on.</param>
    /// <returns>The band this capability is in.</returns>
    public ExpiryBand BandOn(DateOnly on)
    {
        if (ValidUntil is not { } expiry)
        {
            return ExpiryBand.DoesNotLapse;
        }

        if (expiry < on)
        {
            return ExpiryBand.Expired;
        }

        var days = expiry.DayNumber - on.DayNumber;

        return days switch
        {
            <= 7 => ExpiryBand.Within7Days,
            <= 30 => ExpiryBand.Within30Days,
            <= 60 => ExpiryBand.Within60Days,
            _ => ExpiryBand.Valid,
        };
    }
}
