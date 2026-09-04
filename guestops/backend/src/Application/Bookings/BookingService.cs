using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Bookings;

/// <summary>One stay to create, inside a booking.</summary>
/// <param name="RoomTypeId">The anchor. A room is assigned later.</param>
/// <param name="ArrivalDate">The night the guest arrives.</param>
/// <param name="DepartureDate">The morning they leave.</param>
/// <param name="Adults">R9's counts, carried separately.</param>
/// <param name="Children">R9's counts, carried separately.</param>
/// <param name="Guests">May be empty — <i>"not yet named"</i> is a valid party.</param>
/// <param name="WalkIn">How the guest arrived. Not the same fact as the channel.</param>
/// <param name="Terms">What it was sold on, where the desk knows.</param>
public sealed record NewStay(
    Guid RoomTypeId,
    DateOnly ArrivalDate,
    DateOnly DepartureDate,
    int Adults,
    int Children,
    IReadOnlyList<NewGuest> Guests,
    bool WalkIn,
    CommercialTerms? Terms);

/// <summary>A person on a stay, as the desk took them.</summary>
public sealed record NewGuest(
    string NameAsGiven,
    string? NameGiven,
    string? NameFamily,
    string? Phone,
    string? Email,
    bool? IsPrimary);

/// <summary>A booking to create.</summary>
public sealed record NewBooking(
    IReadOnlyList<NewStay> Stays,
    string? Channel,
    string? TravelAgent,
    string? MarketCode,
    string? MealPlan,
    int ExpectedStayCount);

