using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Catalogue;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Queries;

/// <summary>
/// What the screens read — <c>job.read</c>: the board page, one job with all its
/// tabs, the Live tab's presence, the catalogue. Every judgment (concern,
/// accountable, running) is computed here so no screen recomputes a rule.
/// </summary>
public class JobQueries(JobsDbContext db, IKernelAuthorizer authorizer, TimeProvider clock)
{
    /// <summary>The largest page this service will serve, whatever is asked for.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The page size when a caller expresses no preference.</summary>
    public const int DefaultPageSize = 24;

    /// <summary>
    /// A page of the board, or of Scheduled — frames 1 and 6.
    /// </summary>
    /// <returns>
    /// The rows, the count matching the query, and <b>the page size actually
    /// applied</b> — CORE-Q13: a caller that asked for five hundred and quietly
    /// got a hundred would otherwise compute every page number wrongly.
    /// </returns>
    public async Task<(IReadOnlyList<JobRow> Rows, int Total, int PageSize)> ListAsync(
        RequestScope scope, JobFilter filter, CancellationToken cancellationToken)
    {
        await ReaderAsync(scope, cancellationToken);
        var query = db.Jobs.Where(j => j.PropertyId == scope.PropertyId && j.DeletedAt == null);
        query = filter.ScheduledOnly
            ? query.Where(j => j.JobStatus == JobStatus.Scheduled)
            : filter.Statuses.Count > 0
                ? query.Where(j => filter.Statuses.Contains(j.JobStatus))
                : query.Where(j => JobStatus.Open.Contains(j.JobStatus));
        if (filter.DepartmentCode is { } code) query = query.Where(j => j.DepartmentCode == code);
        if (filter.RaisedKind is { } kind) query = query.Where(j => j.RaisedKind == kind);
        if (filter.RestrictedOnly) query = query.Where(j => j.Restricted);

        var total = await query.CountAsync(cancellationToken);
        var size = filter.PageSize <= 0 ? DefaultPageSize : Math.Min(filter.PageSize, MaxPageSize);
        var page = Math.Max(0, filter.Page);
        var jobs = await query
            .OrderBy(j => j.ScheduledFor).ThenBy(j => j.DueAt ?? DateTimeOffset.MaxValue).ThenBy(j => j.CreatedAt)
            .Skip(page * size).Take(size)
            .ToListAsync(cancellationToken);
        var rows = await DecorateAsync(jobs, cancellationToken);
        if (filter.AssigneeUserId is { } assignee)
        {
            rows = rows.Where(r => r.Assignment?.AssigneeUserId == assignee).ToList();
        }

        return (rows, total, size);
    }

