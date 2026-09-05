using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Everything the property has sold, as the bundle's <c>Bookings</c> — gold frame 2.
/// </summary>
/// <remarks>
/// <para>
/// <b>A booking's row shows its stays, not a room</b> (GUEST-Q2). The rooms
/// column is a count and, where the source claimed more than it has sent, the
/// design's own <i>1 of 3 known</i>. The stays that have not arrived are not
/// rows here — a placeholder row would be a stay nobody booked.
/// </para>
/// <para>
/// <b>The filters are returned, not applied.</b> The screen draws three
/// choosers and this build serves one setting for each: the query shape exists
/// on <see cref="BookingQuery"/> and nothing in the bundle can yet send a
/// different one. Returning what is in force — rather than an empty list the
/// screen fills in from its own defaults — keeps the labels a fact about the
/// answer instead of a caption the client wrote over it.
/// </para>
/// </remarks>
public sealed class BookingsView(BookingReadService bookings)
{
    /// <summary>One page of the list.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Paging.Window page, CancellationToken cancellationToken)
    {
        var found = await bookings.ListAsync(
            scope,
            new BookingQuery(Search: null, Arriving: null, Status: null, page),
            cancellationToken);

        return new
        {
            search = string.Empty,

            filters = new object[]
            {
                Filter("when", "Arriving · next 30 days"),
                Filter("status", "Any status"),
                Filter("source", "Any source"),
            },

            total = found.Total,
            rows = found.Rows.Select(Row).ToArray(),
        };
    }

    /// <summary>One chooser, showing what is in force.</summary>
    private static object Filter(string key, string label)
        => new { key, choices = new[] { new { label, on = true } } };

    /// <summary>One booking, as the design draws it.</summary>
    private static object Row(BookingSummary booking)
        => new
        {
            id = booking.Id.ToString(),

            // "Not yet named" is the design's own words for a real state, not a
            // placeholder standing in for a name that failed to load (R25).
            guest = booking.Unnamed ? "Not yet named" : booking.Guest,
            unnamed = booking.Unnamed,

            // **Absent, and this is a reported gap** — GUEST-Q12. Contacts are
            // stored encrypted and the protector has only a write direction, so
            // nothing here can mask a value it cannot read. The design draws
            // one; an approximation would be a phone number nobody has.
            contact = (string?)null,

            reference = booking.Reference ?? "created here",
            createdHere = booking.Reference is null,
            confirmation = booking.Confirmation,

            rooms = Rooms(booking),
            dates = Dates(booking),

            status = Status(booking.Status),
            statusTone = Tone(booking.Status),

            chips = Chips(booking),
        };

    /// <summary>`1`, or the incomplete group said out loud.</summary>
    /// <remarks>
    /// The claim is the <i>source's</i>, which is why it is only stated when
    /// there is one: a booking this desk created has no expected count, and
    /// printing <i>1 of 1 known</i> over it would attribute a claim to nobody.
    /// </remarks>
    private static string Rooms(BookingSummary booking)
        => booking.ExpectedStayCount is { } expected && expected > booking.StayCount
            ? $"{booking.StayCount} of {expected} known"
            : booking.StayCount.ToString();

    /// <summary>`31 Aug → 2 Sep`, over every stay in the booking.</summary>
    /// <remarks>
    /// A group's dates are its earliest arrival and its latest departure —
    /// which is what a receptionist means by *when are they here*, even when
    /// two rooms of the booking leave on different days.
    /// </remarks>
    private static string? Dates(BookingSummary booking)
    {
        if (booking.Arrival is not { } from)
        {
            return null;
        }

        var arrival = from.ToString("d MMM");

        return booking.Departure is { } to
            ? to == from ? $"{arrival} · day use" : $"{arrival} → {to:d MMM}"
            : arrival;
    }

    /// <summary>The design's own word for each lifecycle.</summary>
    private static string Status(StayLifecycle lifecycle)
        => lifecycle switch
        {
            StayLifecycle.InHouse => "In house",
            StayLifecycle.Booked => "Booked",
            StayLifecycle.Waitlisted => "Waitlisted",
            StayLifecycle.Departed => "Departed",
            StayLifecycle.Cancelled => "Cancelled",
            StayLifecycle.NoShow => "No-show",
            _ => lifecycle.ToString(),
        };

    /// <summary>
    /// Which tone the pill takes.
    /// </summary>
    /// <remarks>
    /// <c>bad</c> is <i>over</i>, not <i>wrong</i>: a cancelled reservation and
    /// a no-show are both ordinary outcomes, and both stay in the list (S25,
    /// S27, ADR 0062). <c>warn</c> is Waitlisted alone, because it is the one
    /// state that needs a decision — GUEST-Q9, a queue position holding no room.
    /// </remarks>
    private static string Tone(StayLifecycle lifecycle)
        => lifecycle switch
        {
            StayLifecycle.InHouse => "ok",
            StayLifecycle.Waitlisted => "warn",
            StayLifecycle.Cancelled or StayLifecycle.NoShow => "bad",
            _ => "neutral",
        };

    /// <summary>Where the booking came from, and what disagrees about it.</summary>
    /// <remarks>
    /// Only what the row itself establishes. The design's <i>Opera says
    /// cancelled</i> names the aspect of a disagreement, which this projection
    /// does not read — so it says <i>disagrees</i>, which is true and less
    /// specific, rather than naming an aspect it did not look at.
    /// </remarks>
    private static object[] Chips(BookingSummary booking)
    {
        var chips = new List<object>();

        if (booking.WalkIn)
        {
            chips.Add(new { text = "walk-in", mark = "walkin" });
        }

        if (booking.Reference is not null)
        {
            chips.Add(new { text = "Opera", mark = "pms" });
        }

        if (booking.Overridden)
        {
            chips.Add(new { text = "override", mark = "override" });
        }
        else if (booking.Disagrees)
        {
            chips.Add(new { text = "disagrees", mark = "disagrees" });
        }

        if (booking.PmsUnknown)
        {
            chips.Add(new { text = "the PMS has not sent this", mark = "unknown" });
        }

        if (!booking.AnyRoomAssigned && booking.Status is StayLifecycle.Booked)
        {
            chips.Add(new { text = "no rooms assigned", mark = "missing" });
        }

        if (booking.Status is StayLifecycle.Waitlisted)
        {
            chips.Add(new { text = "holds no room", mark = "missing" });
        }

        return [.. chips];
    }
}
