using HotelOS.Contracts.Integration.V1;
using PmsOracle.Hosting;
using PmsOracle.Normalisation;
using PmsOracle.Vocabularies;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// What a normalisation outcome becomes on the wire — ADR 0288, ADR 0295.
/// </summary>
/// <remarks>
/// <b>The spellings are written out here rather than read from the mapping.</b>
/// A test that asserts against the same constant the code under test uses is a
/// tautology: it would pass if every reason were spelled <c>BANANA</c>, so long
/// as both sides agreed. These four are ADR 0295's, as page 75 §1 writes them,
/// and this file is where a change to one has to be argued with.
/// </remarks>
public sealed class NormalizeReplyTests
{
    [Theory]
    [InlineData(RejectionReason.MissingRequiredField, "MISSING_REQUIRED_FIELD")]
    [InlineData(RejectionReason.UnreadableValue, "UNREADABLE_VALUE")]
    [InlineData(RejectionReason.UnknownStatus, "UNKNOWN_STATUS")]
    [InlineData(RejectionReason.PropertyMismatch, "PROPERTY_MISMATCH")]
    public void Each_ruled_reason_is_spelled_as_the_platform_writes_it(
        RejectionReason reason, string spelling)
    {
        var reply = NormalizeReply.From(
            new NormalisationOutcome.Rejected(reason, "housekeepingStatus", "Occupied"));

        Assert.Equal(NormalizeResult.OutcomeOneofCase.Rejection, reply.OutcomeCase);
        Assert.Equal(spelling, reply.Rejection.Reason);
    }

