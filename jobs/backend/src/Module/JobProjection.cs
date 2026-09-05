using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

using static HotelOS.Jobs.Module.ModuleViews;

namespace HotelOS.Jobs.Module;

/// <summary>
/// One job with all seven of its tabs — frames 2 to 2g.
/// </summary>
/// <remarks>
/// <para>
/// <b>The running timer is the service's figure, never the desktop's.</b> The
/// screen is given seconds worked as of this reply and counts up from there; a
/// desktop whose clock is minutes off would otherwise show a number the
/// property never had. The audit found exactly that, reading 42:16:53 for a
/// session that had run twenty-three minutes.
/// </para>
/// <para>
/// The composed lines a screen used to build from prose — how a job was raised,
/// what it ended as — arrive as parts, so every instant on the screen goes
/// through the one formatter and wears the property's zone and locale.
/// </para>
/// </remarks>
public sealed class JobProjection(JobsDbContext db, JobQueries queries, BoardProjection board, TimeProvider clock)
{
    /// <summary>The whole job view.</summary>
    public async Task<JobDetailView> DetailAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken)
    {
        var rows = await queries.DetailAsync(scope, jobId, cancellationToken);
        var now = clock.GetUtcNow();
        var job = rows.Row.Job;
        var running = rows.Sessions.FirstOrDefault(s => s.StoppedAt is null);
        var row = await board.RowAsync(scope, rows.Row, cancellationToken);

        return new JobDetailView(
            row,
            new RaisedView(job.CreatedAt.ToString("o"), job.RaisedVia, job.RaisedKind, Naming.Raiser(job)),
            JobStatus.IsTerminal(job.JobStatus) ? job.UpdatedAt.ToString("o") : null,
            running?.WorkedSecondsAt(now),
            running is null ? null : "Staff member",
            rows.Sessions.Sum(s => s.WorkedSecondsAt(now)),
            Accountable(rows.Row),
            [new("Where", row.Where), new("What", job.Summary), new("Details", job.Details ?? "—"), new("Department", job.DepartmentCode)],
            [new("Raised by", row.RaisedBy), new("Via", job.RaisedVia), new("Kind", job.RaisedKind)],
            PriorityAndTime(job),
            Assignment(rows),
            Resolution(rows),
            Sessions(rows),
            History(rows),
            Notes(rows, job),
            await StepsAsync(scope, rows, cancellationToken),
            await LinksAsync(scope, rows, job, cancellationToken),
            Rating(rows, job),
            Record(job));
    }

    private static string Accountable(JobRow row) =>
        row.Concern is { } concern
            ? concern.AccountableUserId is null
                ? $"{concern.AccountableRole} · not resolved"
                : concern.AccountableRole
            : LadderRole.Assignee;

    private static IReadOnlyList<DetailView> PriorityAndTime(Job job) =>
    [
        new("Priority", job.Priority),
        new("Decided by", job.PriorityDecidedBy),
        new("Due", job.DueAt?.ToString("o") ?? "no clock"),
        new("Scheduled for", job.ScheduledFor?.ToString("yyyy-MM-dd") ?? "—"),
    ];

    private static IReadOnlyList<DetailView> Assignment(JobDetailRows rows)
    {
        var current = rows.Assignments.LastOrDefault(a => a.EndedAt is null);
        return
        [
            new("Holder", Naming.Assignee(current)),
            new("How", current?.How ?? "—"),
            new("Assigned", current?.AssignedAt.ToString("o") ?? "—"),
            new("Accepted", current?.AcceptedAt?.ToString("o") ?? "not yet"),
            new("Assignments", rows.Assignments.Count.ToString()),
        ];
    }

    private static string? Resolution(JobDetailRows rows) =>
        rows.Resolution is { } resolution
            ? resolution.Note is { Length: > 0 } note ? note : "resolved"
            : null;

    private static IReadOnlyList<SessionView> Sessions(JobDetailRows rows)
    {
        var no = 0;
        return rows.Sessions.Select(s => new SessionView(
            ++no,
            "Staff member",
            s.StartedAt.ToString("o"),
            s.PausedAt?.ToString("o"),
            s.PauseReason,
            s.ResumedAt?.ToString("o"),
            s.StoppedAt?.ToString("o"),
            s.WorkedSeconds)).ToList();
    }

    /// <summary>Status, concern and work in one column, newest first.</summary>
    private static IReadOnlyList<HistoryLineView> History(JobDetailRows rows)
    {
        var lines = new List<HistoryLineView>();
        lines.AddRange(rows.StatusHistory.Select(h => new HistoryLineView(
            h.At.ToString("o"), "status", $"{h.FromStatus} → {h.ToStatus}", Who(h.ByUserId, h.ByWhat), h.Note ?? string.Empty)));
        lines.AddRange(rows.ConcernHistory.Select(c => new HistoryLineView(
            c.Since.ToString("o"), "concern", c.Concern, c.AccountableRole, c.Reason)));
        lines.AddRange(rows.Sessions.Select(s => new HistoryLineView(
            s.StartedAt.ToString("o"), "work", s.StoppedAt is null ? "started" : "worked", "Staff member",
            $"{s.WorkedSeconds}s")));
        return lines.OrderByDescending(l => l.At).ToList();
    }

    private static string Who(Guid? user, string? byWhat) =>
        byWhat is { Length: > 0 } what ? what : user is null ? Naming.Nobody : "Staff member";

    /// <summary>The notes, with the raising text marked as such.</summary>
    private static IReadOnlyList<NoteView> Notes(JobDetailRows rows, Job job)
    {
        var notes = rows.Notes.Select(n => new NoteView(
            n.AuthorKind == RaisedKind.Guest ? "Guest" : n.AuthorKind == RaisedKind.Application ? "Another application" : "Staff member",
            n.At.ToString("o"),
            n.Text,
            null,
            false)).ToList();

        if (job.Details is { Length: > 0 } details)
        {
            notes.Add(new NoteView(Naming.Raiser(job), job.CreatedAt.ToString("o"), details, null, true));
        }

        return notes;
    }

    private async Task<IReadOnlyList<StepView>> StepsAsync(
        RequestScope scope, JobDetailRows rows, CancellationToken cancellationToken)
    {
        var steps = new List<StepView>();
        foreach (var step in rows.Steps)
        {
            var view = await board.RowAsync(scope, step, cancellationToken);
            steps.Add(new StepView(
                step.Job.StepNo ?? 0, view.Number, view.What, view.Status,
                view.DueAt ?? "no clock", view.AssignedTo));
        }

        return steps;
    }

    private async Task<IReadOnlyList<LinkView>> LinksAsync(
        RequestScope scope, JobDetailRows rows, Job job, CancellationToken cancellationToken)
    {
        var others = rows.Links.Select(l => l.JobId == job.Id ? l.LinkedJobId : l.JobId).ToList();
        if (others.Count == 0) return [];

        var linked = await db.Jobs
            .Where(j => others.Contains(j.Id) && j.PropertyId == scope.PropertyId && j.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var assignments = await db.Assignments
            .Where(a => others.Contains(a.JobId) && a.EndedAt == null)
            .ToDictionaryAsync(a => a.JobId, cancellationToken);

        return linked.Select(j => new LinkView(
            j.JobNumber, j.DepartmentCode, j.Summary, j.JobStatus,
            Naming.Assignee(assignments.GetValueOrDefault(j.Id)))).ToList();
    }

    private static RatingView? Rating(JobDetailRows rows, Job job)
    {
        if (rows.Rating is not { } rating) return null;
        var resolved = rows.Resolution?.ResolvedAt ?? job.UpdatedAt;
        return new RatingView(
            rating.Stars,
            rating.Text ?? string.Empty,
            rating.RatedAt.ToString("o"),
            resolved.ToString("o"),
            resolved.AddHours(24).ToString("o"),
            "Staff member",
            (int)(resolved - job.CreatedAt).TotalMinutes);
    }

    /// <summary>The Record tab — the row as it is stored, for the person who needs the fact.</summary>
    private static IReadOnlyList<DetailView> Record(Job job) =>
    [
        new("Job id", job.Id.ToString()),
        new("Number", job.JobNumber),
        new("Property", job.PropertyId.ToString()),
        new("Category", job.CategoryId.ToString()),
        new("Item", job.ItemId.ToString()),
        new("Location", job.LocationId.ToString()),
        new("Asset", job.AssetId?.ToString() ?? "—"),
        new("Policy", job.ConcernPolicyId?.ToString() ?? "—"),
        new("Created", job.CreatedAt.ToString("o")),
        new("Updated", job.UpdatedAt.ToString("o")),
        new("Version", job.Version.ToString()),
        new("Restricted", job.Restricted ? "yes" : "no"),
    ];
}
