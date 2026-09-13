namespace HotelOS.RoomCare.Domain;

/// <summary>One room's condition as Room Care owns it, beside what it has observed — chapter 03 §2.1.</summary>
/// <remarks>
/// <para>
/// <b>The row is keyed by Master Data's room id and there is no room table
/// here</b> (ADR 0051): no name, no number, no type. A screen that needs the
/// number reads it from Master Data at answer time.
/// </para>
/// <para>
/// <see cref="Version"/> is the optimistic guard <b>and</b> the
/// <c>entity_version</c> every <c>room.*</c> event carries, bumped in the
/// same commit — one number, never two that could disagree (§7.10).
/// </para>
/// </remarks>
public class RoomState
{
    public Guid RoomId { get; set; }

    public Guid PropertyId { get; set; }

    public string Condition { get; set; } = Domain.Condition.Dirty;

    public DateTimeOffset ConditionSetAt { get; set; }

    public string ConditionSetByKind { get; set; } = ActorKind.System;

    public Guid? ConditionSetById { get; set; }

    public string ConditionSource { get; set; } = Domain.ConditionSource.System;

    public string Occupancy { get; set; } = Domain.Occupancy.Unknown;

    public DateTimeOffset? NextSoldAt { get; set; }

    public List<string> StayStatuses { get; set; } = [];

    public bool IsPseudoRoom { get; set; }

    /// <summary>When the last observation of any kind arrived — the board's "PMS silent since".</summary>
    public DateTimeOffset? LastObservedAt { get; set; }

    public string? DisagreementObservedCondition { get; set; }

    public DateTimeOffset? DisagreementObservedAt { get; set; }

    public string? DisagreementSource { get; set; }

    public Guid? DisagreementClearedBy { get; set; }

    public DateTimeOffset? DisagreementClearedAt { get; set; }

    /// <summary>Which side the clearing kept — <c>OURS</c> or <c>THEIRS</c>.</summary>
    public string? DisagreementClearedKept { get; set; }

    /// <summary>The room's linen date — reset by a departure clean and by a service that changed linen (S5 c11).</summary>
    public DateOnly? LinenLastChangedOn { get; set; }

    /// <summary>Consecutive operating days that ended without service (S5 c9 — declined days count).</summary>
    public int DaysWithoutService { get; set; }

    /// <summary>The last closed business day already counted — so a tick that runs twice counts once.</summary>
    public DateOnly? DaysCountedThrough { get; set; }

    /// <summary>The day the threshold fired; never cleared while the stay continues.</summary>
    public DateOnly? SupervisedSince { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long Version { get; set; }

    /// <summary>Whether an unresolved disagreement stands on this room.</summary>
    public bool HasDisagreement => DisagreementObservedCondition is not null && DisagreementClearedAt is null;

    /// <summary>Record a change and move the version the event will carry.</summary>
    public void Touch(DateTimeOffset at)
    {
        UpdatedAt = at;
        Version += 1;
    }
}

/// <summary>The two sides a disagreement may be cleared to.</summary>
public static class DisagreementKept
{
    public const string Ours = "OURS";
    public const string Theirs = "THEIRS";

    public static readonly IReadOnlyList<string> All = [Ours, Theirs];
}
