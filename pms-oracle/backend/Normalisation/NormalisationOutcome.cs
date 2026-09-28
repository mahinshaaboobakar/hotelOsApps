using HotelOS.Contracts.Integration.V1;

namespace PmsOracle.Normalisation;

/// <summary>Why a source message did not become a fact.</summary>
public enum RejectionReason
{
    /// <summary>A required field was absent or empty.</summary>
    MissingRequiredField,

    /// <summary>A field was present and could not be read — a date, a number.</summary>
    UnreadableValue,

    /// <summary>
    /// A status value no declared meaning covers.
    /// </summary>
    /// <remarks>
    /// Its own reason rather than <see cref="UnreadableValue"/>, because it is
    /// usually a PMS that gained a value rather than a fault — and because the
    /// operator's next action differs: someone decides whether the vocabulary
    /// should grow, then the held records replay.
    /// </remarks>
    UnknownStatus,

    /// <summary>
    /// The property this message claims is not the one its integration is
    /// configured for.
    /// </summary>
    /// <remarks>
    /// The reference took the property from the body and believed it, on an
    /// endpoint with no authentication — so a body was enough to write into any
    /// property. Here the ingress knows which integration was posted to, and a
    /// disagreement is a rejection rather than a redirect.
    /// </remarks>
    PropertyMismatch,

    /// <summary>
    /// The integration's own configuration cannot support normalisation — no
    /// property clock, no currency, no declared tax basis.
    /// </summary>
    /// <remarks>
    /// A configuration fault, not a message fault. The message is held and
    /// replays once the integration is configured correctly; nothing about it
    /// was wrong.
    /// </remarks>
    IntegrationNotConfigured,
}

/// <summary>
/// What normalising one on-site message produced: a fact, half of one, or a
/// rejection that names what it could not use.
/// </summary>
/// <remarks>
/// <para>
/// Three outcomes rather than two, because this source has a third real state.
/// A <c>"Checked In"</c> message carries contact details and no room, and a
/// <c>"CHECKED IN"</c> carries a room and no contact details (R6): neither is a
/// publishable fact, and neither is wrong. Calling the first half a rejection
/// would alert somebody about a message that is behaving exactly as designed.
/// </para>
/// <para>
/// A rejection <b>carries the value it could not use</b>, for the same reason
/// <see cref="Vocabularies.Reading{T}"/> does: the operator screen shows
/// <c>"NO SHOW"</c> rather than a silence, and growing the vocabulary is then a
/// decision somebody makes rather than a discovery years later.
/// </para>
/// </remarks>
public abstract record NormalisationOutcome
{
    private NormalisationOutcome()
    {
    }

    /// <summary>A complete room-stay fact, ready for the Hub to enrich and publish.</summary>
    /// <param name="Fact">
    /// Populated with everything the source determines. <b>Three things are
    /// deliberately left empty</b> for the Hub: <c>header.business_date</c>,
    /// which the Hub derives through the property's operating-day boundary and
    /// a connector never computes (ADR 0128 §6); <c>header.provenance</c>,
    /// which is the inbox row's; and <c>room_id</c>, which Enrich resolves from
    /// the external reference carried here.
    /// </param>
    public sealed record StayNormalised(RoomStayFact Fact) : NormalisationOutcome;

    /// <summary>A complete room-state fact.</summary>
    /// <param name="Fact">
    /// The four axes as the source reported them, with the same three fields
    /// left for the Hub. Its own variant rather than a shared one holding a
    /// base type: the two facts are different shapes with different consumers,
    /// and a caller that had to test which it received would be a caller that
    /// could forget to.
    /// </param>
    public sealed record RoomStateNormalised(RoomStateFact Fact) : NormalisationOutcome;

    /// <summary>
    /// Half of a two-part check-in, waiting for its partner.
    /// </summary>
    /// <param name="Part">Which half this is.</param>
    /// <param name="JoinKey">What it will be paired on.</param>
    public sealed record AwaitingJoin(
        Vocabularies.OnSiteMessagePart Part,
        OnSiteJoinKey JoinKey) : NormalisationOutcome;

    /// <summary>
    /// The payload is valid and one computation on it could not be made —
    /// <c>CONN-Q75</c>, ruled (b).
    /// </summary>
    /// <param name="Prerequisite">
    /// The platform fact that was not supplied, by the name the wire gives it.
    /// </param>
    /// <param name="Field">The source field whose value could not be read.</param>
    /// <remarks>
    /// <para>
    /// <b>Not a rejection, and not an absent value.</b> A rejection means the
    /// payload itself was refused; here it remains valid and is re-normalised
    /// once the prerequisite lands. And an absent amount is a different, valid
    /// state — the source sent none — which is why the two cannot share one
    /// answer: *"the absence of an amount must NOT be overloaded to mean
    /// normalization is blocked."*
    /// </para>
    /// <para>
    /// <b>The remedies differ, which is the whole reason for the distinction.</b>
    /// A source that sent no amount has none — nothing to do. An amount we
    /// could not scale is waiting on the currency catalogue (<c>ARCH-Q38b</c>),
    /// and it becomes a fact the moment that publishes. On any surface the two
    /// would look like the same silence.
    /// </para>
    /// <para>
    /// <b>How this reaches the Hub is deliberately not decided here.</b> The
    /// ruling established the mechanism and its purpose and left the field or
    /// message that exposes an amount-level prerequisite as a follow-on
    /// contract decision. So this is the state, and nothing in this package
    /// invents a wire for it.
    /// </para>
    /// </remarks>
    public sealed record Unresolved(string Prerequisite, string Field) : NormalisationOutcome;

    /// <summary>The message could not become a fact.</summary>
    /// <param name="Reason">What kind of failure.</param>
    /// <param name="Field">The field at fault, in this connector's terms.</param>
    /// <param name="RawValue">What arrived, carried forward rather than discarded.</param>
    public sealed record Rejected(
        RejectionReason Reason,
        string Field,
        string? RawValue) : NormalisationOutcome;
}
