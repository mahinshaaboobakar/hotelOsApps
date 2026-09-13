namespace HotelOS.RoomCare.Module.Views;

/// <summary>A page of a bounded list — <c>common.v1</c> paged-with-total, so the pager's "of N" is a fact (page 64 §6).</summary>
public sealed record Paging(int Page, int PageSize, int Total);

/// <summary>The Prepare screen — the window, what changed, and the proposal (frame 2).</summary>
public sealed record PrepareView(
    string? Window,
    string? WindowStarts,
    string? WindowEnds,
    bool Open,
    string Day,
    string? PreparedAt,
    string? PreparedBy,
    int Tasks,
    int ChangesSince,
    string TriggerMode,
    string At,
    IReadOnlyList<ChangeView> Changes,
    Paging ChangesPaging,
    ProposalView Proposal);

/// <summary>One fact that arrived since the last press, and what the next press would do with it.</summary>
/// <remarks>Instants are fields, never inside the sentences — the screen formats them in the property's locale.</remarks>
public sealed record ChangeView(string At, string RoomId, string Room, string Source, string What, string? SoldAt, string OnNextPress);

/// <summary>One thing known about a room in the lane, with the instant it refers to when it has one.</summary>
public sealed record KnownView(string Text, string? At);

/// <summary>The proposal — per attendant, the rooms and minutes; the rooms nobody could take; who is here.</summary>
public sealed record ProposalView(
    string Strategy,
    IReadOnlyList<ProposedPersonView> People,
    IReadOnlyList<UnassignedRoomView> NobodyAvailable,
    int Proposed,
    int Candidates,
    bool ZoneOnPosting);

public sealed record ProposedPersonView(string UserId, string Name, int Rooms, IReadOnlyList<string> RoomNumbers, int Minutes, bool Accepted);

/// <summary>A person posted to Housekeeping, as a reassignment names them.</summary>
public sealed record AttendantView(string UserId, string Name);

public sealed record UnassignedRoomView(string TaskId, long TaskVersion, string Room, string Service, int Priority, string? SoldAt);

/// <summary>The Room states tab — every room's four facts, one data set for three views (frames 4c–4e).</summary>
public sealed record RoomStatesPageView(
    int Rooms,
    int Dirty,
    int Occupied,
    int SoldTonight,
    string? SilentSince,
    string DefaultView,
    IReadOnlyList<StatesZoneView> Zones);

public sealed record StatesZoneView(string? ZoneId, string Name, IReadOnlyList<StateRowView> Rooms);

/// <summary>One room's editable facts, the version the edit is based on, and whether it may be edited here.</summary>
public sealed record StateRowView(
    string RoomId,
    string Number,
    string Condition,
    string Occupancy,
    string? SoldAt,
    string Stay,
    string Source,
    string? SourceBy,
    string SourceAt,
    long Version,
    bool Blocked);

/// <summary>The Supervision lane — the rooms that are the supervisor's today (frame 5).</summary>
public sealed record SupervisionView(int NeedDecision, int DecidedToday, string At, IReadOnlyList<LaneRowView> Rows, Paging Paging);

public sealed record LaneRowView(
    string? SupervisionId,
    string RoomId,
    string Room,
    long RoomVersion,
    string Reason,
    string Since,
    IReadOnlyList<KnownView> WhatWeKnow,
    int? Days,
    string? TaskId,
    long? TaskVersion,
    string? Decision,
    string? DecidedBy,
    string? DecidedAt,
    string? Note);

/// <summary>The Deep clean tab — due, planned, blocked, in the job's hands, returning (frame 6).</summary>
public sealed record DeepCleanPageView(int DueThisMonth, int Planned, int InProgress, IReadOnlyList<PlanLineView> Plan, IReadOnlyList<DeepCleanRowView> Rows, Paging Paging);

public sealed record PlanLineView(string RoomType, int EveryMonths);

public sealed record DeepCleanRowView(
    string? DeepCleanId,
    long? Version,
    string RoomId,
    string Room,
    string Type,
    string? LastDone,
    string Due,
    string? WindowFrom,
    string? WindowTo,
    string? BlockRequestedAt,
    string? BlockAppliedAt,
    string? JobId,
    string? JobStatus,
    string State);
