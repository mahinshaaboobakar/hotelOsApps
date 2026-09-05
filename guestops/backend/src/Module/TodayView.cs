using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The front desk day, as the bundle's <c>Today</c> — gold frame 1.
/// </summary>
/// <remarks>
/// <para>
/// The shape is the bundle's, not the platform's: <c>book/model.ts</c> declares
/// it and this produces it. That asymmetry is deliberate — the envelope defines
/// the path and the status vocabulary and nothing inside it, so an
/// application's UI vocabulary never becomes the platform's contract language.
/// </para>
/// <para>
/// <b>Four lists, four counts, one page of rows.</b> The counts are every list's
/// total; the rows are the paged list the desk is looking at. That is what makes
/// the tabs honest while the table stays bounded — and it is why
/// <see cref="StayListService"/> returns both.
/// </para>
/// </remarks>
public sealed class TodayView(
    GuestOpsDbContext db,
    StayListService stays,
    IBusinessDay businessDay)
{
    /// <summary>The four lists, with one of them paged.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Paging.Window page, CancellationToken cancellationToken)
    {
        var date = await businessDay.CurrentAsync(scope, cancellationToken);
        var bounds = date is { } day
            ? await businessDay.BoundsAsync(scope, day, cancellationToken)
            : null;

        var lists = new List<object>();
        var stats = new List<object>();

        foreach (var (view, label) in Views)
        {
            var found = await stays.ListAsync(
                scope, new StayQuery(view, date, page), cancellationToken);

            var rows = await RowsAsync(scope, found.Rows, cancellationToken);

            lists.Add(new
            {
                key = view.ToString().ToLowerInvariant(),
                label,
                count = found.Total.ToString(),
                rows,
            });

            stats.Add(new { value = found.Total.ToString(), label });
        }

        return new
        {
            // The date the property is on, as the property states it. Null
            // rather than today's date when Context cannot answer: a business
            // day this application computed would be the one thing ADR 0128 §6
            // says it must never do.
            businessDate = date?.ToString("dd MMM"),

            // The roll time, read off the boundary the property configured. The
            // offset is baked into `Start` by the adapter, so formatting it
            // gives the property's own local hour rather than the server's.
            rollsAt = bounds?.Start.ToString("HH:mm"),

            // Whether a PMS writes this property's lifecycle. A feed mark exists
            // only once a fact has arrived through the Hub, so this is a fact
            // about the property rather than a setting somebody ticked.
            connected = await db.FeedMarks
                .AnyAsync(mark => mark.PropertyId == scope.PropertyId, cancellationToken),

            stats,
            lists,
        };
    }

    /// <summary>The four lists, in the design's order.</summary>
    private static readonly (StayView View, string Label)[] Views =
    [
        (StayView.Arrivals, "Arrivals"),
        (StayView.InHouse, "In house"),
        (StayView.Departures, "Departures"),
        (StayView.Attention, "Attention"),
    ];

    /// <summary>One page of stays, as the design's rows.</summary>
    /// <remarks>
    /// Names are resolved in two queries for the whole page rather than one per
    /// row: a room type is the same for most of a morning's arrivals, and a
    /// lookup per row is the N+1 a list screen notices first.
    /// </remarks>
    private async Task<IReadOnlyList<object>> RowsAsync(
        RequestScope scope,
        IReadOnlyList<RoomStay> page,
        CancellationToken cancellationToken)
    {
        if (page.Count == 0)
        {
            return [];
        }

        var typeIds = page.Select(s => s.RoomTypeId).Distinct().ToArray();
        var roomIds = page.Where(s => s.CurrentRoomId is not null)
            .Select(s => s.CurrentRoomId!.Value).Distinct().ToArray();

        var types = await db.Set<MasterDataRoomTypeName>()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var rooms = await db.Set<MasterDataRoom>()
            .Where(r => r.PropertyId == scope.PropertyId && roomIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.RoomNumber, cancellationToken);

        var names = await NamesAsync(page, cancellationToken);
        var refs = await ReferencesAsync(page, cancellationToken);

        return [.. page.Select(stay => Row(stay, types, rooms, names, refs))];
    }

    /// <summary>The named guest on each stay, where the party has one.</summary>
    private async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        IReadOnlyList<RoomStay> page, CancellationToken cancellationToken)
    {
        var stayIds = page.Select(s => s.Id).ToArray();

        var party = await db.Party
            .Where(member => stayIds.Contains(member.StayId))
            .Join(db.Guests, member => member.GuestId, guest => guest.Id,
                (member, guest) => new
                {
                    member.StayId,
                    Primary = member.IsPrimary == true,
                    guest.NameAsGiven,
                })
            .ToListAsync(cancellationToken);

        // The primary where one is marked, otherwise the first the source sent.
        // A party with nobody marked primary is ordinary — `IsPrimary` is
        // nullable precisely because a source may never have said.
        return party
            .GroupBy(member => member.StayId)
            .ToDictionary(
                group => group.Key,
                group => (group.FirstOrDefault(member => member.Primary) ?? group.First())
                    .NameAsGiven);
    }

    /// <summary>What each stay's booking is called, where anything calls it that.</summary>
    /// <remarks>
    /// <para>
    /// <b>A booking has no reference of its own.</b> It carries a UUID and
    /// whatever a source sent — so <c>BK-4471</c> exists for a booking Opera
    /// created and does not exist for one this desk made. The design already
    /// says so: its Bookings frame draws <i>created here</i> in that column for
    /// a walk-in.
    /// </para>
    /// <para>
    /// This projection said <i>created here</i> for every row until it was
    /// looked at, which would have printed it over a booking Opera named — a
    /// value standing in for one nobody read. Now the reference is read, and
    /// the phrase is what remains when there genuinely is none.
    /// </para>
    /// </remarks>
    private async Task<IReadOnlyDictionary<Guid, string>> ReferencesAsync(
        IReadOnlyList<RoomStay> page, CancellationToken cancellationToken)
    {
        var bookingIds = page.Select(s => s.BookingId).Distinct().ToArray();

        var refs = await db.BookingExternalRefs
            .Where(reference => bookingIds.Contains(reference.BookingId))
            .Select(reference => new { reference.BookingId, reference.ExternalId })
            .ToListAsync(cancellationToken);

        return refs
            .Where(reference => !string.IsNullOrWhiteSpace(reference.ExternalId))
            .GroupBy(reference => reference.BookingId)
            .ToDictionary(group => group.Key, group => group.First().ExternalId);
    }

    /// <summary>One stay, as the design draws it.</summary>
    private static object Row(
        RoomStay stay,
        IReadOnlyDictionary<Guid, string> types,
        IReadOnlyDictionary<Guid, string> rooms,
        IReadOnlyDictionary<Guid, string> names,
        IReadOnlyDictionary<Guid, string> refs)
    {
        var named = names.TryGetValue(stay.Id, out var name) && !string.IsNullOrWhiteSpace(name);

        return new
        {
            id = stay.Id.ToString(),

            // "Not yet named" is the design's own words for a real state, not a
            // placeholder standing in for a name we failed to load (R25).
            guest = named ? name : "Not yet named",
            unnamed = !named,

            // **Absent, and this is a reported gap rather than an oversight.**
            // The design draws `+91 98470 •••• 12`; contacts are stored
            // encrypted and `IContactProtector` has only a write direction, so
            // there is no way to mask a value this process cannot read. An
            // approximation here would be a phone number nobody has.
            contact = (string?)null,

            // The source's reference where one exists, and the design's own
            // words where none does — never one printed over the other.
            booking = refs.TryGetValue(stay.BookingId, out var reference)
                ? reference
                : "created here",

            roomType = types.TryGetValue(stay.RoomTypeId, out var type) ? type : null,

            room = stay.CurrentRoomId is { } id && rooms.TryGetValue(id, out var number)
                ? number
                : null,

            nights = Nights(stay),
            chips = Chips(stay),
        };
    }

    /// <summary>`31 Aug → 2 Sep`, or the day-use form.</summary>
    /// <remarks>
    /// An unknown instant renders as nothing rather than as a date: a stay whose
    /// arrival nobody has recorded is a real state, and
    /// <see cref="StayTime.None"/> exists to keep it distinct from midnight.
    /// </remarks>
    private static string? Nights(RoomStay stay)
    {
        var from = stay.ArrivalAt.Date;
        var to = stay.DepartureAt.Date;

        if (from is null)
        {
            return null;
        }

        var arrival = from.Value.ToString("d MMM");

        if (to is null)
        {
            return arrival;
        }

        return to.Value == from.Value ? $"{arrival} · day use" : $"{arrival} → {to.Value:d MMM}";
    }

    /// <summary>What the row is missing, in the design's vocabulary.</summary>
    /// <remarks>
    /// Only what this projection can state from the row in front of it. The
    /// design's other chips — a disagreement, a held fact — belong to the
    /// Attention list and are that view's to draw.
    /// </remarks>
    private static IReadOnlyList<object> Chips(RoomStay stay)
    {
        var chips = new List<object>();

        if (stay.CurrentRoomId is null)
        {
            chips.Add(new { text = "no room", mark = "missing" });
        }

        if (stay.PmsUnknown)
        {
            chips.Add(new { text = "the PMS has not sent this", mark = "unknown" });
        }

        return chips;
    }
}
