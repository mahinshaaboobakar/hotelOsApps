using Google.Protobuf.WellKnownTypes;
using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Contracts.V1;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Catalogue;
using HotelOS.Jobs.Domain.Policy;

namespace HotelOS.Jobs.Grpc;

/// <summary>Domain rows to wire views — one place, so a column added to a table is added to the wire once.</summary>
public static class Views
{
    private static string S(Guid? id) => id?.ToString() ?? string.Empty;

    private static Timestamp? T(DateTimeOffset? at) => at is { } a ? Timestamp.FromDateTimeOffset(a) : null;

    public static JobView Job(JobRow row)
    {
        var j = row.Job;
        return new JobView
        {
            Id = j.Id.ToString(), JobNumber = j.JobNumber, PropertyId = j.PropertyId.ToString(),
            CategoryId = j.CategoryId.ToString(), ItemId = j.ItemId.ToString(), LocationId = j.LocationId.ToString(),
            AssetId = S(j.AssetId), DepartmentCode = j.DepartmentCode, Summary = j.Summary, Details = j.Details ?? string.Empty,
            Priority = j.Priority, PriorityDecidedBy = j.PriorityDecidedBy, RaisedVia = j.RaisedVia, RaisedKind = j.RaisedKind,
            RaisedById = S(j.RaisedById), StayId = S(j.StayId),
            ScheduledFor = j.ScheduledFor?.ToString("yyyy-MM-dd") ?? string.Empty, DueAt = T(j.DueAt),
            JobStatus = j.JobStatus, Cycle = j.Cycle ?? string.Empty, Restricted = j.Restricted,
            HoldReason = j.HoldReason ?? string.Empty, HoldUntil = T(j.HoldUntil),
            ParentJobId = S(j.ParentJobId), StepNo = j.StepNo ?? 0, ConcernPolicyId = S(j.ConcernPolicyId),
            CreatedAt = T(j.CreatedAt), UpdatedAt = T(j.UpdatedAt), Version = j.Version,
            Concern = row.Concern?.Concern ?? Domain.Concern.OnTrack,
            AccountableRole = row.Concern?.AccountableRole ?? string.Empty,
            AccountableUserId = S(row.Concern?.AccountableUserId),
            AssigneeUserId = S(row.Assignment?.AssigneeUserId), TeamId = S(row.Assignment?.TeamId),
            Accepted = row.Assignment?.AcceptedAt is not null, SessionRunning = row.SessionRunning,
        };
    }

    public static JobDetail Detail(JobDetailRows d, DateTimeOffset now)
    {
        var detail = new JobDetail { Job = Job(d.Row) };
        detail.Assignments.AddRange(d.Assignments.Select(a => new AssignmentView
        {
            Id = a.Id.ToString(), AssigneeUserId = S(a.AssigneeUserId), TeamId = S(a.TeamId), How = a.How,
            AssignedBy = S(a.AssignedBy), AssignedAt = T(a.AssignedAt), AcceptedAt = T(a.AcceptedAt), EndedAt = T(a.EndedAt),
        }));
        detail.Sessions.AddRange(d.Sessions.Select(s => Session(s, now)));
        detail.StatusHistory.AddRange(d.StatusHistory.Select(h => new StatusHistoryView
        {
            FromStatus = h.FromStatus, ToStatus = h.ToStatus, ByUserId = S(h.ByUserId), ByWhat = h.ByWhat ?? string.Empty,
            At = T(h.At), Note = h.Note ?? string.Empty,
        }));
        detail.ConcernHistory.AddRange(d.ConcernHistory.Select(c => new ConcernHistoryView
        {
            Concern = c.Concern, AccountableRole = c.AccountableRole, LadderStep = c.LadderStep,
            AccountableUserId = S(c.AccountableUserId), Since = T(c.Since), Reason = c.Reason,
        }));
        detail.Notes.AddRange(d.Notes.Select(n => new NoteView
        {
            Id = n.Id.ToString(), AuthorKind = n.AuthorKind, AuthorId = S(n.AuthorId), Text = n.Text, Internal = n.Internal, At = T(n.At),
        }));
        detail.Attachments.AddRange(d.Attachments.Select(a => new AttachmentView
        {
            Id = a.Id.ToString(), MediaId = a.MediaId.ToString(), Name = a.Name, Bytes = a.Bytes, AddedBy = S(a.AddedBy), At = T(a.At),
        }));
        if (d.Resolution is { } r)
        {
            detail.Resolution = new ResolutionView
            {
                ResolutionId = S(r.ResolutionId), Note = r.Note ?? string.Empty, ResolvedBy = r.ResolvedBy.ToString(), ResolvedAt = T(r.ResolvedAt),
            };
        }

        if (d.Rating is { } g)
        {
            detail.Rating = new RatingView { Stars = g.Stars, Text = g.Text ?? string.Empty, RatedAt = T(g.RatedAt) };
        }

        detail.Links.AddRange(d.Links.Select(l => new LinkView
        {
            JobId = (l.JobId == d.Row.Job.Id ? l.LinkedJobId : l.JobId).ToString(), At = T(l.At),
        }));
        detail.Steps.AddRange(d.Steps.Select(Job));
        return detail;
    }

    public static WorkSessionView Session(JobWorkSession s, DateTimeOffset now) => new()
    {
        Id = s.Id.ToString(), UserId = s.UserId.ToString(), StartedAt = T(s.StartedAt), PausedAt = T(s.PausedAt),
        PauseReason = s.PauseReason ?? string.Empty, ResumedAt = T(s.ResumedAt), StoppedAt = T(s.StoppedAt),
        WorkedSeconds = s.WorkedSecondsAt(now),
    };

    public static CategoryView Category(Category c) => new()
    {
        Id = c.Id.ToString(), Code = c.Code, Name = c.Name, DepartmentCode = c.DepartmentCode, Active = c.Active, Version = c.Version,
    };

    public static ItemView Item(Item i, IEnumerable<string> aliases)
    {
        var view = new ItemView
        {
            Id = i.Id.ToString(), CategoryId = i.CategoryId.ToString(), Code = i.Code, Name = i.Name,
            DefaultPriority = i.DefaultPriority, DueWithinMinutes = i.DueWithinMinutes ?? 0,
            RestrictedByDefault = i.RestrictedByDefault, GuestRequestable = i.GuestRequestable,
            PhotoOnCompletion = i.PhotoOnCompletion, Active = i.Active, Version = i.Version,
        };
        view.Aliases.AddRange(aliases);
        return view;
    }

    public static CatalogueResolutionView Resolution(Resolution r) => new()
    {
        Id = r.Id.ToString(), CategoryId = S(r.CategoryId), ItemId = S(r.ItemId), Name = r.Name, NoteRequired = r.NoteRequired,
    };

    public static NoteView Note(JobNote n) => new()
    {
        Id = n.Id.ToString(), AuthorKind = n.AuthorKind, AuthorId = S(n.AuthorId), Text = n.Text, Internal = n.Internal, At = T(n.At),
    };

    public static AttachmentView Attachment(JobAttachment a) => new()
    {
        Id = a.Id.ToString(), MediaId = a.MediaId.ToString(), Name = a.Name, Bytes = a.Bytes, AddedBy = S(a.AddedBy), At = T(a.At),
    };

    public static PresenceView Presence(DepartmentPresence p) => new()
    {
        DepartmentCode = p.DepartmentCode, Enabled = p.Enabled, FollowShifts = p.FollowShifts, Staffed = p.Staffed,
        Since = T(p.Since), OnShift = p.OnShift,
    };
}