/// <summary>
/// Taking a booking — the group, and the stays inside it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A group of one is not a special case.</b> GUEST-Q2: every operation
/// happens to a stay, so the one-room booking and the fifty-room booking take
/// the same path and no caller has to know which it is holding.
/// </para>
/// <para>
/// <b>What is absent is recorded, never invented</b> (R25). An unnamed party is
/// valid and gets an <see cref="StayAbsence"/> rather than a placeholder guest;
/// a stay with no contact detail is a real stay. The system this replaces met
/// both and did the two wrong things — dropped one silently, fabricated the
/// other.
/// </para>
/// </remarks>
public sealed class BookingService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    IEventAppender events,
    IBusinessDay businessDay,
    IContactProtector contacts,
    TimeProvider clock)
{
    /// <summary>Create the booking and its stays, in one transaction.</summary>
    public async Task<Booking> CreateAsync(
        RequestScope scope, NewBooking command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.StayCreate, ResourceTypes.Property, scope.PropertyId,
            cancellationToken);

        if (command.Stays.Count == 0)
        {
            throw new InvalidRequestException("a booking needs at least one stay");
        }

        var now = clock.GetUtcNow();

        // Asked, never computed. The boundary is Property Registration's
        // configuration and the date is the Context Service's derivation —
        // ADR 0128 §6, and this application computes neither.
        var businessDate = await businessDay.CurrentAsync(scope, cancellationToken);

        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,

            // Zero means the caller did not say, which is a different state
            // from "one" — and collapsing them loses the incomplete group.
            ExpectedStayCount = command.ExpectedStayCount > 0
                ? command.ExpectedStayCount
                : null,

            // Nothing is asserted about completeness here: it is the *source's*
            // claim, and a desk creating a booking is not making one.
            IsComplete = null,

            Origin = RecordOrigin.Staff,
            CreatedAt = now,
            CreatedBy = scope.UserId,
            Version = 1,
        };

        db.Bookings.Add(booking);

        foreach (var request in command.Stays)
        {
            db.Stays.Add(await BuildStayAsync(scope, booking, request, command, businessDate, now, cancellationToken));
        }

        events.Append(scope, "reservation.created", "reservation", booking.Id, booking.Version, new
        {
            booking_id = booking.Id,
            property_id = booking.PropertyId,
            expected_stay_count = booking.ExpectedStayCount,
            origin = booking.Origin.ToString(),
        });

        await db.SaveChangesAsync(cancellationToken);
        return booking;
    }

    private async Task<RoomStay> BuildStayAsync(
        RequestScope scope,
        Booking booking,
        NewStay request,
        NewBooking command,
        DateOnly? businessDate,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (request.DepartureDate < request.ArrivalDate)
        {
            throw new InvalidRequestException("the departure is before the arrival");
        }

        var stay = new RoomStay
        {
            Id = Guid.CreateVersion7(),
            BookingId = booking.Id,
            PropertyId = scope.PropertyId,
            RoomTypeId = request.RoomTypeId,

            // No room. The anchor is the type, and the assignment comes later —
            // the ordinary case rather than the exception (S8).
            CurrentRoomId = null,

            Lifecycle = StayLifecycle.Booked,

            // Dates the desk gave, turned into instants against the property's
            // clock. Marked derived, because that is what they are: nobody has
            // observed an arrival yet, and a report that could not tell would
            // measure the reservation rather than the guest (R13).
            ArrivalAt = await businessDay.AtCheckInAsync(scope, request.ArrivalDate, cancellationToken),
            DepartureAt = await businessDay.AtCheckOutAsync(scope, request.DepartureDate, cancellationToken),

            BusinessDate = businessDate,
            WalkIn = request.WalkIn,

            // Staff-created, and this property has no PMS view of it. Whether
            // that matters is the connector's presence, not this call's — the
            // flag says who knows, and a candidate link joins them later if the
            // PMS ever sends its own version (GUEST-Q5).
            PmsUnknown = true,

            Origin = RecordOrigin.Staff,
            CreatedAt = now,
            CreatedBy = scope.UserId,
            Version = 1,

            Source = new StaySource
            {
                Channel = command.Channel,
                TravelAgent = command.TravelAgent,
                MarketCode = command.MarketCode,
                MealPlan = command.MealPlan,
                Adults = request.Adults,
                Children = request.Children,
            },

            Terms = request.Terms,
        };

        stay.Absences.Add(Absent(stay.Id, AbsentFields.Assignment, now));

        if (request.Guests.Count == 0)
        {
            stay.Absences.Add(Absent(stay.Id, AbsentFields.Party, now));
        }

        foreach (var guest in request.Guests)
        {
            stay.Party.Add(new StayGuest
            {
                StayId = stay.Id,
                GuestId = await EnsureGuestAsync(scope, guest, now, cancellationToken),
                IsPrimary = guest.IsPrimary,
                AddedAt = now,
                Origin = RecordOrigin.Staff,
            });
        }

        if (request.Guests.Count > 0
            && request.Guests.All(g => string.IsNullOrWhiteSpace(g.Phone)
                                       && string.IsNullOrWhiteSpace(g.Email)))
        {
            stay.Absences.Add(Absent(stay.Id, AbsentFields.Contact, now));
        }

        events.Append(scope, "stay.created", "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            booking_id = booking.Id,
            property_id = stay.PropertyId,
            room_type_id = stay.RoomTypeId,
            lifecycle = stay.Lifecycle.ToString(),
            business_date = businessDate?.ToString("yyyy-MM-dd"),
            walk_in = stay.WalkIn,
            pms_unknown = stay.PmsUnknown,
        });

        return stay;
    }

    private async Task<Guid> EnsureGuestAsync(
        RequestScope scope, NewGuest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Matched on an exact contact only, and never on a name. The system
        // this replaces resolved guests by surname and arrival date against its
        // own copy, and a wrong match silently merges two people's histories —
        // worse than a duplicate, which is G360-Q1's reasoning. Where nothing
        // matches, a new record is created and Guest360 may link them later.
        var existing = await FindByContactAsync(scope.PropertyId, request, cancellationToken);
        if (existing is not null)
        {
            return existing.Value;
        }

        var guest = new GuestIdentity
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            NameAsGiven = request.NameAsGiven,
            NameGiven = request.NameGiven,
            NameFamily = request.NameFamily,
            Origin = RecordOrigin.Staff,
            CreatedAt = now,
            Version = 1,
        };

        foreach (var contact in contacts.Protect(request))
        {
            contact.Id = Guid.CreateVersion7();
            contact.GuestId = guest.Id;
            guest.Contacts.Add(contact);
        }

        db.Guests.Add(guest);

        events.Append(scope, "guest.created", "guest", guest.Id, guest.Version, new
        {
            guest_id = guest.Id,
            property_id = guest.PropertyId,
            name = guest.NameAsGiven,
        });

        return guest.Id;
    }

    private async Task<Guid?> FindByContactAsync(
        Guid propertyId, NewGuest request, CancellationToken cancellationToken)
    {
        foreach (var probe in contacts.Protect(request))
        {
            var match = await db.Contacts
                .Where(c => c.Kind == probe.Kind && c.ValueIndex == probe.ValueIndex)
                .Join(db.Guests.Where(g => g.PropertyId == propertyId),
                    c => c.GuestId, g => g.Id, (_, g) => g.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (match != Guid.Empty)
            {
                return match;
            }
        }

        return null;
    }

    private static StayAbsence Absent(Guid stayId, string field, DateTimeOffset now)
        => new()
        {
            Id = Guid.CreateVersion7(),
            StayId = stayId,
            Field = field,
            Reason = AbsenceReason.NotSupplied,
            RecordedAt = now,
        };
}
