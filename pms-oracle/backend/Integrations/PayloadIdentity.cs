using System.Security.Cryptography;
using System.Text.Json;
using PmsOracle.Integrations.Cloud;

namespace PmsOracle.Integrations;

/// <summary>
/// The identity a payload is deduplicated on.
/// </summary>
/// <remarks>
/// <para>
/// <b>One home, because two callers must agree.</b> The drain supplies a key
/// with every payload it returns (ADR 0246), and the <c>dedupe_key</c>
/// capability keys the two kinds of bytes that arrive without one — a
/// quarantined payload being re-submitted, and a push the Hub has staged and
/// answered 202 for (ADR 0255 §2, ADR 0272 §3). <b>The Hub never computes one
/// itself</b>, so if those two callers disagreed, one payload drained and
/// later re-submitted would be two facts — which is the harm deduplication
/// exists to prevent.
/// </para>
/// <para>
/// <b>A pure function of the kind and the bytes, and the protocol says so:</b>
/// <i>"nothing is read from the source, so a cancelled or repeated invocation
/// loses nothing."</i> That sentence is a constraint rather than a
/// description — a key that changed between two invocations of the same bytes
/// would make a RETRY produce a second fact, which is the one moment
/// deduplication has to work.
/// </para>
/// <para>
/// <b>This is why a fresh identifier cannot be the fallback.</b> The drain's
/// keying returned <c>Guid.CreateVersion7()</c> for an event carrying no id,
/// reasoning that <i>"one that cannot collide is better than one that collides
/// with everything else missing an id"</i>. The first half is right and a
/// digest satisfies it: two id-less payloads with different bytes get
/// different keys. The second half is what a digest adds — the SAME bytes get
/// the SAME key, so a redelivered malformed event deduplicates instead of
/// arriving twice. The identifier was strictly weaker, and it was mine.
/// </para>
/// <para>
/// <b>Every key is prefixed with its kind</b>, which the adapters' convention
/// already established, so two keys for one change can never differ only in
/// shape.
/// </para>
/// </remarks>
public static class PayloadIdentity
{
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    /// <summary>The key these bytes are deduplicated on.</summary>
    /// <param name="payloadKind">The kind the connector declared for them.</param>
    /// <param name="payload">The bytes exactly as the Hub holds them.</param>
    /// <returns>A key, stable for these bytes under this kind.</returns>
    /// <remarks>
    /// <b>Two kinds carry the source's own identity and the rest are digested.</b>
    /// An OHIP business event and a guarantee both name a fact the source can
    /// restate — so keying them on the bytes would make two deliveries of one
    /// unchanged fact into two facts, and a policy the property edited into a
    /// third, with nothing saying which is current (ADR 0147).
    /// </remarks>
    public static string For(string payloadKind, ReadOnlySpan<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(payloadKind);

        if (payloadKind == OhipBusinessEventQueue.EventPayload)
        {
            return Compose(payloadKind, BusinessEventId(payload), payload);
        }

        // Read from the one home rather than spelled again — ADR 0272 lets code
        // consume a declared kind and forbids a competing list. That home is on
        // the retired adapter (`CONN-Q42`) because the kinds have no other
        // declaration in this package; reading a constant is not building onto
        // that seam, and a second spelling would be.
        if (payloadKind == Adapters.OracleCloudAdapter.GuaranteePayload)
        {
            return Compose(payloadKind, GuaranteeKey(payload), payload);
        }

        return Compose(payloadKind, null, payload);
    }

    /// <summary>The source's identifier where it has one, a digest otherwise.</summary>
    /// <param name="payloadKind">The declared kind, which prefixes the key.</param>
    /// <param name="identity">The source's own identifier, or <c>null</c>.</param>
    /// <param name="payload">The bytes, digested when there is no identity.</param>
    /// <returns>The composed key.</returns>
    private static string Compose(string payloadKind, string? identity, ReadOnlySpan<byte> payload) =>
        string.IsNullOrWhiteSpace(identity)
            ? $"{payloadKind}:{Convert.ToHexStringLower(SHA256.HashData(payload))}"
            : $"{payloadKind}:{identity}";

    /// <summary>OHIP's <c>businessEventId.id</c>, or <c>null</c>.</summary>
    /// <param name="payload">The item as the queue returned it.</param>
    /// <returns>The identifier, or <c>null</c> where the shape does not carry one.</returns>
    /// <remarks>
    /// <b>The nested shape, which is what OHIP sends</b> — the flat
    /// <c>BusinessEventNotification</c> beside this reads null on every field
    /// of a real item, and a key built from it would be a key stable across
    /// nothing.
    /// </remarks>
    private static string? BusinessEventId(ReadOnlySpan<byte> payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload.ToArray());
            return document.RootElement.TryGetProperty("businessEventId", out var identifier)
                && identifier.ValueKind is JsonValueKind.Object
                && identifier.TryGetProperty("id", out var value)
                && value.ValueKind is JsonValueKind.String
                    ? value.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>A guarantee's property, arrival date and integration — ADR 0147.</summary>
    /// <param name="payload">The record as it was fetched.</param>
    /// <returns>The key, or <c>null</c> where the bytes will not read as one.</returns>
    private static string? GuaranteeKey(ReadOnlySpan<byte> payload)
    {
        try
        {
            return JsonSerializer.Deserialize<GuaranteeRecord>(payload, Json)?.Key();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
