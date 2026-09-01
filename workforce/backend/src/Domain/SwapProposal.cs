namespace HotelOS.Workforce.Domain;

/// <summary>Where a swap proposal has got to.</summary>
/// <remarks>
/// <b>Acceptance is its own state</b>, and that is the whole of what
/// <c>WF-Q9</c>(a) bought: <i>the colleague accepts before the manager sees
/// it</i>. It costs one state and it buys the thing that matters — <b>a
/// manager's approval must never commit somebody who did not agree</b>. Without
/// it, approving a swap volunteers a person's Saturday for them.
/// </remarks>
public enum SwapProposalState
{
    /// <summary>Raised, and waiting on the colleague.</summary>
    Proposed = 0,

    /// <summary>The colleague agreed. Now waiting on the manager.</summary>
    Accepted = 1,

    /// <summary>Approved, and the two cells have been exchanged.</summary>
    Approved = 2,

    /// <summary>Refused — by the colleague, or by the manager. The rota is untouched.</summary>
    Declined = 3,

    /// <summary>Withdrawn by whoever raised it.</summary>
    Cancelled = 4,
}

/// <summary>
/// A staff member asking to exchange a shift with a colleague.
/// </summary>
/// <remarks>
/// <para>
/// <c>WF-Q9</c>, owner 2026-08-31: <i>"a staff member may propose exchanging a
/// shift with a colleague, and it takes effect only on the manager's
/// approval."</i>
/// </para>
/// <para>
/// <b>This is not the manager's swap.</b> That one is an <i>action</i> — pick two
/// cells, exchange them, done, consent-free, because the manager is the
/// authority. This is an <i>object with a state</i>, an author and an outcome.
/// They produce the same rota and are not the same thing, and folding them would
/// lose the record of who agreed to what.
/// </para>
/// <para>
/// <b>It is not a reshaped <see cref="LeaveRequest"/> either.</b> That concerns
/// one person; this concerns two. That changes one person's availability; this
/// changes <b>two rota cells atomically</b>. And leave has no concept of a second
/// party's consent, which is the state this aggregate exists to hold.
/// </para>
/// </remarks>
public class SwapProposal
{
    /// <summary>This proposal's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Who asked.</summary>
    public Guid ProposerStaffId { get; set; }

    /// <summary>Who is being asked.</summary>
    public Guid ColleagueStaffId { get; set; }

    /// <summary>The proposer's cell.</summary>
    public Guid ProposerAssignmentId { get; set; }

    /// <summary>The colleague's cell.</summary>
    public Guid ColleagueAssignmentId { get; set; }

    /// <summary>Where it has got to.</summary>
    public SwapProposalState State { get; set; }

    /// <summary>Why, in the proposer's words.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Which account raised it.</summary>
    /// <remarks>
    /// <para>
    /// <c>WF-Q9</c>(b): <b>both entry paths, provenance mandatory.</b> A staff
    /// member with a login proposes for themselves; a supervisor proposes for
    /// everyone else — most of the workforce, because most staff have no
    /// account. The two are one mechanism distinguished by this field rather
    /// than two mechanisms.
    /// </para>
    /// <para>
    /// Without it the record quietly claims a staff member did something a
    /// supervisor did for them.
    /// </para>
    /// </remarks>
    public Guid? EnteredByUserId { get; set; }

    /// <summary>When the colleague agreed.</summary>
    public DateTimeOffset? AcceptedAt { get; set; }

    /// <summary>Who decides it, resolved when it was raised.</summary>
    public Guid? ApproverStaffId { get; set; }

    /// <summary>When it was decided.</summary>
    public DateTimeOffset? DecidedAt { get; set; }

    /// <summary>What the decider said.</summary>
    public string DecisionNote { get; set; } = string.Empty;

    /// <summary>When it was raised.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency.</summary>
    public long Version { get; set; }
}
