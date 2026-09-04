using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Application.Abstractions;

/// <summary>
/// The property's operating day, and the clock its dates become instants
/// against.
/// </summary>
/// <remarks>
/// <para>
/// <b>This application computes none of it.</b> ADR 0128 §6: the boundary is
/// Property Registration's stable configuration, and the <i>current business
/// date</i> is the Context Service's derivation —
/// <c>operating_day(timestamp, boundary)</c>, derived and stored by nobody. The
/// Hub attaches it to a normalised fact; a staff-created stay asks for it here.
/// </para>
/// <para>
/// <b>And the zone is the property's, always.</b> Turning a date into an
/// instant needs the property's IANA zone: built in UTC or from an offset, a
/// derived timestamp carries the wrong date near midnight and R12's whole
/// distinction is lost silently — which is R16's failure, the one with the
/// widest blast radius because it is plausible and looks like correct data.
/// </para>
/// <para>
/// An interface because the answer is a <b>platform call</b>, and a service
/// under test must be able to stand still at 03:59 without a Context Service
/// running.
/// </para>
/// </remarks>
public interface IBusinessDay
{
    /// <summary>Which business day it is now, for this property.</summary>
    Task<DateOnly?> CurrentAsync(RequestScope scope, CancellationToken cancellationToken);

    /// <summary>A date, at the property's check-in hour, in its zone.</summary>
    Task<StayTime> AtCheckInAsync(
        RequestScope scope, DateOnly date, CancellationToken cancellationToken);

    /// <summary>A date, at the property's check-out hour, in its zone.</summary>
    Task<StayTime> AtCheckOutAsync(
        RequestScope scope, DateOnly date, CancellationToken cancellationToken);

    /// <summary>The instants a business day begins and ends at.</summary>
    /// <remarks>
    /// <para>
    /// <b>Implementation choice, not a ruling</b> — Stream FF, 2026-09-04, and
    /// it may be reversed. The departures list selects stays whose departure
    /// falls inside a business day, and a departure is stored <i>only</i> as a
    /// timestamp: there is no departure-date column, and
    /// <see cref="StayTime.Date"/> is computed in C# so it cannot be translated
    /// to SQL. A half-open instant range is the only shape that query can take.
    /// </para>
    /// <para>
    /// It belongs here rather than in a service for the reason the rest of this
    /// port exists: the roll time and the zone are the property's
    /// configuration, and turning them into instants is the <b>adapter's</b>
    /// conversion — the same one <see cref="AtCheckInAsync"/> already makes. A
    /// service deriving the boundary itself would be computing the operating
    /// day, which the paragraph opening this interface forbids in as many
    /// words.
    /// </para>
    /// <para>
    /// <c>null</c> when the property has no usable boundary or zone. A caller
    /// returns nothing rather than guessing a whole day — a guessed window is
    /// wrong by a day near midnight and looks like correct data.
    /// </para>
    /// </remarks>
    Task<DayBounds?> BoundsAsync(
        RequestScope scope, DateOnly date, CancellationToken cancellationToken);
}

/// <summary>A business day, as a half-open instant range.</summary>
/// <param name="Start">The roll time on the day itself, inclusive.</param>
/// <param name="End">The roll time on the next day, exclusive.</param>
/// <remarks>
/// Half-open so consecutive days neither overlap nor leave a gap: a departure
/// recorded exactly at the roll time belongs to the day starting then, and to
/// exactly one day.
/// </remarks>
public sealed record DayBounds(DateTimeOffset Start, DateTimeOffset End);

/// <summary>Turns a guest's contact details into what is stored.</summary>
/// <remarks>
/// <para>
/// Encryption and the blind index in one place, because they must agree: the
/// index is an HMAC of the <b>normalised</b> value, and a caller that
/// normalised differently would write a row nothing could ever find.
/// </para>
/// <para>
/// <b>The key material is the platform's</b>, versioned and never destroyed. An
/// interface here so this application holds no key handling of its own — and so
/// a test can protect a value without a vault.
/// </para>
/// </remarks>
public interface IContactProtector
{
    /// <summary>The contact rows a new guest's details become. May be empty.</summary>
    /// <remarks>
    /// Empty is a valid answer: a stay with no contact detail is a real stay,
    /// and refusing it would lose one (R25).
    /// </remarks>
    IReadOnlyList<ContactPoint> Protect(Bookings.NewGuest guest);
}
