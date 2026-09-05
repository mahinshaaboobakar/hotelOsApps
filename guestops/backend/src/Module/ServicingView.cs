using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// What happened in the room while the guest was in it — gold frame 6.
/// </summary>
/// <remarks>
/// <para>
/// <b>GuestOps owns none of this.</b> It announces occupancy and departure;
/// Room Care decides what work that becomes (APPS-Q1, S21). The tab reports and
/// asserts nothing — which is why a declined day is <i>declined</i> rather than
/// clean or dirty, and why the strip is per night rather than one status.
/// </para>
/// <para>
/// <b>So this projection has nothing of its own to return, and returns
/// nothing.</b> The nights come from Room Care through the Context Service, and
/// this application cannot make that call yet: an installed package has no
/// service certificate, and nothing enrols one at install. What it <i>can</i>
/// establish is whether Room Care answers for this property at all, which is
/// what decides between a dimmed tab and a populated one.
/// </para>
/// <para>
/// <b>Empty nights and absent nights are the same value here on purpose.</b>
/// Both are <c>null</c>, because both mean *this application has no servicing
/// record to show you* — and inventing a night per date with an empty status
/// would be exactly the fabrication the strip's design exists to avoid: a
/// design with one room status per day would have to lie about a declined day
/// (R1).
/// </para>
/// </remarks>
public sealed class ServicingView(INeighbours neighbours)
{
    /// <summary>Whether Room Care is here, and what it said.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, CancellationToken cancellationToken)
        => new
        {
            // Null in every case this build can reach. It is not a placeholder
            // for a list that failed to load — it is the absence of a Context
            // read this application is not yet able to make, and the screen
            // renders it as *nothing to show* rather than as *nothing happened*.
            nights = (object?)null,

            roomCareInstalled = await neighbours.InstalledAsync(
                scope, Neighbours.RoomCare, cancellationToken),
        };
}
