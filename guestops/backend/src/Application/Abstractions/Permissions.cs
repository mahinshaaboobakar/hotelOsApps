namespace HotelOS.GuestOps.Application.Abstractions;

/// <summary>
/// The permissions this application requests, and an administrator approves.
/// </summary>
/// <remarks>
/// <para>
/// ADR 0007's naming — one per capability, the verb naming what it lets a
/// person do. Declared in the manifest as <b>requests</b>, never as tuples: the
/// administrator's approval is the authorization decision and the Kernel
/// materialises it (ADR 0092 rule 2, ADR 0114).
/// </para>
/// <para>
/// Constants rather than literals, because a typo in a permission string is not
/// a compile error: the Kernel refuses an unknown permission and the symptom is
/// *"this screen does not work for anyone"*.
/// </para>
/// </remarks>
public static class Permissions
{
    /// <summary>The four lists, the stay page, the group page.</summary>
    public const string ReservationRead = "reservation.read";

    /// <summary>Create a booking and its stays.</summary>
    /// <remarks>
    /// Standalone, and the PMS-unknown stay in a connected property — GUEST-Q5:
    /// *"all guest operations are done here by staff"* already granted it.
    /// </remarks>
    public const string StayCreate = "stay.create";

    /// <summary>The lifecycle: check in, check out, cancel, no-show, correct.</summary>
    /// <remarks>
    /// <para>
    /// <b>The same permission makes an override, clears a disagreement and
    /// resolves a contradiction</b> — GUEST-Q3. There is deliberately no
    /// <c>disagreement.clear</c>: author-only clearing fails across shifts,
    /// supervisor-only escalates a routine reconciliation, and a separate
    /// permission would re-introduce the escalation the ruling refused.
    /// </para>
    /// <para>
    /// It is also what reveals a masked contact (GUEST-Q7): the permission that
    /// lets a person act on the stay is the one that lets them ring the guest.
    /// </para>
    /// </remarks>
    public const string StayWrite = "stay.write";

    /// <summary>Assign a room, and move one.</summary>
    public const string StayAssign = "stay.assign";

    /// <summary>Guest identity records, contact points, preferences.</summary>
    public const string GuestWrite = "guest.write";

    /// <summary>The registration card, its documents and its signature.</summary>
    public const string RegistrationCapture = "registration.capture";

    /// <summary>Recording that a guest filing was made — S19b.</summary>
    /// <remarks>
    /// Separate from <see cref="RegistrationCapture"/> because it is an
    /// assertion about an <i>external obligation</i> rather than about our own
    /// record: the person who types the card and the person who files with an
    /// authority are not always the same, and a filing is a legal assertion.
    /// </remarks>
    public const string ReportingFile = "reporting.file";

    /// <summary>Guest requests, and their hand-off to Jobs.</summary>
    public const string RequestManage = "request.manage";

    /// <summary>This application's own settings — the required fields, the series, the reporting policy.</summary>
    public const string Configure = "guestops.configure";
}

/// <summary>The object types authorization is asked about.</summary>
/// <remarks>
/// Get names the instance; a collection operation names the container — ADR
/// 0015/0018. A list of a day's arrivals is asked of the <b>property</b>,
/// because the stays are its.
/// </remarks>
public static class ResourceTypes
{
    /// <summary>The property — what a list of a day's stays is asked of.</summary>
    public const string Property = "property";

    /// <summary>One room-stay — what an operation on a guest is asked of.</summary>
    public const string Stay = "stay";
}