    /// <summary>One job with every satellite — frames 2 to 2g.</summary>
    public async Task<JobDetailRows> DetailAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken)
    {
        await ReaderAsync(scope, cancellationToken);
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.PropertyId == scope.PropertyId && j.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("job", jobId);
        var row = (await DecorateAsync([job], cancellationToken))[0];
        var steps = await db.Jobs.Where(j => j.ParentJobId == job.Id && j.DeletedAt == null).OrderBy(j => j.StepNo).ToListAsync(cancellationToken);
        return new JobDetailRows(
            row,
            await db.Assignments.Where(a => a.JobId == job.Id).OrderBy(a => a.AssignedAt).ToListAsync(cancellationToken),
            await db.WorkSessions.Where(s => s.JobId == job.Id).OrderBy(s => s.StartedAt).ToListAsync(cancellationToken),
            await db.StatusHistory.Where(h => h.JobId == job.Id).OrderByDescending(h => h.At).ToListAsync(cancellationToken),
            await db.ConcernHistory.Where(c => c.JobId == job.Id).OrderByDescending(c => c.Since).ToListAsync(cancellationToken),
            await db.Notes.Where(n => n.JobId == job.Id).OrderByDescending(n => n.At).ToListAsync(cancellationToken),
            await db.Attachments.Where(a => a.JobId == job.Id).OrderBy(a => a.At).ToListAsync(cancellationToken),
            await db.Resolutions.Where(r => r.JobId == job.Id).OrderByDescending(r => r.ResolvedAt).FirstOrDefaultAsync(cancellationToken),
            await db.Ratings.FirstOrDefaultAsync(r => r.JobId == job.Id, cancellationToken),
            await db.Links.Where(l => l.JobId == job.Id || l.LinkedJobId == job.Id).ToListAsync(cancellationToken),
            await DecorateAsync(steps, cancellationToken));
    }

    public async Task<IReadOnlyList<DepartmentPresence>> PresenceAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        await ReaderAsync(scope, cancellationToken);
        return await db.Presence.Where(p => p.PropertyId == scope.PropertyId).OrderBy(p => p.DepartmentCode).ToListAsync(cancellationToken);
    }

    /// <summary>The catalogue as this property sees it: active items, with aliases and resolutions.</summary>
    public async Task<CatalogueRows> CatalogueAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        await ReaderAsync(scope, cancellationToken);
        var off = await db.ItemPolicies.Where(p => p.PropertyId == scope.PropertyId && !p.ActiveHere).Select(p => p.ItemId).ToListAsync(cancellationToken);
        var items = await db.Items.Where(i => i.DeletedAt == null && i.Active && !off.Contains(i.Id)).OrderBy(i => i.Name).ToListAsync(cancellationToken);
        var ids = items.Select(i => i.Id).ToList();
        return new CatalogueRows(
            await db.Categories.Where(c => c.DeletedAt == null && c.Active).OrderBy(c => c.Name).ToListAsync(cancellationToken),
            items,
            await db.ItemAliases.Where(a => ids.Contains(a.ItemId)).ToListAsync(cancellationToken),
            await db.CatalogueResolutions.Where(r => r.DeletedAt == null && r.Active).ToListAsync(cancellationToken));
    }

    private async Task<List<JobRow>> DecorateAsync(List<Job> jobs, CancellationToken cancellationToken)
    {
        var ids = jobs.Select(j => j.Id).ToList();
        var assignments = await db.Assignments.Where(a => ids.Contains(a.JobId) && a.EndedAt == null).ToDictionaryAsync(a => a.JobId, cancellationToken);
        var running = await db.WorkSessions.Where(s => ids.Contains(s.JobId) && s.StoppedAt == null).Select(s => s.JobId).ToHashSetAsync(cancellationToken);
        var concerns = (await db.ConcernHistory.Where(c => ids.Contains(c.JobId)).ToListAsync(cancellationToken))
            .GroupBy(c => c.JobId).ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Since).First());
        return jobs.Select(j => new JobRow(
            j,
            assignments.GetValueOrDefault(j.Id),
            concerns.GetValueOrDefault(j.Id),
            running.Contains(j.Id))).ToList();
    }

    private Task ReaderAsync(RequestScope scope, CancellationToken cancellationToken) =>
        authorizer.RequireAsync(scope, Permissions.Read, "property", scope.PropertyId, cancellationToken);

    /// <summary>Now, for a live worked-seconds figure.</summary>
    public DateTimeOffset Now => clock.GetUtcNow();
}

/// <summary>The board's filters — frame 1's chips and pager.</summary>
public sealed record JobFilter(
    string? DepartmentCode, IReadOnlyList<string> Statuses, bool ScheduledOnly, Guid? AssigneeUserId, int PageSize, int Page)
{
    /// <summary>Only jobs a guest raised — frame 1's chip.</summary>
    /// <remarks>
    /// The board's chips are the access model drawn, so each one is a filter
    /// here rather than a word the screen keeps to itself. Two of them —
    /// this and <see cref="RestrictedOnly"/> — have no field on
    /// <c>ListJobsRequest</c>; they are the module surface's, and the gRPC
    /// contract gains them when something on it asks.
    /// </remarks>
    public string? RaisedKind { get; init; }

    /// <summary>Only the restricted ones.</summary>
    public bool RestrictedOnly { get; init; }
}

/// <summary>A job with what the row derives.</summary>
public sealed record JobRow(Job Job, JobAssignment? Assignment, JobConcernHistory? Concern, bool SessionRunning);

/// <summary>Every tab of the job view.</summary>
public sealed record JobDetailRows(
    JobRow Row,
    IReadOnlyList<JobAssignment> Assignments,
    IReadOnlyList<JobWorkSession> Sessions,
    IReadOnlyList<JobStatusHistory> StatusHistory,
    IReadOnlyList<JobConcernHistory> ConcernHistory,
    IReadOnlyList<JobNote> Notes,
    IReadOnlyList<JobAttachment> Attachments,
    JobResolution? Resolution,
    JobRating? Rating,
    IReadOnlyList<JobLink> Links,
    IReadOnlyList<JobRow> Steps);

/// <summary>The catalogue as read.</summary>
public sealed record CatalogueRows(
    IReadOnlyList<Category> Categories,
    IReadOnlyList<Item> Items,
    IReadOnlyList<ItemAlias> Aliases,
    IReadOnlyList<Resolution> Resolutions);
