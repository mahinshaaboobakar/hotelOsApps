using System.Text.Json.Serialization;

namespace HotelOS.Workforce.Application.Shifts;

/// <summary>What a shift boundary announcement carries.</summary>
/// <remarks>
/// <para>
/// <b>Every wire name is stated, never left to a convention.</b>
/// <c>EventAppender</c> serialises with no options, so a property named
/// <c>PropertyId</c> reaches the store as <c>"PropertyId"</c> while every
/// consumer reads <c>property_id</c> — and the event would be stored, relayed
/// and acknowledged exactly as though it had worked. The posting announcements
/// carry these same attributes against the same hazard.
/// </para>
/// <para>
/// <b>Both the shift's id and the department's code.</b> The code is what the
/// fact <i>means</i> and what a consumer keys a presence row on; the catalogue
/// id says which shift, because a department can have several and
/// <i>Housekeeping's morning ended</i> and <i>Housekeeping's split shift ended</i>
/// are different facts on one day.
/// </para>
/// </remarks>
public sealed record ShiftBoundaryAnnouncement
{
    /// <summary>The tenancy boundary.</summary>
    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    /// <summary>The canon code — what a consumer's presence row is keyed on.</summary>
    [JsonPropertyName("department_code")]
    public required string DepartmentCode { get; init; }

    /// <summary>Which shift began or finished.</summary>
    [JsonPropertyName("shift_id")]
    public required Guid ShiftId { get; init; }

    /// <summary>
    /// The rota date the cells belong to, ISO — not the calendar date of
    /// <see cref="At"/>.
    /// </summary>
    /// <remarks>
    /// A night shift belongs to the day it starts on, so its end at 07:00
    /// carries yesterday's. A consumer reconciling against a rota needs the date
    /// the rota used, not the one the clock shows.
    /// </remarks>
    [JsonPropertyName("business_date")]
    public required string BusinessDate { get; init; }

    /// <summary>The boundary instant.</summary>
    /// <remarks>
    /// Carried so a consumer can ignore an announcement older than the state it
    /// already holds. The event's own timestamp is when it was <i>appended</i>,
    /// which is a sweep's tick rather than the moment the shift turned.
    /// </remarks>
    [JsonPropertyName("at")]
    public required DateTimeOffset At { get; init; }

    /// <summary>
    /// How many people are covered in that department immediately after this.
    /// </summary>
    /// <remarks>
    /// <b>The consumer sets its presence from this, never from the verb.</b> At a
    /// handover both events carry the same number, so the boolean lands
    /// correctly whichever arrives last — the ordering hazard is removed rather
    /// than mitigated, which is the ruling's own reading of it.
    /// </remarks>
    [JsonPropertyName("on_now_after")]
    public required int OnNowAfter { get; init; }
}

/// <summary>The subjects and the aggregate these announcements use.</summary>
/// <remarks>
/// <b>Constants, so a rename is a compile-move</b> — the same split the posting
/// announcements use, and the same reason: the manifest's <c>publishes:</c> list
/// and these strings must agree exactly, and an application may publish only
/// what it declares.
/// </remarks>
public static class ShiftAnnouncements
{
    /// <summary>People came on — a department gained a shift.</summary>
    public const string Started = "shift.started";

    /// <summary>People went off.</summary>
    public const string Ended = "shift.ended";

    /// <summary>
    /// The announcement row itself — <i>announce against what you own</i>.
    /// </summary>
    /// <remarks>
    /// Not the catalogue entry, whose version never moves when a shift starts:
    /// two announcements would then collide on <c>(aggregate, version)</c> and a
    /// consumer that dedupes on the pair would drop the second.
    /// </remarks>
    public const string Aggregate = "shift_boundary";
}
