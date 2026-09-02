using System.Text.Json.Serialization;
using HotelOS.GuestOps.Application.Requests;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Events;

/// <summary>Jobs' reply, as it arrives on the bus.</summary>
/// <remarks>
/// Only the two fields this application reads. The envelope carries the rest,
/// and a payload record that mirrored every field Jobs publishes would be a
/// second copy of their contract — one that breaks on a field they add.
/// </remarks>
/// <param name="JobId">The job Jobs created.</param>
public sealed record JobCreated([property: JsonPropertyName("job_id")] Guid JobId);

/// <summary>
/// A request the desk raised has become a job — <c>GUEST-Q11</c>, <c>EVT-Q3</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The correlation id is the link, and it is on the envelope.</b> The desk
/// announced <c>stay.request_raised</c> under a correlation id; Jobs replies
/// with <c>job.created</c> carrying the same one. There is no call back and no
/// blocking wait — <c>EVT-Q3</c>: between two applications a reply is an event
/// carrying a correlation id.
/// </para>
/// <para>
/// <b>An unknown correlation id is not an error.</b> The reply may be for
/// another application's request, or for one this property never made. The
/// service returns false and this returns normally, so the host acknowledges:
/// throwing would dead-letter a message that is merely not ours.
/// </para>
/// </remarks>
public sealed class JobCreatedHandler(StayRequestService requests)
    : IEventHandler<JobCreated>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        RequestScope scope,
        JobCreated payload,
        EventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(envelope.CorrelationId, out var correlation))
        {
            // A reply with no correlation id cannot be matched to anything. Not
            // ours by definition, and acknowledged for the same reason an
            // unknown one is.
            return;
        }

        await requests.RecordJobAsync(correlation, payload.JobId, cancellationToken);
    }
}
