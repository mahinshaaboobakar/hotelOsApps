using System.Text;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Adapters;
using PmsOracle.Integrations.Cloud;
using PmsOracle.Normalisation;
using Xunit;
using static PmsOracle.Tests.TestExponent;

namespace PmsOracle.Tests;

/// <summary>
/// The three registered flavours, as the Hub sees them.
/// </summary>
/// <remarks>
/// The seams are `HotelOS.Connector`'s and the normalisers are this package's;
/// what is under test here is the wiring between them — which is where a
/// connector is most likely to be quietly wrong, because both halves work.
/// </remarks>
public sealed class AdapterTests
{
    private static readonly IntegrationSettings Cloud = Settings("oracle-cloud");
    private static readonly IntegrationSettings OnPremise = Settings("oracle-onpremise");
    private static readonly IntegrationSettings Web = Settings("oracle-web");

    // -------------------------------------------------------------------------
    // Three identifiers, never one — R28
    // -------------------------------------------------------------------------

    [Fact]
    public void The_three_flavours_are_three_identities()
    {
        // `oracle-cloud`, never `oracle`. One vendor, three integrations
        // differing in transport, credential model and vocabulary — collapsing
        // them would make every Oracle fact's provenance say which company
        // wrote the PMS rather than which system it came out of.
        Assert.Equal(
            ["oracle-cloud", "oracle-onpremise", "oracle-web"],
            new string[]
            {
                new OracleCloudAdapter(Cloud, new NoQueue(), new NoGuarantees(), new HttpClient(), MinorUnits, "oracle-cloud").IntegrationId,
                new OracleOnSiteAdapter(OnPremise, MinorUnits, "oracle-onpremise").IntegrationId,
                new OracleOnSiteAdapter(Web, MinorUnits, "oracle-web").IntegrationId,
            });
    }

    [Fact]
    public void Only_the_cloud_flavour_polls_and_only_the_on_site_ones_join()
    {
        // A type test, which is what makes "does this poll?" answerable without
        // calling it — the reason the seams are three interfaces rather than
        // one with methods most implementers throw from.
        Assert.IsAssignableFrom<IPollingConnector>(new OracleCloudAdapter(Cloud, new NoQueue(), new NoGuarantees(), new HttpClient(), MinorUnits, "oracle-cloud"));
        Assert.IsNotAssignableFrom<IJoiningConnector>(new OracleCloudAdapter(Cloud, new NoQueue(), new NoGuarantees(), new HttpClient(), MinorUnits, "oracle-cloud"));

        Assert.IsAssignableFrom<IJoiningConnector>(new OracleOnSiteAdapter(OnPremise, MinorUnits, "oracle-onpremise"));
        Assert.IsNotAssignableFrom<IPollingConnector>(new OracleOnSiteAdapter(OnPremise, MinorUnits, "oracle-onpremise"));
    }

    // -------------------------------------------------------------------------
    // Validation is structural, and refuses what it cannot place
    // -------------------------------------------------------------------------

    [Fact]
    public void An_unparseable_body_is_rejected_with_what_the_parser_said()
    {
        var result = new OracleOnSiteAdapter(OnPremise, MinorUnits, "oracle-onpremise")
            .Validate(Bytes("<html>not json</html>"), OracleOnSiteAdapter.StayPayload);

        Assert.Equal(InboxOutcome.Rejected, result.Outcome);
        Assert.Equal("body", result.Field);

        // The parser's own message, because an engineer reading the inbox queue
        // wants the position it stopped at.
        Assert.NotNull(result.RawValue);
    }

    [Fact]
    public void A_message_kind_this_connector_does_not_serve_is_rejected()
    {
        var result = new OracleOnSiteAdapter(Web, MinorUnits, "oracle-web").Validate(Bytes("{}"), "something-else");

        Assert.Equal(InboxOutcome.Rejected, result.Outcome);
        Assert.Equal("payload_kind", result.Field);
        Assert.Equal("something-else", result.RawValue);
    }

    // -------------------------------------------------------------------------
    // The dedupe promise each source can actually keep — §9.3
    // -------------------------------------------------------------------------

    [Fact]
    public void The_cloud_flavour_keys_a_notification_on_its_own_event_id()
    {
        var adapter = new OracleCloudAdapter(Cloud, new NoQueue(), new NoGuarantees(), new HttpClient(), MinorUnits, "oracle-cloud");
        var body = Bytes("""{"eventId":"evt-1","moduleName":"Reservation"}""");

        Assert.Equal(
            $"{OracleCloudAdapter.NotificationPayload}:evt-1",
            adapter.DedupeKey(body, OracleCloudAdapter.NotificationPayload));
    }