    [Fact]
    public void Every_reason_the_connector_can_produce_is_spelled_or_refused()
    {
        // **The walk, so a member added later cannot send an empty reason.**
        // An empty reason is a MALFORMED envelope rather than a rejection
        // (`CONN-Q62`), which the Hub retries forever — so the finding never
        // reaches the operator and the payload never lands. A silent default
        // here would be the quietest possible failure.
        foreach (var reason in Enum.GetValues<RejectionReason>())
        {
            var outcome = new NormalisationOutcome.Rejected(reason, "field", null);

            try
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(NormalizeReply.From(outcome).Rejection.Reason),
                    $"{reason} produced an empty reason");
            }
            catch (NotSupportedException refusal)
            {
                // **The walk asserts SPELLED-OR-REFUSED and not the ground.**
                // It pinned "no ruled spelling" until `CONN-Q84(b)` gave the
                // configuration state a spelling and ADR 0316 gave its refusal
                // a different reason — so the walk started failing on a member
                // it was never about. Which ground governs is the two dedicated
                // tests' subject; a walk that also asserts the reason fails
                // whenever a reason legitimately changes.
                Assert.False(
                    string.IsNullOrWhiteSpace(refusal.Message),
                    $"{reason} refused without saying why");
            }
        }
    }

    [Fact]
    public void The_configuration_state_is_spelled_and_is_still_not_a_connectors_to_send()
    {
        // **The refusal survived its own justification.** It used to read "no
        // ruled spelling", which `CONN-Q84(b)` made false by spelling it
        // INTEGRATION_NOT_CONFIGURED. It still refuses, on ADR 0316's ground:
        // the Hub reports WAITING and does not invoke the connector, so this
        // end sending it would be answering for a decision nobody asked it to
        // make. Two tests rather than one, because a single assertion would
        // pass under either reason and say nothing about which governs.
        var refusal = Assert.Throws<NotSupportedException>(() => NormalizeReply.From(
            new NormalisationOutcome.Rejected(
                RejectionReason.IntegrationNotConfigured, "amountTaxBasis", null)));

        Assert.Contains("ADR 0316", refusal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("no ruled spelling", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_refusal_with_no_source_value_leaves_raw_value_absent()
    {
        // ADR 0295's one worded distinction: UNREADABLE_VALUE is a value that
        // EXISTS and cannot be interpreted; a missing field has no such value.
        // Absent, rather than an empty string standing in for one.
        var reply = NormalizeReply.From(new NormalisationOutcome.Rejected(
            RejectionReason.MissingRequiredField, "ReservationId", null));

        Assert.False(reply.Rejection.HasRawValue);
    }

    [Fact]
    public void A_refusal_carrying_a_source_value_sends_it_whole()
    {
        var reply = NormalizeReply.From(new NormalisationOutcome.Rejected(
            RejectionReason.UnreadableValue, "arrivalDate", "31/08/2026"));

        Assert.True(reply.Rejection.HasRawValue);
        Assert.Equal("31/08/2026", reply.Rejection.RawValue);
    }

    [Fact]
    public void A_platform_prerequisite_travels_on_its_own_arm()
    {
        // CONN-Q84 ruled, ADR 0332/0333. This used to throw, because
        // `NormalizeResult` had two arms and this outcome is neither: the
        // source is fine and our reference data is empty, so a rejection would
        // blame the hotel's PMS for a gap of ours, and facts-with-no-amount is
        // the collapse CONN-Q75 was ruled to prevent.
        var reply = NormalizeReply.From(
            new NormalisationOutcome.Unresolved("minor_unit_digits", "Amount"));

        Assert.Equal(NormalizeResult.OutcomeOneofCase.Unresolved, reply.OutcomeCase);
        Assert.Equal("minor_unit_digits", reply.Unresolved.Prerequisite);
    }

    [Fact]
    public void The_source_field_is_not_put_on_that_arm()
    {
        // `prerequisite` is the WHOLE message by design: it names a platform
        // prerequisite, never a source field, "and must not be carried in
        // `Rejection.field` or `raw_value`". So `Unresolved.Field` — "Amount"
        // — stays connector-local, and a later author adding it to the wire
        // has to argue with this.
        var reply = NormalizeReply.From(
            new NormalisationOutcome.Unresolved("minor_unit_digits", "Amount"));

        Assert.DoesNotContain("Amount", reply.Unresolved.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_configuration_prerequisite_has_no_place_on_that_arm()
    {
        // **The arm must not become a generic escape hatch.** ADR 0316 splits
        // by OWNERSHIP: a platform fact the Hub cannot resolve travels on the
        // arm above; configuration a property owes makes the Hub withhold the
        // dispatch (ADR 0321). A connector answering here would be
        // rediscovering a prerequisite the Hub was required to gate.
        //
        // Reachable today only because the arm that produces this is HELD
        // behind the Hub's withholding going live — so this is what stops it
        // reaching the wire in the meantime.
        var refusal = Assert.Throws<NotSupportedException>(() => NormalizeReply.From(
            new NormalisationOutcome.Unresolved(
                IntegrationSettings.TaxBasisSetting, "Amount")));

        Assert.Contains("ADR 0316", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("withholds", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_half_of_a_check_in_has_no_arm_because_the_hub_assembles_the_parts()
    {
        // "Assembling the parts is normalising them" — the Hub sends the
        // assembled set, so waiting for a partner is its inbox state and can
        // never be an answer to `normalize`.
        var refusal = Assert.Throws<NotSupportedException>(() => NormalizeReply.From(
            new NormalisationOutcome.AwaitingJoin(
                OnSiteMessagePart.ContactHalf,
                new OnSiteJoinKey("Menon", "Asha", new DateOnly(2026, 9, 28)))));

        Assert.Contains("assembled set", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_normalised_stay_travels_as_facts()
    {
        var reply = NormalizeReply.From(
            new NormalisationOutcome.StayNormalised(new RoomStayFact()));

        Assert.Equal(NormalizeResult.OutcomeOneofCase.Facts, reply.OutcomeCase);
        Assert.Single(reply.Facts.RoomStays);
        Assert.Empty(reply.Facts.RoomStates);
    }

    [Fact]
    public void A_normalised_room_state_travels_as_facts()
    {
        var reply = NormalizeReply.From(
            new NormalisationOutcome.RoomStateNormalised(new RoomStateFact()));

        Assert.Equal(NormalizeResult.OutcomeOneofCase.Facts, reply.OutcomeCase);
        Assert.Single(reply.Facts.RoomStates);
        Assert.Empty(reply.Facts.RoomStays);
    }
}
