using System.Security.Cryptography;
using System.Text.Json;
using HotelOS.Connector;
using PmsOracle.Integrations.OnSite;
using PmsOracle.Normalisation;
using PmsOracle.Vocabularies;

namespace PmsOracle.Adapters;

/// <summary>
/// The two on-site flavours — the PMS agent posts, and we take what it sends.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two registered identifiers, one implementation</b> — `oracle-onpremise`
/// and `oracle-web` speak the same wire and differ in endpoint and credential
/// (R28, `CONN-Q2(a)`). Each is constructed with its own identifier, so their
/// facts' provenance stays distinct and their health is reported separately,
/// while the parsing they share is written once.
/// </para>
/// <para>
/// <b>A content digest for deduplication</b>, because this source promises
/// nothing better: the agent sends no event id and no change timestamp, so the
/// only thing stable across a redelivery is the bytes. Declared as such in
/// <c>PmsOracleCapabilities</c>, where the Hub reads what a connector can
/// promise rather than assuming.
/// </para>
/// <para>
/// <b>It joins</b>, because a check-in arrives as two messages with no
/// correlation identifier between them (R6). The key is three fields and the
/// risk of a wrong join is real and stated where <see cref="OnSiteJoinKey"/>
/// is defined; the window is declared here and enforced by the Hub.
/// </para>
/// </remarks>
public sealed class OracleOnSiteAdapter(IntegrationSettings settings)
    : IConnectorAdapter, IJoiningConnector
{
    /// <summary>A stay message from the agent.</summary>
    public const string StayPayload = "onsite-stay";

    /// <summary>A room-status message from the agent.</summary>
    public const string RoomStatusPayload = "onsite-room-status";

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public string IntegrationId => settings.IntegrationId;

    /// <inheritdoc />
    /// <remarks>
    /// <b>Thirty minutes, and it is this connector's number.</b> The agent
    /// sends both halves of a check-in within one operation, so a half still
    /// alone after half an hour is not late — the other message is not coming.
    /// The Hub enforces it and never chooses it (`HUB-Q5`).
    /// </remarks>
    public TimeSpan JoinWindow => TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    /// <remarks>
    /// <b>Structural only.</b> Whether the bytes parse and carry the fields
    /// this integration's contract makes mandatory — not whether the stay makes
    /// sense, which is the normaliser's and belongs where the business rules
    /// can be found.
    /// </remarks>
    public PipelineResult Validate(byte[] payload, string payloadKind)
    {
        if (payloadKind is not (StayPayload or RoomStatusPayload))
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
            // The exception's message, not a generic one: an engineer reading
            // the inbox queue wants the position the parser stopped at.
            return PipelineResult.Reject("unparseable json", "body", e.Message);
        }

        return PipelineResult.Continue();
    }

    /// <inheritdoc />
    /// <remarks>
    /// A SHA-256 over the bytes as received. Not over the normalised fact: the
    /// key has to be computable at receipt, before anything is parsed, because
    /// that is where the Hub deduplicates.
    /// </remarks>
    public string DedupeKey(byte[] payload, string payloadKind) =>
        $"{payloadKind}:{Convert.ToHexStringLower(SHA256.HashData(payload))}";

    /// <inheritdoc />
    public NormalisedPayload Normalise(byte[] payload, string payloadKind) =>
        payloadKind == RoomStatusPayload
            ? OutcomeMapping.ToPayload(
                new RoomStateNormaliser(settings).Normalise(Read<OnSiteRoomStatusPush>(payload)))
            : OutcomeMapping.ToPayload(
                new OnSiteNormaliser(settings).Normalise(Read<OnSitePush>(payload)));

    /// <inheritdoc />
    /// <remarks>
    /// <b>Only a stay message can be half of anything.</b> A room-status push is
    /// a whole fact, and returning a pairing for it would hold the Hub's join
    /// store open on messages that have no partner and never will.
    /// </remarks>
    public JoinCandidate? JoinFor(byte[] payload, string payloadKind)
    {
        if (payloadKind != StayPayload)
        {
            return null;
        }

        var push = Read<OnSitePush>(payload);

        if (string.IsNullOrWhiteSpace(push.Status))
        {
            return null;
        }

        // An unrecognised status is the normaliser's rejection to make, with
        // the value carried. Guessing a part here would decide the message is
        // half of something before anything has read what it says.
        if (!OnSiteStayStatus.Read(push.Status).TryGet(out var status)
            || status.Part == OnSiteMessagePart.Whole)
        {
            return null;
        }

        var key = OnSiteJoinKey.For(push.Surname, push.FirstName, ParseDate(push.ArrivalDate));

        // A key that could not be built matches nothing rather than matching
        // everything — `OnSiteJoinKey.For` refuses a blank name for exactly
        // that reason, and null here means the normaliser rejects it with the
        // field named instead.
        return key is null
            ? null
            : new JoinCandidate(
                $"{key.Value.Surname}|{key.Value.FirstName}|{key.Value.ArrivalDate:yyyy-MM-dd}",
                status.Part.ToString());
    }

    /// <inheritdoc />
    /// <remarks>
    /// The normaliser already knows how to assemble a stay from two parts; what
    /// arrives here is both halves' bytes, and it reads them in the order the
    /// Hub paired them.
    /// </remarks>
    public NormalisedPayload NormaliseJoined(IReadOnlyList<JoinedPart> parts)
    {
        var normaliser = new OnSiteNormaliser(settings);
        var outcome = normaliser.Normalise(Merge(parts));

        return OutcomeMapping.ToPayload(outcome);
    }

    /// <summary>Both halves as one message.</summary>
    /// <remarks>
    /// <b>Field by field, taking the first non-empty value.</b> The two halves
    /// carry disjoint fields by construction — one has the contact details and
    /// one has the room — so a later half never overwrites an earlier one's
    /// value with a blank, and a genuine disagreement is impossible rather than
    /// silently resolved.
    /// </remarks>
    private OnSitePush Merge(IReadOnlyList<JoinedPart> parts)
    {
        var pushes = parts.Select(part => Read<OnSitePush>(part.Payload)).ToList();

        return new OnSitePush
        {
            ReservationId = First(pushes, push => push.ReservationId),
            Status = First(pushes, push => push.Status),
            Surname = First(pushes, push => push.Surname),
            FirstName = First(pushes, push => push.FirstName),
            ArrivalDate = First(pushes, push => push.ArrivalDate),
            DepartureDate = First(pushes, push => push.DepartureDate),
            RoomNo = First(pushes, push => push.RoomNo),
            RoomType = First(pushes, push => push.RoomType),
            NoOfRooms = First(pushes, push => push.NoOfRooms),
            PropertyCode = First(pushes, push => push.PropertyCode),
        };
    }

    private static string? First(IEnumerable<OnSitePush> pushes, Func<OnSitePush, string?> field) =>
        pushes.Select(field).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;

    private static T Read<T>(byte[] payload)
        where T : new() =>
        JsonSerializer.Deserialize<T>(payload, Json) ?? new T();
}
