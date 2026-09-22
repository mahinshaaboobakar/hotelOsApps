using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Adapters;
using PmsOracle.Normalisation;

namespace PmsOracle.Hosting;

/// <summary>
/// Serving <c>drain</c>: take what OHIP's business-event queue is holding —
/// ADR 0194. <b>Dispatched, and it refuses with what is missing.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is a refusal and not an empty drain.</b> OHIP's queue hands a
/// message over once, so a drain that answered zero payloads would record a
/// successful pass that took nothing — a measurement nobody made, and one the
/// Hub would count as a healthy poll. The fault names what is absent instead,
/// which is the difference between "nothing was waiting" and "nothing could
/// have been taken".
/// </para>
/// <para>
/// <b>Two halves are missing, and they are different work.</b> The transport
/// is <see cref="IOhipQueue"/> and <see cref="IOhipGuarantees"/>, which this
/// package declares and does not implement — the only implementations anywhere
/// are test doubles. The second is quieter: <see cref="IntegrationSettings"/>
/// carries the property's clock, currency and tax basis, and <b>nothing reads
/// it from a configuration map</b>; it is constructed in five tests and nowhere
/// else. So even with a queue, a drain arriving as
/// <c>map&lt;string, string&gt;</c> has no reader to become what the drain
/// logic takes.
/// </para>
/// <para>
/// <b>The kind is dispatched anyway, deliberately.</b> An unrecognised kind and
/// an unimplemented one are different facts, and folding them together would
/// tell an operator the connector does not know what <c>drain</c> means. It
/// knows exactly what it means and cannot do it yet.
/// </para>
/// </remarks>
public static class QueueDrainInvocation
{
    /// <summary>Refuse one drain, naming what is absent.</summary>
    /// <param name="invocation">The <c>drain</c> invocation.</param>
    /// <param name="cancellationToken">Unused: a refusal does no work to cancel.</param>
    /// <returns>A faulted task, always — the session answers it as a <c>Fault</c>.</returns>
    /// <remarks>
    /// The payload is parsed before refusing, so a malformed
    /// <see cref="DrainInvocation"/> is reported as the protocol error it is
    /// rather than hidden behind the transport's absence. A refusal that
    /// reports the wrong reason is the class this package has already paid for
    /// once, in the sentence this file replaced.
    /// </remarks>
    public static ValueTask<ReadOnlyMemory<byte>> ServeAsync(
        ConnectorInvocation invocation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        _ = DrainInvocation.Parser.ParseFrom(invocation.Payload.Span);

        return ValueTask.FromException<ReadOnlyMemory<byte>>(new NotSupportedException(
            "pms-oracle understands 'drain' and cannot serve one yet. Two things are absent: "
            + $"{nameof(IOhipQueue)} and {nameof(IOhipGuarantees)} are ports this package "
            + "declares and does not implement, so nothing can be taken from OHIP's queue; and "
            + $"nothing reads {nameof(IntegrationSettings)} from a configuration map, so the "
            + "settings this invocation carries cannot become what the drain needs. Answering "
            + "an empty drain instead would record a pass that took nothing from a source that "
            + "hands each message over once."));
    }
}
