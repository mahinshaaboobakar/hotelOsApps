using System.Text.Json.Serialization;
using HotelOS.Platform;
using HotelOS.RoomCare.Application.Assignment;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Events;

/// <summary><c>user.posted</c> — Workforce placed a person in a department; Room Care notes it for the proposal.</summary>
public sealed class UserPostedHandler(RoomCareDbContext db) : IEventHandler<PostingAnnounced>
{
    public async Task HandleAsync(RequestScope scope, PostingAnnounced payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var seen = await db.Postings.FirstOrDefaultAsync(p => p.PostingId == payload.PostingId, cancellationToken);
        if (seen is null)
        {
            db.Postings.Add(new PostingSeen
            {
                PostingId = payload.PostingId,
                PropertyId = scope.PropertyId,
                UserId = payload.UserId,
                StaffId = payload.StaffId,
                DepartmentId = payload.DepartmentId,
                DepartmentCode = payload.DepartmentCode,
                PostedAt = payload.OccurredAt,
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary><c>user.posting_ended</c> — the person is no longer a candidate; rooms already theirs stay until reassigned.</summary>
public sealed class UserPostingEndedHandler(RoomCareDbContext db) : IEventHandler<PostingAnnounced>
{
    public async Task HandleAsync(RequestScope scope, PostingAnnounced payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var seen = await db.Postings.FirstOrDefaultAsync(p => p.PostingId == payload.PostingId, cancellationToken);
        if (seen is { EndedAt: null })
        {
            seen.EndedAt = payload.OccurredAt;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary><c>shift.started</c> / <c>shift.ended</c> — how many are on now in a department, never the roster itself.</summary>
public sealed class ShiftBoundaryHandler(RoomCareDbContext db) : IEventHandler<ShiftBoundary>
{
    public async Task HandleAsync(RequestScope scope, ShiftBoundary payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var presence = await db.Presence.FirstOrDefaultAsync(
            p => p.PropertyId == scope.PropertyId && p.DepartmentCode == payload.DepartmentCode, cancellationToken);
        if (presence is null)
        {
            presence = new ShiftPresence { PropertyId = scope.PropertyId, DepartmentCode = payload.DepartmentCode };
            db.Presence.Add(presence);
        }
        else if (presence.At > payload.At)
        {
            return;
        }

        presence.OnNow = payload.OnNowAfter;
        presence.At = payload.At;
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary><c>staff.exited</c> — a person who left holds no rooms; their tasks return to the proposal.</summary>
public sealed class StaffExitedHandler(RoomCareDbContext db, AssignmentService assignment) : IEventHandler<StaffExited>
{
    public async Task HandleAsync(RequestScope scope, StaffExited payload, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        if (payload.UserId is not { } user)
        {
            return;
        }

        await assignment.ReleaseAsync(scope, user, AssignmentEnd.StaffExited, cancellationToken);
        var postings = await db.Postings.Where(p => p.PropertyId == scope.PropertyId && p.UserId == user && p.EndedAt == null)
            .ToListAsync(cancellationToken);
        postings.ForEach(p => p.EndedAt = envelope.OccurredAt);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Workforce's posting announcement — its own record's field names.</summary>
public sealed record PostingAnnounced(
    [property: JsonPropertyName("user_id")] Guid UserId,
    [property: JsonPropertyName("staff_id")] Guid StaffId,
    [property: JsonPropertyName("department_id")] Guid DepartmentId,
    [property: JsonPropertyName("department_code")] string DepartmentCode,
    [property: JsonPropertyName("posting_id")] Guid PostingId,
    [property: JsonPropertyName("occurred_at")] DateTimeOffset OccurredAt);

/// <summary>Workforce's shift boundary announcement.</summary>
public sealed record ShiftBoundary(
    [property: JsonPropertyName("department_code")] string DepartmentCode,
    [property: JsonPropertyName("at")] DateTimeOffset At,
    [property: JsonPropertyName("on_now_after")] int OnNowAfter);

/// <summary>The body of <c>staff.exited</c>.</summary>
public sealed record StaffExited(
    [property: JsonPropertyName("user_id")] Guid? UserId,
    [property: JsonPropertyName("staff_id")] Guid StaffId);
