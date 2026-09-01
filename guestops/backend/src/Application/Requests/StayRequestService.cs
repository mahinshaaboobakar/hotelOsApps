using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Requests;

/// <summary>
/// What the guest asked for, and its hand-off to whoever does the work — S18.
/// </summary>
/// <remarks>
/// <para>
/// <b>GuestOps owns the request; Jobs owns the work</b> (APPS-Q1). Handing off
/// records the request and announces it; Jobs creates the job, assigns it and
/// owns its status. No assignee and no job state is stored here — the one thing
/// about a job that lives on the row is its identifier, learned from Jobs' own
/// reply.
/// </para>
/// <para>
/// <b>Not every request becomes work.</b> A late checkout is answered at the
/// desk. The request is a fact about the stay and lives here whether or not
/// anything follows from it — which is also why it survives when Jobs is not
/// installed at all: an absent dependency loses its capability, never the flow
/// (APPS-Q2).
/// </para>
/// </remarks>
public sealed class StayRequestService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    IEventAppender events,
    TimeProvider clock)
{
    /// <summary>Log something the guest asked for.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="stayId">The stay it is about.</param>
    /// <param name="text">What was asked.</param>
    /// <param name="handOff">Whether to announce it for another application to act on.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The stored request.</returns>
    /// <remarks>
    /// <b>Recording and handing off are one call, deliberately.</b> The desk
    /// decides at the moment of typing whether this is work; a separate
    /// hand-off step would let a request be recorded and then forgotten in the
    /// gap, which is the failure the paper log had.
    /// </remarks>
    public async Task<StayRequest> LogAsync(
        RequestScope scope,
        Guid stayId,
        string text,
        bool handOff,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RequestHandle, ResourceTypes.Stay, stayId, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidRequestException("text is required");
        }

        var stay = await db.Stays
            .FirstOrDefaultAsync(
                s => s.Id == stayId && s.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        var request = new StayRequest
        {
            Id = Uuid7.NewUuid7(),
            StayId = stay.Id,
            Text = text,
            LoggedBy = scope.UserId,
            LoggedAt = clock.GetUtcNow(),
            HandedOff = handOff,

            // Minted with the request, not at publication: the row must be able
            // to find its own reply even if the process stops between the two.
            CorrelationId = handOff ? Guid.NewGuid() : null,
        };

        db.Requests.Add(request);

        if (handOff)
        {
            // The subject is GUEST-Q11's, ruled 2026-09-01: the fact is about
            // a stay, so it joins the `stay.*` family, and the reply is
            // EVT-Q3's correlated event rather than a call back.
            events.Append(
                scope,
                "stay.request_raised",
                "stay",
                stay.Id,
                stay.Version,
                new Contracts.V1.StayRequestRaised
                {
                    RequestId = request.Id.ToString(),
                    StayId = stay.Id.ToString(),
                    PropertyId = stay.PropertyId.ToString(),
                    RoomId = stay.CurrentRoomId?.ToString() ?? string.Empty,
                    Text = text,
                    CorrelationId = request.CorrelationId!.Value.ToString(),
                });
        }

        await db.SaveChangesAsync(cancellationToken);
        return request;
    }

    /// <summary>Store the job Jobs created, from its reply — EVT-Q3.</summary>
    /// <param name="correlationId">The id this request was announced under.</param>
    /// <param name="jobId">The job Jobs created.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>True when a request was found and updated.</returns>
    /// <remarks>
    /// <para>
    /// <b>Driven by a recorded fact, as slice 2's inbound consumer is.</b> There
    /// is no .NET subscription surface on this platform yet, so this is the
    /// method a subscription will call and the flip is a one-line switch on a
    /// tested surface.
    /// </para>
    /// <para>
    /// <b>An unknown correlation id is not an error.</b> The reply may be for
    /// another application's request, or for one this property never made — a
    /// consumer that threw would dead-letter a message that is merely not ours.
    /// It returns false and the caller acknowledges.
    /// </para>
    /// <para>
    /// <b>No authorization is asked.</b> This is not a person acting; it is a
    /// fact arriving. The permission that governed the hand-off was checked when
    /// the desk raised the request.
    /// </para>
    /// </remarks>
    public async Task<bool> RecordJobAsync(
        Guid correlationId, Guid jobId, CancellationToken cancellationToken)
    {
        var request = await db.Requests
            .FirstOrDefaultAsync(r => r.CorrelationId == correlationId, cancellationToken);

        if (request is null)
        {
            return false;
        }

        // Idempotent: a redelivered reply must not look like a second job.
        if (request.JobId == jobId)
        {
            return true;
        }

        request.JobId = jobId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Add a remark about this stay.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="stayId">The stay it is about.</param>
    /// <param name="text">The remark.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The stored note.</returns>
    /// <remarks>
    /// <b>A note is about these nights, and a preference is about the guest.</b>
    /// The distinction is not decorative: a preference should be true next time
    /// and lives on the guest identity; a note dies with the stay. Writing a
    /// preference here would lose it at check-out.
    /// </remarks>
    public async Task<StayNote> AddNoteAsync(
        RequestScope scope, Guid stayId, string text, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RequestHandle, ResourceTypes.Stay, stayId, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidRequestException("text is required");
        }

        var stay = await db.Stays
            .FirstOrDefaultAsync(
                s => s.Id == stayId && s.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        var note = new StayNote
        {
            Id = Uuid7.NewUuid7(),
            StayId = stay.Id,
            Text = text,
            Author = scope.UserId,
            At = clock.GetUtcNow(),
        };

        db.Notes.Add(note);
        await db.SaveChangesAsync(cancellationToken);
        return note;
    }
}
