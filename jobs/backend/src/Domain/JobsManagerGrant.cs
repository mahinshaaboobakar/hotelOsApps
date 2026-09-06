namespace HotelOS.Jobs.Domain;

/// <summary>
/// One person the general manager has made a jobs manager — design §4.2's
/// <c>property#jobs_manager</c>, from this application's side of it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the record of an action, not a copy of the tuple.</b> The
/// authorization graph is the Kernel's: Jobs announces
/// <c>user.jobs_manager_granted</c> and <c>user.jobs_manager_revoked</c>, and
/// the Kernel folds them into the relation (§4.2, AUTHZ-Q25's mechanism). Jobs
/// never writes a tuple and never reads one to answer a question.
/// </para>
/// <para>
/// So why keep a row at all: <b>because the screen has to show the general
/// manager who they have granted</b>, and an action with no visible result is
/// one a person performs twice. Nothing else in the platform publishes this
/// relation's events — the manifest declares Jobs as their only source — so this
/// table and the graph cannot disagree except by a fold that failed, which is a
/// platform fault and shows up as a grant that does not work rather than as a
/// list that lies.
/// </para>
/// <para>
/// <b>Revocation is a column, not a delete.</b> Who held property-wide job
/// management and when is exactly the question an audit asks after the fact,
/// and a deleted row answers nothing.
/// </para>
/// </remarks>
public class JobsManagerGrant
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>The person who may now manage every job at this property.</summary>
    public Guid UserId { get; set; }

    public DateTimeOffset GrantedAt { get; set; }

    /// <summary>The general manager who granted it — the envelope's actor, kept here too.</summary>
    public Guid GrantedBy { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? RevokedBy { get; set; }

    /// <summary>Whether this grant is the one currently standing.</summary>
    public bool IsLive => RevokedAt is null;
}
