using System.Text.Json;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Authentication;
using PmsOracle.Integrations.Cloud;
using PmsOracle.Normalisation;
using PmsOracle.Vocabularies;

namespace PmsOracle.Adapters;

/// <summary>
/// The OHIP flavour — we would dial out, and the queue empties as we read it.
/// </summary>
/// <remarks>
/// <para>
/// <b>NOTHING ON THE DATA PATH DIALS ORACLE TODAY, and this paragraph is here
/// because the line above used to say it did.</b> <see cref="IOhipQueue"/> and
/// <see cref="IOhipGuarantees"/> are declared at the foot of this file and
/// implemented <b>nowhere in <c>backend/</c></b> — every implementation in the
/// repository is a test double. A port with no adapter is the house pattern and
/// is not a defect; a summary line reading <i>we dial out</i> over one is.
/// </para>
/// <para>
/// <b>One path does dial, and it is real</b> — <see cref="TestAsync"/> reaches
/// <c>OhipTokenAttempt</c>, which POSTs a password grant to OHIP's token
/// endpoint over a live <see cref="HttpClient"/>. So this adapter is not
/// entirely unwired: it can prove a credential set against Oracle, and it
/// cannot yet fetch a reservation.
/// </para>
/// <para>
/// <b>What it is waiting for, measured rather than assumed.</b> The obvious
/// answer — the per-property token read from the Token Vault — is what the
/// <see cref="IOhipQueue"/> comment has said for weeks, and it is not the
/// binding one. <b>The Integration Hub composes no connector adapter at all</b>:
/// <c>ConnectorHost</c> takes <c>IEnumerable&lt;IConnectorAdapter&gt;</c>,
/// the Hub's <c>Program.cs</c> registers none, and the service loads no
/// assembly (<c>Assembly.</c> appears zero times in its source). So
/// <c>PollScheduler</c> starts zero loops, and a finished transport here would
/// still never be called.
/// </para>
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
public sealed class OracleCloudAdapter(
    IntegrationSettings settings,
    IOhipQueue queue,
    IOhipGuarantees guarantees,
    HttpClient http)
    : IConnectorAdapter, IPollingConnector, ITestableConnection
{
    /// <summary>A reservation as OHIP returns it.</summary>
    public const string ReservationPayload = "ohip-reservation";

    /// <summary>A housekeeping room record as OHIP returns it.</summary>
    public const string HousekeepingPayload = "ohip-housekeeping-room";

    /// <summary>A guarantee policy, fetched for a property and an arrival date.</summary>
    /// <remarks>
    /// <b>Its own kind because it is its own fact</b> — ADR 0147 rules it a
    /// Hub-held source fact rather than a field on a reservation, since one
    /// policy serves every reservation arriving on that date. It carries the
    /// key the source does not echo; see <see cref="GuaranteeRecord"/>.
    /// </remarks>
    public const string GuaranteePayload = "ohip-guarantee";

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
    /// <b>Two tiers, from configuration — `CONN-Q12`, frame 3.</b> Three hours
    /// ordinarily and fifteen minutes around check-in, in the property's own
    /// zone. It was a fixed thirty seconds, which is 360 times the interval the
    /// frame draws against an API that rate-limits.
    /// </para>
    /// <para>
    /// The old reasoning was sound and answered a different question: the queue
    /// is emptied by reading, so a long interval makes a backlog rather than
    /// saving work. That argues for polling harder <i>when there is traffic</i>,
    /// which is what the tighter window is — not for a permanently short
    /// interval, which spends a rate limit on empty reads for twenty-two hours
    /// a day.
    /// </para>
    /// </remarks>
    public TimeSpan NextPollAfter(
        IReadOnlyDictionary<string, string> configuration, DateTimeOffset now) =>
        OhipPollingSchedule.Read(configuration).Wait(settings.Clock, now);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>The queue, then the guarantees the queue asked for</b> — R18 and
    /// ADR 0147. A guarantee policy is fetched per property and arrival date
    /// and only for reservations still in <c>Reserved</c> (study <c>:664</c>,
    /// <c>cloud:112-113</c>), so the arrival dates come from what was just
    /// drained. <b>Once per distinct date, never once per reservation</b>: one
    /// policy serves them all, which is the same fact the reference had right
    /// and keyed wrong.
    /// </para>
    /// <para>
    /// <b>A guarantee failure must never discard what the queue already gave
    /// up.</b> The read is the delete (R22), so an exception thrown after the
    /// drain and before the return loses a hotel's changes permanently — to
    /// fetch a policy. The catch is deliberately broad for that reason, and it
    /// is a test rather than a sentence: a guarantee source that throws still
    /// leaves every drained payload returned.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<PolledPayload>> DrainAsync(
        CancellationToken cancellationToken)
    {
        var drained = await queue.DrainAsync(settings, cancellationToken);

        try
        {
            var fetched = await guarantees.FetchAsync(
                settings, AwaitingGuarantee(drained), cancellationToken);

            return [.. drained, .. fetched.Select(Envelope)];
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return drained;
        }
    }

    /// <summary>The arrival dates the drained reservations need a policy for.</summary>
    /// <param name="drained">What the queue just gave up.</param>
    /// <returns>Each distinct arrival date, once.</returns>
    /// <remarks>
    /// <b>Still <c>Reserved</c> only</b>, which is the source's own rule
    /// (<c>cloud:112-113</c>) and not a saving invented here: a guarantee is
    /// what a booking is held on, and a stay already in house or checked out is
    /// not held on anything. Read through <see cref="CloudStayStatus"/> rather
    /// than compared to a literal, so a value OHIP gains does not silently
    /// become <i>not reserved</i>.
    /// </remarks>
    private static IReadOnlyCollection<string> AwaitingGuarantee(
        IReadOnlyList<PolledPayload> drained) =>
        drained
            .Where(payload => payload.PayloadKind == ReservationPayload)
            .Select(payload => Read<OhipReservation>(payload.Payload))
            .Where(reservation =>
                reservation?.ReservationStatus is { } status
                && CloudStayStatus.Read(status).TryGet(out var lifecycle)
                && lifecycle is StayLifecycle.Booked)
            .Select(reservation => reservation!.RoomStay?.ArrivalDate)
            .Where(arrival => !string.IsNullOrWhiteSpace(arrival))
            .Select(arrival => arrival!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>Put a fetched policy on the wire the Hub reads.</summary>
    /// <param name="record">The identified policy.</param>
    /// <returns>One drained payload.</returns>
    private PolledPayload Envelope(GuaranteeRecord record) =>
        new(Guid.Parse(settings.PropertyId),
            JsonSerializer.SerializeToUtf8Bytes(record, Json),
            GuaranteePayload);

    /// <inheritdoc />
    /// <remarks>
    /// Exactly the set <see cref="OhipCredentials"/> needs for a password
    /// grant, and no more. The Token Vault prefix is shared with the ingress,
    /// whose shared secret has no business reaching an outbound dial.
    /// </remarks>
    public IReadOnlyList<string> RequiredSecrets => OhipCredentials.SecretNames;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Completeness first, because it is the failure that actually
    /// happens.</b> A half-filled form is answered here, while somebody is
    /// still looking at the screen, instead of as a 401 at the next poll with
    /// nobody watching — and it is answered without dialling anyone, which
    /// matters because an incomplete set could not authenticate anyway.
    /// </para>
    /// <para>
    /// <b>Then a real token request</b> — the smallest OHIP call that proves
    /// the whole set at once, and the one call with no side effect. The queue
    /// is emptied by reading, so a test that drained it would discard a hotel's
    /// changes in order to prove it could reach them.
    /// </para>
    /// <para>
    /// <b>It runs through the credential path the poller will use</b>, which is
    /// what makes the answer worth anything: a green result means what a
    /// successful poll would mean, rather than that a separate test path
    /// happened to work.
    /// </para>
    /// </remarks>
    public async Task<ConnectionTest> TestAsync(
        IReadOnlyDictionary<string, string> configuration,
        IReadOnlyDictionary<string, string> secrets,
        CancellationToken cancellationToken)
    {
        var reading = OhipCredentials.Read(configuration, secrets);

        if (!reading.TryGet(out var credentials))
        {
            return ConnectionTest.Incomplete(reading.Missing);
        }

        return await OhipTokenAttempt.TryAsync(http, credentials, cancellationToken);
    }

    /// <inheritdoc />
    public PipelineResult Validate(byte[] payload, string payloadKind)
    {
        if (payloadKind is not (ReservationPayload or HousekeepingPayload
            or NotificationPayload or GuaranteePayload))
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
        if (payloadKind == GuaranteePayload)
        {
            // **The source fact's own identity, not a digest of it** — ADR 0147
            // keys a guarantee at property plus arrival date plus integration.
            // A digest would make two fetches of an unchanged policy two facts,
            // and a policy the property edited a third, with nothing saying
            // which is current.
            var record = Read<GuaranteeRecord>(payload);
            return record is null
                ? $"{payloadKind}:{Guid.CreateVersion7()}"
                : $"{payloadKind}:{record.Key()}";
        }

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

        // **A guarantee produces no fact here, and this is not the
        // notification's reason.** A notification is provenance; a guarantee is
        // a source fact in its own right — ADR 0147 puts its durability and
        // keyed lookup with the Hub, and has ENRICHMENT resolve a reservation
        // against it. `NormalisedPayload` carries exactly `RoomStayFact` and
        // `RoomStateFact`, so there is no shape here to return it in, and
        // `CommercialTermsReading` is the reader enrichment will use.
        //
        // Deferring rather than rejecting: nothing is wrong with the payload,
        // and it is stored and deduplicated on the strength of `DedupeKey`
        // above. Two arms rather than one, because folding them would give one
        // sentence to two different reasons.
        GuaranteePayload => NormalisedPayload.Nothing(PipelineResult.Defer(
            "a guarantee policy is a Hub-held source fact, applied at enrichment")),

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
/// <b>A port with no adapter. Nothing in <c>backend/</c> implements this</b> —
/// the only implementations in the repository are test doubles, and that is
/// stated here rather than left for a reader to infer from an empty search.
///
/// <b>Two things are owed, and they are not the same size.</b> The one this
/// comment used to name alone is the credential: OHIP is reached with a
/// per-property token from the Token Vault (`HUB-Q6`, the Kernel secret
/// store's <c>connector/</c> namespace). The larger one is that <b>the
/// Integration Hub composes no connector adapter</b> — measured, not recalled:
/// <c>ConnectorHost</c> is handed <c>IEnumerable&lt;IConnectorAdapter&gt;</c>,
/// the Hub registers none, and it loads no assembly. An implementation of this
/// interface would compile, pass its tests, and never be constructed.
///
/// Everything above this line — validation, the dedupe promise, normalisation,
/// the destructive-queue shape, the guarantee key — is finished and testable
/// against a double. What is owed is a socket <i>and</i> somewhere for it to be
/// plugged in.
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

/// <summary>
/// OHIP's guarantee endpoint, as this connector reaches it.
/// </summary>
/// <remarks>
/// <b>A seam beside <see cref="IOhipQueue"/>, and owed for the same reason</b>
/// — the per-property token read from the Token Vault is unimplemented
/// (<c>HUB-Q6</c>). What is finished above this line is the identification, the
/// keying, the once-per-date rule and the reading; only the socket is owed.
/// </remarks>
public interface IOhipGuarantees
{
    /// <summary>Fetch the policy for each arrival date.</summary>
    /// <param name="settings">Which integration and property to fetch for.</param>
    /// <param name="arrivalDates">
    /// Each distinct arrival date, exactly as OHIP stated it on the
    /// reservations that need a policy.
    /// </param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>What was fetched, identified.</returns>
    /// <remarks>
    /// <b>Dates in, records out, and the caller has already made them
    /// distinct.</b> A method taking one date would be called in a loop the
    /// caller could not describe, and the reference's defect is what happens
    /// when the one-to-many shape is lost between the query and the answer.
    /// </remarks>
    Task<IReadOnlyList<GuaranteeRecord>> FetchAsync(
        IntegrationSettings settings,
        IReadOnlyCollection<string> arrivalDates,
        CancellationToken cancellationToken);
}
