using System.Net;
using System.Text;
using System.Text.Json;
using PmsOracle.Authentication;
using PmsOracle.Integrations.Cloud;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// Taking OHIP's business-event queue, as the reference describes it.
/// </summary>
/// <remarks>
/// <para>
/// <c>providers/oracle/cloud/services/impl/OracleCloudEventServiceImpl.java:44-84</c>
/// — <c>GET …/businessEvents?limit=20</c>, <c>200</c> carrying
/// <c>businessEventData[]</c>, <c>204</c> meaning the queue is empty — with
/// <c>x-hotelid</c>, <c>x-app-key</c> and a bearer token
/// (<c>…/services/OracleCloudBaseService.java:83-86</c>).
/// </para>
/// <para>
/// <b>Reading empties it.</b> Every assertion below is really about the same
/// property: what this call returns is the only copy that will ever exist, so
/// nothing may be dropped, reordered out of the source's order, or lost to a
/// failure that happened after it was taken.
/// </para>
/// </remarks>
public class OhipBusinessEventQueueTests
{
    [Fact]
    public async Task An_empty_queue_is_no_payloads_and_one_call()
    {
        var ohip = new Ohip(Page.Empty());

        var taken = await DrainAsync(ohip);

        // 204 is OHIP's own word for empty, and the drain ends on it rather
        // than on a count this connector chose.
        Assert.Empty(taken);
        Assert.Equal(1, ohip.Calls);
    }

    [Fact]
    public async Task Every_item_is_returned_as_its_own_payload_in_the_order_ohip_gave_them()
    {
        var ohip = new Ohip(Page.Of("first", "second", "third"));

        var taken = await DrainAsync(ohip);

        Assert.Equal(3, taken.Count);
        Assert.All(taken, one => Assert.Equal("ohip-business-event", one.PayloadKind));

        // Each item on its own, so the Hub stores one fact per record — and
        // each carries OHIP's own field names, because a rejection an operator
        // reads must quote the spelling the source actually sent.
        Assert.Equal(
            ["first", "second", "third"],
            taken.Select(one => JsonDocument.Parse(one.Payload)
                .RootElement.GetProperty("businessEventId").GetProperty("id").GetString()));
    }

    [Fact]
    public async Task A_full_page_is_followed_by_another_call_and_a_short_one_ends_it()
    {
        var full = Enumerable.Range(1, OhipBusinessEventQueue.PageSize).Select(n => $"e{n}").ToArray();
        var ohip = new Ohip(Page.Of(full), Page.Of("last"));

        var taken = await DrainAsync(ohip);

        // Twenty is what OHIP was asked for, so more may be waiting; nineteen
        // or fewer means the queue is drained and a further call would ask a
        // question whose answer is already known.
        Assert.Equal(OhipBusinessEventQueue.PageSize + 1, taken.Count);
        Assert.Equal(2, ohip.Calls);
    }

