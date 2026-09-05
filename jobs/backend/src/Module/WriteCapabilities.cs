using HotelOS.Jobs.Application.Assignment;
using HotelOS.Jobs.Application.Cancellation;
using HotelOS.Jobs.Application.Completion;
using HotelOS.Jobs.Application.Course;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Application.Notes;
using HotelOS.Jobs.Application.Work;
using HotelOS.Jobs.Domain;
using HotelOS.Platform;
using Microsoft.Extensions.DependencyInjection;

using static HotelOS.Platform.ModuleEnvelope;

namespace HotelOS.Jobs.Module;

/// <summary>
/// The six capabilities that change a job — <c>job.create</c>,
/// <c>job.assign</c>, <c>job.complete</c>, <c>job.cancel</c> and
/// <c>job.amend</c>, as the screens' buttons call them.
/// </summary>
/// <remarks>
/// <para>
/// Each method is a thin translation: the bundle's JSON to the command the
/// application service already takes, and the service's own answer back. No
/// rule lives here — a rule in a route is a rule the gRPC surface does not
/// have, and two answers to one question is what the layering forbids.
/// </para>
/// <para>
/// <b>Every one of these is versioned.</b> The screen sends the version it
/// drew, and the service refuses a stale one — which is what makes a conflict
/// visible to the second person rather than silently deciding for them.
/// </para>
/// </remarks>
public static class WriteCapabilities
{
    /// <summary><c>job.create</c> — raise, from frame 3.</summary>
    public static async Task<object?> CreateAsync(
        IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        if (request.Method != "raise") throw new InvalidRequestException($"job.create has no method '{request.Method}'");

        var body = request.Body;
        var job = await services.GetRequiredService<JobService>().RaiseAsync(
            request.Scope,
            new RaiseJobCommand
            {
                ItemId = body.Id("itemId"),
                LocationId = body.Id("locationId"),
                AssetId = body.OptionalId("assetId"),
                Summary = body.Text("summary"),
                Details = body.OptionalText("details"),
                Priority = body.OptionalText("priority"),
                RaisedVia = body.OptionalText("raisedVia") ?? RaisedVia.App,
                RaisedKind = body.OptionalText("raisedKind") ?? RaisedKind.Staff,
                RaisedById = request.Caller.UserId,
                StayId = body.OptionalId("stayId"),
                ScheduledFor = Day(body.OptionalText("scheduledFor")),
                Restricted = body.Flag("restricted") ? true : null,
                AssignToUserId = body.OptionalId("assignToUserId"),
                AssignToTeamId = body.OptionalId("assignToTeamId"),
                ParentJobId = body.OptionalId("parentJobId"),
            },
            cancellationToken);

        return Answer(job);
    }

    /// <summary><c>job.assign</c> — assign, reassign, and accepting one's own.</summary>
    public static async Task<object?> AssignAsync(
        IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var assignment = services.GetRequiredService<AssignmentService>();

        var job = request.Method switch
        {
            "assign" or "reassign" => await assignment.AssignAsync(
                request.Scope,
                new AssignCommand
                {
                    JobId = body.Id("id"),
                    ExpectedVersion = body.Version(),
                    UserId = body.OptionalId("userId"),
                    TeamId = body.OptionalId("teamId"),
                },
                cancellationToken),

            // "Take it" — the person asking becomes the assignee. The id comes
            // from the token rather than from the body: a screen that could
            // name whose job it is would be assigning on somebody else's
            // behalf under the word "take".
            "take" => await assignment.AssignAsync(
                request.Scope,
                new AssignCommand
                {
                    JobId = body.Id("id"),
                    ExpectedVersion = body.Version(),
                    UserId = request.Caller.UserId,
                },
                cancellationToken),

            "accept" => await assignment.AcceptAsync(request.Scope, body.Id("id"), body.Version(), cancellationToken),

            _ => throw new InvalidRequestException($"job.assign has no method '{request.Method}'"),
        };

        return Answer(job);
    }

