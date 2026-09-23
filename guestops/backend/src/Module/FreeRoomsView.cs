using HotelOS.GuestOps.Application.Availability;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Which rooms the desk may give, of one type, over these dates — gold frame 10.
/// </summary>
/// <remarks>
/// <para>
/// <b>It did not exist, and its absence was a screen nobody could finish.</b>
/// <c>WalkInCommand</c> has required a <c>roomId</c> since it was written —
/// check-in needs a room, S8, the one hard gate — and no read in this
/// application listed rooms. So the walk-in sheet could be drawn, and no caller
/// could have completed it.
/// </para>
/// <para>
/// <b>That class of gap is invisible to a fidelity sweep.</b> Comparing a
/// drawing to a build finds nodes that differ; a control nobody could populate
/// has no node to differ from. It is found by asking what the WRITE needs and
/// what the reads actually send.
/// </para>
/// <para>
/// <b>The answer says nothing about cleanliness.</b> Whether a room is clean is
/// Room Care's, and an absent neighbour loses its capability and never this
/// flow (APPS-Q2). Frame 10 draws <i>vacant · clean</i>; this sends the first,
/// because the room being in the list is what establishes it, and withholds the
/// second because this application would be asserting somebody else's fact.
/// </para>
/// </remarks>
/// <param name="availability">Where "what is free" is decided, for both grains.</param>
public sealed class FreeRoomsView(AvailabilityService availability)
{
    /// <summary>The rooms free for a type over a range.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="roomTypeId">The type the desk chose.</param>
    /// <param name="from">Arrival, inclusive.</param>
    /// <param name="to">Departure, inclusive.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The rooms, and how many the type has at all.</returns>
    public async Task<object?> AnswerAsync(
        RequestScope scope,
        Guid roomTypeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var rooms = await availability.FreeRoomsAsync(
            scope, roomTypeId, from, to, cancellationToken);

        return new
        {
            rooms = rooms.Free
                .Select(room => new { id = room.Id.ToString(), number = room.Number })
                .ToArray(),

            // **Sent so an empty list is not one answer to two questions.** No
            // rooms of this type at all is a property to configure; every room
            // taken is a type to change. The screen says which, and could not
            // if this were absent.
            ofType = rooms.Total,
        };
    }
}
