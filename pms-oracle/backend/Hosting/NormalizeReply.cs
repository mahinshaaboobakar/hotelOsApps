using HotelOS.Contracts.Integration.V1;
using PmsOracle.Normalisation;

namespace PmsOracle.Hosting;

/// <summary>
/// What one normalisation outcome becomes on the wire.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not <see cref="Adapters.OutcomeMapping"/>, which maps the same outcomes
/// to <c>NormalisedPayload</c></b> — the in-process type of the seam
/// <c>CONN-Q42</c> retires. Two mappings of one vocabulary is the shape this
/// package keeps paying for, and the answer is not to share them: they target
/// different types for different callers. What must not diverge is the
/// MEANING, which is why the reasons are spelled from one list here and the
/// ruled spellings are asserted rather than typed twice.
/// </para>
/// <para>
/// <b>The reasons are the platform's, not ours</b> — ADR 0288 makes the
/// vocabulary OPEN and platform-defined rather than a closed enum, and
/// ADR 0295 fixes the four currently required members, spelled as page 75 §1
/// writes them. A new category is a ruling; this file may translate into the
/// vocabulary and may not extend it.
/// </para>
/// <para>
/// <b>No width is enforced on a reason.</b> The contract comment still says
/// <i>"at most 64 characters"</i> and <c>CONN-Q62</c> removed that bound on
/// 2026-09-27 as implementation-invented — <i>"do not introduce a width limit
/// unless a separate contract decision establishes one."</i> The comment is
/// stale rather than governing, and re-implementing it from the contract text
/// is exactly how an invented constraint survives its own removal.
/// </para>
/// </remarks>
public static class NormalizeReply
{
    /// <summary>The ruled spellings — ADR 0295, page 75 §1.</summary>
    /// <remarks>
    /// <b>Every member of the connector's enum appears here or the translation
    /// refuses.</b> Silence would send an empty reason, and an empty reason is
    /// a malformed envelope rather than a rejection (<c>CONN-Q62</c>): the Hub
    /// would retry it forever and the operator would never see the finding.
    /// </remarks>
    private static readonly IReadOnlyDictionary<RejectionReason, string> Spellings =
        new Dictionary<RejectionReason, string>
        {
            [RejectionReason.MissingRequiredField] = "MISSING_REQUIRED_FIELD",
            [RejectionReason.UnreadableValue] = "UNREADABLE_VALUE",
            [RejectionReason.UnknownStatus] = "UNKNOWN_STATUS",
            [RejectionReason.PropertyMismatch] = "PROPERTY_MISMATCH",
        };

    /// <summary>Turn what the normaliser decided into what the Hub is told.</summary>
    /// <param name="outcome">The normaliser's decision about one fact.</param>
    /// <returns>Facts, or the platform's rejection envelope.</returns>
    /// <exception cref="NotSupportedException">
    /// The outcome has no wire representation. Both cases are stated below
    /// rather than defaulted, because a silent arm here would answer the Hub
    /// with something nobody chose.
    /// </exception>
    public static NormalizeResult From(NormalisationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        return outcome switch
        {
            NormalisationOutcome.StayNormalised stay => new NormalizeResult
            {
                Facts = new NormalizedFacts { RoomStays = { stay.Fact } },
            },

            NormalisationOutcome.RoomStateNormalised state => new NormalizeResult
            {
                Facts = new NormalizedFacts { RoomStates = { state.Fact } },
            },

            NormalisationOutcome.Rejected rejected => new NormalizeResult
            {
                Rejection = Refuse(rejected),
            },

            // **CONN-Q84, open.** An amount the source sent that nothing can
            // scale is neither facts nor a rejection: the source is fine and
            // our reference data is empty, so a rejection would send the
            // operator to the hotel's PMS for a gap of ours. `NormalizeResult`
            // carries two arms and the proto instructs this state at :169
            // without giving it one. Refused rather than mapped to either,
            // because picking would be a contract decision made by a connector.
            NormalisationOutcome.Unresolved unresolved => throw new NotSupportedException(
                $"'{unresolved.Prerequisite}' is unavailable, so '{unresolved.Field}' could not "
                + "be normalised. CONN-Q84 is open on how an unresolved outcome travels; "
                + "NormalizeResult carries facts or a rejection and this is neither."),

            // The Hub assembles the parts and sends them together — "assembling
            // the parts is normalising them" — so waiting for a partner is the
            // Hub's inbox state and cannot be an answer to `normalize`.
            NormalisationOutcome.AwaitingJoin => throw new NotSupportedException(
                "a half of a check-in reached `normalize`. The Hub sends the assembled set, "
                + "so waiting for a partner is its inbox state and has no arm on this reply."),

            _ => throw new NotSupportedException($"no wire arm for {outcome.GetType().Name}."),
        };
    }

    /// <summary>The platform's rejection envelope for one refusal.</summary>
    /// <param name="rejected">What the normaliser refused, and why.</param>
    /// <returns>The envelope, carrying the source's own spelling of the field.</returns>
    /// <remarks>
    /// <b><c>raw_value</c> is the ACTUAL source value and is ABSENT where none
    /// exists</b> — ADR 0288, and ADR 0295's one worded distinction:
    /// <c>UNREADABLE_VALUE</c> is a value that EXISTS and cannot be
    /// interpreted, while a document that does not parse has no such value. It
    /// is never a diagnostic.
    /// </remarks>
    private static Rejection Refuse(NormalisationOutcome.Rejected rejected)
    {
        if (!Spellings.TryGetValue(rejected.Reason, out var reason))
        {
            throw new NotSupportedException(
                $"{rejected.Reason} has no ruled spelling. ADR 0288 makes the vocabulary "
                + "platform-defined and extensible only by a ruling, so a connector cannot "
                + "mint one — and an empty reason is a malformed envelope, not a rejection.");
        }

        var envelope = new Rejection { Reason = reason, Field = rejected.Field };

        if (rejected.RawValue is not null)
        {
            envelope.RawValue = rejected.RawValue;
        }

        return envelope;
    }
}
