using System.Text.Json;
using HotelOS.Connector;
using PmsOracle.Authentication;
using PmsOracle.Integrations.Cloud;
using PmsOracle.Normalisation;

namespace PmsOracle.Adapters;

/// <summary>
/// The OHIP flavour — we dial out, and the queue empties as we read it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The queue is destructive, and that is the requirement this adapter is
/// shaped by</b> (R22). Reading a business-event notification removes it: there
/// is no cursor to rewind, no acknowledgement to withhold, and no way to ask
/// again. So <see cref="DrainAsync"/> hands the Hub what it took and makes no
/// promise about it — the guarantee that every drained payload is stored before
/// the next drain is the poll loop's, which is why
/// <see cref="IPollingConnector"/> gives a connector no way to claim it.
/// </para>
/// <para>
/// <b>The event id is the dedupe key</b>, because for once the source promises
/// one. Declared in <c>PmsOracleCapabilities</c> as <c>DedupePromise.EventId</c>
/// rather than assumed by the Hub — the two on-site flavours promise nothing
/// better than a content digest, and the Hub implements all three because only
/// the connector knows which its source can keep.
/// </para>
/// <para>
/// <b>It does not join.</b> OHIP sends a whole reservation in one read, so
/// implementing <see cref="IJoiningConnector"/> here would hold the Hub's join
/// store open on messages that are already complete.
/// </para>
/// </remarks>
public sealed class OracleCloudAdapter(IntegrationSettings settings, IOhipQueue queue)
    : IConnectorAdapter, IPollingConnector, ITestableConnection
{
    /// <summary>A reservation as OHIP returns it.</summary>
    public const string ReservationPayload = "ohip-reservation";

    /// <summary>A housekeeping room record as OHIP returns it.</summary>
    public const string HousekeepingPayload = "ohip-housekeeping-room";

    /// <summary>A business-event notification, stored before anything is fetched.</summary>
    /// <remarks>
    /// Stored in its own right rather than consumed in passing. The reference
    /// dropped every notification whose module it did not read, which is why
    /// nobody can now say what else OHIP emits; storing first keeps the
    /// question answerable.
    /// </remarks>
    public const string NotificationPayload = "ohip-business-event";

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public string IntegrationId => settings.IntegrationId;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Three hours — `CONN-Q12`, and this is the value that actually
    /// polls.</b> It said thirty seconds, which is 360 times the interval frame
    /// 3 draws, against a vendor API that rate-limits. The form's hint was
    /// corrected first and that was only half the fix: a hint is a placeholder
    /// and this is the number the loop runs on, so correcting the hint alone
    /// would have left the load exactly where it was.
    /// </para>
    /// <para>
    /// The old reasoning was sound and answered a different question: the queue
    /// is emptied by reading, so a long interval makes a backlog rather than
    /// saving work. That argues against hours *when there is traffic*, and
    /// frame 3's answer is the tighter window around check-in rather than a
    /// permanently short interval — which is step 4's two-tier schedule, read
    /// from configuration rather than fixed here.
    /// </para>
    /// </remarks>
    public TimeSpan PollInterval => TimeSpan.FromHours(3);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PolledPayload>> DrainAsync(
        CancellationToken cancellationToken) =>
        await queue.DrainAsync(settings, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Completeness, which is the failure that actually happens.</b> A
    /// half-filled form is answered here, while somebody is still looking at
    /// the screen, instead of as a 401 at the next poll with nobody watching.
    /// The missing names come from <see cref="OhipCredentials.MissingFrom"/> —
    /// the same list a credential read builds, so the test and the poll cannot
    /// disagree about what is absent.
    /// </para>
    /// <para>
    /// <b>Reachability is not answered, and saying so is the honest outcome.</b>
    /// Reaching OHIP means a password grant, which means credential *values*,
    /// and nothing in the Hub reads a connector credential back out of the
    /// Token Vault — <c>IConnectorSecretWriter</c> has no read method by
    /// design. Opening one as a side effect of a button is a decision with a
    /// ruling owed, not an implementation detail. So a complete configuration
    /// answers <c>NOT_SUPPORTED</c> with a sentence that says which half was
    /// checked, rather than a green light that tested nothing.
    /// </para>
    /// </remarks>
    public Task<ConnectionTest> TestAsync(
        IReadOnlyDictionary<string, string> configuration,
        IReadOnlyList<string> configuredSecrets,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var missing = OhipCredentials.MissingFrom(configuration, configuredSecrets);

        return Task.FromResult(missing.Count > 0
            ? ConnectionTest.Incomplete(missing)
            : ConnectionTest.NotSupported(
                "Every credential is set. Reaching OHIP to check them needs the "
                + "polling transport, which is not built yet."));
    }

    /// <inheritdoc />
    public PipelineResult Validate(byte[] payload, string payloadKind)
    {
        if (payloadKind is not (ReservationPayload or HousekeepingPayload
            or NotificationPayload))
        {
            return PipelineResult.Reject(
                "unknown message kind", "payload_kind", payloadKind);
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
        }
        catch (JsonException e)
        {
            return PipelineResult.Reject("unparseable json", "body", e.Message);
        }

        return PipelineResult.Continue();
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>The notification's own id, where there is one.</b> A fetched
    /// reservation or housekeeping record is keyed by the notification that
    /// caused it to be fetched, which the poller carries as the payload's
    /// identity — so a redelivered notification and its fetch deduplicate
    /// together rather than separately.
    /// </remarks>
    public string DedupeKey(byte[] payload, string payloadKind)
    {
        if (payloadKind == NotificationPayload)
        {
            var notification = Read<BusinessEventNotification>(payload);
            return string.IsNullOrWhiteSpace(notification?.EventId)
                // A notification with no id is malformed and `Validate` will
                // not catch it — the Hub still needs *a* key, and one that
                // cannot collide is better than one that collides with
                // everything else missing an id.
                ? $"{payloadKind}:{Guid.CreateVersion7()}"
                : $"{payloadKind}:{notification.EventId}";
        }

        return $"{payloadKind}:{Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(payload))}";
    }

    /// <inheritdoc />
    public NormalisedPayload Normalise(byte[] payload, string payloadKind) => payloadKind switch
    {
        HousekeepingPayload => OutcomeMapping.ToPayload(
            new CloudRoomStateNormaliser(settings)
                .Normalise(Read<OhipHousekeepingRoom>(payload)!)),

        ReservationPayload => OutcomeMapping.ToPayload(
            new CloudNormaliser(settings).Normalise(Read<OhipReservation>(payload)!)),

        // A notification is provenance, not a fact. It is stored, it is
        // deduplicated, and it produces nothing on its own — the fetch it
        // triggers does. Deferring rather than rejecting, because nothing is
        // wrong with it.
        _ => NormalisedPayload.Nothing(
            PipelineResult.Defer("a notification carries no fact of its own")),
    };

    private static T? Read<T>(byte[] payload) => JsonSerializer.Deserialize<T>(payload, Json);
}

/// <summary>
/// OHIP's business-event queue, as this connector reaches it.
/// </summary>
/// <remarks>
/// <b>A seam, because the transport needs credentials nothing can supply
/// yet.</b> OHIP is reached with a per-property token from the Token Vault
/// (`HUB-Q6`: the Kernel secret store's <c>connector/</c> namespace), and that
/// read is unimplemented. Everything above this line — validation, the dedupe
/// promise, normalisation, the destructive-queue shape — is finished and
/// testable against a double; only the socket is owed.
/// </remarks>
public interface IOhipQueue
{
    /// <summary>Take whatever the queue is holding.</summary>
    /// <param name="settings">Which integration and property to drain for.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>What was taken — and it has already left OHIP.</returns>
    Task<IReadOnlyList<PolledPayload>> DrainAsync(
        IntegrationSettings settings, CancellationToken cancellationToken);
}
