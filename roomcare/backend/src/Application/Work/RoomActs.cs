using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Announcing;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using HotelOS.RoomCare.Infrastructure;

namespace HotelOS.RoomCare.Application.Work;

/// <summary>What an attendant records in a room besides the service — a restock, and a fault found (RC-Q2; redline 2).</summary>
/// <remarks>
/// Both are the attendant's own record first and an event second: a restock
/// names Inventory's item ids and quantities and nothing else; an issue keeps
/// the attendant's note whether or not Jobs is installed, and Jobs answers on
/// the correlation id with <c>job.created</c> (EVT-Q3) — replayed the day it
/// installs (EVT-Q4).
/// </remarks>
public sealed class RoomActs(RoomCareDbContext db, TaskWriter writer, IEventAppender events)
{
    public async Task<Restock> RestockAsync(
        RequestScope scope, Guid taskId, IReadOnlyList<RestockItem> items, Guid? stayId, CancellationToken cancellationToken)
    {
        if (items.Count == 0 || items.Any(i => string.IsNullOrWhiteSpace(i.ItemId) || i.Quantity is < 1 or > 99))
        {
            throw new InvalidRequestException("a restock names at least one item, each between 1 and 99");
        }

        var task = await MineAsync(scope, taskId, cancellationToken);
        var restock = new Restock
        {
            Id = Guid.CreateVersion7(),
            PropertyId = task.PropertyId,
            TaskId = task.Id,
            RoomId = task.RoomId!.Value,
            StayId = stayId,
            At = writer.Now,
            ByUserId = task.AssignedToUserId!.Value,
            Items = items.ToList(),
        };
        db.Restocks.Add(restock);
        events.Append(scope, EventTypes.RoomRestocked, EventTypes.RestockAggregate, restock.Id, 1, new RestockedAnnouncement
        {
            RestockId = restock.Id,
            TaskId = task.Id,
            RoomId = restock.RoomId,
            PropertyId = restock.PropertyId,
            StayId = stayId,
            Items = restock.Items,
            ById = restock.ByUserId,
            At = restock.At,
        });
        await db.SaveChangesAsync(cancellationToken);
        return restock;
    }

    public async Task<TaskIssue> IssueAsync(
        RequestScope scope, Guid taskId, string note, string? itemHint, Guid? mediaId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new InvalidRequestException("say what was found, so whoever fixes it knows");
        }

        var task = await MineAsync(scope, taskId, cancellationToken);
        var issue = new TaskIssue
        {
            Id = Guid.CreateVersion7(),
            PropertyId = task.PropertyId,
            TaskId = task.Id,
            RoomId = task.RoomId!.Value,
            ItemHint = itemHint,
            Note = note.Trim(),
            MediaId = mediaId,
            ByUserId = task.AssignedToUserId!.Value,
            At = writer.Now,
        };
        issue.CorrelationId = $"issue:{issue.Id}";
        db.Issues.Add(issue);
        events.Append(scope with { CorrelationId = issue.CorrelationId }, EventTypes.IssueFound, EventTypes.IssueAggregate, issue.Id, 1,
            new IssueFoundAnnouncement
            {
                IssueId = issue.Id,
                TaskId = task.Id,
                RoomId = issue.RoomId,
                PropertyId = issue.PropertyId,
                ItemHint = itemHint,
                Note = issue.Note,
                MediaId = mediaId,
                ById = issue.ByUserId,
                CorrelationId = issue.CorrelationId,
                OccurredAt = issue.At,
            });
        await db.SaveChangesAsync(cancellationToken);
        return issue;
    }

    private async Task<RoomTask> MineAsync(RequestScope scope, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await writer.RequireAsync(scope, taskId, null, cancellationToken);
        Gate.Assignee(scope, task);
        return task.RoomId is null
            ? throw new InvalidRequestException("an area's routine has no room to restock or report on")
            : task;
    }
}
