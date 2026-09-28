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
                Assert.Contains("no ruled spelling", refusal.Message, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void A_reason_with_no_ruled_spelling_refuses_rather_than_inventing_one()
    {
        // `IntegrationNotConfigured` is in the connector's enum and is produced
        // by nothing. ADR 0288 makes the vocabulary platform-defined and
        // extensible only by a ruling, so translating it would be a connector
        // minting a platform category.
        var refusal = Assert.Throws<NotSupportedException>(() => NormalizeReply.From(
            new NormalisationOutcome.Rejected(
                RejectionReason.IntegrationNotConfigured, "taxBasis", null)));

        Assert.Contains("no ruled spelling", refusal.Message, StringComparison.Ordinal);
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
    public void An_unresolved_prerequisite_refuses_because_it_has_no_arm()
    {
        // CONN-Q84, open. The source is fine and our reference data is empty,
        // so a rejection would blame the hotel's PMS for a gap of ours — and
        // facts-with-no-amount is the collapse CONN-Q75 was ruled to prevent.
        // Picking either would be a contract decision made by a connector.
        var refusal = Assert.Throws<NotSupportedException>(() => NormalizeReply.From(
            new NormalisationOutcome.Unresolved("minor_unit_digits", "Amount")));

        Assert.Contains("CONN-Q84", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("minor_unit_digits", refusal.Message, StringComparison.Ordinal);
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
