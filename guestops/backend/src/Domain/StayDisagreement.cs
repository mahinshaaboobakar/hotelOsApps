namespace HotelOS.GuestOps.Domain;

/// <summary>What a staff write and a PMS fact disagreed about.</summary>
/// <remarks>
/// Closed and small on purpose. An open vocabulary here becomes a per-field diff
/// engine, and a desk cannot act on <i>"eleven fields differ"</i>.
/// </remarks>
public enum DisagreementAspect
{
    /// <summary>Booked, in house, departed, cancelled.</summary>
    Lifecycle = 1,

    /// <summary>Which room.</summary>
    Assignment = 2,

    /// <summary>Arrival or departure.</summary>
    Dates = 3,
}

/// <summary>Where an override, and then a disagreement, has got to.</summary>
/// <remarks>
/// The sequence is one row's life, not two records: a staff write on a
/// PMS-managed stay is recorded the moment it happens, and what the PMS later
/// says decides which way it goes.
/// </remarks>
public enum DisagreementState
{
    /// <summary>
    /// A staff write stands, and no inbound fact has spoken since.
    /// </summary>
    /// <remarks>
    /// The ordinary state of an override — GUEST-Q1's amendment. Recorded at
    /// the moment of the write with who, when, and what the PMS said then,
    /// because that last one is not recoverable afterwards and is what makes
    /// the override explicable months later.
    /// </remarks>
    Overridden = 1,

    /// <summary>The two sources differ and nobody has decided.</summary>
    /// <remarks>
    /// The disagreement proper. While it stands, <b>the override is still the
    /// answer everywhere</b> — the flag rides on that one truth rather than
    /// becoming a second one (GUEST-Q3).
    /// </remarks>
    Standing = 2,

    /// <summary>
    /// A later fact matched what we held. Settled silently — GUEST-Q4.
    /// </summary>
    /// <remarks>
    /// Recorded and surfacing nothing. Agreement arriving late is not work, and
    /// a design that flagged every late confirmation would bury the two real
    /// reconciliations in twenty.
    /// </remarks>
    Confirmed = 3,

    /// <summary>A person kept ours.</summary>
    ClearedOurs = 4,

    /// <summary>A person took the PMS's, and the correction was published.</summary>
    ClearedPms = 5,
}

/// <summary>
/// A staff write on a PMS-managed stay, and what the PMS said about it.
/// </summary>
/// <remarks>
/// <para>
/// GUEST-Q1's amendment and GUEST-Q3. PMS-connected is <b>PMS-writes-first,
/// staff-may-override</b>, never read-only: a staff action is recorded as an
/// override — who, when, and what the PMS said at that moment — and a later PMS
/// fact that differs is a <b>recorded disagreement, not a silent overwrite</b>.
/// </para>
/// <para>
/// <b>One truth still leaves the application.</b> While a disagreement stands,
/// the standing override is the answer — on the board, to every consumer, and
/// through Context. The disagreement is a <i>flag on that one answer</i>, never
/// a second answer: two rooms cannot both be occupied by one guest in Room
/// Care's world. A recorded override is a person looking at the guest; the
/// inbound fact is automation and possibly stale — and if the PMS silently won,
/// *"staff can override"* would be a suggestion.
/// </para>
/// <para>
/// <b>Only differing values are a disagreement.</b> A fact that matches settles
/// as <see cref="DisagreementState.Confirmed"/> and surfaces nothing, which is
/// what turns a six-hour outage's fourteen returning facts into the one that is
/// real.
/// </para>
/// <para>
/// <b>No <c>disagreement.*</c> event is published.</b> A disagreement is a fact
/// about our records, not about the hotel — ADR 0016 Part 2 publishes the fact
/// and not the process. Clearing to the PMS's side emits the same correction a
/// room move does, so Room Care re-plans from the event stream as always.
/// </para>
/// </remarks>
public class StayDisagreement
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }

    public DisagreementAspect Aspect { get; set; }

    /// <summary>What the stay holds — the staff value.</summary>
    public string OurValue { get; set; } = string.Empty;

    /// <summary>What the inbound fact said.</summary>
    public string PmsValue { get; set; } = string.Empty;

    /// <summary>Who made the override, and when — GUEST-Q1.</summary>
    public Guid? OverrideActor { get; set; }

    public DateTimeOffset OverrideAt { get; set; }

    /// <summary>What the PMS said at the moment of the override.</summary>
    /// <remarks>
    /// Kept because it is what makes the override explicable months later: the
    /// desk acted on what it could see, and that value is not recoverable from
    /// the current state.
    /// </remarks>
    public string? PmsValueAtOverride { get; set; }

    public DateTimeOffset RaisedAt { get; set; }

    public DisagreementState State { get; set; }

    public Guid? ClearedBy { get; set; }

    public DateTimeOffset? ClearedAt { get; set; }

    public RoomStay? Stay { get; set; }
}

/// <summary>Where a proposed join has got to.</summary>
public enum CandidateState
{
    /// <summary>Raised, and waiting for a person.</summary>
    Proposed = 1,

    /// <summary>The same stay. The held fact applies to the local one.</summary>
    Confirmed = 2,

    /// <summary>Two different stays — and a double-booked room, honestly.</summary>
    Rejected = 3,
}

/// <summary>
/// A PMS fact that might be a stay we already created — GUEST-Q5.
/// </summary>
/// <remarks>
/// <para>
/// The desk creates a walk-in at 11:00 with the PMS unreachable; at 15:05 the
/// PMS sends its own version of the same night. The two are joined by a
/// <b>staff-confirmed link, never an automatic match</b>.
/// </para>
/// <para>
/// <b>The candidate test is same room and overlapping dates.</b> Name
/// similarity may <i>rank</i> the list and may never <i>link</i> it: the system
/// this replaces joined stays by correlating
/// <c>(companyId, siteId, surname, firstName, arrivalDate)</c> against its own
/// private copy, and a wrong match silently merges two guests' histories —
/// worse than a duplicate, which is G360-Q1's reasoning for guests applied to
/// stays.
/// </para>
/// <para>
/// <b>Rejecting produces two stays and a double-booked room, because that is
/// then the truth.</b> It is also why the conflict check warns rather than
/// forbids: a hard block would put a ruled outcome out of reach.
/// </para>
/// </remarks>
public class StayLinkCandidate
{
    public Guid Id { get; set; }

    /// <summary>The stay this property created, marked PMS-unknown.</summary>
    public Guid LocalStayId { get; set; }

    /// <summary>The held inbound fact, not a second stay.</summary>
    /// <remarks>
    /// Nothing is published while a candidate is undecided. Creating the PMS's
    /// stay and merging later would announce a stay to every consumer that we
    /// intend to withdraw, and there is no honest event for that.
    /// </remarks>
    public Guid HeldFactId { get; set; }

    /// <summary>How alike the names are. Ranks the list; joins nothing.</summary>
    public double RankScore { get; set; }

    public CandidateState State { get; set; }

    public Guid? DecidedBy { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public DateTimeOffset RaisedAt { get; set; }

    public RoomStay? LocalStay { get; set; }
}
