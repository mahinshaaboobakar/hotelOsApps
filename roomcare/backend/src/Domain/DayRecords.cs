using System.Text.Json.Serialization;

namespace HotelOS.RoomCare.Domain;

/// <summary>A room in the supervisor's lane for a day — the decision is final (chapter 03 §2.5, S5 c9).</summary>
public class RoomSupervision
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid RoomId { get; set; }

    public DateOnly OperatingDay { get; set; }

    public string Reason { get; set; } = SupervisionReason.DaysWithoutService;

    public DateTimeOffset OpenedAt { get; set; }

    public string? Decision { get; set; }

    public Guid? DecidedByUserId { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public string? Note { get; set; }

    public bool IsOpen => Decision is null;
}

/// <summary>What an attendant put back in a room — the act only; no name, price or stock (RC-Q2).</summary>
public class Restock
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid TaskId { get; set; }

    public Guid RoomId { get; set; }

    public Guid? StayId { get; set; }

    public DateTimeOffset At { get; set; }

    public Guid ByUserId { get; set; }

    public List<RestockItem> Items { get; set; } = [];
}

/// <summary>One of Inventory's items and how many.</summary>
public sealed record RestockItem(
    [property: JsonPropertyName("item_id")] string ItemId,
    [property: JsonPropertyName("quantity")] int Quantity);

/// <summary>One press of the button, or the automatic run in its place (chapter 03 §2.7).</summary>
public class PrepareRun
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public DateOnly OperatingDay { get; set; }

    public string Window { get; set; } = ServiceWindowName.Morning;

    public DateTimeOffset At { get; set; }

    public string ByKind { get; set; } = ActorKind.User;

    public Guid? ById { get; set; }

    public string Via { get; set; } = Domain.Via.App;

    public string Kind { get; set; } = RunKind.First;

    public int RoomsConsidered { get; set; }

    public int TasksCreated { get; set; }

    public int TasksUpdated { get; set; }

    public int TasksSkipped { get; set; }

    public int Pending { get; set; }

    public int Unassignable { get; set; }

    public int ChangesSincePrevious { get; set; }
}

/// <summary>A planned deep clean of one room — a project, not a service (chapter 03 §2.9, S0).</summary>
public class DeepClean
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid RoomId { get; set; }

    public DateOnly DueOn { get; set; }

    public DateOnly? WindowFrom { get; set; }

    public DateOnly? WindowTo { get; set; }

    public string? BlockCorrelationId { get; set; }

    public DateTimeOffset? BlockRequestedAt { get; set; }

    public DateTimeOffset? BlockAppliedAt { get; set; }

    public string? JobCorrelationId { get; set; }

    public Guid? JobId { get; set; }

    public string? JobStatusSeen { get; set; }

    public string Status { get; set; } = DeepCleanStatus.Planned;

    public DateOnly? DoneOn { get; set; }

    public Guid? PlannedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long Version { get; set; }
}

/// <summary>A person's Housekeeping posting as Workforce announced it — who may be proposed rooms.</summary>
/// <remarks>
/// A projection of <c>user.posted</c> / <c>user.posting_ended</c>, never a
/// roster: Room Care writes no posting, and no name is kept — a screen reads
/// the name from Master Data at answer time. The zone on the posting is
/// Workforce's to add (chapter 03 §9); until then the proposal groups by
/// department and says so.
/// </remarks>
public class PostingSeen
{
    public Guid PostingId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid UserId { get; set; }

    public Guid StaffId { get; set; }

    public Guid DepartmentId { get; set; }

    public string DepartmentCode { get; set; } = string.Empty;

    public DateTimeOffset PostedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }
}

/// <summary>Whether a department has anyone on shift, as Workforce last announced it.</summary>
public class ShiftPresence
{
    public Guid PropertyId { get; set; }

    public string DepartmentCode { get; set; } = string.Empty;

    public int OnNow { get; set; }

    public DateTimeOffset At { get; set; }
}

/// <summary>The general manager's grant of property-wide Room Care access — the record of an action, not the graph.</summary>
public class RoomCareManagerGrant
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset GrantedAt { get; set; }

    public Guid GrantedBy { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? RevokedBy { get; set; }
}
