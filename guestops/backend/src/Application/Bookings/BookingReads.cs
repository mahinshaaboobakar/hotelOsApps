using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Application.Bookings;

/*
 * What a booking read asks for, and what it gives back.
 *
 * Separated from `BookingReadService` when that file reached 297 of ADR 0027's
 * 300 — along the boundary that was already there rather than one invented at
 * the moment of splitting (ADR 0036). These five are the read's **contract**:
 * three module projections consume them and never touch the service's queries,
 * so a reader asking *what does a booking read return* has one file to open and
 * a reader asking *how does it answer* has the other.
 */

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
    Guid? RoomId,
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
