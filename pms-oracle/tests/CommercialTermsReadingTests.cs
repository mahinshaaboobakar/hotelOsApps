using PmsOracle.Integrations.Cloud;
using PmsOracle.Normalisation;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// Reading an OHIP guarantee policy — R18.
/// </summary>
/// <remarks>
/// <para>
/// <b>Written from the study's citations rather than from the reader.</b> Every
/// field asserted here is one the study names at a line
/// (<c>OracleCloudReservationGuarantees.java:13-27</c>, <c>:50-52</c>,
/// <c>:75-79</c>, <c>:85-92</c>), and the three absences are asserted for the
/// same reason: an assertion taken from the code just written inherits its
/// author's reading, and two of the three refusals here are exactly the kind of
/// thing that reading would have filled in.
/// </para>
/// </remarks>
public sealed class CommercialTermsReadingTests
{
    /// <summary>A policy with everything the study establishes OHIP sends.</summary>
    private static OhipGuarantee Whole() => new(
        GuaranteeCode: "CC",
        OnHold: false,
        ReserveInventory: true,
        DefaultGuarantee: true,
        DepositPolicy: new OhipDepositPolicy(
            OffsetFromBookingDate: 7,
            Amount: new OhipPolicyAmount("NIGHTS", 1, "INR")),
        CancellationPolicy: new OhipCancellationPolicy(
            OffsetFromArrival: 2,
            OffsetDropTime: "18:00",
            Amount: new OhipPolicyAmount("NIGHTS", 1, "INR")));

    [Fact]
    public void the_deadlines_are_carried_as_offsets_and_never_resolved()
    {
        var terms = CommercialTermsReading.Read(Whole());

        Assert.NotNull(terms);

        // R18's whole point. The reference resolved both to formatted strings
        // at fetch time (`cloud:243-248`), so a reservation whose arrival later
        // moved carried a cancellation deadline that had stopped matching it.
        Assert.Equal(7, terms.DepositOffsetDaysFromBooking);
        Assert.Equal(2, terms.CancelOffsetDaysFromArrival);
        Assert.Equal("18:00", terms.CancelDropTime);
    }

    [Fact]
    public void the_three_flags_are_carried_as_the_source_stated_them()
    {
        var terms = CommercialTermsReading.Read(Whole());

        Assert.NotNull(terms);
        Assert.Equal("CC", terms.GuaranteeCode);
        Assert.False(terms.OnHold);
        Assert.True(terms.ReservesInventory);
        Assert.True(terms.IsDefault);
    }

    /// <summary>
    /// The first ratified refusal. The study's citation for the penalty block
    /// names <c>basisType</c>, <c>nights</c> and <c>currencyCode</c> and no
    /// monetary value, so there is nothing to read into a <c>Money</c> — and a
    /// penalty stated in nights cannot become money without a rate this fact
    /// does not have.
    /// </summary>
    [Fact]
    public void a_penalty_in_nights_never_becomes_an_amount()
    {
        var terms = CommercialTermsReading.Read(Whole());

        Assert.NotNull(terms);

        // What the source said.
        Assert.Equal("NIGHTS", terms.PenaltyBasis);
        Assert.Equal(1, terms.PenaltyNights);

        // And what it did not. A `Money` here would be a chargeable number this
        // connector invented from a rate it was never given.
        Assert.Null(terms.PenaltyAmount);
    }

    /// <summary>
    /// The second ratified refusal. Nothing in the study establishes that OHIP
    /// sends a rate code or name on the guarantee path; the reservation carries
    /// a rate elsewhere, and taking it from there would put a value read from
    /// one message into a fact fetched from another.
    /// </summary>
    [Fact]
    public void the_rate_is_left_empty_rather_than_taken_from_somewhere_adjacent()
    {
        var terms = CommercialTermsReading.Read(Whole());

        Assert.NotNull(terms);
        Assert.Equal(string.Empty, terms.RateCode);
        Assert.Equal(string.Empty, terms.RateName);
    }

    /// <summary>
    /// The third, and it was found reading the study rather than ruled in
    /// advance: chapter 02 has the guarantee's short description in prose
    /// (<c>02-…md:429-430</c>) and the study names no field spelling for it, so
    /// <c>OhipGuarantee</c> transcribes none. Filling it from the code would
    /// show an operator a code where a sentence belongs.
    /// </summary>
    [Fact]
    public void the_description_is_empty_because_no_source_field_is_established()
    {
        var terms = CommercialTermsReading.Read(Whole());

        Assert.NotNull(terms);
        Assert.Equal(string.Empty, terms.GuaranteeDescription);
        Assert.NotEqual(terms.GuaranteeCode, terms.GuaranteeDescription);
    }

    /// <summary>
    /// Nothing rather than an empty record — <c>AmountReading</c>'s refusal at
    /// this boundary. Terms with every field at its proto3 default are
    /// indistinguishable from a property whose policy is *no deposit, no
    /// window, no penalty*, which is a real and different arrangement.
    /// </summary>
    [Fact]
    public void a_policy_carrying_nothing_produces_no_terms()
    {
        Assert.Null(CommercialTermsReading.Read(null));

        Assert.Null(CommercialTermsReading.Read(new OhipGuarantee(
            GuaranteeCode: null,
            OnHold: false,
            ReserveInventory: false,
            DefaultGuarantee: false,
            DepositPolicy: null,
            CancellationPolicy: null)));
    }

    /// <summary>
    /// The flags cannot recognise a policy on their own, because <c>false</c>
    /// is both "OHIP said no" and "OHIP said nothing". A reader that accepted
    /// them would produce terms from a message that carried none.
    /// </summary>
    [Fact]
    public void flags_alone_are_not_a_policy()
    {
        Assert.Null(CommercialTermsReading.Read(new OhipGuarantee(
            GuaranteeCode: null,
            OnHold: true,
            ReserveInventory: true,
            DefaultGuarantee: true,
            DepositPolicy: null,
            CancellationPolicy: null)));
    }

    /// <summary>
    /// Zero is a real answer — *due on the day of booking*, *free until the day
    /// of arrival* — and proto3 scalars cannot tell it from absent. So a policy
    /// stating zero must still produce terms, which is what distinguishes this
    /// from the empty case above.
    /// </summary>
    [Fact]
    public void a_zero_offset_is_a_policy_and_not_an_absence()
    {
        var terms = CommercialTermsReading.Read(new OhipGuarantee(
            GuaranteeCode: "6PM",
            OnHold: false,
            ReserveInventory: false,
            DefaultGuarantee: false,
            DepositPolicy: new OhipDepositPolicy(OffsetFromBookingDate: 0, Amount: null),
            CancellationPolicy: new OhipCancellationPolicy(0, "18:00", null)));

        Assert.NotNull(terms);
        Assert.Equal(0, terms.DepositOffsetDaysFromBooking);
        Assert.Equal(0, terms.CancelOffsetDaysFromArrival);
        Assert.Equal("18:00", terms.CancelDropTime);
    }
}
