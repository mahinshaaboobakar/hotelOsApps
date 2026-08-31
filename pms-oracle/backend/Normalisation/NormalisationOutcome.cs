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

    /// <summary>A complete fact, ready for the Hub to enrich and publish.</summary>
    /// <param name="Fact">
    /// Populated with everything the source determines. <b>Three things are
    /// deliberately left empty</b> for the Hub: <c>header.business_date</c>,
    /// which the Hub derives through the property's operating-day boundary and
    /// a connector never computes (ADR 0128 §6); <c>header.provenance</c>,
    /// which is the inbox row's; and <c>room_id</c>, which Enrich resolves from
    /// the external reference carried here.
    /// </param>
    public sealed record Normalised(RoomStayFact Fact) : NormalisationOutcome;

    /// <summary>
    /// Half of a two-part check-in, waiting for its partner.
    /// </summary>
    /// <param name="Part">Which half this is.</param>
    /// <param name="JoinKey">What it will be paired on.</param>
    public sealed record AwaitingJoin(
        Vocabularies.OnSiteMessagePart Part,
        OnSiteJoinKey JoinKey) : NormalisationOutcome;

    /// <summary>The message could not become a fact.</summary>
    /// <param name="Reason">What kind of failure.</param>
    /// <param name="Field">The field at fault, in this connector's terms.</param>
    /// <param name="RawValue">What arrived, carried forward rather than discarded.</param>
    public sealed record Rejected(
        RejectionReason Reason,
        string Field,
        string? RawValue) : NormalisationOutcome;
}
