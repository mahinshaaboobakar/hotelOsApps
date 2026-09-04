using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Notes;

/// <summary>
/// What a person adds to a job without changing its course — a note, a photo,
/// a reminder for themselves (S9 D3), and reading a nudge. All need only
/// <c>job.read</c>: anyone who may see a job may speak on it.
/// </summary>
public class NoteService(JobsDbContext db, IKernelAuthorizer authorizer, JobRecords records)
{
    public async Task<JobNote> AddNoteAsync(RequestScope scope, Guid jobId, string text, bool internalOnly, CancellationToken cancellationToken)
    {
        var job = await ReadableAsync(scope, jobId, cancellationToken);
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidRequestException("a note needs text");

        var note = new JobNote
        {
            Id = Uuid7.NewUuid7(), JobId = job.Id, PropertyId = job.PropertyId,
            AuthorKind = scope.Caller == CallerKind.User ? RaisedKind.Staff : RaisedKind.Application,
            AuthorId = scope.UserId, Text = text.Trim(), Internal = internalOnly, At = records.Now,
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync(cancellationToken);
        return note;
    }

    public async Task<JobAttachment> AttachAsync(RequestScope scope, Guid jobId, Guid mediaId, string name, long bytes, CancellationToken cancellationToken)
    {
        var job = await ReadableAsync(scope, jobId, cancellationToken);
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidRequestException("an attachment needs a name");

        var attachment = new JobAttachment
        {
            Id = Uuid7.NewUuid7(), JobId = job.Id, PropertyId = job.PropertyId,
            MediaId = mediaId, Name = name.Trim(), Bytes = bytes, AddedBy = scope.UserId, At = records.Now,
        };
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);
        return attachment;
    }

    /// <summary>A reminder for the caller, on a job they can see (S9 D3).</summary>
    public async Task<JobReminder> RemindMeAsync(RequestScope scope, Guid jobId, DateTimeOffset at, string note, CancellationToken cancellationToken)
    {
        var job = await ReadableAsync(scope, jobId, cancellationToken);
        var user = scope.UserId ?? throw new InvalidRequestException("a reminder is a person's");
        if (at <= records.Now) throw new InvalidRequestException("remind_at must be in the future");

        var reminder = new JobReminder
        {
            Id = Uuid7.NewUuid7(), JobId = job.Id, PropertyId = job.PropertyId, ForUserId = user,
            RemindAt = at, Note = note.Trim(), Kind = ReminderKind.Manual,
        };
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(cancellationToken);
        return reminder;
    }

    /// <summary>Mark the caller's nudges on a job read.</summary>
    public async Task<int> ReadNudgesAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken)
    {
        if (scope.UserId is not { } user) return 0;

        var unread = await db.Nudges
            .Where(n => n.JobId == jobId && n.PropertyId == scope.PropertyId && n.ToUserId == user && n.ReadAt == null)
            .ToListAsync(cancellationToken);
        foreach (var nudge in unread) nudge.ReadAt = records.Now;
        await db.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    private async Task<Job> ReadableAsync(RequestScope scope, Guid jobId, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Read, "property", scope.PropertyId, cancellationToken);
        return await records.LoadAsync(scope, jobId, cancellationToken);
    }
}
