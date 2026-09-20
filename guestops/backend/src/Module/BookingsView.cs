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

            // Numbers, not a phrase: `1 of 3 known` is English and its digits
            // are a locale's (ADR 0175). `claimed` is null unless the source
            // said more than it has sent, so the screen has the same two states
            // the sentence used to carry and does not have to parse one back
            // out of prose.
            rooms = booking.StayCount,
            claimed = booking.ExpectedStayCount is { } expected && expected > booking.StayCount
                ? expected
                : (int?)null,

            arrive = Iso(booking.Arrival),
            depart = Iso(booking.Departure),

            status = Status(booking.Status),
            statusTone = Tone(booking.Status),

            chips = Chips(booking),
        };

    /// <summary>A day as ISO-8601, or null — never a rendering.</summary>
    /// <remarks>
    /// <para>
    /// <b>A group's dates are its earliest arrival and its latest departure</b>
    /// — what a receptionist means by *when are they here*, even when two rooms
    /// of the booking leave on different days. Those two days travel; the span
    /// between them is drawn by <c>chrome/when.ts</c>'s <c>span()</c> in the
    /// property's locale.
    /// </para>
    /// <para>
    /// This sent <c>31 Aug → 2 Sep</c> until 2026-09-20 — the server's month
    /// abbreviation, the server's order, and <i>day use</i> in English. The
    /// order is the part that looks like a format and is not: a locale that
    /// writes the month first reads the same two days differently, and no
    /// server-side string can be right for both.
    /// </para>
    /// </remarks>
    private static string? Iso(DateOnly? day) => day?.ToString("yyyy-MM-dd");

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
            // It named Opera until 2026-09-19, whatever PMS the property runs. The
            // configured name is the Integration Hub's and not reachable from here
            // yet (SourceNameTests' remarks), so it says the function instead.
            chips.Add(new { text = "PMS", mark = "pms" });
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
