namespace HotelOS.Workforce.Application.Abstractions;

/// <summary>
/// The permission identifiers this application asks for.
/// </summary>
/// <remarks>
/// <para>
/// Constants rather than literals at the call sites, because a typo in a
/// permission string is not a compile error and does not fail loudly — the
/// Kernel refuses an unknown permission, so the request is denied and the
/// symptom reads as a permission-model problem rather than a spelling one.
/// </para>
/// <para>
/// <b>Named for the resource and the business action, never for this
/// application</b> — ADR 0007. <c>posting.manage</c>, not
/// <c>workforce.posting.manage</c>: a posting exists regardless of which package
/// owns the code, and if postings ever moved these names would not change.
/// </para>
/// <para>
/// <b>An installed application declares permission <i>requests</i>, never
/// tuples</b> — ADR 0092 and ADR 0128 §2. These strings go in
/// <c>manifest.yaml</c>, the administrator approves them at install, and the
/// Kernel materialises the decision. Nothing here grants anything to anybody,
/// and this application has no OpenFGA client.
/// </para>
/// </remarks>
public static class Permissions
{
    // --- slice 1 · postings -----------------------------------------------

    /// <summary>Create, amend and end a posting.</summary>
    /// <remarks>
    /// One capability rather than three, per AUTHZ-Q15's both-directions-in-one
    /// convention: whoever may post a person may un-post them, and splitting
    /// them would let an administrator create postings nobody could ever end.
    /// </remarks>
    public const string PostingManage = "posting.manage";

    /// <summary>Read the workforce surface — postings, and later the rota.</summary>
    /// <remarks>
    /// Deliberately one read permission for the application rather than one per
    /// aggregate. The screens are not separable in practice: a rota shows who is
    /// posted, and a posting list shows who is on shift. Splitting the read
    /// would produce a half-rendered screen whose blanks nobody could explain.
    /// </remarks>
    public const string WorkforceRead = "workforce.read";

    // --- later slices, named here so the vocabulary is decided once ---------
    //
    // Declared but not yet asked for by any code path. They are written down
    // now because the manifest declares the application's whole permission
    // request at install and an administrator approving a package should see
    // what it will ever need — not be asked again at each update.
    //
    // Chapter 01 §4 is where they are justified; each is one capability over
    // one resource.

    /// <summary>Build and change the rota — slice 3.</summary>
    public const string ShiftManage = "shift.manage";
    /// <summary>Raise a leave request, including on somebody's behalf — slice 4.</summary>
    public const string LeaveRequest = "leave.request";
    /// <summary>Decide a leave request — slice 4.</summary>
    public const string LeaveApprove = "leave.approve";
    /// <summary>Propose a shift swap, or enter one on somebody's behalf — slice 4.</summary>
    public const string SwapPropose = "swap.propose";
    /// <summary>Approve an accepted swap, committing both cells — slice 4.</summary>
    public const string SwapApprove = "swap.approve";
    /// <summary>Assign the Manager on Duty over a span — slice 3.</summary>
    public const string DutyAssign = "duty.assign";
    /// <summary>Mark attendance — slice 5.</summary>
    public const string AttendanceRecord = "attendance.record";
    /// <summary>Correct a recorded attendance fact — slice 5, and separate from recording it because amending somebody else's record is a different authority.</summary>
    public const string AttendanceAmend = "attendance.amend";
    /// <summary>The property's shift catalogue, leave policy and overtime threshold — slice 3.</summary>
    public const string PolicyManage = "policy.manage";
    /// <summary>Skills, languages and certification expiry — slice 2.</summary>
    public const string CapabilityManage = "capability.manage";

    // No `department.grant_access`, and there never will be.
    //
    // ADR 0061: a service never writes an authorization tuple. A posting is
    // *announced*; the Kernel's registration consumer materialises
    // `department:{id}#posted@user:{uid}`. There is no permission here for
    // granting because there is no granting here — the capability an
    // administrator approves is "may post a person", and the access is a
    // consequence rather than a second decision.
}
