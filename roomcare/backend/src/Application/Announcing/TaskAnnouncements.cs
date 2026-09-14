using System.Text.Json.Serialization;

namespace HotelOS.RoomCare.Application.Announcing;

/// <summary>The body of every <c>roomcare.task.*</c> event — chapter 03 §3.2.</summary>
/// <remarks>
/// What the chapter's table names, and nothing shaped for the authorization
/// graph. <c>room_task</c> is not a master entity and ADR 0061 does not reach
/// it; nothing registers it today, so its checks fail closed. Whether an
/// application-owned authorization object registers through the lifecycle
/// consumer as Knowledge's <c>folder</c> does (<c>tuples.rs</c>, ADR 0095/0096),
/// and who declares its tuple shape, is unruled and asked — this body is not
/// built toward either answer.
/// </remarks>
public sealed record TaskAnnouncement
{
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("room_id")]
    public Guid? RoomId { get; init; }

    [JsonPropertyName("location_id")]
    public required Guid LocationId { get; init; }

    [JsonPropertyName("operating_day")]
    public required string OperatingDay { get; init; }

    [JsonPropertyName("window")]
    public required string Window { get; init; }

    [JsonPropertyName("service")]
    public required string Service { get; init; }

    [JsonPropertyName("priority")]
    public required string Priority { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("outcome")]
    public string? Outcome { get; init; }

    [JsonPropertyName("found")]
    public string? Found { get; init; }

    [JsonPropertyName("partial_done")]
    public IReadOnlyList<string>? PartialDone { get; init; }

    [JsonPropertyName("user_id")]
    public Guid? UserId { get; init; }

    [JsonPropertyName("what")]
    public string? What { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("by_kind")]
    public required string ByKind { get; init; }

    [JsonPropertyName("by_id")]
    public Guid? ById { get; init; }

    [JsonPropertyName("via")]
    public required string Via { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>The body of <c>roomcare.day.prepared</c> — a run and its counts.</summary>
public sealed record DayPreparedAnnouncement
{
    [JsonPropertyName("run_id")]
    public required Guid RunId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("operating_day")]
    public required string OperatingDay { get; init; }

    [JsonPropertyName("window")]
    public required string Window { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("rooms_considered")]
    public required int RoomsConsidered { get; init; }

    [JsonPropertyName("tasks_created")]
    public required int TasksCreated { get; init; }

    [JsonPropertyName("tasks_updated")]
    public required int TasksUpdated { get; init; }

    [JsonPropertyName("pending")]
    public required int Pending { get; init; }

    [JsonPropertyName("unassignable")]
    public required int Unassignable { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}
