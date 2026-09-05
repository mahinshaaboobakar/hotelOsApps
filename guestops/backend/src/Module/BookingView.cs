using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// One booking and its stays — gold frames 8 and 9.
/// </summary>
/// <remarks>
/// <para>
/// <b>One projection for two frames, because the difference is in the data.</b>
/// Frame 8 is a complete booking being cancelled and frame 9 is an incomplete
/// one drawn honestly; what separates them is whether the source claimed more
/// stays than it has sent. A second projection would be two places to keep the
/// same rules.
/// </para>
/// <para>
/// <b>The stays the source has not sent are not rows.</b> They have no room
/// type, no dates and no guest, so a row for one would be a stay nobody booked
/// — GUEST-Q2, and frame 9's whole point. What the source claimed is said in a
/// sentence instead, where it is a statement <i>about</i> the booking.
/// </para>
/// </remarks>
public sealed class BookingView(
    GuestOpsDbContext db,
    BookingReadService bookings)
{
    /// <summary>The booking the bundle asked for.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Guid bookingId, CancellationToken cancellationToken)
    {
        var record = await bookings.GetAsync(scope, bookingId, cancellationToken);
        var types = await TypesAsync(record, cancellationToken);

        return new
        {
            id = record.Id.ToString(),
            guest = record.Guest ?? "Not yet named",
            reference = record.Reference ?? "created here",

            summary = Summary(record),

            // Only where a source manages it. In a standalone property there is
            // no PMS to name, and a sentence saying one manages this booking
            // would be an attribution to a system nobody installed.
            managedBy = record.Reference is null ? null : "Opera manages this booking",

            stays = record.Stays.Select(stay => Stay(stay, types)).ToArray(),
            incomplete = Incomplete(record),
        };
    }

    /// <summary>`Two stays · 3 Sep → 7 Sep`.</summary>
    /// <remarks>
    /// The count is spelled for the small numbers a booking usually has,
    /// because the design spells it — <i>Two stays</i> reads as a sentence and
    /// <i>2 stays</i> reads as a field.
    /// </remarks>
    private static string Summary(BookingRecord record)
    {
        var count = record.Stays.Count switch
        {
            1 => "One stay",
            2 => "Two stays",
            3 => "Three stays",
            var many => $"{many} stays",
        };

        if (record.Arrival is not { } from)
        {
            return count;
        }

        return record.Departure is { } to
            ? $"{count} · {from:d MMM} → {to:d MMM}"
            : $"{count} · {from:d MMM}";
    }

    /// <summary>One stay, as the design draws it.</summary>
    private static object Stay(
        BookingStayRow stay, IReadOnlyDictionary<string, string> types)
        => new
        {
            id = stay.Id.ToString(),
            guest = stay.Unnamed ? "Not yet named" : stay.Guest,
            unnamed = stay.Unnamed,

            // Elided the way the design elides it: a stay's id is a UUID and
            // the column is one of six. Both ends are kept because that is what
            // makes it recognisable against a log line.
            stayId = Elide(stay.Id),

            roomType = stay.RoomTypeId is { } id && types.TryGetValue(id, out var name)
                ? name
                : null,

            dates = Dates(stay),
            status = Status(stay.Status),
            statusTone = Tone(stay.Status),
            chips = Chips(stay),
        };

    /// <summary>`01J9M…22B1` — enough of an id to recognise, not enough to read.</summary>
    private static string Elide(Guid id)
    {
        var text = id.ToString("N").ToUpperInvariant();
        return $"{text[..5]}…{text[^4..]}";
    }

    /// <summary>`3 Sep → 7 Sep`.</summary>
    private static string? Dates(BookingStayRow stay)
    {
        if (stay.Arrival is not { } from)
        {
            return null;
        }

        return stay.Departure is { } to
            ? to == from ? $"{from:d MMM} · day use" : $"{from:d MMM} → {to:d MMM}"
            : $"{from:d MMM}";
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

    /// <summary>Which tone the pill takes.</summary>
    private static string Tone(StayLifecycle lifecycle)
        => lifecycle switch
        {
            StayLifecycle.InHouse => "ok",
            StayLifecycle.Waitlisted => "warn",
            StayLifecycle.Cancelled or StayLifecycle.NoShow => "bad",
            _ => "neutral",
        };

    /// <summary>What this stay is missing, in the design's vocabulary.</summary>
    private static object[] Chips(BookingStayRow stay)
    {
        var chips = new List<object>();

        if (stay.Unnamed)
        {
            chips.Add(new { text = "party unnamed", mark = "missing" });
        }

        if (!stay.Assigned && stay.Status is StayLifecycle.Booked)
        {
            chips.Add(new { text = "no room", mark = "missing" });
        }

        if (stay.PmsUnknown)
        {
            chips.Add(new { text = "the PMS has not sent this", mark = "unknown" });
        }

        return [.. chips];
    }

    /// <summary>
    /// What the source claimed and has not sent — frame 9's sentence.
    /// </summary>
    /// <remarks>
    /// Null when the booking is complete, so the screen draws nothing rather
    /// than a note saying everything is fine. It is stated as the source's
    /// claim rather than as our own count, because that is what it is: we know
    /// how many stays exist, and only the source knows how many it means to
    /// send.
    /// </remarks>
    private static string? Incomplete(BookingRecord record)
    {
        if (record.Expected is not { } expected || expected <= record.Stays.Count)
        {
            return null;
        }

        var missing = expected - record.Stays.Count;

        return $"Opera says this booking has {expected} rooms and has sent "
            + $"{record.Stays.Count}. The other {missing} "
            + (missing == 1 ? "is not a row" : "are not rows")
            + " — not a placeholder, and not counted. They appear when the "
            + "source sends them.";
    }

    /// <summary>The room type names for this booking's stays.</summary>
    /// <remarks>
    /// Read from Master Data's own table through the read model, never copied
    /// into this schema: the type is a canonical entity and this application
    /// references it (ADR 0051).
    /// </remarks>
    private async Task<IReadOnlyDictionary<string, string>> TypesAsync(
        BookingRecord record, CancellationToken cancellationToken)
    {
        var ids = record.Stays
            .Select(stay => stay.RoomTypeId)
            .Where(id => id is not null)
            .Select(id => Guid.Parse(id!))
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<string, string>();
        }

        var types = await db.Set<MasterDataRoomTypeName>()
            .Where(type => ids.Contains(type.Id))
            .ToListAsync(cancellationToken);

        return types.ToDictionary(type => type.Id.ToString(), type => type.Name);
    }
}
