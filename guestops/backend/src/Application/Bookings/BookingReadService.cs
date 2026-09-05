using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Bookings;

/// <summary>What the desk is looking for.</summary>
/// <param name="Search">
/// What a guest at the counter can actually say — a name, or the number they
/// were told to quote. Empty matches everything, which is the ordinary state of
/// the screen.
/// </param>
/// <param name="Arriving">
/// Bookings with a stay arriving in this window. Null is every booking the
/// property has ever taken.
/// </param>
/// <param name="Status">One lifecycle, or null for any.</param>
/// <param name="Page">Already clamped by <see cref="Paging.Of"/>.</param>
public sealed record BookingQuery(
    string? Search,
    DateRange? Arriving,
    StayLifecycle? Status,
    Paging.Window Page);

/// <summary>An inclusive span of days.</summary>
public sealed record DateRange(DateOnly From, DateOnly To);

/// <summary>One booking, as the list draws it.</summary>
/// <remarks>
/// The two counts are what make this a booking row rather than a stay row:
/// <see cref="StayCount"/> is how many stays exist and
/// <see cref="ExpectedStayCount"/> is how many the source claimed. When they
/// differ the list says <i>1 of 3 known</i> — GUEST-Q2's incomplete group,
/// stated rather than papered over with rows nobody booked.
/// </remarks>
public sealed record BookingSummary(
    Guid Id,
    string? Guest,
    bool Unnamed,
    string? Reference,
    string? Confirmation,
    int StayCount,
    int? ExpectedStayCount,
    DateOnly? Arrival,
    DateOnly? Departure,
    StayLifecycle Status,
    bool WalkIn,
    bool PmsUnknown,
    bool Disagrees,
    bool Overridden,
    bool AnyRoomAssigned);

/// <summary>One stay inside a booking, as frames 8 and 9 draw it.</summary>
public sealed record BookingStayRow(
    Guid Id,
    string? Guest,
    bool Unnamed,
    string? RoomTypeId,
    DateOnly? Arrival,
    DateOnly? Departure,
    StayLifecycle Status,
    bool Assigned,
    bool PmsUnknown);

/// <summary>One booking and the stays it holds.</summary>
/// <remarks>
/// <c>Expected</c> is what the <i>source</i> claimed. Null when nobody claimed
/// anything, which is every booking this desk created — and which is a
/// different state from claiming one. Collapsing the two would lose the
/// incomplete group frame 9 exists to draw.
/// </remarks>
public sealed record BookingRecord(
    Guid Id,
    string? Guest,
    string? Reference,
    int? Expected,
    DateOnly? Arrival,
    DateOnly? Departure,
    IReadOnlyList<BookingStayRow> Stays);

/// <summary>
/// Reading bookings — the list, and one of them. Frames 2, 8 and 9.
/// </summary>
/// <remarks>
/// <para>
/// <b>Its own file because a booking read is not a stay read.</b>
/// <c>StayListService</c> answers <i>what is happening today</i> and pages over
/// stays; this answers <i>what has this property ever sold</i> and pages over
/// <b>bookings</b>. Sharing a service would mean one query that sometimes
/// counts stays and sometimes counts groups, which is the sort of parameter
/// that ends up wrong in one of its two modes (ADR 0038).
/// </para>
/// <para>
/// <b>Cancelled and no-show bookings stay in the list.</b> They are excluded
/// only when the desk asks for a different status — never removed, because a
/// cancelled reservation exists, its penalty may be chargeable, and a no-show
/// is reportable (S25, S27, ADR 0062).
/// </para>
/// <para>
/// <b>Contacts are neither searched nor returned.</b> They are stored encrypted
/// and <c>IContactProtector</c> has only a write direction, so a search over
/// them would have to decrypt every row in the property to answer one query.
/// GUEST-Q12 records that as a known gap rather than as a search that quietly
/// does less than the screen's own placeholder promises.
/// </para>
/// </remarks>
public sealed class BookingReadService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer)
{
    /// <summary>One page of the list, and how many bookings match.</summary>
    public async Task<PagedResult<BookingSummary>> ListAsync(
        RequestScope scope, BookingQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ReservationRead, ResourceTypes.Property, scope.PropertyId,
            cancellationToken);

