using System.Text.Json.Serialization;

namespace HotelOS.RoomCare.Application.Announcing;

/// <summary>The body of <c>room.cleaned</c>, <c>room.inspected</c> and <c>room.condition_changed</c> — chapter 03 §3.1.</summary>
/// <remarks>
/// One shape for the three, so a consumer reading the room's condition reads
/// one set of names. Every member names its wire field: the appender's policy
/// would produce the same, and a rename here must not quietly change the wire.
/// </remarks>
public sealed record RoomConditionAnnouncement
{
    [JsonPropertyName("room_id")]
    public required Guid RoomId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("from")]
    public required string From { get; init; }

    [JsonPropertyName("to")]
    public required string To { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("by_kind")]
    public required string ByKind { get; init; }

    [JsonPropertyName("by_id")]
    public Guid? ById { get; init; }

    [JsonPropertyName("via")]
    public required string Via { get; init; }

    [JsonPropertyName("task_id")]
    public Guid? TaskId { get; init; }

    [JsonPropertyName("operating_day")]
    public string? OperatingDay { get; init; }

    [JsonPropertyName("inspection_ref")]
    public string? InspectionRef { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>The body of <c>roomcare.disagreement.flagged</c> and <c>.cleared</c> — S4.</summary>
public sealed record DisagreementAnnouncement
{
    [JsonPropertyName("room_id")]
    public required Guid RoomId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("ours")]
    public required string Ours { get; init; }

    [JsonPropertyName("theirs")]
    public required string Theirs { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("kept")]
    public string? Kept { get; init; }

    [JsonPropertyName("by_id")]
    public Guid? ById { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>The body of <c>user.roomcare_manager_granted</c> / <c>_revoked</c> — the Kernel folds it (AUTHZ-Q25).</summary>
/// <remarks>
/// <c>user_id</c> is the person gaining or losing access, never the general
/// manager who decided — the envelope carries the decider. Both are ids a test
/// keeps distinct, because they coincide in every test that does not.
/// </remarks>
public sealed record ManagerGrantAnnouncement
{
    [JsonPropertyName("user_id")]
    public required Guid UserId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}
