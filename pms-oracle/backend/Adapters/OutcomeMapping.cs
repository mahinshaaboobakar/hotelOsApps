using HotelOS.Connector;
using PmsOracle.Normalisation;

namespace PmsOracle.Adapters;

/// <summary>
/// The connector's own outcome vocabulary, in the Hub's terms.
/// </summary>
/// <remarks>
/// <para>
/// One place, because three adapters produce the same
/// <see cref="NormalisationOutcome"/> and a mapping written per adapter would
/// disagree the first time somebody added a rejection reason in a hurry.
/// </para>
/// <para>
/// <b>Every <see cref="RejectionReason"/> maps to a Hub outcome, and the
/// switch is exhaustive on purpose.</b> A default arm would silently absorb a
/// reason added later — which is the failure the reference made, ending
/// <c>default: return null</c> and leaving nobody able to say what its PMS
/// emitted.
/// </para>
/// </remarks>
public static class OutcomeMapping
{
    /// <summary>Turn one normalisation outcome into what the Hub understands.</summary>
    /// <param name="outcome">What the normaliser decided.</param>
    /// <returns>Facts, or a terminating result carrying the field and the value.</returns>
    public static NormalisedPayload ToPayload(NormalisationOutcome outcome) => outcome switch
    {
        NormalisationOutcome.StayNormalised stay =>
            new NormalisedPayload(PipelineResult.Continue(), [stay.Fact], []),

        NormalisationOutcome.RoomStateNormalised state =>
            new NormalisedPayload(PipelineResult.Continue(), [], [state.Fact]),

        // The Hub holds the half and the window; this says only that one is a
        // half. `HUB-Q5`: the key and the part are the connector's, and they
        // reach the Hub through `IJoiningConnector.JoinFor` rather than here —
        // by the time a payload is normalised, the pairing has already
        // happened.
        NormalisationOutcome.AwaitingJoin =>
            NormalisedPayload.Nothing(PipelineResult.AwaitingJoin()),

        NormalisationOutcome.Rejected rejected =>
            NormalisedPayload.Nothing(PipelineResult.Reject(
                Describe(rejected.Reason), rejected.Field, rejected.RawValue)),

        _ => throw new ArgumentOutOfRangeException(
            nameof(outcome),
            outcome,
            "an outcome this mapping does not know — add it here rather than "
            + "letting a default arm absorb it"),
    };

    /// <summary>A few words an operator reads, per reason.</summary>
    /// <param name="reason">The connector's own reason.</param>
    /// <returns>The Hub's short description.</returns>
    /// <remarks>
    /// Spelled out rather than <c>ToString()</c>: the enum's names are this
    /// connector's internal vocabulary, and an operator in Operations Center is
    /// reading a sentence rather than a symbol. It also means renaming a member
    /// cannot silently change what a hotel sees.
    /// </remarks>
    private static string Describe(RejectionReason reason) => reason switch
    {
        RejectionReason.MissingRequiredField => "a required field was absent",
        RejectionReason.UnreadableValue => "a value could not be read",
        RejectionReason.UnknownStatus => "unknown status",
        RejectionReason.PropertyMismatch => "the message names another property",
        _ => throw new ArgumentOutOfRangeException(
            nameof(reason), reason, "a rejection reason this mapping does not describe"),
    };
}
