namespace HotelOS.RoomCare.Application.Abstractions;

/// <summary>The seven permissions the manifest requests — chapter 03 §4.1, registry rows verified 2026-09-13.</summary>
public static class Permissions
{
    /// <summary><c>room: housekeeping_operator</c> — the attendant's done rides the assignment besides.</summary>
    public const string Clean = "room.clean";

    /// <summary><c>room: inspector</c> — held by the inspection app's inspector; Room Care applies the outcome.</summary>
    public const string Inspect = "room.inspect";

    /// <summary><c>property: roomcare_viewer</c> (RC-Q5).</summary>
    public const string Read = "roomcare.read";

    /// <summary><c>room_task: can_assign</c>.</summary>
    public const string Assign = "roomcare.assign";

    /// <summary><c>room_task: can_amend</c>.</summary>
    public const string Amend = "roomcare.amend";

    /// <summary><c>property: roomcare_configurer</c>.</summary>
    public const string Configure = "roomcare.configure";

    /// <summary><c>property: roomcare_planner</c> (RC-Q5).</summary>
    public const string Plan = "roomcare.plan";

    /// <summary>Every permission, in the manifest's order.</summary>
    public static readonly IReadOnlyList<string> All = [Clean, Inspect, Read, Assign, Amend, Configure, Plan];
}

/// <summary>The object types each permission is answerable on — ADR 0018's per-scope map, as the registry states it.</summary>
public static class ObjectTypes
{
    public const string Property = "property";

    public const string Room = "room";

    public const string RoomTask = "room_task";
}
