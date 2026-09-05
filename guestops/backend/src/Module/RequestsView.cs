using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The guest's requests, and what Jobs made of them — gold frames 5 and 5b.
/// </summary>
/// <remarks>
/// <para>
/// <b>The request is ours; the work is not</b> (S18, APPS-Q1). GuestOps records
/// the guest's request and announces it; Jobs creates the job and owns
/// everything after that. This projection reads <c>guestops.stay_requests</c>
/// and <b>never</b> reaches into Jobs — applications communicate through events
/// and the Context Service, never by reading each other's tables.
/// </para>
/// <para>
/// <b>Not every request is a job.</b> A late checkout is answered at the desk,
/// and it lives here anyway: a request is a fact about the guest's stay whether
/// or not any work follows from it.
/// </para>
/// <para>
/// <b>The jobs panel is absent in two different ways, and they must not
/// collapse.</b> <c>null</c> is *Jobs is not installed* — the invitation. An
/// empty list is *Jobs is here and this stay has raised nothing*. Telling a
/// property to install what they already have is the mistake that would follow
/// from merging them.
/// </para>
/// </remarks>
public sealed class RequestsView(
    GuestOpsDbContext db,
    INeighbours neighbours)
{
    /// <summary>What was asked for, and what became of it.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Guid stayId, CancellationToken cancellationToken)
    {
        var installed = await neighbours.InstalledAsync(
            scope, Neighbours.Jobs, cancellationToken);

        var requests = await db.Requests
            .Where(request => request.StayId == stayId)
            .OrderBy(request => request.LoggedAt)
            .Select(request => new
            {
                request.LoggedAt,
                request.Text,
                request.HandedOff,
                request.JobId,
            })
            .ToListAsync(cancellationToken);

        return new
        {
            ours = requests.Select(request => new
            {
                key = request.LoggedAt.ToString("HH:mm"),
                what = request.Text,
                state = State(request.HandedOff, request.JobId, installed),
                stateTone = request.HandedOff ? "warn" : "neutral",
                note = (string?)null,
            }).ToArray(),

            // **Not built, and null is the wrong answer here.** Null means
            // *Jobs is not installed*, and this projection does not know
            // whether this stay has jobs — resolving `stay → jobs` is a Context
            // read this application cannot make yet (no service certificate).
            // So when Jobs IS installed the panel is returned empty rather than
            // absent: the desk sees a panel that says Jobs is here and shows
            // nothing, which is true, instead of an invitation to install what
            // they have.
            jobs = installed == false ? null : Array.Empty<object>(),

            jobsInstalled = installed,
        };
    }

    /// <summary>
    /// What became of a request, in the design's words.
    /// </summary>
    /// <remarks>
    /// With Jobs absent every request reads <i>logged</i>, because that is all
    /// that happened to it — frame 5b. With Jobs present a handed-off request
    /// names the job where it has an id, and says it was raised where the id
    /// has not come back yet: the hand-off is an event, so there is a real
    /// moment between announcing it and hearing which job it became.
    /// </remarks>
    private static string State(bool handedOff, Guid? jobId, bool? installed)
    {
        if (installed == false)
        {
            return "logged";
        }

        if (!handedOff)
        {
            return "no job needed";
        }

        return jobId is { } id ? $"raised as {Short(id)}" : "raised — awaiting the job";
    }

    /// <summary>
    /// A job's id, shortened for a chip.
    /// </summary>
    /// <remarks>
    /// <b>Not <c>JOB-8821</c>.</b> The design draws Jobs' own human reference,
    /// and what this application holds is the UUID it heard on <c>job.created</c>
    /// — the reference is Jobs' to issue and ours to display only if it is sent.
    /// Printing an invented <c>JOB-</c> number would be a reference nobody could
    /// look up, in the field a receptionist would quote to an engineer.
    /// </remarks>
    private static string Short(Guid id)
        => id.ToString("N").ToUpperInvariant()[..6];
}
