using Grpc.Core;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Contracts.V1;
using HotelOS.Platform;

namespace HotelOS.Jobs.Grpc;

/// <summary>The verbs that change a job — raise, assign, work, complete, cancel, amend, speak, rate.</summary>
public partial class JobsGrpcService
{
    public override async Task<JobView> RaiseJob(RaiseJobRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await jobs.RaiseAsync(scope, new RaiseJobCommand
        {
            ItemId = ParseId(r.ItemId, "item_id"), LocationId = ParseId(r.LocationId, "location_id"),
            AssetId = ParseOptionalId(r.AssetId, "asset_id"), Summary = r.Summary, Details = Blank(r.Details),
            Priority = Blank(r.Priority), FlowPriority = Blank(r.FlowPriority), RaisedVia = r.RaisedVia, RaisedKind = r.RaisedKind,
            RaisedById = ParseOptionalId(r.RaisedById, "raised_by_id"), StayId = ParseOptionalId(r.StayId, "stay_id"),
            ScheduledFor = ParseOptionalDate(r.ScheduledFor, "scheduled_for"), Cycle = Blank(r.Cycle),
            Restricted = r.HasRestricted ? r.Restricted : null,
            AssignToUserId = ParseOptionalId(r.AssignToUserId, "assign_to_user_id"),
            AssignToTeamId = ParseOptionalId(r.AssignToTeamId, "assign_to_team_id"),
            ParentJobId = ParseOptionalId(r.ParentJobId, "parent_job_id"),
        }, context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<JobView> AssignJob(AssignJobRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await assignment.AssignAsync(scope, new AssignCommand
        {
            JobId = ParseId(r.Id, "id"), ExpectedVersion = r.ExpectedVersion,
            UserId = ParseOptionalId(r.UserId, "user_id"), TeamId = ParseOptionalId(r.TeamId, "team_id"),
        }, context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<JobView> AcceptJob(JobVersionRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await assignment.AcceptAsync(scope, ParseId(r.Id, "id"), r.ExpectedVersion, context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<WorkSessionView> StartWork(JobRequest r, ServerCallContext context) =>
        Views.Session(await work.StartAsync(r.Context.ToScope(CallerContext.Get(context)), ParseId(r.Id, "id"), context.CancellationToken), queries.Now);

    public override async Task<WorkSessionView> PauseWork(PauseWorkRequest r, ServerCallContext context) =>
        Views.Session(await work.PauseAsync(r.Context.ToScope(CallerContext.Get(context)), ParseId(r.Id, "id"), Blank(r.Reason), context.CancellationToken), queries.Now);

    public override async Task<WorkSessionView> ResumeWork(JobRequest r, ServerCallContext context) =>
        Views.Session(await work.ResumeAsync(r.Context.ToScope(CallerContext.Get(context)), ParseId(r.Id, "id"), context.CancellationToken), queries.Now);

    public override async Task<WorkSessionView> StopWork(JobRequest r, ServerCallContext context) =>
        Views.Session(await work.StopAsync(r.Context.ToScope(CallerContext.Get(context)), ParseId(r.Id, "id"), context.CancellationToken), queries.Now);

    public override async Task<JobView> ResolveJob(ResolveJobRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await completion.ResolveAsync(scope, new ResolveCommand
        {
            JobId = ParseId(r.Id, "id"), ExpectedVersion = r.ExpectedVersion,
            ResolutionId = ParseOptionalId(r.ResolutionId, "resolution_id"), Note = Blank(r.Note),
        }, context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<JobView> CloseJob(JobVersionRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await completion.CloseAsync(scope, ParseId(r.Id, "id"), r.ExpectedVersion, context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<JobView> ReopenJob(ReopenJobRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await completion.ReopenAsync(scope, ParseId(r.Id, "id"), r.ExpectedVersion, Blank(r.Note), context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<JobView> CancelJob(CancelJobRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await cancellation.CancelAsync(scope, new CancelCommand
        {
            JobId = ParseId(r.Id, "id"), ExpectedVersion = r.ExpectedVersion, Reason = r.Reason,
        }, context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<JobView> HoldJob(HoldJobRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await course.HoldAsync(scope, new HoldCommand
        {
            JobId = ParseId(r.Id, "id"), ExpectedVersion = r.ExpectedVersion, Reason = r.Reason, Until = At(r.Until),
        }, context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<JobView> ResumeJob(JobVersionRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await course.ResumeAsync(scope, ParseId(r.Id, "id"), r.ExpectedVersion, context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<JobView> AmendJob(AmendJobRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        var job = await course.AmendAsync(scope, new AmendCommand
        {
            JobId = ParseId(r.Id, "id"), ExpectedVersion = r.ExpectedVersion, Priority = Blank(r.Priority),
            ScheduledFor = r.HasScheduledFor ? Optional<DateOnly?>.Of(ParseOptionalDate(r.ScheduledFor, "scheduled_for")) : Optional<DateOnly?>.Absent,
            Restricted = r.HasRestricted ? r.Restricted : null, LinkJobId = ParseOptionalId(r.LinkJobId, "link_job_id"),
        }, context.CancellationToken);
        return await RowAsync(scope, job, context.CancellationToken);
    }

    public override async Task<NoteView> AddNote(AddNoteRequest r, ServerCallContext context) =>
        Views.Note(await notes.AddNoteAsync(r.Context.ToScope(CallerContext.Get(context)), ParseId(r.Id, "id"), r.Text, r.Internal, context.CancellationToken));

    public override async Task<AttachmentView> Attach(AttachRequest r, ServerCallContext context) =>
        Views.Attachment(await notes.AttachAsync(
            r.Context.ToScope(CallerContext.Get(context)), ParseId(r.Id, "id"), ParseId(r.MediaId, "media_id"), r.Name, r.Bytes, context.CancellationToken));

    public override async Task<RemindMeResponse> RemindMe(RemindMeRequest r, ServerCallContext context)
    {
        var at = At(r.At) ?? throw new InvalidRequestException("at is required");
        var reminder = await notes.RemindMeAsync(r.Context.ToScope(CallerContext.Get(context)), ParseId(r.Id, "id"), at, r.Note, context.CancellationToken);
        return new RemindMeResponse { ReminderId = reminder.Id.ToString() };
    }

    public override async Task<RatingView> RateJob(RateJobRequest r, ServerCallContext context)
    {
        var rated = await rating.RateAsync(
            r.Context.ToScope(CallerContext.Get(context)), ParseId(r.Id, "id"), ParseId(r.StayId, "stay_id"), r.Stars, Blank(r.Text), context.CancellationToken);
        return new RatingView { Stars = rated.Stars, Text = rated.Text ?? string.Empty, RatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(rated.RatedAt) };
    }
}