    [Fact]
    public async Task The_headers_ohip_requires_are_on_every_call()
    {
        var full = Enumerable.Range(1, OhipBusinessEventQueue.PageSize).Select(n => $"e{n}").ToArray();
        var ohip = new Ohip(Page.Of(full), Page.Empty());

        await DrainAsync(ohip);

        Assert.Equal(2, ohip.Calls);
        Assert.All(ohip.Sent, sent =>
        {
            Assert.Equal("KOCHI01", sent.Headers.GetValues(OhipRequestHeaders.HotelId).Single());
            Assert.Equal("key", sent.Headers.GetValues(OhipRequestHeaders.ApplicationKey).Single());
            Assert.Equal("Bearer ohip-token", sent.Headers.GetValues("Authorization").Single());
        });

        // The address is the reference's, hotel and external system in the path.
        Assert.Contains(
            "/int/v1/externalSystem/HOTELOS/hotels/KOCHI01/businessEvents?limit=20",
            ohip.Sent[0].RequestUri!.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failure_before_anything_is_taken_is_raised_rather_than_swallowed()
    {
        var ohip = new Ohip(Page.Failing());

        // Nothing left the source, so there is nothing to lose by throwing —
        // and the Hub is owed the difference between "nothing waiting" and
        // "could not ask" (ADR 0194).
        await Assert.ThrowsAsync<HttpRequestException>(() => DrainAsync(ohip));
    }

    /// <summary>
    /// Interim, pending the ruling: a failure AFTER items were taken returns
    /// them rather than losing them.
    /// </summary>
    /// <remarks>
    /// <c>DrainResult</c> has one field, so "took some, then failed" has no
    /// shape. Faulting would destroy facts that have already left OHIP and can
    /// never be re-read; replying hides that the drain stopped early. This keeps
    /// the data, and a persistent failure surfaces on the next drain's first
    /// page — where nothing has been taken and the fault is safe. If the ruling
    /// goes the other way, this test is where it lands.
    /// </remarks>
    [Fact]
    public async Task A_failure_after_a_page_returns_what_was_already_taken()
    {
        var full = Enumerable.Range(1, OhipBusinessEventQueue.PageSize).Select(n => $"e{n}").ToArray();
        var ohip = new Ohip(Page.Of(full), Page.Failing());

        var taken = await DrainAsync(ohip);

        Assert.Equal(OhipBusinessEventQueue.PageSize, taken.Count);
        Assert.Equal(2, ohip.Calls);
    }

    [Fact]
    public async Task Each_item_is_keyed_by_the_event_id_ohip_gave_it()
    {
        var taken = await DrainAsync(new Ohip(Page.Of("first", "second")));

        // OHIP's own identity, not a digest: `OracleCloudEventServiceImpl.java:61`
        // reads `businessEventId.id` out of this same response.
        Assert.Equal(
            ["ohip-business-event:first", "ohip-business-event:second"],
            taken.Select(one => one.DedupeKey));
    }

    [Fact]
    public async Task The_same_event_delivered_twice_carries_the_same_key()
    {
        var first = await DrainAsync(new Ohip(Page.Of("evt-1")));
        var again = await DrainAsync(new Ohip(Page.Of("evt-1")));

        // The whole point of the field — ADR 0246. Two drains are two separate
        // reads of a destructive queue, so the only thing that can recognise a
        // redelivery is a key the source's own identity produced.
        Assert.Equal(first[0].DedupeKey, again[0].DedupeKey);
    }

    [Fact]
    public async Task An_item_with_no_event_id_is_keyed_uniquely_rather_than_identically()
    {
        var taken = await DrainAsync(new Ohip(Page.Unidentified(2)));

        // Two malformed items are two facts, not one. A shared key — or an
        // empty one — would deduplicate unrelated changes into each other,
        // which for a queue that cannot be re-read destroys them.
        Assert.Equal(2, taken.Count);
        Assert.NotEqual(taken[0].DedupeKey, taken[1].DedupeKey);
        Assert.All(taken, one => Assert.StartsWith(
            "ohip-business-event:", one.DedupeKey, StringComparison.Ordinal));
    }

    [Fact]
    public async Task No_drained_item_ever_carries_an_empty_key()
    {
        var full = Enumerable.Range(1, OhipBusinessEventQueue.PageSize).Select(n => $"e{n}").ToArray();
        var taken = await DrainAsync(new Ohip(Page.Of(full), Page.Unidentified(1)));

        // An empty string is the connector not having said, and the Hub is
        // required to treat it as no key at all rather than as a key that
        // happens to be blank — so there is no payload this connector may emit
        // without one.
        Assert.NotEmpty(taken);
        Assert.All(taken, one => Assert.False(string.IsNullOrWhiteSpace(one.DedupeKey)));
    }

    private static async Task<IReadOnlyList<DrainedEvent>> DrainAsync(Ohip ohip)
    {
        using var http = new HttpClient(ohip) { Timeout = TimeSpan.FromSeconds(5) };

        Assert.True(OhipCredentials.Read(Settings(), Secrets()).TryGet(out var credentials));

        return await OhipBusinessEventQueue.DrainAsync(
            http, credentials, "ohip-token", CancellationToken.None);
    }

    private static Dictionary<string, string> Settings() => new(StringComparer.Ordinal)
    {
        [OhipCredentials.EndpointSetting] = "https://ohip.example",
        [OhipCredentials.HotelCodeSetting] = "KOCHI01",
        [OhipCredentials.ExternalSystemCodeSetting] = "HOTELOS",
        [OhipCredentials.ClientIdSetting] = "client",
        [OhipCredentials.PmsUsernameSetting] = "supervisor",
    };

    private static Dictionary<string, string> Secrets() => new(StringComparer.Ordinal)
    {
        [OhipCredentials.ApplicationKeySecret] = "key",
        [OhipCredentials.PmsPasswordSecret] = "password",
        [OhipCredentials.ClientSecretSecret] = "secret",
    };

    /// <summary>One answer OHIP gives to a queue read.</summary>
    private sealed record Page(HttpStatusCode Status, string? Body, bool Throws)
    {
        public static Page Empty() => new(HttpStatusCode.NoContent, null, false);

        public static Page Failing() => new(HttpStatusCode.OK, null, true);

        /// <summary>A page whose items carry no <c>businessEventId</c> at all.</summary>
        /// <remarks>
        /// Not a hypothetical: the reference reads that id without checking it
        /// is there (<c>OracleCloudEventServiceImpl.java:61</c>), so an item
        /// shaped this way is one the source can send and nothing upstream
        /// refuses.
        /// </remarks>
        public static Page Unidentified(int count) => new(
            HttpStatusCode.OK,
            "{\"businessEventData\":["
            + string.Join(",", Enumerable.Range(1, count).Select(n =>
                "{\"businessEvent\":{\"header\":{\"moduleName\":\"Reservation\","
                + "\"primaryKey\":\"pk-" + n + "\"}}}"))
            + "]}",
            false);

        /// <summary>A page in OHIP's own shape, ids only — the rest is opaque here.</summary>
        public static Page Of(params string[] ids) => new(
            HttpStatusCode.OK,
            "{\"businessEventData\":[" + string.Join(",", ids.Select(Item)) + "]}",
            false);

        /// <summary>One queue item, spelled as OHIP spells it.</summary>
        private static string Item(string id) =>
            "{\"businessEventId\":{\"id\":\"" + id + "\"},"
            + "\"businessEvent\":{\"header\":{"
            + "\"moduleName\":\"Reservation\",\"actionType\":\"Create\","
            + "\"primaryKey\":\"" + id + "-key\",\"hotelId\":\"KOCHI01\","
            + "\"createdDateTime\":\"2026-09-22T09:00:00Z\"}}}";
    }

    /// <summary>OHIP, answering each call with the next page.</summary>
    private sealed class Ohip(params Page[] pages) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public List<HttpRequestMessage> Sent { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Sent.Add(request);

            var page = pages[Math.Min(Calls, pages.Length - 1)];

            Calls++;

            if (page.Throws)
            {
                throw new HttpRequestException("the connection was reset");
            }

            return Task.FromResult(new HttpResponseMessage(page.Status)
            {
                Content = page.Body is null
                    ? new StringContent(string.Empty)
                    : new StringContent(page.Body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
