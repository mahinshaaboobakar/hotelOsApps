using System.Text.Json;
using Google.Protobuf;
using HotelOS.Connector;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.OnSite;
using PmsOracle.Normalisation;

namespace PmsOracle.Hosting;

/// <summary>
/// Serving <c>join</c>: is this message whole, or half of a check-in?
/// </summary>
/// <remarks>
/// <para>
/// <b>The on-site agent sends a check-in as two messages and supplies no
/// correlation identifier</b> (R6), so the Hub cannot pair them on anything it
/// can see. This answers what it needs: the key the two halves share, which
/// half this one is, and how long to hold it.
/// </para>
/// <para>
/// <b>The decision is <see cref="OnSiteJoinKey.Candidate"/>'s, not this
/// file's.</b> It moved there when <c>join</c> crossed the protocol, because
/// <c>OracleOnSiteAdapter.JoinFor</c> answers the same question and this path
/// cannot construct that adapter. Two copies would be two answers to *is this
/// a part*, and the halves would stop pairing the day they disagreed.
/// </para>
/// <para>
/// <b>Anything that is not a recognised half is <c>whole</c>.</b> A cloud
/// payload, a blank or unrecognised status, a name the key cannot be built
/// from — all whole. That is not a refusal: a message declared whole goes
/// straight to normalisation, which is what refuses it with the field named.
/// Answering <c>member</c> on a doubtful message would hold a payload for a
/// partner that is never coming, and it would expire as
/// <c>join_window_expired</c> rather than as the rejection it actually is.
/// </para>
/// <para>
/// <b>Thirty minutes, and it is the connector's to state.</b> The window is
/// the agent's behaviour rather than a platform policy — the two messages
/// arrive together in practice, and the window is the margin for a retry, not
/// an expectation. It travels in <c>hold_seconds</c> because only this end
/// knows what its source does, which is the same reason the drain states its
/// own interval.
/// </para>
/// </remarks>
public static class PartJoinInvocation
{
    /// <summary>How long the Hub holds a half, waiting for its partner.</summary>
    /// <remarks>
    /// Kept here rather than read from <c>OracleOnSiteAdapter.JoinWindow</c>:
    /// that adapter is the retired in-process seam (<c>CONN-Q42</c>) and this
    /// is the path the Hub will use. The two agree today, and the protocol's
    /// is the one that answers a live invocation.
    /// </remarks>
    public static readonly TimeSpan HoldWindow = TimeSpan.FromMinutes(30);

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    /// <summary>Answer one <c>join</c>.</summary>
    /// <param name="invocation">The invocation, carrying one payload.</param>
    /// <param name="cancellationToken">The invocation's.</param>
    /// <returns>A <see cref="JoinResult"/>, serialised.</returns>
    public static ValueTask<ReadOnlyMemory<byte>> ServeAsync(
        ConnectorInvocation invocation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        cancellationToken.ThrowIfCancellationRequested();

        var asked = JoinInvocation.Parser.ParseFrom(invocation.Payload.Span);

        return ValueTask.FromResult<ReadOnlyMemory<byte>>(Answer(asked).ToByteArray());
    }

    /// <summary>What this payload is, as the protocol says it.</summary>
    /// <param name="asked">The parsed invocation.</param>
    /// <returns>Whole, or a membership.</returns>
    private static JoinResult Answer(JoinInvocation asked)
    {
        // Only the on-site stay message is ever half of anything. A room-status
        // push and every OHIP payload are whole by construction, so the kind is
        // checked before the body is read rather than after.
        //
        // The identifier is read from its one home rather than spelled again —
        // ADR 0272 lets code consume a declared kind and forbids a competing
        // list. That home is on the retired adapter (`CONN-Q42`) because the
        // pushes have no producer in this package to own it; reading a constant
        // is not building onto that seam, and a second spelling would be.
        if (asked.PayloadKind != Adapters.OracleOnSiteAdapter.StayPayload)
        {
            return new JoinResult { Whole = new WholePayload() };
        }

        // A body that will not parse is whole, and the normaliser rejects it
        // naming the field. Holding it for a partner would turn a refusal an
        // operator can act on into a window that quietly expires.
        var push = Read(asked.Payload);

        if (push is null || OnSiteJoinKey.Candidate(push) is not { } candidate)
        {
            return new JoinResult { Whole = new WholePayload() };
        }

        return new JoinResult
        {
            Member = new JoinMembership
            {
                Key = candidate.Key,
                Part = candidate.Part,
                HoldSeconds = (uint)HoldWindow.TotalSeconds,
            },
        };
    }

    /// <summary>The message, or <c>null</c> when it will not parse.</summary>
    /// <param name="payload">The bytes as the agent sent them.</param>
    /// <returns>The message, or <c>null</c>.</returns>
    private static OnSitePush? Read(ByteString payload)
    {
        try
        {
            return JsonSerializer.Deserialize<OnSitePush>(payload.Span, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
