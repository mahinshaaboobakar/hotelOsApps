namespace HotelOS.GuestOps.Domain;

/// <summary>
/// Something the guest asked for — S18.
/// </summary>
/// <remarks>
/// <para>
/// <b>GuestOps owns the request; Jobs owns the work.</b> *"What needs doing"* is
/// Jobs' domain (APPS-Q1), so raising a job from the stay page records this and
/// announces it — Jobs creates the job, assigns it and owns its status. No job
/// id, no assignee and no job state is stored here: applications communicate
/// through events and the Context Service, never by reaching into each other.
/// </para>
/// <para>
/// <b>Not every request becomes work.</b> A late checkout is answered at the
/// desk. The request is a fact about the stay and lives here whether or not
/// anything follows from it — which is also why it survives when Jobs is not
/// installed at all: an absent dependency loses its capability, never the flow
/// (APPS-Q2).
/// </para>
/// </remarks>
public class StayRequest
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }

    public string Text { get; set; } = string.Empty;

    public Guid? LoggedBy { get; set; }

    public DateTimeOffset LoggedAt { get; set; }

    /// <summary>Whether this was announced for another application to act on.</summary>
    public bool HandedOff { get; set; }

    /// <summary>The id this request was announced under — EVT-Q3.</summary>
    /// <remarks>
    /// <b>Between applications, a reply is an event carrying a correlation id,
    /// never a blocking call.</b> GuestOps publishes the request with this id;
    /// Jobs publishes its own fact carrying the same id plus the job it
    /// created; GuestOps stores that on consumption. A call would break the
    /// events-only rule and APPS-Q2 at once — an absent Jobs would hang the
    /// desk instead of leaving a request with no job yet.
    /// </remarks>
    public Guid? CorrelationId { get; set; }

    /// <summary>The job Jobs created, learned from its reply.</summary>
    /// <remarks>
    /// <para>
    /// <b>Stored on consumption, not resolved on read</b> — EVT-Q3's shape.
    /// This is the one thing about a job that lives here, and it is an
    /// identifier rather than state: what the job is <i>doing</i> stays Jobs',
    /// and asking that is a Context question.
    /// </para>
    /// <para>
    /// Null means <b>no job yet</b> — which is also what an uninstalled Jobs
    /// looks like, and deliberately so. The desk sees a request that has not
    /// become work; nothing waits, and nothing fails.
    /// </para>
    /// </remarks>
    public Guid? JobId { get; set; }

    public RoomStay? Stay { get; set; }
}
