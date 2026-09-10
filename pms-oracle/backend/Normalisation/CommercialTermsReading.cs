using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.Cloud;

namespace PmsOracle.Normalisation;

/// <summary>
/// Reads an OHIP guarantee policy into the contract's
/// <see cref="CommercialTerms"/> — or nothing.
/// </summary>
/// <remarks>
/// <para>
/// R18, and <see cref="AmountReading"/>'s shape at the same boundary: a static
/// reader that either produces the contract's type or returns <c>null</c>,
/// never a partly-filled one. It sits here rather than inside
/// <c>CloudNormaliser</c> because the guarantee is not part of a reservation
/// message — it is a source fact of its own, fetched per property and arrival
/// date, and folding its reading into the reservation normaliser would put two
/// subjects in one file (ADR 0038) as well as growing it.
/// </para>
/// <para>
/// <b>Offsets, never resolved deadlines.</b> An offset from arrival survives
/// the arrival date changing and a resolved timestamp does not. The reference
/// resolved both and kept the results as formatted strings
/// (<c>cloud:243-248</c>), so a reservation that moved carried a cancellation
/// deadline that had quietly stopped matching it.
/// </para>
/// <para>
/// <b>This has no production caller yet, and the reason is ADR 0147.</b> The
/// connector fetches and identifies the source fact; the <i>Hub</i> owns its
/// durability and keyed lookup, and <i>enrichment</i> resolves a reservation
/// against the Hub-held fact and populates
/// <c>RoomStayFact.commercial_terms</c>. That store is new surface and is not
/// built. So this is a reader ready for its caller — it is not evidence that
/// one exists, and nothing in this connector's own pipeline calls it, because
/// nothing in this connector's pipeline is the party that joins one policy to
/// many reservations.
/// </para>
/// </remarks>
public static class CommercialTermsReading
{
    /// <summary>Read one OHIP guarantee policy.</summary>
    /// <param name="guarantee">The policy exactly as OHIP returned it.</param>
    /// <returns>
    /// The commercial terms, or <c>null</c> where the policy carries nothing
    /// this contract can express.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Nothing rather than an empty record</b>, which is the same refusal
    /// <see cref="AmountReading"/> makes. A <see cref="CommercialTerms"/> with
    /// every field at its proto3 default is indistinguishable, downstream, from
    /// a property whose policy is *no deposit, no cancellation window, no
    /// penalty* — a real and different arrangement. Producing one would state
    /// terms nobody sent.
    /// </para>
    /// <para>
    /// The test for "carries nothing" is the guarantee's own identity: a policy
    /// with no code and neither offset is not a policy this connector read, and
    /// the flags alone cannot be told from their defaults.
    /// </para>
    /// </remarks>
    public static CommercialTerms? Read(OhipGuarantee? guarantee)
    {
        if (guarantee is null || Empty(guarantee))
        {
            return null;
        }

        var terms = new CommercialTerms
        {
            GuaranteeCode = guarantee.GuaranteeCode ?? string.Empty,

            // **Left empty deliberately, and this is a refusal.** The wire
            // carries `guarantee_description`; the study establishes a code and
            // three flags at `:13-27` and names no spelling for a description,
            // so `OhipGuarantee` transcribes none and there is nothing here to
            // read. Filling it from `GuaranteeCode` would show an operator a
            // code where a sentence belongs and would read as the vendor's
            // wording.
            GuaranteeDescription = string.Empty,

            // **Also empty, and for the other ratified reason.** Nothing in the
            // study establishes that OHIP sends a rate code or rate name on the
            // guarantee path. The reservation carries a rate elsewhere; taking
            // it from there would put a value read from one message into a fact
            // fetched from another, and a derived value is indistinguishable
            // from a read one once written.
            RateCode = string.Empty,
            RateName = string.Empty,

            OnHold = guarantee.OnHold,
            ReservesInventory = guarantee.ReserveInventory,
            IsDefault = guarantee.DefaultGuarantee,
        };

        // proto3 scalars have no presence, so an absent offset and a zero-day
        // offset are one value on the wire. Zero is a real answer here — *due
        // on the day of booking*, *free until the day of arrival* — so the
        // absent case is left at the default rather than written, and a
        // consumer reads the pair with `guarantee_code` beside it. Named
        // because it is a genuine limit of the contract, not an oversight.
        if (guarantee.DepositPolicy?.OffsetFromBookingDate is { } deposit)
        {
            terms.DepositOffsetDaysFromBooking = deposit;
        }

        if (guarantee.CancellationPolicy is { } cancellation)
        {
            if (cancellation.OffsetFromArrival is { } arrival)
            {
                terms.CancelOffsetDaysFromArrival = arrival;
            }

            terms.CancelDropTime = cancellation.OffsetDropTime ?? string.Empty;
            Penalty(terms, cancellation.Amount);
        }

        return terms;
    }

    /// <summary>Carry what the policy says the penalty is.</summary>
    /// <param name="terms">The terms being read into.</param>
    /// <param name="amount">OHIP's penalty block, where it sent one.</param>
    /// <remarks>
    /// <para>
    /// <b><c>penalty_amount</c> is left absent, and it is the ratified refusal
    /// this method exists to make visible.</b> The study's citation for this
    /// block names <c>basisType</c>, <c>nights</c> and <c>currencyCode</c> and
    /// no monetary value (<c>:85-92</c>), so there is nothing to read into a
    /// <see cref="Money"/>.
    /// </para>
    /// <para>
    /// The tempting move is to multiply <c>nights</c> by the stay's nightly
    /// rate and call the result the penalty. That would be a chargeable number
    /// this connector invented — <b>a penalty stated in nights cannot become
    /// money without a rate this fact does not have</b>, which is the contract's
    /// own comment at the field. The basis and the nights are carried instead,
    /// so whoever displays it can say what the policy actually is.
    /// </para>
    /// <para>
    /// <c>currencyCode</c> is read by nothing here for the same reason: a
    /// currency with no amount is a denomination for a number that does not
    /// exist, and <see cref="Money"/> cannot express one without inventing the
    /// other two thirds of it (R19).
    /// </para>
    /// </remarks>
    private static void Penalty(CommercialTerms terms, OhipPolicyAmount? amount)
    {
        if (amount is null)
        {
            return;
        }

        terms.PenaltyBasis = amount.BasisType ?? string.Empty;

        if (amount.Nights is { } nights)
        {
            terms.PenaltyNights = nights;
        }
    }

    /// <summary>Whether the policy carries anything this contract can state.</summary>
    /// <param name="guarantee">The policy.</param>
    /// <returns><c>true</c> when there is nothing to produce terms from.</returns>
    /// <remarks>
    /// The flags are excluded on purpose: <c>false</c> is both "OHIP said no"
    /// and "OHIP said nothing", so a policy recognised only by its flags would
    /// be recognised by values that cannot distinguish those.
    /// </remarks>
    private static bool Empty(OhipGuarantee guarantee) =>
        string.IsNullOrWhiteSpace(guarantee.GuaranteeCode)
        && guarantee.DepositPolicy?.OffsetFromBookingDate is null
        && guarantee.CancellationPolicy?.OffsetFromArrival is null
        && string.IsNullOrWhiteSpace(guarantee.CancellationPolicy?.OffsetDropTime)
        && guarantee.CancellationPolicy?.Amount is null;
}
