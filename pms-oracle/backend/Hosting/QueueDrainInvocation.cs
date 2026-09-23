using Google.Protobuf;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Authentication;
using PmsOracle.Integrations.Cloud;

namespace PmsOracle.Hosting;

/// <summary>
/// Serving <c>drain</c>: take what OHIP's business-event queue is holding — ADR 0194.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this replaces.</b> Until the transport existed, this answered every
/// drain with a fault naming two absences: no queue client, and no reader
/// turning a settings map into <c>IntegrationSettings</c>. The first is now
/// built (<see cref="OhipBusinessEventQueue"/>).
/// </para>
/// <para>
/// <b>The second is answered by ADR 0220, and this paragraph used to say it
/// was unfixable here.</b> It read: <i>"four of that type's seven fields are
/// property facts the protocol deliberately does not carry"</i> — true of the
/// message set as it stood, and the planner ruled the other way round: the
/// <b>Hub supplies</b> a property-context snapshot over this same session,
/// carrying the timezone, the two clock times, the currency, the tax basis and
/// the guarantee freshness. The connector never queries Core Administration,
/// Master Data or Context for them. So the facts arrive; they are not fetched,
/// and they are not this package's to own.
/// </para>
/// <para>
/// <b>It never blocked a drain in any case.</b> <see cref="DrainedPayload"/>
/// carries no property, because the Hub attributes payloads to the instance it
/// dispatched to — so taking bytes and labelling their kind needs credentials
/// and nothing else. What waits on ADR 0220's message is <i>normalisation</i>,
/// which is a different seam, and the guarantee enrichment that needs a
/// freshness the Hub will supply.
/// </para>
/// <para>
/// <b>A token per drain, held by nobody.</b> The credentials are requested
/// inside this invocation and a token is acquired with them; neither outlives
/// the call. A connector that kept a token between invocations would be holding
/// a credential derivative across a boundary the protocol draws deliberately —
/// and the refresher that would justify holding one belongs to the poll loop,
/// which is the Runtime's (<c>CONN-Q32a</c>).
/// </para>
/// <para>
/// <b>An incomplete configuration is a fault, not an empty drain.</b> Unlike a
/// test, a drain has no vocabulary for "your configuration is missing three
/// things" — <c>DrainResult</c> carries payloads only. Answering empty would
/// report a healthy poll of a queue that was never asked, so the fault says
/// which names are missing and the Hub keeps its own record of the failure.
/// </para>
/// </remarks>
public static class QueueDrainInvocation
{
    /// <summary>Why the Hub is asked for the credentials, for its own record.</summary>
    private const string Purpose = "drain the OHIP business-event queue";

    /// <summary>Drain once and answer what was taken.</summary>
    /// <param name="invocation">The <c>drain</c> invocation, carrying the settings.</param>
    /// <param name="http">The client to dial OHIP with.</param>
    /// <param name="cancellationToken">The invocation's.</param>
    /// <returns>A <see cref="DrainResult"/>, serialised.</returns>
    /// <exception cref="NotSupportedException">The configuration cannot make a call.</exception>
    public static async ValueTask<ReadOnlyMemory<byte>> ServeAsync(
        ConnectorInvocation invocation, HttpClient http, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var asked = DrainInvocation.Parser.ParseFrom(invocation.Payload.Span);

        var secrets = await InvocationCredentials.GrantedAsync(
            invocation, OhipCredentials.SecretNames, Purpose, cancellationToken);

        var reading = OhipCredentials.Read(asked.Settings, secrets);

        if (!reading.TryGet(out var credentials))
        {
            throw new NotSupportedException(
                "pms-oracle cannot drain: this integration's configuration is incomplete — "
                + string.Join(", ", reading.Missing)
                + ". Nothing was taken from the queue, so nothing was lost.");
        }

        var acquired = await OhipAccessToken.AcquireAsync(
            http, credentials, DateTimeOffset.UtcNow, cancellationToken);

        if (acquired.AccessToken is not { } token)
        {
            // The same finding a test would report, as a fault: a drain has
            // nowhere to put REFUSED or UNREACHABLE, and inventing an empty
            // result would record a poll that never reached OHIP.
            throw new NotSupportedException(
                $"pms-oracle cannot drain: {acquired.Finding.Detail} Nothing was taken from "
                + "the queue, so nothing was lost.");
        }

        var taken = await OhipBusinessEventQueue.DrainAsync(
            http, credentials, token, cancellationToken);

        var result = new DrainResult();

        result.Payloads.AddRange(taken.Select(one => new DrainedPayload
        {
            Payload = ByteString.CopyFrom(one.Payload),
            PayloadKind = one.PayloadKind,
        }));

        return result.ToByteArray();
    }
}
