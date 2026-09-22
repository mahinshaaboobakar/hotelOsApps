using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using PmsOracle.Authentication;

namespace PmsOracle.Integrations.Cloud;

/// <summary>
/// Taking what OHIP's business-event queue is holding for one hotel.
/// </summary>
/// <remarks>
/// <para>
/// <b>The call is the reference's</b>, transcribed under OHIP's own spellings —
/// <c>providers/oracle/cloud/services/impl/OracleCloudEventServiceImpl.java:44-84</c>:
/// </para>
/// <code>
/// GET {host}/int/v1/externalSystem/{externalSystemCode}/hotels/{hotelId}/businessEvents?limit=20
///   200  businessEventData[]   more may be waiting; ask again
///   204  the queue is empty    stop
/// </code>
/// <para>
/// with <c>x-hotelid</c>, <c>x-app-key</c> and a bearer token on every call —
/// <c>providers/oracle/cloud/services/OracleCloudBaseService.java:83-86</c>.
/// </para>
/// <para>
/// <b>Reading it empties it (R22).</b> Every item this returns has already left
/// OHIP, so it exists in exactly one place: the reply. That is why nothing here
/// filters, parses or interprets — a payload dropped for being uninteresting is
/// a fact the hotel can never see again, and the Hub stores raw bytes precisely
/// so a later reading can be different from today's.
/// </para>
/// <para>
/// <b>A page that fails after earlier pages succeeded returns what was taken —
/// interim, pending the ruling.</b> <c>DrainResult</c> has one field, so the
/// three states a destructive queue can end in do not all fit:
/// </para>
/// <code>
/// took nothing, could not ask   Fault          ruled by ADR 0194
/// took everything waiting       payloads       ruled
/// took some, then failed        ← no shape for this
/// </code>
/// <para>
/// Faulting would destroy what was already taken; replying hides that the drain
/// stopped early. This returns the payloads, because losing a hotel's facts is
/// irreversible and a hidden failure is not: a persistent fault recurs on the
/// next drain's <i>first</i> page, where nothing has been taken yet and the
/// fault is safe. What that leaves invisible is a transient failure after the
/// first page — which cost no data and is followed by another drain.
/// </para>
/// </remarks>
public static class OhipBusinessEventQueue
{
    /// <summary>OHIP's own page size, as the reference asks for it.</summary>
    public const int PageSize = 20;

    /// <summary>The payload kind every item from this queue carries.</summary>
    public const string EventPayload = "ohip-business-event";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Drain the queue until OHIP says it is empty.</summary>
    /// <param name="http">The client to dial with.</param>
    /// <param name="credentials">The endpoint, the hotel and the application key.</param>
    /// <param name="accessToken">A token from <see cref="OhipAccessToken"/>.</param>
    /// <param name="cancellationToken">The invocation's.</param>
    /// <returns>Every item taken, in the order OHIP gave them.</returns>
    public static async Task<IReadOnlyList<DrainedEvent>> DrainAsync(
        HttpClient http,
        OhipCredentials credentials,
        string accessToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var taken = new List<DrainedEvent>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<DrainedEvent>? page;

            try
            {
                page = await PageAsync(http, credentials, accessToken, cancellationToken);
            }
            catch (Exception failed) when (failed is HttpRequestException or JsonException
                or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                // Nothing was taken by the call that failed — a page is read
                // whole or not at all — so what is here is intact. Returning it
                // rather than throwing is the interim reading documented above.
                if (taken.Count == 0)
                {
                    throw;
                }

                return taken;
            }

            // 204: OHIP says there is nothing more. The one place this loop ends
            // on the source's own word rather than on a count we chose.
            if (page is null)
            {
                return taken;
            }

            taken.AddRange(page);

            // A short page means the queue is drained, and asking again would be
            // a request whose answer we already know. The reference asks until a
            // 204; this stops one call earlier and still ends on OHIP's answer.
            if (page.Count < PageSize)
            {
                return taken;
            }
        }
    }

    /// <summary>One page, or <c>null</c> when OHIP answers that the queue is empty.</summary>
    private static async Task<IReadOnlyList<DrainedEvent>?> PageAsync(
        HttpClient http,
        OhipCredentials credentials,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Address(credentials));

        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        foreach (var (name, value) in OhipRequestHeaders.ForEveryCall(credentials))
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        using var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NoContent)
        {
            return null;
        }

        if (response.StatusCode is not HttpStatusCode.OK)
        {
            throw new HttpRequestException(
                $"OHIP answered the business-event queue with {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var page = JsonSerializer.Deserialize<BusinessEventPage>(body, Json);

        if (page?.BusinessEventData is not { Count: > 0 } events)
        {
            // A 200 carrying no items. OHIP signals empty with 204, so this is
            // the source saying something this connector does not model — and
            // an empty page ends the drain rather than looping forever on it.
            return [];
        }

        // Each item is re-serialised on its own, so the Hub stores one fact per
        // record rather than a page it would have to split later. The bytes are
        // OHIP's own field names throughout: this is the vendor's schema, and
        // renaming it here would make a rejection quote a spelling nobody sent.
        return [.. events.Select(one => new DrainedEvent(
            JsonSerializer.SerializeToUtf8Bytes(one, Json), EventPayload))];
    }

    /// <summary>The queue's address for this hotel.</summary>
    private static string Address(OhipCredentials credentials) =>
        $"{credentials.Endpoint.TrimEnd('/')}/int/v1/externalSystem/{credentials.ExternalSystemCode}"
        + $"/hotels/{credentials.HotelCode}/businessEvents?limit={PageSize}";

    /// <summary>OHIP's page, in OHIP's spellings.</summary>
    private sealed record BusinessEventPage(
        [property: JsonPropertyName("businessEventData")]
        IReadOnlyList<JsonElement>? BusinessEventData);
}

/// <summary>One item taken from the queue, and what shape it is.</summary>
/// <param name="Payload">The bytes as OHIP gave them.</param>
/// <param name="PayloadKind">
/// The connector's own name for the shape — declared rather than sniffed,
/// because this end knows which endpoint produced it.
/// </param>
public sealed record DrainedEvent(byte[] Payload, string PayloadKind);
