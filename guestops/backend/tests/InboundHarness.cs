using HotelOS.GuestOps.Application.Inbound;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The inbound half, over a migrated scratch database.
/// </summary>
/// <remarks>
/// <para>
/// <b>The provider is not a detail here.</b> This harness was written over the
/// in-memory provider on the reasoning that what is under test is which branch
/// a fact takes, and no branch is a query the provider changes. That reasoning
/// never got the chance to be wrong: <c>AddPlatformEventStore()</c> maps
/// <c>StoredEvent.Payload</c> as a <c>JsonDocument</c>, which only Npgsql can
/// map, so the model threw before any rule ran.
/// </para>
/// <para>
/// The fix was not to exclude the event store from the model. A suite green
/// against a model the service does not ship is the divergence this repository
/// has ruled against repeatedly — so it takes a real schema, and the partial
/// indexes and check constraints are now under test rather than deferred.
/// </para>
/// </remarks>
public sealed class InboundHarness : IAsyncDisposable
{
    private readonly GuestOpsScratch _scratch;

    private InboundHarness(GuestOpsScratch scratch, GuestOpsDbContext db, ManualClock clock)
    {
        _scratch = scratch;
        Db = db;
        Clock = clock;
        Events = new RecordingAppender();
        Authorizer = new RecordingAuthorizer();

        var matcher = new StayMatcher(db);
        Inbound = new InboundFactService(db, matcher, Events, clock);
        Reconciliation = new Application.Reconciliation.ReconciliationService(
            db, Authorizer, Events, clock);
    }

    public GuestOpsDbContext Db { get; }

    public ManualClock Clock { get; }

    public RecordingAppender Events { get; }

    public RecordingAuthorizer Authorizer { get; }

    public InboundFactService Inbound { get; }

    public Application.Reconciliation.ReconciliationService Reconciliation { get; }

    public static readonly Guid Property = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid RoomType = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid Room = Guid.Parse("33333333-3333-3333-3333-333333333333");

    /// <summary>A database of this test's own, and the services over it.</summary>
    /// <returns>A prepared harness.</returns>
    /// <remarks>
    /// One database per test rather than one per class: every assertion below is
    /// a <c>SingleAsync</c> over a table, and rows surviving between tests would
    /// make each one depend on the order the runner chose.
    /// </remarks>
    public static async Task<InboundHarness> CreateAsync()
    {
        var scratch = await GuestOpsScratch.CreateAsync();

        return new InboundHarness(
            scratch,
            scratch.Context(),
            new ManualClock(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero)));
    }

    public RequestScope Scope() => new() { PropertyId = Property, UserId = Guid.NewGuid() };

    /// <summary>A fact, with the parts a test cares about.</summary>
    public static InboundStayFact Fact(
        StayLifecycle lifecycle,
        string externalId = "84119377",
        Guid? room = null,
        DateOnly? arrival = null,
        DateOnly? departure = null,
        string guest = "Rajesh Pillai")
    {
        var from = arrival ?? new DateOnly(2026, 8, 31);
        var to = departure ?? new DateOnly(2026, 9, 4);

        return new InboundStayFact(
            "oracle-cloud",
            Property,
            [new InboundRef("oracle-cloud", "reservation", externalId)],
            [new InboundRef("oracle-cloud", "booking", $"B-{externalId}")],
            null,
            null,
            lifecycle,
            RoomType,
            room,
            new StayTime(from.ToDateTime(new TimeOnly(14, 0), DateTimeKind.Utc), TimeBasis.Derived),
            new StayTime(to.ToDateTime(new TimeOnly(11, 0), DateTimeKind.Utc), TimeBasis.Derived),
            from,
            false,
            [new InboundGuest(guest, null, null, null, null, true)],
            null,

            // A fact that sent no commercial segment still counted a party.
            // The record is always present, so "the source said nothing" and
            // "the source said zero adults" stay different values.
            new InboundSegment(null, null, null, null, 1, 0),
            []);
    }

    /// <summary>A stay this property created, which the PMS does not know.</summary>
    public async Task<RoomStay> SeedLocalStayAsync(
        Guid room, DateOnly arrival, DateOnly departure, string guest = "Joseph Mathew")
    {
        var booking = new Booking
        {
            Id = Uuid7.NewUuid7(),
            PropertyId = Property,
            Origin = RecordOrigin.Staff,
            CreatedAt = Clock.GetUtcNow(),
            Version = 1,
        };

        var guestRow = new GuestIdentity
        {
            Id = Uuid7.NewUuid7(),
            PropertyId = Property,
            NameAsGiven = guest,
            Origin = RecordOrigin.Staff,
            CreatedAt = Clock.GetUtcNow(),
            Version = 1,
        };

        var stay = new RoomStay
        {
            Id = Uuid7.NewUuid7(),
            BookingId = booking.Id,
            PropertyId = Property,
            RoomTypeId = RoomType,
            CurrentRoomId = room,
            Lifecycle = StayLifecycle.InHouse,
            ArrivalAt = new StayTime(
                arrival.ToDateTime(new TimeOnly(11, 4), DateTimeKind.Utc), TimeBasis.Observed),
            DepartureAt = new StayTime(
                departure.ToDateTime(new TimeOnly(11, 0), DateTimeKind.Utc), TimeBasis.Derived),
            WalkIn = true,
            PmsUnknown = true,
            Origin = RecordOrigin.Staff,
            CreatedAt = Clock.GetUtcNow(),
            Version = 1,
        };

        stay.Party.Add(new StayGuest
        {
            StayId = stay.Id,
            GuestId = guestRow.Id,
            IsPrimary = true,
            AddedAt = Clock.GetUtcNow(),
            Origin = RecordOrigin.Staff,
        });

        Db.Bookings.Add(booking);
        Db.Guests.Add(guestRow);
        Db.Stays.Add(stay);
        await Db.SaveChangesAsync();

        return stay;
    }

    /// <summary>Close the context, then drop the database.</summary>
    /// <returns>When both are gone.</returns>
    /// <remarks>
    /// In that order: <c>DROP DATABASE</c> fails while a connection is open, and
    /// the shared harness reports that as a harness bug rather than forcing it.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _scratch.DisposeAsync();
    }
}
