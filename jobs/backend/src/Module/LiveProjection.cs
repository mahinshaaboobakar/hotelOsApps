using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

using static HotelOS.Jobs.Module.ModuleViews;

namespace HotelOS.Jobs.Module;

/// <summary>
/// The Live tab and the <c>jobs-now</c> widget — frames 5 and 9.
/// </summary>
/// <remarks>
/// <para>
/// Both surfaces answer one question — <i>what is happening right now</i> — so
/// they are counted here together and from the same rows. The widget and the
/// board disagreeing by one is the defect a person would report and nobody
/// could reproduce, and two files counting separately is how that happens.
/// </para>
/// <para>
/// <b>Who is working is as far as this service can see.</b> The people on shift
/// are Workforce's, and no client exists; the department's own presence row and
/// its running sessions are what Jobs knows, so that is what it says.
/// </para>
/// </remarks>
public sealed class LiveProjection(JobsDbContext db, JobQueries queries, BoardProjection board, TimeProvider clock)
{
    /// <summary>The Live tab.</summary>
    public async Task<ModuleViews.LiveView> LiveAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var presence = await queries.PresenceAsync(scope, cancellationToken);
        var open = await db.Jobs
            .Where(j => j.PropertyId == scope.PropertyId && j.DeletedAt == null && JobStatus.Open.Contains(j.JobStatus))
            .Select(j => new { j.Id, j.DepartmentCode, j.JobNumber })
            .ToListAsync(cancellationToken);
        var concerns = await board.LatestConcernAsync(open.Select(j => j.Id).ToList(), cancellationToken);
        var running = await db.WorkSessions
            .Where(s => s.PropertyId == scope.PropertyId && s.StoppedAt == null)
            .ToListAsync(cancellationToken);

        var departments = presence.Select(p =>
        {
            var mine = open.Where(j => j.DepartmentCode == p.DepartmentCode).ToList();
            var jobs = mine.Select(j => j.Id).ToHashSet();
            var working = running.Where(s => jobs.Contains(s.JobId)).ToList();
            return new LiveDepartmentView(
                p.DepartmentCode,
                p.DepartmentCode,
                p.Staffed ? "present" : p.Enabled ? "hours" : "off",
                p.Staffed ? $"{p.OnShift} on shift" : p.Enabled ? "nobody on shift" : "not followed here",
                working.Select(s => new LivePersonView(
                    "Staff member",
                    s.IsPaused ? "paused" : "working",
                    s.IsPaused ? "hold" : "run")).ToList(),
                p.OnShift,
                mine.Count,
                mine.Count(j => concerns.GetValueOrDefault(j.Id) == Domain.Concern.Breached));
        }).ToList();

        return new ModuleViews.LiveView(departments, await ConcernTableAsync(scope, cancellationToken), clock.GetUtcNow().ToString("o"));
    }

    /// <summary>The concern table — what the sweep last decided, newest first.</summary>
    private async Task<IReadOnlyList<ConcernRowView>> ConcernTableAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var rows = await db.ConcernHistory
            .Where(c => c.PropertyId == scope.PropertyId && c.Concern != Domain.Concern.OnTrack)
            .OrderByDescending(c => c.Since)
            .Take(50)
            .ToListAsync(cancellationToken);

        var jobs = await db.Jobs
            .Where(j => rows.Select(r => r.JobId).Contains(j.Id) && j.DeletedAt == null)
            .ToDictionaryAsync(j => j.Id, cancellationToken);

        var nudges = await db.Nudges
            .Where(n => rows.Select(r => r.JobId).Contains(n.JobId))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(c => c.JobId)
            .Where(g => jobs.ContainsKey(g.Key) && JobStatus.IsOpen(jobs[g.Key].JobStatus))
            .Select(g => g.OrderByDescending(c => c.Since).First())
            .Where(c => c.Concern != Domain.Concern.OnTrack)
            .Select(c => new ConcernRowView(
                jobs[c.JobId].JobNumber,
                jobs[c.JobId].DepartmentCode,
                c.Concern,
                c.Since.ToString("o"),
                c.AccountableUserId is null ? $"{c.AccountableRole} · not resolved" : c.AccountableRole,
                nudges.Where(n => n.JobId == c.JobId).OrderByDescending(n => n.SentAt).FirstOrDefault()?.SentAt.ToString("o") ?? "—"))
            .ToList();
    }

    /// <summary>The widget's three numbers and its worst rows.</summary>
    public async Task<JobsNowView> NowAsync(
        RequestScope scope, string? department, CancellationToken cancellationToken)
    {
        var today = await board.TodayAsync(scope, department, cancellationToken);
        var open = await db.Jobs
            .Where(j => j.PropertyId == scope.PropertyId && j.DeletedAt == null && JobStatus.Open.Contains(j.JobStatus))
            .Where(j => department == null || j.DepartmentCode == department)
            .Select(j => new { j.Id, j.JobNumber, j.DueAt })
            .ToListAsync(cancellationToken);
        var concerns = await board.LatestConcernAsync(open.Select(j => j.Id).ToList(), cancellationToken);
        var now = clock.GetUtcNow();

        var worst = open
            .Select(j => new { j.JobNumber, j.DueAt, Concern = concerns.GetValueOrDefault(j.Id, Domain.Concern.OnTrack) })
            .Where(j => j.Concern != Domain.Concern.OnTrack)
            .OrderBy(j => j.Concern == Domain.Concern.Breached ? 0 : j.Concern == Domain.Concern.Stuck ? 1 : 2)
            .ThenBy(j => j.DueAt ?? DateTimeOffset.MaxValue)
            .Take(3)
            .Select(j => new WorstRowView(
                j.JobNumber,
                j.DueAt is { } due
                    ? due < now ? $"{(int)(now - due).TotalMinutes}m over" : $"{(int)(due - now).TotalMinutes}m left"
                    : j.Concern.ToLowerInvariant(),
                j.Concern == Domain.Concern.AtRisk ? "warn" : "bad"))
            .ToList();

        var unread = await db.Nudges.CountAsync(
            n => n.PropertyId == scope.PropertyId && n.ReadAt == null && n.ToUserId == scope.UserId, cancellationToken);

        return new JobsNowView(
            department is null ? "this property" : department,
            today.Open,
            today.Running,
            concerns.Count(c => c.Value == Domain.Concern.AtRisk),
            today.Breached,
            today.Stuck,
            worst,
            unread);
    }
}
