using HotelOS.Contracts.Integration.V1;
using PmsOracle.Authentication;
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

    /// <summary>
    /// The invocation prerequisites this package declares — ADR 0321, ADR 0322.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The manifest is the authority and this is a copy of it</b>, held
    /// honest in both directions by
    /// <c>ManifestDeclarationTests.Every_prerequisite_the_manifest_declares_is_refused_on_the_unresolved_arm</c>.
    /// The same shape as this package's secrets and payload kinds: the compiler
    /// cannot read a signed YAML file, so the agreement is a test rather than a
    /// derivation.
    /// </para>
    /// <para>
    /// <b>The union of three integrations' declarations, because this method
    /// has no integration context.</b> ADR 0322 declares them per integration —
    /// <c>hotelCode</c> is the two pushed ones' and not Cloud's — and refusing
    /// the union is the conservative direction: the cost is a refusal for a
    /// combination that cannot occur, and the cost of the other direction is
    /// the escape hatch the contract forbids.
    /// </para>
    /// </remarks>
    private static readonly IReadOnlySet<string> DeclaredPrerequisites =
        new HashSet<string>(StringComparer.Ordinal)
        {
            IntegrationSettings.TaxBasisSetting,
            OhipCredentials.HotelCodeSetting,
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

            // **CONN-Q84 is RULED and the arm exists** — ADR 0332, ADR 0333. A
            // platform fact the Hub could not supply is neither facts nor a
            // rejection: the source is fine and our reference data is empty, so
            // a rejection would send the operator to the hotel's PMS for a gap
            // of ours.
            NormalisationOutcome.Unresolved unresolved => Unresolvable(unresolved),

            // The Hub assembles the parts and sends them together — "assembling
            // the parts is normalising them" — so waiting for a partner is the
            // Hub's inbox state and cannot be an answer to `normalize`.
            NormalisationOutcome.AwaitingJoin => throw new NotSupportedException(
                "a half of a check-in reached `normalize`. The Hub sends the assembled set, "
                + "so waiting for a partner is its inbox state and has no arm on this reply."),

            _ => throw new NotSupportedException($"no wire arm for {outcome.GetType().Name}."),
        };
    }

    /// <summary>A platform prerequisite the Hub could not supply.</summary>
    /// <param name="unresolved">What could not be resolved, and for which field.</param>
    /// <returns>The reply's third arm.</returns>
    /// <exception cref="NotSupportedException">
    /// The prerequisite is CONFIGURATION rather than a platform fact. See below.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>The source FIELD is deliberately not sent.</b> <c>prerequisite</c> is
    /// the whole message, and the proto says why: it names a platform
    /// prerequisite, never a source field, <i>"and must not be carried in
    /// `Rejection.field` or `raw_value`"</i>. <c>Unresolved.Field</c> stays
    /// connector-local, for the diagnostic and the test.
    /// </para>
    /// <para>
    /// <b>A CONFIGURATION prerequisite is refused here, and that is the whole
    /// point of the check.</b> ADR 0316 splits them by ownership: a platform
    /// fact the Hub cannot resolve travels on this arm, while configuration a
    /// property owes makes the Hub withhold the dispatch entirely
    /// (ADR 0321). Putting <c>amountTaxBasis</c> on this wire would be
    /// <c>CONN-Q84</c> becoming <i>"a generic escape hatch allowing connectors
    /// to rediscover configuration prerequisites that the Hub was required to
    /// gate"</i>, which the ruling forbids by name.
    /// </para>
    /// <para>
    /// <b>The refusal is the CONTRACT's and outlives the arm below it.</b> This
    /// paragraph said <i>"paired with a held arm, and it goes when that
    /// does"</i>, which was wrong in the direction that invites deletion:
    /// <c>NormalizationUnresolved</c> states the rule in its own comment —
    /// <i>"Not an escape hatch. A prerequisite an integration DECLARES is gated
    /// by the Hub before dispatch (ADR 0321, ADR 0327), so it never arrives
    /// here"</i> — so a declared name reaching this method is a contract
    /// violation whenever it happens, not only while something is held. What is
    /// temporary is that the normalisers still <i>produce</i>
    /// <c>Unresolved(amountTaxBasis)</c>, sequenced behind the Hub's
    /// withholding going live; this guard is what keeps that off the wire in
    /// the meantime and stays afterwards.
    /// </para>
    /// <para>
    /// <b>Keyed on what the connector DECLARES, not on a list of platform
    /// facts.</b> <c>prerequisite</c> is OPEN — <i>"that is the currently
    /// required member, not the universe of platform facts … a new one is named
    /// here as it arrives"</i> — so an allow-list of the one platform fact that
    /// exists today would close an open vocabulary to solve a scoping problem,
    /// and would refuse the next platform fact on the day it is ruled.
    /// </para>
    /// </remarks>
    private static NormalizeResult Unresolvable(NormalisationOutcome.Unresolved unresolved)
    {
        if (DeclaredPrerequisites.Contains(unresolved.Prerequisite))
        {
            throw new NotSupportedException(
                $"'{unresolved.Prerequisite}' is configuration a property owes, not a platform "
                + "fact, so it has no place on `NormalizeResult.unresolved` (ADR 0316, "
                + "ADR 0321). The Hub withholds the dispatch instead; a connector answering "
                + "here would be rediscovering a prerequisite the Hub was required to gate.");
        }

        return new NormalizeResult
        {
            Unresolved = new NormalizationUnresolved { Prerequisite = unresolved.Prerequisite },
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
        // **Two refusals, for two different reasons.** They were one until
        // `CONN-Q84(b)` ruled the configuration state's spelling, and the
        // justification I wrote then — "no ruled spelling" — is now false for
        // one of them while the refusal itself still stands. A reader meeting
        // an expired reason concludes the constraint has expired with it.
        if (rejected.Reason is RejectionReason.IntegrationNotConfigured)
        {
            throw new NotSupportedException(
                "INTEGRATION_NOT_CONFIGURED is spelled and is not a connector's to send. "
                + "ADR 0316 puts it in the configuration/waiting classification, never "
                + "connector validation: the Hub reports WAITING and does not invoke the "
                + "connector at all, so a rejection carrying it would be this end answering "
                + "for a decision it was never asked to make.");
        }

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
