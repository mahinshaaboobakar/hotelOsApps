using HotelOS.Jobs.Application.Calendar;
using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Module;

/// <summary>
/// What the dock widgets ask — <c>SHELL-Q35</c>'s canvas, approved 2026-09-03.
/// </summary>
/// <remarks>
/// <para>
/// A widget answers <b>one question, whole</b>, so each has its own read rather
/// than a screen's payload trimmed at the edge: a widget fed from the board's
/// page would show whatever that page happened to hold.
/// </para>
/// <para>
/// Two are served here — <i>The Board</i> and <i>Blocked</i> — because those
/// two are computable exactly as drawn. The other three drawn for Jobs are
/// not, and the reasons are recorded in the design chapter's §9 rather than
/// papered over with an approximation: <b>an uncomputable number is absent,
/// never approximate</b> (56 §"Shape rules").
/// </para>
/// </remarks>
public sealed class WidgetProjection(JobsDbContext db, TimeProvider clock, IPropertyDirectory directory)
{
    /// <summary>How many rows a widget lists — 56's rule: it shows what fits.</summary>
    private const int Rows = 3;

    /// <summary>
    /// <i>The Board</i> — the shape of the work, and what has waited longest
    /// unclaimed.
    /// </summary>
    /// <remarks>
    /// The four figures are the canvas's, read against this design's own
    /// vocabulary: <c>new</c> is RAISED, <c>done</c> is what reached RESOLVED
    /// or CLOSED today. The frame says the rest are counted in the app and not
    /// here, which is why ASSIGNED and ACCEPTED have no figure.
    /// </remarks>
    public async Task<ModuleViews.BoardWidgetView> BoardAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        // "done" is today's, and today began at the property's midnight — it was
        // the last 24 hours, which counted yesterday evening's work as today's.
        var since = await DayStartedAsync(scope, cancellationToken);
        var jobs = db.Jobs.Where(j => j.PropertyId == scope.PropertyId && j.DeletedAt == null);

        var raised = await jobs.CountAsync(j => j.JobStatus == JobStatus.Raised, cancellationToken);
        var running = await jobs.CountAsync(j => j.JobStatus == JobStatus.InProgress, cancellationToken);
        var held = await jobs.CountAsync(j => j.JobStatus == JobStatus.OnHold, cancellationToken);
        var done = await jobs.CountAsync(
            j => (j.JobStatus == JobStatus.Resolved || j.JobStatus == JobStatus.Closed) && j.UpdatedAt >= since,
            cancellationToken);

        // "Longest in NEW — nobody has taken these": oldest first, because the
        // question is what has been waiting, not what is newest.
        var waiting = await jobs
            .Where(j => j.JobStatus == JobStatus.Raised)
            .OrderBy(j => j.CreatedAt)
            .Take(Rows)
            .Select(j => new { j.Id, j.JobNumber, j.Summary, j.LocationId, j.CreatedAt })
            .ToListAsync(cancellationToken);

        var rows = new List<ModuleViews.WidgetRowView>(waiting.Count);
        foreach (var job in waiting)
        {
            rows.Add(new ModuleViews.WidgetRowView(
                job.Id.ToString(),
                job.JobNumber,
                job.Summary,
                Elapsed(now - job.CreatedAt),
                "warn"));
        }

