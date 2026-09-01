namespace HotelOS.Workforce.Domain;

/// <summary>
/// The Manager on Duty register — a span, never a date.
/// </summary>
/// <remarks>
/// <para>
/// <c>WF-Q8</c>, owner 2026-08-31: <i>"we can't do per-day, because MOD may run
/// 8:00 pm to 8:00 am — it covers two dates."</i> One sentence, and it is
/// structural rather than a preference: revision 1 of the chapter said
/// <i>"date (or date range)"</i> and a duty running 20:00→08:00 is neither.
/// </para>
/// <para>
/// <b>"Who is MOD right now" is derived, never stored.</b> No
/// <c>is_current_mod</c> flag, no <c>current_mod_staff_id</c> on the property,
/// no nightly job moving a marker — each is a value that can be wrong while the
/// data beside it is right. The span is stored; the current is computed. It is
/// the same shape ADR 0128 §6 gave the business date, and the fourth
/// clock-dependent column this application has refused.
/// </para>
/// <para>
/// <b>The duty grants nothing.</b> <c>WF-Q1</c>, ruled: MOD is a duty
/// assignment, not an authorization role — display, roster, visibility where the
/// person's existing permissions already allow it, audit, and <b>no FGA tuples
/// in v1</b>. The person keeps their own posting: security stays security. Any
/// future elevation needs its own authorization ADR, and this record's lifecycle
/// events are the hook it would use.
/// </para>
/// </remarks>
public class DutyAssignment
{
    /// <summary>This duty's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary. A duty is property-wide within it.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Who holds it — Master Data's person.</summary>
    /// <remarks>
    /// Any active staff member, from any department, which is the owner's exact
    /// scenario: <i>"a front office staff may be MOD for a day, another day
    /// security."</i>
    /// </remarks>
    public Guid StaffId { get; set; }

    /// <summary>What kind of duty. v1 ships exactly one.</summary>
    /// <remarks>
    /// A column rather than a constant, because the register is the shape a
    /// second duty type would use and leaving it out would make adding one a
    /// migration of every row. It is not a vocabulary the property extends in
    /// v1 — nothing offers a second value.
    /// </remarks>
    public string DutyType { get; set; } = DutyTypes.ManagerOnDuty;

    /// <summary>When the duty begins.</summary>
    public DateTimeOffset StartsAt { get; set; }

    /// <summary>When it ends.</summary>
    /// <remarks>
    /// After <see cref="StartsAt"/>, always — a span that ends before it starts
    /// cannot be true, so it is refused rather than warned (<c>WF-Q16</c>).
    /// Crossing midnight is ordinary and needs no special case: these are
    /// instants, and 20:00 to 08:00 is twelve hours like any other twelve.
    /// </remarks>
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>What the outgoing manager wants the incoming one to know.</summary>
    /// <remarks>
    /// Free text, optional, and <b>blocking nothing</b>. A mandatory handover
    /// note is a field people type <i>"n/a"</i> into, and a required field
    /// nobody means is worse than an empty one.
    /// </remarks>
    public string HandoverNote { get; set; } = string.Empty;

    /// <summary>When the duty was assigned.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it was last amended.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }

    /// <summary>Is this duty in force at <paramref name="instant"/>?</summary>
    /// <remarks>
    /// Half-open — the start is included and the end is not — so a duty ending
    /// at 08:00 and the next beginning at 08:00 do not both hold at 08:00, and
    /// <i>"who is MOD now"</i> has exactly one answer at every instant.
    /// </remarks>
    /// <param name="instant">The moment to ask about.</param>
    /// <returns>Whether this duty covers it.</returns>
    public bool CoversAt(DateTimeOffset instant) =>
        instant >= StartsAt && instant < EndsAt;

    /// <summary>Does this duty overlap the span given?</summary>
    /// <remarks>
    /// <para>
    /// The rule that replaced <i>"one MOD per property per day"</i>. As a date
    /// that was a unique key; as a span it cannot be, and keeping the sentence
    /// while changing the column would have lost the guarantee silently.
    /// </para>
    /// <para>
    /// Half-open on both sides, so back-to-back duties do not overlap — which is
    /// the ordinary handover and must not be refused.
    /// </para>
    /// </remarks>
    /// <param name="otherStart">The other duty's start.</param>
    /// <param name="otherEnd">The other duty's end.</param>
    /// <returns>Whether the two spans intersect.</returns>
    public bool Overlaps(DateTimeOffset otherStart, DateTimeOffset otherEnd) =>
        StartsAt < otherEnd && otherStart < EndsAt;
}

/// <summary>The duty types this register carries.</summary>
/// <remarks>
/// One, and named rather than left as a literal at the two call sites that write
/// it. A second would arrive with its own screen and its own rules; what exists
/// today is the Manager on Duty.
/// </remarks>
public static class DutyTypes
{
    /// <summary>The Manager on Duty — the owner's founding scenario.</summary>
    public const string ManagerOnDuty = "mod";
}
