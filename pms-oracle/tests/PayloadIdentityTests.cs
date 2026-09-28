using System.Text;
using PmsOracle.Adapters;
using PmsOracle.Integrations;
using PmsOracle.Integrations.Cloud;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The key a payload is deduplicated on: one home, and a pure function.
/// </summary>
/// <remarks>
/// <b>The purity tests are the ones written from the defect.</b> The drain's
/// keying returned a fresh <c>Guid.CreateVersion7()</c> for an event carrying
/// no identifier, so the same bytes keyed differently on every call — and the
/// protocol calls <c>dedupe_key</c> a pure function of the bytes precisely
/// because a RETRY must not produce a second fact. Nothing in the suite
/// covered that branch, which is why it survived: 256 tests passed both before
/// and after the behaviour changed.
/// </remarks>
public sealed class PayloadIdentityTests
{
    private const string Event = OhipBusinessEventQueue.EventPayload;

    private static byte[] Bytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void The_same_bytes_key_the_same_way_every_time()
    {
        // No `businessEventId`, which is the branch that used to mint an
        // identifier. Two calls, one payload.
        var payload = Bytes("""{"moduleName":"RESERVATION"}""");

        Assert.Equal(
            PayloadIdentity.For(Event, payload),
            PayloadIdentity.For(Event, payload));
    }

    [Fact]
    public void Two_id_less_payloads_with_different_bytes_do_not_collide()
    {
        // The half the identifier got right, and a digest keeps it: an
        // unkeyable payload must not collide with every other unkeyable one.
        Assert.NotEqual(
            PayloadIdentity.For(Event, Bytes("""{"moduleName":"RESERVATION"}""")),
            PayloadIdentity.For(Event, Bytes("""{"moduleName":"HOUSEKEEPING"}""")));
    }

    [Fact]
    public void A_business_event_is_keyed_on_the_identifier_the_source_sent()
    {
        // OHIP's nested shape — `businessEventId.id`. The flat
        // `BusinessEventNotification` beside it reads null on a real item, and
        // a key built from that would be stable across nothing.
        var payload = Bytes("""{"businessEventId":{"id":"BE-4471"},"moduleName":"RESERVATION"}""");

        Assert.Equal($"{Event}:BE-4471", PayloadIdentity.For(Event, payload));
    }

    [Fact]
    public void A_guarantee_is_keyed_on_the_facts_identity_and_never_a_digest()
    {
        // ADR 0147: property, arrival date, integration. A digest would make
        // two fetches of an unchanged policy two facts, and a policy the
        // property edited a third, with nothing saying which is current.
        var payload = Bytes(
            """
            {"integrationId":"oracle-cloud","propertyCode":"KOCHI01",
             "arrivalDate":"2026-09-28","observedAt":"2026-09-28T04:00:00Z",
             "guarantee":{}}
            """);

        Assert.Equal(
            $"{OracleCloudAdapter.GuaranteePayload}:oracle-cloud:KOCHI01:2026-09-28",
            PayloadIdentity.For(OracleCloudAdapter.GuaranteePayload, payload));
    }

    [Fact]
    public void A_payload_whose_bytes_will_not_parse_is_still_keyed()
    {
        // An unkeyable payload is quarantined rather than discarded (ADR 0248),
        // and quarantine needs a key. A parse failure is not an empty answer.
        var key = PayloadIdentity.For(Event, Bytes("{ this is not json"));

        Assert.StartsWith($"{Event}:", key, StringComparison.Ordinal);
        Assert.NotEqual($"{Event}:", key);
    }

    [Fact]
    public void Every_key_carries_its_kind_so_two_keys_cannot_differ_only_in_shape()
    {
        var payload = Bytes("""{"roomNo":"402"}""");

        Assert.StartsWith(
            $"{OracleOnSiteAdapter.StayPayload}:",
            PayloadIdentity.For(OracleOnSiteAdapter.StayPayload, payload),
            StringComparison.Ordinal);
    }

    [Fact]
    public void One_payload_under_two_kinds_is_two_facts()
    {
        // The prefix is what makes that true, and it is why the kind is part of
        // the key rather than beside it.
        var payload = Bytes("""{"roomNo":"402"}""");

        Assert.NotEqual(
            PayloadIdentity.For(OracleOnSiteAdapter.StayPayload, payload),
            PayloadIdentity.For(OracleOnSiteAdapter.RoomStatusPayload, payload));
    }
}
