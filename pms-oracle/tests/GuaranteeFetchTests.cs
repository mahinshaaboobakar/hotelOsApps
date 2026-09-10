using System.Text;
using System.Text.Json;
using HotelOS.Connector;
using PmsOracle.Adapters;
using PmsOracle.Integrations.Cloud;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Normalisation;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The drain-side guarantee fetch — R18, ADR 0147, ADR 0150.
/// </summary>
/// <remarks>
/// <para>
/// Its own file rather than more cases in <c>AdapterTests</c>: this is a
/// subject — *how one policy is fetched for many reservations, and what
/// identifies it* — and it needs doubles that answer where that file's
/// deliberately throw.
/// </para>
/// <para>
/// <b>The first test is the reference's defect, asserted as unreachable.</b>
/// It cached per <c>hotelId</c> while querying per arrival date
/// (<c>OracleCloudGuaranteeServiceImpl.java:36-39</c>, <c>:52-55</c>,
/// <c>:63</c>), so one arrival date's policy was served to every reservation at
/// that property for an hour. A connector written to avoid it should be able to
/// prove it cannot reproduce it.
/// </para>
/// </remarks>
public sealed class GuaranteeFetchTests
{
    private static readonly Guid Property = Guid.Parse("0192f100-0000-7000-8000-000000000002");

    private static IntegrationSettings Settings() => new(
        IntegrationId: "oracle-cloud",
        PropertyId: Property.ToString(),
        PropertyCode: "KOCHI01",
        Clock: PropertyClock.For("Asia/Kolkata", new TimeOnly(14, 0), new TimeOnly(12, 0))!,
        Currency: "INR",
        AmountTaxBasis: TaxBasis.Net,
        GuaranteeMaximumFreshness: null);

    private static byte[] Reservation(string status, string arrival) =>
        Encoding.UTF8.GetBytes($$"""
            {
              "reservationIdList": [ { "id": "R-1", "type": "Reservation" } ],
              "reservationStatus": "{{status}}",
              "roomStay": { "arrivalDate": "{{arrival}}", "departureDate": "2026-10-04" },
              "reservationGuests": []
            }
            """);

    private static OracleCloudAdapter Adapter(Queue queue, IOhipGuarantees guarantees) =>
        new(Settings(), queue, guarantees, new HttpClient());

