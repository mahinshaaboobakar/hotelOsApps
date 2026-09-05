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
public sealed class WidgetProjection(JobsDbContext db, TimeProvider clock)
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
        var since = now.AddHours(-24);
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

    /// <summary>How long, in the shortest true form — 2d, 4h, 22m.</summary>
    private static string Elapsed(TimeSpan span) => span switch
    {
        { TotalDays: >= 1 } => $"{(int)span.TotalDays}d",
        { TotalHours: >= 1 } => $"{(int)span.TotalHours}h",
        _ => $"{Math.Max(0, (int)span.TotalMinutes)}m",
    };
}