        return new ModuleViews.BoardWidgetView(raised, running, held, done, rows);
    }

    /// <summary>
    /// <i>Blocked</i> — what is waiting, and whose clock runs while it waits.
    /// </summary>
    /// <remarks>
    /// Two states and not one, because the difference is whose delay it is: a
    /// job ON_HOLD has its concern clock stopped, and a job whose session is
    /// paused does not — the clock keeps running. That distinction is the
    /// widget's whole point, and the design carries it in two different places
    /// (the job's status, and the work session's pause), which is why this
    /// reads both.
    /// </remarks>
    public async Task<ModuleViews.BlockedWidgetView> BlockedAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var held = await db.Jobs
            .Where(j => j.PropertyId == scope.PropertyId && j.DeletedAt == null && j.JobStatus == JobStatus.OnHold)
            .OrderBy(j => j.UpdatedAt)
            .Select(j => new { j.Id, j.JobNumber, j.Summary, j.HoldReason, j.UpdatedAt })
            .ToListAsync(cancellationToken);

        var paused = await db.WorkSessions
            .Where(s => s.PropertyId == scope.PropertyId && s.StoppedAt == null && s.PausedAt != null && s.ResumedAt == null)
            .OrderBy(s => s.PausedAt)
            .Join(db.Jobs, s => s.JobId, j => j.Id, (s, j) => new { j.Id, j.JobNumber, j.Summary, s.PauseReason, s.PausedAt })
            .ToListAsync(cancellationToken);

        return new ModuleViews.BlockedWidgetView(
            held.Count,
            paused.Count,
            held.Take(Rows).Select(j => new ModuleViews.WidgetRowView(
                j.Id.ToString(), j.JobNumber, j.HoldReason ?? "on hold", Elapsed(now - j.UpdatedAt), "hold")).ToList(),
            paused.Take(Rows).Select(p => new ModuleViews.WidgetRowView(
                p.Id.ToString(), p.JobNumber, p.PauseReason ?? "paused", Elapsed(now - (p.PausedAt ?? now)), "run")).ToList());
    }


    /// <summary>
    /// <i>By Priority</i> — how much of the open work is urgent, and how much
    /// nobody has judged yet.
    /// </summary>
    /// <remarks>
    /// The four figures count <b>open</b> jobs, which is what the frame draws: a
    /// closed P1 is not pressure. <c>NOT_TRIAGED</c> stands apart from P3
    /// because an unjudged job is not a low one — it is a job whose priority
    /// nobody has set, and folding it into P3 would hide exactly the queue this
    /// widget exists to surface.
    /// </remarks>
    public async Task<ModuleViews.PriorityWidgetView> ByPriorityAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var open = db.Jobs.Where(j =>
            j.PropertyId == scope.PropertyId && j.DeletedAt == null && JobStatus.Open.Contains(j.JobStatus));

        var counts = await open
            .GroupBy(j => j.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int Held(string priority) => counts.FirstOrDefault(c => c.Priority == priority)?.Count ?? 0;

        // P1 and P2 only, and oldest first — the frame's own heading. Newest
        // first would put the job raised a minute ago above the one that has
        // been urgent for an hour.
        var pressing = await open
            .Where(j => j.Priority == Priority.P1 || j.Priority == Priority.P2)
            .OrderBy(j => j.Priority)
            .ThenBy(j => j.CreatedAt)
            .Take(Rows)
            .Select(j => new { j.Id, j.JobNumber, j.Summary, j.Priority, j.RaisedKind, j.PriorityDecidedBy })
            .ToListAsync(cancellationToken);

        return new ModuleViews.PriorityWidgetView(
            Held(Priority.P1),
            Held(Priority.P2),
            Held(Priority.P3),
            Held(Priority.NotTriaged),
            pressing
                .Select(j => new ModuleViews.PriorityRowView(
                    j.Id.ToString(), j.JobNumber, j.Summary, j.Priority, Asked(j.RaisedKind, j.PriorityDecidedBy)))
                .ToList());
    }

    /// <summary>
    /// <i>Due Soon</i> — what is already late, then what falls due within two
    /// hours.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two groups, one clock.</b> This follows the frame as corrected on
    /// 2026-09-06, whose headings had been naming each other's rows: overdue
    /// furthest-past-due first, then soonest-due first. Both groups are the same
    /// <c>due_at</c> compared with now — an urgency split, which the model
    /// carries. The deadline-source split the frame used to draw was not one:
    /// <c>priority_decided_by</c> records FLOW as the source of a priority,
    /// never of a deadline.
    /// </para>
    /// <para>
    /// A job with no <c>due_at</c> is in neither figure. Nothing about it is
    /// late and nothing is close, and counting it would need a deadline this
    /// design has not got.
    /// </para>
    /// </remarks>
    public async Task<ModuleViews.DueSoonWidgetView> DueSoonAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var horizon = now.AddHours(2);

        var due = db.Jobs.Where(j =>
            j.PropertyId == scope.PropertyId
            && j.DeletedAt == null
            && JobStatus.Open.Contains(j.JobStatus)
            && j.DueAt != null);

        var late = await due
            .Where(j => j.DueAt < now)
            .OrderBy(j => j.DueAt)
            .Take(Rows)
            .Select(j => new { j.Id, j.JobNumber, j.Summary, j.DueAt, j.CreatedAt })
            .ToListAsync(cancellationToken);

        var soon = await due
            .Where(j => j.DueAt >= now && j.DueAt <= horizon)
            .OrderBy(j => j.DueAt)
            .Take(Rows)
            .Select(j => new { j.Id, j.JobNumber, j.Summary, j.DueAt, j.CreatedAt })
            .ToListAsync(cancellationToken);

        var overdue = await due.CountAsync(j => j.DueAt < now, cancellationToken);
        var within = await due.CountAsync(j => j.DueAt >= now && j.DueAt <= horizon, cancellationToken);

        return new ModuleViews.DueSoonWidgetView(
            overdue,
            within,
            late.Select(j => new ModuleViews.DueRowView(
                j.Id.ToString(),
                j.JobNumber,
                j.Summary,
                Allowance(j.CreatedAt, j.DueAt),
                "+" + Elapsed(now - j.DueAt!.Value),
                "bad")).ToList(),
            soon.Select(j => new ModuleViews.DueRowView(
                j.Id.ToString(),
                j.JobNumber,
                j.Summary,
                Allowance(j.CreatedAt, j.DueAt),
                "in " + Elapsed(j.DueAt!.Value - now),
                "warn")).ToList());
    }

    /// <summary>
    /// <i>Raised Today</i> — how much came in, how much went out, and of what
    /// kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Of what kind means by category</b>, per the frame redrawn 2026-09-05
    /// and approved 2026-09-08. It counted by <i>intent</i> — Fix · Prepare ·
    /// Deliver · Check — and the walkthrough had already replaced the job
    /// <c>type</c> with the catalogue's category › item, so the old frame asked
    /// for a figure this design cannot produce.
    /// </para>
    /// <para>
    /// <b>Today is the property's day, not UTC's.</b> It starts at the
    /// property's own midnight, through the timezone Master Data holds; a UTC
    /// day rolls over mid-evening in Kochi, and the widget would report that
    /// nothing came in this morning.
    /// </para>
    /// </remarks>
    public async Task<ModuleViews.RaisedTodayWidgetView> RaisedTodayAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var since = await DayStartedAsync(scope, cancellationToken);

        var today = db.Jobs.Where(j =>
            j.PropertyId == scope.PropertyId && j.DeletedAt == null && j.CreatedAt >= since);

        var raised = await today.CountAsync(cancellationToken);
        var closed = await db.Jobs.CountAsync(
            j => j.PropertyId == scope.PropertyId
                && j.DeletedAt == null
                && (j.JobStatus == JobStatus.Closed || j.JobStatus == JobStatus.Resolved)
                && j.UpdatedAt >= since,
            cancellationToken);

        var grouped = await today
            .GroupBy(j => new { j.CategoryId, j.DepartmentCode })
            .Select(g => new { g.Key.CategoryId, g.Key.DepartmentCode, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync(cancellationToken);

        var ids = grouped.Select(g => g.CategoryId).ToList();
        var named = await db.Categories
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return new ModuleViews.RaisedTodayWidgetView(
            raised,
            closed,
            grouped
                .Take(Rows)
                .Select(c => new ModuleViews.CategoryCountView(
                    named.GetValueOrDefault(c.CategoryId, "Uncatalogued"), c.DepartmentCode, c.Count))
                .ToList(),
            // "Everything else · n categories" — how many categories did not
            // fit, not how many jobs are in them. The frame says categories.
            Math.Max(0, grouped.Count - Rows));
    }

    /// <summary>The property's own midnight — the start of the day being counted (calendar day, pending WF-Q21).</summary>
    /// <remarks>
    /// It fell back to UTC midnight when the zone was unknown; the reference refuses
    /// by name instead, because a day in a zone nobody chose is a claim nobody made.
    /// </remarks>
    private async Task<DateTimeOffset> DayStartedAsync(RequestScope scope, CancellationToken cancellationToken) =>
        (await PropertyCalendar.ForAsync(directory, scope.PropertyId, cancellationToken)).TodayStartedAt(clock.GetUtcNow());

    /// <summary>What the job was allowed — from raising to its due time.</summary>
    private static string Allowance(DateTimeOffset createdAt, DateTimeOffset? dueAt) =>
        dueAt is null ? "no deadline" : "SLA " + Elapsed(dueAt.Value - createdAt);

    /// <summary>Who asked, in the frame's vocabulary — a guest, the flow, or the house.</summary>
    private static string Asked(string raisedKind, string decidedBy) =>
        raisedKind == RaisedKind.Guest ? "guest"
        : decidedBy == PriorityDecidedBy.Flow ? "flow"
        : "staff";

    /// <summary>How long, in the shortest true form — 2d, 4h, 22m.</summary>
    private static string Elapsed(TimeSpan span) => span switch
    {
        { TotalDays: >= 1 } => $"{(int)span.TotalDays}d",
        { TotalHours: >= 1 } => $"{(int)span.TotalHours}h",
        _ => $"{Math.Max(0, (int)span.TotalMinutes)}m",
    };
}
