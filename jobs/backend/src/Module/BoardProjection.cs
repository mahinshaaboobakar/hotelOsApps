using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

using static HotelOS.Jobs.Module.ModuleViews;

namespace HotelOS.Jobs.Module;

/// <summary>
/// The board, today's strip and the scheduled list, as the screens draw them —
/// frames 1, 1's strip and 6.
/// </summary>
/// <remarks>
/// Every figure here is counted in the database at the moment of the call. The
/// strip in particular is not a cache: a number a person acts on that was true
/// a minute ago is the class of defect the sweep exists to avoid.
/// </remarks>
public sealed class BoardProjection(JobsDbContext db, JobQueries queries, Naming naming, TimeProvider clock)
{
    /// <summary>A page of the board, or of Scheduled.</summary>
    public async Task<BoardPageView> PageAsync(
        RequestScope scope, JobFilter filter, CancellationToken cancellationToken)
    {
        var (rows, total, size) = await queries.ListAsync(scope, filter, cancellationToken);
        var views = new List<JobRowView>(rows.Count);
        foreach (var row in rows)
        {
            views.Add(await RowAsync(scope, row, cancellationToken));
        }

        return new BoardPageView(views, new ModuleViews.Paging(Math.Max(0, filter.Page), size, total));
    }

    /// <summary>One row, with its place named and its judgments already made.</summary>
    public async Task<JobRowView> RowAsync(RequestScope scope, JobRow row, CancellationToken cancellationToken)
    {
        var job = row.Job;
        return new JobRowView(
            job.Id.ToString(),
            job.JobNumber,
            await naming.PlaceAsync(scope.PropertyId, job.LocationId, cancellationToken),
            job.Summary,
            job.Priority,
            job.JobStatus,
            Naming.Raiser(job),
            Naming.Assignee(row.Assignment),
            row.Concern?.Concern ?? Domain.Concern.OnTrack,
            Detail(row, clock.GetUtcNow()),
            job.DueAt?.ToString("o"),
            Tags(job),
            row.Assignment?.AssigneeUserId is { } holder && scope.UserId == holder);
    }

    /// <summary>The strip's six figures — frame 1, counted now.</summary>
    public async Task<TodayView> TodayAsync(
        RequestScope scope, string? department, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var since = now.AddHours(-24);
        var jobs = db.Jobs.Where(j => j.PropertyId == scope.PropertyId && j.DeletedAt == null);
        if (department is { } code) jobs = jobs.Where(j => j.DepartmentCode == code);

        var open = await jobs.Where(j => JobStatus.Open.Contains(j.JobStatus)).Select(j => j.Id).ToListAsync(cancellationToken);
        var concerns = await LatestConcernAsync(open, cancellationToken);
        var running = await db.WorkSessions
            .CountAsync(s => open.Contains(s.JobId) && s.StoppedAt == null, cancellationToken);

        var closed = await jobs
            .Where(j => j.JobStatus == JobStatus.Closed && j.UpdatedAt >= since)
            .Select(j => new { j.Id, j.CreatedAt, j.UpdatedAt })
            .ToListAsync(cancellationToken);

        return new TodayView(
            open.Count,
            concerns.Count(c => c.Value == Domain.Concern.Breached),
            concerns.Count(c => c.Value == Domain.Concern.Stuck),
            running,
            closed.Count,
            closed.Count == 0 ? 0 : (int)closed.Average(c => (c.UpdatedAt - c.CreatedAt).TotalMinutes),
            department ?? "all departments",
            now.ToString("o"));
    }

    /// <summary>The scheduled list — a date, and nothing about cycles (frame 6).</summary>
    public async Task<IReadOnlyList<ScheduledRowView>> ScheduledAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var page = await PageAsync(
            scope,
            new JobFilter(null, [], ScheduledOnly: true, null, JobQueries.MaxPageSize, 0),
            cancellationToken);

        var jobs = await db.Jobs
            .Where(j => j.PropertyId == scope.PropertyId && j.JobStatus == JobStatus.Scheduled && j.DeletedAt == null)
            .ToDictionaryAsync(j => j.Id.ToString(), j => j.ScheduledFor, cancellationToken);

        return page.Rows.Select(row => new ScheduledRowView(
            jobs.GetValueOrDefault(row.Id)?.ToString("yyyy-MM-dd") ?? string.Empty,
            row.Number,
            row.Where,
            row.What,
            row.Tags,
            row.RaisedBy,
            row.AssignedTo,
            row.DueAt)).ToList();
    }

    /// <summary>The latest concern verdict for each of these jobs.</summary>
    public async Task<Dictionary<Guid, string>> LatestConcernAsync(
        IReadOnlyList<Guid> jobs, CancellationToken cancellationToken)
    {
        var rows = await db.ConcernHistory
            .Where(c => jobs.Contains(c.JobId))
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(c => c.JobId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Since).First().Concern);
    }

    /// <summary>The short phrase under a concern — "14m", "6m left", "not accepted 9m".</summary>
    private static string? Detail(JobRow row, DateTimeOffset now)
    {
        if (row.Concern is not { } concern) return null;
        var over = row.Job.DueAt is { } due ? now - due : TimeSpan.Zero;
        return concern.Concern switch
        {
            Domain.Concern.Breached when over > TimeSpan.Zero => $"{(int)over.TotalMinutes}m over",
            Domain.Concern.AtRisk when over < TimeSpan.Zero => $"{(int)-over.TotalMinutes}m left",
            Domain.Concern.Stuck => concern.Reason is { Length: > 0 } why ? why : "stuck",
            _ => row.Job.JobStatus == JobStatus.OnHold && row.Job.HoldReason is { } held ? $"waiting · {held}" : null,
        };
    }

    /// <summary>The row's small uppercase marks — restricted, a step, a link.</summary>
    private static IReadOnlyList<string> Tags(Job job)
    {
        var tags = new List<string>();
        if (job.Restricted) tags.Add("restricted");
        if (job.StepNo is { } step) tags.Add($"step {step}");
        return tags;
    }
}