        var bookings = Filter(
            db.Bookings.Where(booking => booking.PropertyId == scope.PropertyId),
            query);

        // The count comes from the same query the page is taken from. Building
        // the predicate twice is how a pager offers pages the list cannot
        // produce — the count and the rows must not be able to disagree.
        var total = await bookings.CountAsync(cancellationToken);

        var page = await bookings
            .OrderByDescending(booking => booking.CreatedAt)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .Select(booking => new Loaded(
                booking.Id,
                booking.ExpectedStayCount,
                booking.Stays.Select(stay => new LoadedStay(
                    stay.Id,
                    stay.Lifecycle,
                    stay.WalkIn,
                    stay.PmsUnknown,
                    stay.ArrivalAt.At,
                    stay.DepartureAt.At,
                    stay.CurrentRoomId != null)).ToList(),
                booking.ExternalRefs.Select(reference => new LoadedRef(
                    reference.IdentifierKind, reference.ExternalId)).ToList()))
            .ToListAsync(cancellationToken);

        var stayIds = page.SelectMany(booking => booking.Stays.Select(stay => stay.Id)).ToArray();

        var names = await NamesAsync(stayIds, cancellationToken);
        var flags = await FlagsAsync(stayIds, cancellationToken);

        return new PagedResult<BookingSummary>(
            [.. page.Select(booking => Summarise(booking, names, flags))],
            total);
    }

    /// <summary>One booking and its stays — frames 8 and 9.</summary>
    /// <remarks>
    /// <b>A booking nobody in this property owns is not found</b>, rather than
    /// forbidden: the property filter is in the predicate, so a caller asking
    /// about another property's booking gets the same answer as one asking
    /// about a booking that does not exist. Distinguishing them would confirm
    /// the booking exists to somebody who may not see it (ADR 0041).
    /// </remarks>
    public async Task<BookingRecord> GetAsync(
        RequestScope scope, Guid bookingId, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ReservationRead, ResourceTypes.Property, scope.PropertyId,
            cancellationToken);

        var booking = await db.Bookings
            .Where(one => one.Id == bookingId && one.PropertyId == scope.PropertyId)
            .Select(one => new Loaded(
                one.Id,
                one.ExpectedStayCount,
                one.Stays.Select(stay => new LoadedStay(
                    stay.Id,
                    stay.Lifecycle,
                    stay.WalkIn,
                    stay.PmsUnknown,
                    stay.ArrivalAt.At,
                    stay.DepartureAt.At,
                    stay.CurrentRoomId != null)).ToList(),
                one.ExternalRefs.Select(reference => new LoadedRef(
                    reference.IdentifierKind, reference.ExternalId)).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("booking", bookingId);

        var stayIds = booking.Stays.Select(stay => stay.Id).ToArray();
        var names = await NamesAsync(stayIds, cancellationToken);
        var types = await TypesAsync(stayIds, cancellationToken);

        var stays = booking.Stays.Select(stay =>
        {
            var named = names.TryGetValue(stay.Id, out var name) && !string.IsNullOrWhiteSpace(name);

            return new BookingStayRow(
                stay.Id,
                named ? names[stay.Id] : null,
                Unnamed: !named,
                types.TryGetValue(stay.Id, out var type) ? type : null,
                DateOf(stay.Arrival),
                DateOf(stay.Departure),
                stay.Lifecycle,
                stay.Assigned,
                stay.PmsUnknown);
        }).ToList();

        return new BookingRecord(
            booking.Id,
            stays.FirstOrDefault(stay => !stay.Unnamed)?.Guest,
            Of(booking.Refs, "booking"),
            booking.ExpectedStayCount,
            DateOf(booking.Stays.Min(stay => stay.Arrival)),
            DateOf(booking.Stays.Max(stay => stay.Departure)),
            stays);
    }

    /// <summary>
    /// One stay's current version, for a caller about to write it.
    /// </summary>
    /// <remarks>
    /// <b>A read, and deliberately not part of the write.</b> A dialog is
    /// confirmed a minute after it is drawn, and carrying the version from the
    /// drawing would make the optimistic check fire on the operator's reading
    /// time rather than on concurrent change. Reading it here narrows the window
    /// to the write itself, which is what the check exists to guard.
    /// </remarks>
    public async Task<long> StayVersionAsync(
        RequestScope scope, Guid stayId, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ReservationRead, ResourceTypes.Property, scope.PropertyId,
            cancellationToken);

        var found = await db.Stays
            .Where(stay => stay.Id == stayId && stay.PropertyId == scope.PropertyId)
            .Select(stay => (long?)stay.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return found ?? throw new NotFoundException("stay", stayId);
    }

    /// <summary>Which room type each stay is anchored to.</summary>
    /// <remarks>
    /// The <b>type</b> is the anchor and the room is an assignment (GUEST-Q2
    /// addendum, S8), so this is what a stay always has and the room number is
    /// what it may not have yet.
    /// </remarks>
    private async Task<IReadOnlyDictionary<Guid, string>> TypesAsync(
        IReadOnlyCollection<Guid> stayIds, CancellationToken cancellationToken)
    {
        if (stayIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await db.Stays
            .Where(stay => stayIds.Contains(stay.Id))
            .ToDictionaryAsync(
                stay => stay.Id, stay => stay.RoomTypeId.ToString(), cancellationToken);
    }

    /// <summary>One booking, from what was loaded for the whole page.</summary>
    private static BookingSummary Summarise(
        Loaded booking,
        IReadOnlyDictionary<Guid, string> names,
        Flags flags)
    {
        var named = booking.Stays
            .Select(stay => names.TryGetValue(stay.Id, out var name) ? name : null)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        return new BookingSummary(
            booking.Id,
            named,
            Unnamed: named is null,
            Reference: Of(booking.Refs, "booking"),
            Confirmation: Of(booking.Refs, "confirmation"),
            StayCount: booking.Stays.Count,
            booking.ExpectedStayCount,
            Arrival: DateOf(booking.Stays.Min(stay => stay.Arrival)),
            Departure: DateOf(booking.Stays.Max(stay => stay.Departure)),
            Status: Dominant(booking.Stays.Select(stay => stay.Lifecycle)),
            WalkIn: booking.Stays.Any(stay => stay.WalkIn),
            PmsUnknown: booking.Stays.Any(stay => stay.PmsUnknown),
            Disagrees: booking.Stays.Any(stay => flags.Disagreeing.Contains(stay.Id)),
            Overridden: booking.Stays.Any(stay => flags.Overridden.Contains(stay.Id)),
            AnyRoomAssigned: booking.Stays.Any(stay => stay.Assigned));
    }

    /// <summary>The window, the status and the search, applied in the database.</summary>
    private static IQueryable<Booking> Filter(IQueryable<Booking> bookings, BookingQuery query)
    {
        if (query.Arriving is { } window)
        {
            var from = new DateTimeOffset(
                window.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var to = new DateTimeOffset(
                window.To.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

            bookings = bookings.Where(booking => booking.Stays.Any(stay =>
                stay.ArrivalAt.At != null
                && stay.ArrivalAt.At.Value >= from
                && stay.ArrivalAt.At.Value <= to));
        }

        if (query.Status is { } status)
        {
            bookings = bookings.Where(
                booking => booking.Stays.Any(stay => stay.Lifecycle == status));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();

            bookings = bookings.Where(booking =>
                booking.ExternalRefs.Any(reference => reference.ExternalId.Contains(term))
                || booking.Stays.Any(stay => stay.Party.Any(member =>
                    member.Guest != null && member.Guest.NameAsGiven.Contains(term))));
        }

        return bookings;
    }

    /// <summary>The named guest on each stay, where the party has one.</summary>
    /// <remarks>
    /// One query for the whole page rather than one per row: a page of
    /// twenty-five bookings would otherwise be twenty-five round trips for a
    /// name, which is the N+1 a list screen notices first.
    /// </remarks>
    private async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        IReadOnlyCollection<Guid> stayIds, CancellationToken cancellationToken)
    {
        if (stayIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

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
        // A party with nobody marked primary is ordinary.
        return party
            .GroupBy(member => member.StayId)
            .ToDictionary(
                group => group.Key,
                group => (group.FirstOrDefault(member => member.Primary) ?? group.First())
                    .NameAsGiven);
    }

    /// <summary>Which stays disagree with their source, and which carry an override.</summary>
    /// <remarks>
    /// <para>
    /// <b>Both come from the same row</b>, in one query. A disagreement records
    /// what we say, what the PMS says, and — when a person has written over the
    /// PMS's value — who did it and when. So <i>disagrees</i> is an unresolved
    /// row and <i>override</i> is a row with an actor on it, and frame 2 draws
    /// them as two chips because they are two facts about one condition.
    /// </para>
    /// <para>
    /// <b>Not <c>HeldFacts</c>.</b> That store holds inbound facts the matcher
    /// could not attach to any stay — it is keyed by property and has no stay
    /// at all, so reading an override out of it would have been a flag derived
    /// from an unrelated table.
    /// </para>
    /// </remarks>
    private async Task<Flags> FlagsAsync(
        IReadOnlyCollection<Guid> stayIds, CancellationToken cancellationToken)
    {
        if (stayIds.Count == 0)
        {
            return new Flags([], []);
        }

        var rows = await db.Disagreements
            .Where(one => stayIds.Contains(one.StayId) && one.ClearedAt == null)
            .Select(one => new { one.StayId, Overridden = one.OverrideActor != null })
            .ToListAsync(cancellationToken);

        return new Flags(
            [.. rows.Select(row => row.StayId).Distinct()],
            [.. rows.Where(row => row.Overridden).Select(row => row.StayId).Distinct()]);
    }

    /// <summary>An identifier of one kind, where the source sent one.</summary>
    /// <remarks>
    /// The booking reference and the confirmation number are different
    /// identifiers from different systems — what the property calls the booking
    /// and what the guest was told to quote. Printing one when asked for the
    /// other would send a receptionist looking for a booking nobody can name.
    /// </remarks>
    private static string? Of(IReadOnlyList<LoadedRef> refs, string kind)
        => refs.FirstOrDefault(reference =>
            reference.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase))?.Id;

    /// <summary>The date part of an instant nobody may have recorded.</summary>
    /// <remarks>
    /// The offset is the property's, baked in when the instant was stored, so
    /// taking the date off it gives the day the property was on rather than the
    /// day the server was on — which for a 04:00 roll are different for four
    /// hours every night.
    /// </remarks>
    private static DateOnly? DateOf(DateTimeOffset? at)
        => at is { } value ? DateOnly.FromDateTime(value.Date) : null;

    /// <summary>
    /// The one status that describes the group.
    /// </summary>
    /// <remarks>
    /// In the design's own order of interest: what is happening now beats what
    /// is arranged, which beats what is over. A booking with one cancelled stay
    /// and one in house is <i>In house</i>, because the cancelled stay is no
    /// longer something the desk acts on and the other one is.
    /// </remarks>
    private static StayLifecycle Dominant(IEnumerable<StayLifecycle> lifecycles)
    {
        var all = lifecycles.ToList();

        foreach (var candidate in Interest)
        {
            if (all.Contains(candidate))
            {
                return candidate;
            }
        }

        // A booking with no stays at all. It is not drawn by any frame and it
        // is reachable — a source may announce a group before sending any of
        // it — so it gets the state such a booking is in rather than a throw.
        return StayLifecycle.Booked;
    }

    /// <summary>Most interesting first.</summary>
    private static readonly StayLifecycle[] Interest =
    [
        StayLifecycle.InHouse,
        StayLifecycle.Booked,
        StayLifecycle.Waitlisted,
        StayLifecycle.Departed,
        StayLifecycle.Cancelled,
        StayLifecycle.NoShow,
    ];

    /// <summary>What one page's query brings back, before it is summarised.</summary>
    private sealed record Loaded(
        Guid Id,
        int? ExpectedStayCount,
        List<LoadedStay> Stays,
        List<LoadedRef> Refs);

    private sealed record LoadedStay(
        Guid Id,
        StayLifecycle Lifecycle,
        bool WalkIn,
        bool PmsUnknown,
        DateTimeOffset? Arrival,
        DateTimeOffset? Departure,
        bool Assigned);

    private sealed record LoadedRef(string Kind, string Id);

    private sealed record Flags(HashSet<Guid> Disagreeing, HashSet<Guid> Overridden);
}
