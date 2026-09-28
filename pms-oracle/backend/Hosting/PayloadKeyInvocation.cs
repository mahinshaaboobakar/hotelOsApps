using Google.Protobuf;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations;

namespace PmsOracle.Hosting;

/// <summary>
/// Serving <c>dedupe_key</c>: what are these bytes, across redeliveries?
/// </summary>
/// <remarks>
/// <para>
/// <b>Named for what it serves rather than after the message it parses.</b> A
/// class called <c>DedupeKeyInvocation</c> in this namespace would shadow the
/// contract type of that name, because C# resolves the enclosing namespace
/// before a using-alias (<c>ARCH-Q42</c>) — so <c>Parser.ParseFrom</c> would
/// bind to this class instead. <see cref="PartJoinInvocation"/> is named apart
/// from <c>JoinInvocation</c> for the same reason.
/// </para>
/// <para>
/// <b>Why the capability exists when the drain already carries a key</b>
/// (ADR 0246): two callers hold bytes with no key — a quarantined payload
/// being re-submitted (ADR 0253 C1), and a push the Hub has staged and
/// answered 202 for (ADR 0272 §3). <b>The Hub never computes one itself</b>
/// (ADR 0255 §2), so for a <c>delivery: push</c> integration this invocation
/// is the ONLY way a payload is ever keyed. Both on-site integrations are
/// push, which is why this was not optional.
/// </para>
/// <para>
/// <b>The settings and the property context are read by nothing here, and that
/// is the contract rather than an omission.</b> The protocol calls this a pure
/// function of the bytes — <i>"nothing is read from the source, so a cancelled
/// or repeated invocation loses nothing"</i> — and a key that varied with
/// configuration would change the day somebody edited a setting, turning one
/// stored fact into two. <see cref="PayloadIdentity"/> takes the kind and the
/// bytes, and there is nothing else it could take.
/// </para>
/// </remarks>
public static class PayloadKeyInvocation
{
    /// <summary>Answer one <c>dedupe_key</c>.</summary>
    /// <param name="invocation">The invocation, carrying one payload.</param>
    /// <param name="cancellationToken">The invocation's.</param>
    /// <returns>A <see cref="DedupeKeyResult"/>, serialised.</returns>
    public static ValueTask<ReadOnlyMemory<byte>> ServeAsync(
        ConnectorInvocation invocation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        cancellationToken.ThrowIfCancellationRequested();

        var asked = DedupeKeyInvocation.Parser.ParseFrom(invocation.Payload.Span);

        var result = new DedupeKeyResult
        {
            DedupeKey = PayloadIdentity.For(asked.PayloadKind, asked.Payload.Span),
        };

        return ValueTask.FromResult<ReadOnlyMemory<byte>>(result.ToByteArray());
    }
}
