namespace PmsOracle.Integrations.Cloud;

/// <summary>
/// What a guarantee policy costs, as OHIP states it.
/// </summary>
/// <param name="BasisType">OHIP's own code for how the penalty is expressed.</param>
/// <param name="Nights">A penalty expressed as a number of nights.</param>
/// <param name="CurrencyCode">The currency the policy is denominated in.</param>
/// <remarks>
/// <para>
/// <b>There is no amount here, and its absence is the point.</b> The study's
/// citation for this block names exactly three fields — <c>basisType</c>,
/// <c>nights</c>, <c>currencyCode</c>
/// (<c>cloud/models/OracleCloudReservationGuarantees.java:85-92</c>) — and no
/// monetary value among them.
/// </para>
/// <para>
/// So <c>CommercialTerms.penalty_amount</c> stays absent rather than being
/// computed: <b>a penalty stated in nights cannot become money without a rate
/// this fact does not carry</b>, which is the contract's own comment at the
/// field. Multiplying <paramref name="Nights"/> by a nightly rate taken from
/// the reservation would produce a number a hotel could charge against, derived
/// here, indistinguishable once written from one Oracle sent.
/// </para>
/// <para>
/// If OHIP does carry an amount on this block, the study did not establish it
/// and the reference never read it — the two pre-formatted strings it kept
/// (<c>cloud/services/impl/OracleCloudReservationServiceImpl.java:243-248</c>)
/// discarded the structure entirely. Vendor documentation or a live call
/// settles it, and the field is added here when one does.
/// </para>
/// </remarks>
public sealed record OhipPolicyAmount(
    string? BasisType,
    int? Nights,
    string? CurrencyCode);

/// <summary>The deposit half of a guarantee, as OHIP states it.</summary>
/// <param name="OffsetFromBookingDate">
/// Days after the booking was made that the deposit falls due —
/// <c>:50-52</c>.
/// </param>
/// <param name="Amount">What the deposit is, in OHIP's terms.</param>
/// <remarks>
/// <b>An offset, and it stays one</b> (R18). The reference resolved both
/// deadlines to formatted strings at fetch time, so a reservation whose booking
/// or arrival date later moved carried a deadline that had silently stopped
/// matching it — a chargeable error, since this is the date money is owed on.
/// </remarks>
public sealed record OhipDepositPolicy(
    int? OffsetFromBookingDate,
    OhipPolicyAmount? Amount);

/// <summary>The cancellation half of a guarantee, as OHIP states it.</summary>
/// <param name="OffsetFromArrival">
/// Days before arrival that free cancellation ends — <c>:75-79</c>.
/// </param>
/// <param name="OffsetDropTime">
/// The time of day that window closes, paired with the offset above. OHIP
/// supplies it separately because a policy is *days before arrival, at a
/// time*, and the two are not one value.
/// </param>
/// <param name="Amount">What cancelling costs, in OHIP's terms.</param>
public sealed record OhipCancellationPolicy(
    int? OffsetFromArrival,
    string? OffsetDropTime,
    OhipPolicyAmount? Amount);

/// <summary>
/// One guarantee policy, as OHIP returns it for a property and an arrival date.
/// </summary>
/// <param name="GuaranteeCode">OHIP's code for this policy — <c>:13-27</c>.</param>
/// <param name="OnHold">Whether the booking is held rather than confirmed.</param>
/// <param name="ReserveInventory">Whether it holds inventory.</param>
/// <param name="DefaultGuarantee">Whether it is the property's default arrangement.</param>
/// <param name="DepositPolicy">When a deposit is due, and what it is.</param>
/// <param name="CancellationPolicy">When free cancellation ends, and what it costs.</param>
/// <remarks>
/// <para>
/// <b>OHIP's spellings, unchanged.</b> A connector transcribes its source under
/// the source's own names and maps onto the platform's at the boundary
/// (CLAUDE.md §*No old application names in code*) — renaming here would make
/// a rejection quoting <c>"reserveInventory"</c> lie to the operator reading
/// it. <c>CommercialTermsReading</c> is the boundary; nothing above it renames.
/// </para>
/// <para>
/// <b>This is not a field on a reservation, and that is why R18 needed a
/// round of its own.</b> The policy is fetched per property and arrival date
/// (<c>cloud/services/impl/OracleCloudGuaranteeServiceImpl.java:58-59</c>), and
/// only for reservations still in <c>Reserved</c> (<c>cloud:112-113</c>) — so
/// <b>one policy serves many reservations</b>. That is what ADR 0147 rules a
/// Hub-held source fact rather than a join, and it is why this type exists
/// beside <see cref="OhipReservation"/> rather than inside it.
/// </para>
/// <para>
/// <b>The short description has no field here, and that is a refusal rather
/// than an omission.</b> <c>CommercialTerms.guarantee_description</c> exists on
/// the wire, and the study establishes a code and flags at <c>:13-27</c>
/// without naming a spelling for the description — chapter 02 has it in prose
/// only (<c>02-…md:429-430</c>). Transcribing a guess would put a field name
/// nobody has read into the one file whose whole job is to say what the vendor
/// sends. It is the same refusal as the amount above, from the same cause: the
/// reference kept formatted strings and discarded the structure, so the
/// structure has to come from the vendor rather than from the reference.
/// </para>
/// </remarks>
public sealed record OhipGuarantee(
    string? GuaranteeCode,
    bool OnHold,
    bool ReserveInventory,
    bool DefaultGuarantee,
    OhipDepositPolicy? DepositPolicy,
    OhipCancellationPolicy? CancellationPolicy);
