using System.Text.Json.Serialization;
using HotelOS.RoomCare.Domain;

namespace HotelOS.RoomCare.Application.Announcing;

/// <summary>The body of <c>roomcare.supervision.opened</c> / <c>.decided</c> and <c>roomcare.service_missed</c>.</summary>
public sealed record SupervisionAnnouncement
{
    [JsonPropertyName("supervision_id")]
    public required Guid SupervisionId { get; init; }

    [JsonPropertyName("room_id")]
    public required Guid RoomId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("operating_day")]
    public required string OperatingDay { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonPropertyName("days")]
    public int? Days { get; init; }

    [JsonPropertyName("decision")]
    public string? Decision { get; init; }

    [JsonPropertyName("by_id")]
    public Guid? ById { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>The body of <c>roomcare.inspection.requested</c> — the inspection app answers on the correlation id (RC-Q1(6)).</summary>
public sealed record InspectionRequestedAnnouncement
{
    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    [JsonPropertyName("room_id")]
    public required Guid RoomId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("service")]
    public required string Service { get; init; }

    [JsonPropertyName("checklist_ref")]
    public string? ChecklistRef { get; init; }

    [JsonPropertyName("correlation_id")]
    public required string CorrelationId { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>The body of <c>roomcare.issue.found</c> — Jobs creates the job and <c>job.created</c> carries the id back.</summary>
public sealed record IssueFoundAnnouncement
{
    [JsonPropertyName("issue_id")]
    public required Guid IssueId { get; init; }

    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    [JsonPropertyName("room_id")]
    public required Guid RoomId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("item_hint")]
    public string? ItemHint { get; init; }

    [JsonPropertyName("note")]
    public required string Note { get; init; }

    [JsonPropertyName("media_id")]
    public Guid? MediaId { get; init; }

    [JsonPropertyName("by_id")]
    public required Guid ById { get; init; }

    [JsonPropertyName("correlation_id")]
    public required string CorrelationId { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>The body of <c>roomcare.room.restocked</c> — the act, no price (RC-Q2).</summary>
public sealed record RestockedAnnouncement
{
    [JsonPropertyName("restock_id")]
    public required Guid RestockId { get; init; }

    [JsonPropertyName("task_id")]
    public required Guid TaskId { get; init; }

    [JsonPropertyName("room_id")]
    public required Guid RoomId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("stay_id")]
    public Guid? StayId { get; init; }

    [JsonPropertyName("items")]
    public required IReadOnlyList<RestockItem> Items { get; init; }

    [JsonPropertyName("by_id")]
    public required Guid ById { get; init; }

    [JsonPropertyName("at")]
    public required DateTimeOffset At { get; init; }
}

/// <summary>The body of <c>roomcare.deep_clean.due</c> and the two block requests — correlation ids on each.</summary>
public sealed record DeepCleanAnnouncement
{
    [JsonPropertyName("deep_clean_id")]
    public required Guid DeepCleanId { get; init; }

    [JsonPropertyName("room_id")]
    public required Guid RoomId { get; init; }

    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    [JsonPropertyName("due_on")]
    public required string DueOn { get; init; }

    [JsonPropertyName("from")]
    public string? From { get; init; }

    [JsonPropertyName("to")]
    public string? To { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonPropertyName("correlation_id")]
    public required string CorrelationId { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}
