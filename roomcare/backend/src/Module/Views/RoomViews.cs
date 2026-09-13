namespace HotelOS.RoomCare.Module.Views;

/// <summary>A room's page — its line, its facts, the day in order, the decision, inspection, jobs (frames 4, 4b).</summary>
public sealed record RoomPageView(
    BoardRoomView Line,
    string Type,
    string Zone,
    DisagreementView? Disagreement,
    RoomFactsView Facts,
    IReadOnlyList<TimelineEntryView> Today,
    DecisionView? Decision,
    InspectionCardView Inspection,
    IReadOnlyList<JobTouchView> Jobs,
    IReadOnlyList<HistoryDayView> History,
    string? SupervisionId,
    string WhoLeads);

public sealed record DisagreementView(string Ours, string OursSource, string? OursBy, string OursAt, string Theirs, string TheirsSource, string TheirsAt);

public sealed record RoomFactsView(
    string Occupancy,
    IReadOnlyList<string> StayStatuses,
    string? SoldAt,
    string? LinenLastChangedOn,
    string? LinenDueOn,
    string? DeepCleanDueOn,
    int DaysWithoutService,
    string? SupervisedSince);

/// <summary>One line of the day, in order — prepared, started, an attempt, done, observed, a job, an issue, a decision.</summary>
public sealed record TimelineEntryView(string At, string Kind, string What, string? By);

/// <summary>What the decision saw and answered — recorded, not re-derived.</summary>
public sealed record DecisionView(
    string? Condition,
    string? Occupancy,
    IReadOnlyList<string> StayStatuses,
    string? SoldAt,
    string? Window,
    long RuleVersion,
    string? Reason,
    string Service,
    int Minutes,
    int Priority,
    string InspectionRule,
    string DecidedBy,
    string? RunBy);

/// <summary>The inspection card — the rule, whether it was requested, how it was answered.</summary>
public sealed record InspectionCardView(bool ApplicationInstalled, string Rule, string? RequestedAt, string? AnsweredAt, string? Result, string? Note);

public sealed record JobTouchView(string JobId, string? JobNumber, string At, string? Summary, string Kind, string? By);

/// <summary>One past day of the room, for the fourteen-day history tab.</summary>
public sealed record HistoryDayView(string Day, IReadOnlyList<string> Services, IReadOnlyList<string?> Outcomes);
