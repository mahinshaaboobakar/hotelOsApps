namespace HotelOS.Jobs.Module;

/// <summary>
/// The shapes a Jobs screen is given — the module's view types, mirroring
/// <c>ui/board/model.ts</c> field for field.
/// </summary>
/// <remarks>
/// <para>
/// <b>Records, not dictionaries.</b> The envelope carries the application's own
/// JSON and reads none of it, so nothing but this file says what a screen is
/// promised; an anonymous object would make that promise unreadable and
/// unbreakable-by-the-compiler at the same time.
/// </para>
/// <para>
/// <b>Every judgment arrives made.</b> Concern, accountable, running seconds
/// and the paging arithmetic are computed here, so no screen recomputes a rule
/// — the audit's finding about a timer counted from the desktop's own clock is
/// the worked case.
/// </para>
/// </remarks>
public static class ModuleViews
{
    /// <summary>A page's place in a list — <c>CORE-Q13</c>'s paged pair, as the screens see it.</summary>
    public sealed record Paging(int Page, int PageSize, int Total);

    /// <summary>One row of the board — frame 1's nine columns.</summary>
    public sealed record JobRowView(
        string Id,
        string Number,
        string Where,
        string What,
        string Priority,
        string Status,
        string RaisedBy,
        string AssignedTo,
        string Concern,
        string? ConcernDetail,
        string? DueAt,
        IReadOnlyList<string> Tags,
        bool ViewerIsAssignee);

    /// <summary>A page of the board.</summary>
    public sealed record BoardPageView(IReadOnlyList<JobRowView> Rows, Paging Paging);

    /// <summary>Today's strip above the board.</summary>
    public sealed record TodayView(
        int Open,
        int Breached,
        int Stuck,
        int Running,
        int ClosedToday,
        int AvgResolveMinutes,
        string Department,
        string At);

    /// <summary>A key and its value, on a tab that lists facts.</summary>
    public sealed record DetailView(string K, string V);

    /// <summary>One work session — frame 2b.</summary>
    public sealed record SessionView(
        int No,
        string Who,
        string StartedAt,
        string? PausedAt,
        string? PauseReason,
        string? ResumedAt,
        string? StoppedAt,
        long WorkedSeconds);

    /// <summary>One line of History — status, concern and work interleaved.</summary>
    public sealed record HistoryLineView(string At, string Kind, string What, string By, string Detail);

    /// <summary>A note, and whether it is the text the job was raised with.</summary>
    public sealed record NoteView(string Who, string At, string Text, string? Photo, bool Raising);

    /// <summary>Who is signed in, as the service knows them.</summary>
    public sealed record OperatorView(string Name, string Where);

    /// <summary>A child step of a job.</summary>
    public sealed record StepView(int No, string Number, string What, string Status, string Clock, string AssignedTo);

    /// <summary>A job linked to this one.</summary>
    public sealed record LinkView(string Number, string Department, string What, string Status, string AssignedTo);

    /// <summary>The guest's rating — frame 2f.</summary>
    public sealed record RatingView(
        int Stars,
        string Text,
        string RatedAt,
        string AskedAt,
        string WindowUntil,
        string ResolvedBy,
        int MinutesRaisedToResolved);

    /// <summary>How the job was raised, as parts — the screen composes the sentence.</summary>
    public sealed record RaisedView(string At, string Via, string Kind, string Who);

    /// <summary>Everything the job view's seven tabs draw.</summary>
    public sealed record JobDetailView(
        JobRowView Row,
        RaisedView Raised,
        string? EndedAt,
        long? RunningSeconds,
        string? RunningWho,
        long TotalWorkedSeconds,
        string Accountable,
        IReadOnlyList<DetailView> WhatAndWhere,
        IReadOnlyList<DetailView> WhoAsked,
        IReadOnlyList<DetailView> PriorityAndTime,
        IReadOnlyList<DetailView> Assignment,
        string? Resolution,
        IReadOnlyList<SessionView> Sessions,
        IReadOnlyList<HistoryLineView> History,
        IReadOnlyList<NoteView> Notes,
        IReadOnlyList<StepView> Steps,
        IReadOnlyList<LinkView> Links,
        RatingView? Rating,
        IReadOnlyList<DetailView> Record);

    /// <summary>One department on the Live tab.</summary>
    public sealed record LiveDepartmentView(
        string Code,
        string Name,
        string Presence,
        string PresenceLine,
        IReadOnlyList<LivePersonView> People,
        int PeopleTotal,
        int Open,
        int Breached);

    /// <summary>Somebody working, as far as this service can see.</summary>
    public sealed record LivePersonView(string Name, string Doing, string Tone);

    /// <summary>One row of the Live tab's concern table.</summary>
    public sealed record ConcernRowView(
        string Number,
        string Department,
        string Concern,
        string Since,
        string Accountable,
        string LastNudge);

    /// <summary>The Live tab.</summary>
    public sealed record LiveView(
        IReadOnlyList<LiveDepartmentView> Departments,
        IReadOnlyList<ConcernRowView> Concern,
        string SweptAt);

    /// <summary>A scheduled row — frame 6, which is a date and nothing about cycles.</summary>
    public sealed record ScheduledRowView(
        string ScheduledFor,
        string Number,
        string Where,
        string What,
        IReadOnlyList<string> Tags,
        string RaisedBy,
        string AssignedTo,
        string? DueAt);

    /// <summary>The widget's three numbers and the worst rows.</summary>
    public sealed record JobsNowView(
        string Scope,
        int Open,
        int Running,
        int AtRisk,
        int Breached,
        int Stuck,
        IReadOnlyList<WorstRowView> Worst,
        int UnreadNudges);

    /// <summary>One of the widget's worst rows.</summary>
    public sealed record WorstRowView(string Number, string Line, string Tone);
}