    [Fact]
    public void A_notification_with_no_id_gets_a_key_that_cannot_collide()
    {
        var adapter = new OracleCloudAdapter(Cloud, new NoQueue(), new NoGuarantees(), new HttpClient(), MinorUnits, "oracle-cloud");
        var body = Bytes("""{"moduleName":"Reservation"}""");

        var first = adapter.DedupeKey(body, OracleCloudAdapter.NotificationPayload);
        var second = adapter.DedupeKey(body, OracleCloudAdapter.NotificationPayload);

        // Malformed, and `Validate` will not catch it. The Hub still needs a
        // key, and one that cannot collide beats one that collides with every
        // other notification missing an id.
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void The_on_site_flavours_key_on_the_bytes_because_that_is_all_they_promise()
    {
        var adapter = new OracleOnSiteAdapter(OnPremise, MinorUnits, "oracle-onpremise");
        var body = Bytes("""{"status":"CHECKEDIN"}""");

        // Stable across redeliveries of the same bytes, and different for
        // different bytes — the only promise an agent sending no event id and
        // no change timestamp can keep.
        Assert.Equal(
            adapter.DedupeKey(body, OracleOnSiteAdapter.StayPayload),
            adapter.DedupeKey(body, OracleOnSiteAdapter.StayPayload));

        Assert.NotEqual(
            adapter.DedupeKey(body, OracleOnSiteAdapter.StayPayload),
            adapter.DedupeKey(Bytes("""{"status":"CHECKEDOUT"}"""),
                OracleOnSiteAdapter.StayPayload));
    }

    [Fact]
    public void The_message_kind_is_part_of_the_key()
    {
        var adapter = new OracleOnSiteAdapter(OnPremise, MinorUnits, "oracle-onpremise");
        var body = Bytes("{}");

        // Two different messages that happen to be byte-identical are two
        // facts, not a redelivery. Without the kind in the key the second would
        // be discarded as a duplicate of the first.
        Assert.NotEqual(
            adapter.DedupeKey(body, OracleOnSiteAdapter.StayPayload),
            adapter.DedupeKey(body, OracleOnSiteAdapter.RoomStatusPayload));
    }

    // -------------------------------------------------------------------------
    // A notification is provenance, not a fact
    // -------------------------------------------------------------------------

    [Fact]
    public void A_business_event_notification_produces_no_fact_and_is_not_a_failure()
    {
        var result = new OracleCloudAdapter(Cloud, new NoQueue(), new NoGuarantees(), new HttpClient(), MinorUnits, "oracle-cloud")
            .Normalise(
                Bytes("""{"eventId":"evt-1","moduleName":"Reservation"}"""),
                OracleCloudAdapter.NotificationPayload);

        // Deferred rather than rejected: nothing is wrong with it. It is stored
        // and deduplicated in its own right — the reference dropped every
        // notification whose module it did not read, which is why nobody can
        // now say what else OHIP emits.
        Assert.Equal(InboxOutcome.Deferred, result.Result.Outcome);
        Assert.Empty(result.RoomStays);
        Assert.Empty(result.RoomStates);
    }

    /// <summary>
    /// The identity an adapter reports is the one the bootstrap supplied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The chain item 1 creates, across the parts that exist</b> —
    /// <c>HOTELOS_CONNECTOR_INTEGRATION_ID</c> → <see cref="ConnectorBootstrap"/>
    /// → adapter construction → <c>IntegrationId</c>. Built from a real
    /// <c>ConnectorBootstrap</c> rather than a literal, so the adapter cannot
    /// be handed a value the bootstrap contract would refuse to carry: an
    /// empty instance yields no bootstrap at all (ADR 0330).
    /// </para>
    /// <para>
    /// <b>It fails if the value arrives empty</b>, which is the regression it
    /// exists for. <c>4518b480</c> made both adapters answer
    /// <c>string.Empty</c>, and <c>ConnectorHost</c> keys its dictionary on
    /// this — so two adapters would have collided on <c>""</c>, a guard against
    /// silent dropping firing as a false collision.
    /// </para>
    /// <para>
    /// <b>WHAT THIS DOES NOT COVER, by construction rather than omission:</b>
    /// the Hub-side consumption at <c>ConnectorHost.cs:71</c>. That is another
    /// file in another process, nothing registers an <c>IConnectorAdapter</c>
    /// into the Hub's container, and pms-oracle runs out of process — so there
    /// is no composed path in which a key from this connector reaches that
    /// dictionary. A test constructing <c>ConnectorHost</c> with one of these
    /// adapters by hand would take a path the architecture forbids and no
    /// production code takes, and would prove only that a constructor can be
    /// called.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_identity_an_adapter_reports_is_the_one_the_bootstrap_supplied()
    {
        var bootstrap = ConnectorBootstrap.From("1468", "oracle-onpremise");

        Assert.NotNull(bootstrap);

        var adapter = new OracleOnSiteAdapter(OnPremise, MinorUnits, bootstrap.IntegrationId);

        // Two assertions for two properties: that the adapter PRESERVES what
        // the bootstrap gave, and that the value is the real one rather than a
        // default both sides could share.
        Assert.Equal(bootstrap.IntegrationId, adapter.IntegrationId);
        Assert.Equal("oracle-onpremise", adapter.IntegrationId);
    }

    private static IntegrationSettings Settings(string integrationId) => new(
        PropertyId: Guid.CreateVersion7().ToString(),
        PropertyCode: "KOCHI",
        Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
        Currency: "INR",
        AmountTaxBasis: TaxBasis.Net,
        GuaranteeMaximumFreshness: null);

    private static byte[] Bytes(string body) => Encoding.UTF8.GetBytes(body);

    /// <summary>A queue that is never drained — these tests do not reach OHIP.</summary>
    private sealed class NoQueue : IOhipQueue
    {
        public Task<IReadOnlyList<PolledPayload>> DrainAsync(
            IntegrationSettings settings, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "no test here drains OHIP; the transport is a seam awaiting the Token Vault");
    }

    /// <summary>A guarantee source no test here reaches.</summary>
    /// <remarks>
    /// Throws rather than returning nothing, for the reason <c>NoQueue</c>
    /// does: a double that answered <i>no guarantees</i> would let a test pass
    /// while silently exercising the empty case, which is not the case any of
    /// these tests is about. <c>DrainAsync</c>'s own tests supply a double that
    /// answers.
    /// </remarks>
    private sealed class NoGuarantees : IOhipGuarantees
    {
        public Task<IReadOnlyList<GuaranteeRecord>> FetchAsync(
            IntegrationSettings settings,
            IReadOnlyCollection<string> arrivalDates,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "no test here fetches a guarantee; the transport is a seam "
                + "awaiting the Token Vault");
    }
}
