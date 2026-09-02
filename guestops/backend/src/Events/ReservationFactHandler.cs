using Wire = HotelOS.Contracts.Integration.V1;
using HotelOS.GuestOps.Application.Inbound;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Events;

/// <summary>
/// A reservation fact from the Integration Hub — the PMS half of GUEST-Q1.
/// </summary>
/// <remarks>
/// <para>
/// <b>This closes the chain.</b> Oracle to the connector, the connector to the
/// Hub's pipeline, the Hub to the bus, and the bus to here — where R7's rule
/// decides what the fact does to the stay. Everything behind this method was
/// built against recorded facts; this is the wire delivering the same shapes.
/// </para>
/// <para>
/// <b>The mapping is at the edge, and only here.</b>
/// <see cref="RoomStayFactMapper"/> reads the Hub's contract into
/// <see cref="InboundStayFact"/>; nothing past it knows the wire type exists.
/// </para>
/// <para>
/// <b>What it does not do is decide.</b> Whether the fact creates a stay,
/// settles silently against a standing override, records a disagreement or is
/// held as a candidate link is <see cref="InboundFactService"/>'s, and was
/// ruled long before this handler existed. A handler that branched on the
/// outcome would be a second copy of that rule.
/// </para>
/// </remarks>
public sealed class ReservationFactHandler(InboundFactService facts)
    : IEventHandler<Wire.RoomStayFact>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        RequestScope scope,
        Wire.RoomStayFact payload,
        EventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        // A fact that cannot be read is refused rather than half-applied: the
        // mapper throws when the Hub sends something its own contract forbids —
        // no header, or an identifier Enrich should have resolved. Letting that
        // reach the consumer host is deliberate, because the alternative is a
        // stay built from a fact nobody could read.
        var fact = RoomStayFactMapper.Read(payload);

        await facts.ApplyAsync(scope, fact, cancellationToken);
    }
}
