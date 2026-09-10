using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// One stay — the anchor screen's own read.
/// </summary>
/// <remarks>
/// <para>
/// <b>Named <c>StayDetailView</c> because <c>StayView</c> was taken</b>, and by
/// something load-bearing: <see cref="Application.Stays.StayView"/> is the enum
/// naming the day's four lists — <i>the wire's StayView in the domain's own
/// vocabulary</i>. Two meanings of one word in one assembly is ADR 0037's
/// <c>replay.rs</c> collision, and the compiler found it here rather than a
/// reader finding it later. <c>Detail</c> is the booking's own distinction:
/// <c>BookingView</c> answers one booking, and its model is <c>BookingDetail</c>.
/// </para>
/// </remarks>
/// <remarks>
/// <para>
/// <b>It did not exist, and its absence was invisible.</b> Every TAB of this
/// screen had a view — Activity, Requests, Servicing, Payment — and the stay
/// itself had none. The module asked for <c>reservation.read/stay</c>, the
/// envelope answered <c>"'stay' is not a method this application serves"</c>,
/// and the module fell back to a recorded stay and drew it. So the screen showed
/// one stay, always, and no path existed by which it could show another.
/// </para>
/// <para>
/// <b>Queried here rather than through a read service</b>, which is
/// <see cref="ActivityView"/>'s shape and the right one for a view answering
/// about a single aggregate: a service would add a layer holding one method with
/// one caller. The booking has one because a booking is listed, filtered and
/// paged from three places; a stay is opened.
/// </para>
/// <para>
/// <b>Scoped by property before id</b>, so a stay belonging to another property
/// is <i>not found</i> rather than forbidden — the boundary rule ADR 0054's E2E
/// suite checks, and the reason the predicate names both.
/// </para>
/// </remarks>
public sealed class StayDetailView(GuestOpsDbContext db)
{
    /// <summary>The stay the bundle asked for.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, Guid stayId, CancellationToken cancellationToken)
    {
        var stay = await db.Stays
            .Where(s => s.PropertyId == scope.PropertyId && s.Id == stayId)
            .FirstOrDefaultAsync(cancellationToken)
            // **Scoped by property first, so another property's stay is NOT
            // FOUND rather than forbidden** — a caller must not learn that an id
            // exists somewhere else, which is the boundary ADR 0054's E2E suite
            // checks per application.
            ?? throw new NotFoundException("stay", stayId);

        // **The reference is an external ref, not a column.** A booking created
        // here has none, and that is the ordinary standalone case rather than a
        // missing value — `BookingReadService` reads it the same way, by kind.
        var reference = await db.BookingExternalRefs
            .Where(r => r.BookingId == stay.BookingId
                && r.IdentifierKind.ToLower() == "booking")
            .Select(r => r.ExternalId)
            .FirstOrDefaultAsync(cancellationToken);

        var guest = await db.Party
            .Where(p => p.StayId == stay.Id)
            .OrderByDescending(p => p.IsPrimary == true)
            .Select(p => p.Guest!.NameAsGiven)
            .FirstOrDefaultAsync(cancellationToken);

        var room = stay.CurrentRoomId is not { } assigned
            ? null
            : await db.Set<MasterDataRoom>()
                .Where(r => r.Id == assigned)
                .Select(r => r.RoomNumber)
                .FirstOrDefaultAsync(cancellationToken);

        var type = await db.Set<MasterDataRoomTypeName>()
            .Where(t => t.Id == stay.RoomTypeId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var disagreement = await db.Disagreements
            .Where(d => d.StayId == stay.Id && d.ClearedAt == null)
            .OrderByDescending(d => d.RaisedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new
        {
            id = stay.Id.ToString(),
            stayId = Elide(stay.Id),

            // "Not yet named" is a state, not a placeholder. A stay a feed sent
            // before the guest was known has no name, and inventing one would
            // attribute a room to a person who does not exist.
            guest = string.IsNullOrWhiteSpace(guest) ? "Not yet named" : guest,

            room,

            bookingRef = reference ?? "created here",

            // Only where a source manages it — the rule the booking uses. A
            // standalone property has no PMS to name.
            managedBy = reference is null ? null : "Opera manages this stay",

            actions = Actions(stay),
            tabs = Tabs(),

            banner = disagreement is null ? null : Banner(disagreement),
            standing = disagreement is null ? null : "override standing",

            rows = Rows(stay, room, type),

            // Absent, not empty. The timeline is the Activity tab's and is read
            // by `activity` with its own method; answering it here would put the
            // same facts behind two operations free to disagree.
            timeline = Array.Empty<object>(),

            consequence = Consequence(stay),
        };
    }

    /// <summary>Both ends kept, which is what makes an id recognisable.</summary>
    private static string Elide(Guid id)
    {
        var text = id.ToString("N");
        return $"{text[..4]}…{text[^4..]}";
    }

    /// <summary>What can be done to this stay, from where it is.</summary>
    /// <remarks>
    /// Derived from the lifecycle rather than listed: a screen offering check-in
    /// on a departed stay is a screen that has to be corrected by whoever
    /// presses it.
    /// </remarks>
    private static object[] Actions(RoomStay stay)
        => stay.Lifecycle switch
        {
            StayLifecycle.Booked =>
            [
                new { label = "Check in", danger = false },
                new { label = "Cancel", danger = true },
            ],

            StayLifecycle.InHouse =>
            [
                new { label = "Check out", danger = false },
                new { label = "Move room", danger = false },
            ],

            _ => [],
        };

    /// <summary>The tabs this screen carries, without their counts.</summary>
    /// <remarks>
    /// <b>No counts here, and that is deliberate.</b> A count belongs to the read
    /// that owns the rows — Requests counts requests — and a second count
    /// computed beside it is a number free to disagree with the list it labels.
    /// The module labels the tabs from the reads it already makes.
    /// </remarks>
    private static object[] Tabs()
        =>
        [
            new { label = "Overview" },
            new { label = "Activity" },
            new { label = "Requests" },
            new { label = "Servicing" },
            new { label = "Payment" },
            new { label = "Documents" },
        ];

    /// <summary>A value the feed disagrees with, over a value a person set.</summary>
    private static object Banner(StayDisagreement row)
        => new
        {
            headline = "The PMS disagrees about this stay.",
            detail = $"You have {row.OurValue} — the PMS says {row.PmsValue}, not applied.",
            attribution = "Your entry stands everywhere until somebody decides.",
            actions = new[] { $"Keep {row.OurValue}", $"Take {row.PmsValue}" },
            time = row.RaisedAt.ToString("HH:mm"),
            tone = "warn",
        };

    /// <summary>The overview's rows.</summary>
    private static object[] Rows(RoomStay stay, string? room, string? type)
        =>
        [
            new
            {
                label = "Room",
                value = room is null ? "not assigned" : string.Empty,
                strong = room,
                tail = type is null ? null : $" · {type}",
            },
            new { label = "Arrives", value = When(stay.ArrivalAt), strong = (string?)null, tail = (string?)null },
            new { label = "Departs", value = When(stay.DepartureAt), strong = (string?)null, tail = (string?)null },
            new { label = "Status", value = stay.Lifecycle.ToString(), strong = (string?)null, tail = (string?)null },
        ];

    /// <summary>An instant the property has, or the fact that it has none.</summary>
    /// <remarks>
    /// <see cref="StayTime.None"/> is a stay whose time nobody has set — a booked
    /// arrival with no hour. It says so rather than showing midnight, which is
    /// what a zero renders as and what nobody meant.
    /// </remarks>
    private static string When(StayTime time)
        => time == StayTime.None ? "not set" : time.ToString();

    /// <summary>What happens next, said once.</summary>
    private static string Consequence(RoomStay stay)
        => stay.Lifecycle switch
        {
            StayLifecycle.Booked => "Checking in will mark the room occupied.",
            StayLifecycle.InHouse => "Checking out will release the room for cleaning.",
            _ => "Nothing further is expected on this stay.",
        };
}