    /// <summary><c>job.complete</c> — the work session's verbs, resolving and closing.</summary>
    /// <remarks>
    /// The session verbs ride this capability rather than one of their own:
    /// starting and pausing are the assignee's own acts on a job they hold, and
    /// a separate permission would be one an administrator never granted.
    /// </remarks>
    public static async Task<object?> CompleteAsync(
        IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var work = services.GetRequiredService<WorkSessionService>();
        var completion = services.GetRequiredService<CompletionService>();
        var id = body.Id("id");

        switch (request.Method)
        {
            case "start": return Session(await work.StartAsync(request.Scope, id, cancellationToken));
            case "pause": return Session(await work.PauseAsync(request.Scope, id, body.OptionalText("reason"), cancellationToken));
            case "resume": return Session(await work.ResumeAsync(request.Scope, id, cancellationToken));
            case "stop": return Session(await work.StopAsync(request.Scope, id, cancellationToken));

            case "resolve":
                return Answer(await completion.ResolveAsync(
                    request.Scope,
                    new ResolveCommand
                    {
                        JobId = id,
                        ExpectedVersion = body.Version(),
                        ResolutionId = body.OptionalId("resolutionId"),
                        Note = body.OptionalText("note"),
                    },
                    cancellationToken));

            case "close":
                return Answer(await completion.CloseAsync(request.Scope, id, body.Version(), cancellationToken));

            case "reopen":
                return Answer(await completion.ReopenAsync(
                    request.Scope, id, body.Version(), body.OptionalText("note"), cancellationToken));

            default:
                throw new InvalidRequestException($"job.complete has no method '{request.Method}'");
        }
    }

    /// <summary><c>job.cancel</c> — ending a job that should not have been raised.</summary>
    public static async Task<object?> CancelAsync(
        IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        if (request.Method != "cancel") throw new InvalidRequestException($"job.cancel has no method '{request.Method}'");

        var body = request.Body;
        var job = await services.GetRequiredService<CancellationService>().CancelAsync(
            request.Scope,
            new CancelCommand { JobId = body.Id("id"), ExpectedVersion = body.Version(), Reason = body.Text("reason") },
            cancellationToken);

        return Answer(job);
    }

    /// <summary><c>job.amend</c> — the course changes, the notes, and a reminder.</summary>
    public static async Task<object?> AmendAsync(
        IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var course = services.GetRequiredService<CourseService>();
        var notes = services.GetRequiredService<NoteService>();
        var id = body.Id("id");

        switch (request.Method)
        {
            case "hold":
                return Answer(await course.HoldAsync(
                    request.Scope,
                    new HoldCommand
                    {
                        JobId = id,
                        ExpectedVersion = body.Version(),
                        Reason = body.Text("reason"),
                        Until = body.OptionalText("until") is { } until ? DateTimeOffset.Parse(until) : null,
                    },
                    cancellationToken));

            case "release":
                return Answer(await course.ResumeAsync(request.Scope, id, body.Version(), cancellationToken));

            case "amend":
            case "link":
                return Answer(await course.AmendAsync(
                    request.Scope,
                    new AmendCommand
                    {
                        JobId = id,
                        ExpectedVersion = body.Version(),
                        Priority = body.OptionalText("priority"),
                        ScheduledFor = body.OptionalText("scheduledFor") is { } day
                            ? Optional<DateOnly?>.Of(DateOnly.Parse(day))
                            : Optional<DateOnly?>.Absent,
                        Restricted = body.Flag("restricted", false) ? true : null,
                        LinkJobId = body.OptionalId("linkJobId"),
                    },
                    cancellationToken));

            case "note":
                var note = await notes.AddNoteAsync(
                    request.Scope, id, body.Text("text"), body.Flag("internal"), cancellationToken);
                return new { id = note.Id.ToString(), at = note.At.ToString("o") };

            case "remind":
                var reminder = await notes.RemindMeAsync(
                    request.Scope, id, DateTimeOffset.Parse(body.Text("at")), body.OptionalText("note") ?? string.Empty, cancellationToken);
                return new { id = reminder.Id.ToString() };

            case "readNudges":
                return new { read = await notes.ReadNudgesAsync(request.Scope, id, cancellationToken) };

            default:
                throw new InvalidRequestException($"job.amend has no method '{request.Method}'");
        }
    }

    /// <summary>
    /// What every write answers with — the job's identity and its new version.
    /// </summary>
    /// <remarks>
    /// The version above all: the screen must hold the one the service now has
    /// or its next edit is refused as stale, and it should be refused for a
    /// real conflict rather than for its own last success.
    /// </remarks>
    private static object Answer(Job job) => new
    {
        id = job.Id.ToString(),
        number = job.JobNumber,
        status = job.JobStatus,
        version = job.Version,
    };

    private static object Session(JobWorkSession session) => new
    {
        id = session.Id.ToString(),
        startedAt = session.StartedAt.ToString("o"),
        pausedAt = session.PausedAt?.ToString("o"),
        stoppedAt = session.StoppedAt?.ToString("o"),
        workedSeconds = session.WorkedSeconds,
    };

    private static DateOnly? Day(string? value) =>
        value is null ? null : DateOnly.Parse(value);
}
