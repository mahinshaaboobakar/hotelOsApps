using HotelOS.Platform;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Abstractions;

/// <summary>Every authorization question Room Care asks, on the object type the registry names for it.</summary>
/// <remarks>
/// <para>
/// ADR 0018: a permission is answerable only on the object types its scopes map
/// names, and the Kernel refuses any other. So <c>read</c>, <c>configure</c> and
/// <c>plan</c> are asked on the property; <c>assign</c> and <c>amend</c> on a
/// <c>room_task</c> (<c>permissions.yaml:1413, 1422</c>).
/// </para>
/// <para>
/// <b>An amend that acts on a room rather than a task</b> — the Room states tab,
/// clearing a disagreement — is asked on that room's most recent task, which
/// belongs to the same department and property. A room that has never had a
/// task has no object to ask on, and that is refused in words. Raised with the
/// architect with the build: the registry gives <c>roomcare.amend</c> no
/// room-level scope.
/// </para>
/// <para>
/// The attendant's acts are not asked here: they ride the assignment
/// (<c>room_task#assignee</c> carries no permission), exactly as Jobs'
/// work-session verbs ride <c>job#assignee</c> — the service checks that the
/// caller is the current assignee on Room Care's own row.
/// </para>
/// </remarks>
public sealed class Gate(IKernelAuthorizer authorizer, RoomCareDbContext db)
{
    public Task PropertyAsync(RequestScope scope, string permission, CancellationToken cancellationToken) =>
        authorizer.RequireAsync(scope, permission, ObjectTypes.Property, scope.PropertyId, cancellationToken);

    public Task TaskAsync(RequestScope scope, string permission, Guid taskId, CancellationToken cancellationToken) =>
        authorizer.RequireAsync(scope, permission, ObjectTypes.RoomTask, taskId, cancellationToken);

    /// <summary>An amend on a room, asked on the room's latest task.</summary>
    public async Task RoomAsync(RequestScope scope, string permission, Guid roomId, CancellationToken cancellationToken)
    {
        var task = await db.Tasks
            .Where(t => t.PropertyId == scope.PropertyId && t.RoomId == roomId)
            .OrderByDescending(t => t.OperatingDay)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidRequestException(
                "this room has never had a Room Care task, so there is no task to authorize the change on — "
                + "prepare the day once, or ask the property's Room Care manager");

        await TaskAsync(scope, permission, task, cancellationToken);
    }

    /// <summary>The caller is the task's current assignee, or the act is refused.</summary>
    public static void Assignee(RequestScope scope, RoomTask task)
    {
        var person = Actor.PersonOf(scope, "working a room");
        if (task.AssignedToUserId != person)
        {
            throw new PermissionDeniedException(Permissions.Clean, $"room_task:{task.Id} — only its current assignee works it");
        }
    }
}