    [Fact]
    public async Task the_arrival_date_it_queried_by_is_part_of_the_key()
    {
        var queue = new Queue([
            new PolledPayload(Property, Reservation("Reserved", "2026-10-01"),
                OracleCloudAdapter.ReservationPayload),
            new PolledPayload(Property, Reservation("Reserved", "2026-10-02"),
                OracleCloudAdapter.ReservationPayload),
        ]);

        var guarantees = new Guarantees();
        var adapter = Adapter(queue, guarantees);

        var drained = await adapter.DrainAsync(default);

        var keys = drained
            .Where(p => p.PayloadKind == OracleCloudAdapter.GuaranteePayload)
            .Select(p => adapter.DedupeKey(p.Payload, p.PayloadKind))
            .ToList();

        // **Two dates, two facts.** Under the reference's key both would be one,
        // and the second date's reservations would be told the first date's
        // cancellation policy.
        Assert.Equal(2, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(keys, key => Assert.Contains("KOCHI01", key, StringComparison.Ordinal));
        Assert.Contains(keys, key => key.Contains("2026-10-01", StringComparison.Ordinal));
        Assert.Contains(keys, key => key.Contains("2026-10-02", StringComparison.Ordinal));

        // And the integration, because the same property can be configured
        // against more than one flavour and a cloud policy is not an on-site
        // one — ADR 0147's third key segment (R28).
        Assert.All(keys, key => Assert.Contains("oracle-cloud", key, StringComparison.Ordinal));
    }

    [Fact]
    public async Task one_policy_is_fetched_per_date_and_not_per_reservation()
    {
        var queue = new Queue([
            new PolledPayload(Property, Reservation("Reserved", "2026-10-01"),
                OracleCloudAdapter.ReservationPayload),
            new PolledPayload(Property, Reservation("Reserved", "2026-10-01"),
                OracleCloudAdapter.ReservationPayload),
            new PolledPayload(Property, Reservation("Reserved", "2026-10-01"),
                OracleCloudAdapter.ReservationPayload),
        ]);

        var guarantees = new Guarantees();

        await Adapter(queue, guarantees).DrainAsync(default);

        // The one-to-many shape, kept between the query and the answer. This is
        // also what ADR 0147 refuses to model as a join: one part serving three
        // reservations is not a pairing.
        Assert.Equal(["2026-10-01"], guarantees.Asked);
    }

    /// <summary>
    /// The source's own rule (<c>cloud:112-113</c>), not a saving invented
    /// here: a guarantee is what a booking is held on, and a stay already in
    /// house is not held on anything.
    /// </summary>
    [Fact]
    public async Task only_a_reservation_still_reserved_needs_a_policy()
    {
        var queue = new Queue([
            new PolledPayload(Property, Reservation("InHouse", "2026-10-01"),
                OracleCloudAdapter.ReservationPayload),
            new PolledPayload(Property, Reservation("CheckedOut", "2026-10-02"),
                OracleCloudAdapter.ReservationPayload),
            new PolledPayload(Property, Reservation("Reserved", "2026-10-03"),
                OracleCloudAdapter.ReservationPayload),
        ]);

        var guarantees = new Guarantees();

        await Adapter(queue, guarantees).DrainAsync(default);

        Assert.Equal(["2026-10-03"], guarantees.Asked);
    }

    /// <summary>
    /// <b>R22, and the reason the catch in <c>DrainAsync</c> is broad.</b> The
    /// read is the delete: an exception thrown after the queue has been drained
    /// and before the payloads are returned loses a hotel's changes
    /// permanently, to fetch a policy. Asserted rather than commented, because
    /// a comment saying "this cannot happen" is the specification nobody tests.
    /// </summary>
    [Fact]
    public async Task a_failed_guarantee_fetch_never_discards_what_the_queue_gave_up()
    {
        var queue = new Queue([
            new PolledPayload(Property, Reservation("Reserved", "2026-10-01"),
                OracleCloudAdapter.ReservationPayload),
        ]);

        var drained = await Adapter(queue, new Throws()).DrainAsync(default);

        Assert.Single(drained);
        Assert.Equal(OracleCloudAdapter.ReservationPayload, drained[0].PayloadKind);
    }

    /// <summary>
    /// A guarantee is a source fact in its own right, and produces no
    /// <c>RoomStayFact</c> here — ADR 0147 puts its keyed lookup with the Hub
    /// and has enrichment apply it. Deferred rather than rejected, because
    /// nothing is wrong with it, and with its own sentence rather than the
    /// notification's, because the two are deferred for different reasons.
    /// </summary>
    [Fact]
    public void a_guarantee_validates_and_produces_no_fact_of_its_own()
    {
        var adapter = Adapter(new Queue([]), new Guarantees());
        var payload = Encoding.UTF8.GetBytes("""{"integrationId":"oracle-cloud"}""");

        Assert.Equal(
            InboxOutcome.Pending,
            adapter.Validate(payload, OracleCloudAdapter.GuaranteePayload).Outcome);

        var normalised = adapter.Normalise(payload, OracleCloudAdapter.GuaranteePayload);

        Assert.Empty(normalised.RoomStays);
        Assert.Empty(normalised.RoomStates);
        Assert.Equal(InboxOutcome.Deferred, normalised.Result.Outcome);
        Assert.Contains("enrichment", normalised.Result.Reason, StringComparison.Ordinal);
    }

    /// <summary>A queue holding exactly what it is given.</summary>
    private sealed class Queue(IReadOnlyList<PolledPayload> holding) : IOhipQueue
    {
        public Task<IReadOnlyList<PolledPayload>> DrainAsync(
            IntegrationSettings settings, CancellationToken cancellationToken) =>
            Task.FromResult(holding);
    }

    /// <summary>A guarantee source that answers, and records what it was asked.</summary>
    private sealed class Guarantees : IOhipGuarantees
    {
        public List<string> Asked { get; } = [];

        public Task<IReadOnlyList<GuaranteeRecord>> FetchAsync(
            IntegrationSettings settings,
            IReadOnlyCollection<string> arrivalDates,
            CancellationToken cancellationToken)
        {
            Asked.AddRange(arrivalDates);

            IReadOnlyList<GuaranteeRecord> records = [.. arrivalDates.Select(date =>
                new GuaranteeRecord(
                    settings.IntegrationId,
                    settings.PropertyCode,
                    date,
                    DateTimeOffset.UnixEpoch,
                    new OhipGuarantee("CC", false, true, true, null, null)))];

            return Task.FromResult(records);
        }
    }

    /// <summary>A guarantee source whose endpoint is unreachable.</summary>
    private sealed class Throws : IOhipGuarantees
    {
        public Task<IReadOnlyList<GuaranteeRecord>> FetchAsync(
            IntegrationSettings settings,
            IReadOnlyCollection<string> arrivalDates,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("OHIP did not answer");
    }
}
