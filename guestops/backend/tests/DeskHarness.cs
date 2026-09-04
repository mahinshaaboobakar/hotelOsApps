using HotelOS.GuestOps.Application.Registrations;
using HotelOS.GuestOps.Application.Reporting;
using HotelOS.GuestOps.Application.Requests;
using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The desk's own records, over a migrated scratch database.
/// </summary>
/// <remarks>
/// A second harness rather than more constructor parameters on the inbound one:
/// they share the database mechanism through <see cref="GuestOpsScratch"/> and
/// the doubles through <c>Recorders.cs</c>, so what is duplicated here is four
/// lines of composition rather than any machinery.
/// </remarks>
public sealed class DeskHarness : IAsyncDisposable
{
    private readonly GuestOpsScratch _scratch;

    private DeskHarness(GuestOpsScratch scratch, GuestOpsDbContext db, ManualClock clock)
    {
        _scratch = scratch;
        Db = db;
        Clock = clock;
        Events = new RecordingAppender();
        Authorizer = new RecordingAuthorizer();

        Settings = new SettingsService(db, Authorizer);
        Registrations = new RegistrationService(db, Authorizer, Settings, clock);
        Reporting = new ReportingService(db, Authorizer, clock);
        Requests = new StayRequestService(db, Authorizer, Events, clock);
    }

    public GuestOpsDbContext Db { get; }

    public ManualClock Clock { get; }

    public RecordingAppender Events { get; }

    public RecordingAuthorizer Authorizer { get; }

    public SettingsService Settings { get; }

    public RegistrationService Registrations { get; }

    public ReportingService Reporting { get; }

    public StayRequestService Requests { get; }

    public static readonly Guid Property = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid RoomType = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid Room = Guid.Parse("33333333-3333-3333-3333-333333333333");

    /// <summary>A database of this test's own, and the desk services over it.</summary>
    /// <returns>A prepared harness.</returns>
    public static async Task<DeskHarness> CreateAsync()
    {
        var scratch = await GuestOpsScratch.CreateAsync();

        return new DeskHarness(
            scratch,
            scratch.Context(),
            new ManualClock(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero)));
    }

    public RequestScope Scope() => new() { PropertyId = Property, UserId = Guid.NewGuid() };

    /// <summary>
    /// A property configured the way most of these tests need it.
    /// </summary>
    /// <param name="homeCountry">Which nationality counts as domestic.</param>
    /// <param name="reportingRequired">Whether this property files at all.</param>
    /// <param name="appliesTo">Who the obligation covers.</param>
    /// <returns>The stored configuration.</returns>
    /// <remarks>
    /// <b>The home country is a parameter, never a constant.</b> These tests use
    /// two different ones precisely so that nothing can quietly come to depend
    /// on a particular country — the rule this application is built on is that
    /// the same build serves every market.
    /// </remarks>
    public async Task<GuestOpsSettings> ConfigureAsync(
        string homeCountry = "IN",
        bool reportingRequired = true,
        ReportingScope appliesTo = ReportingScope.FromOutside)
    {
        var settings = new GuestOpsSettings
        {
            PropertyId = Property,
            HomeCountry = homeCountry,
            RequiredForHomeCountry = ["name_as_on_id", "id_type", "id_number"],
            RequiredForVisitors =
                ["name_as_on_id", "nationality", "passport_number", "visa_number"],
            AcceptedIdTypes = ["passport", "national_id", "driving_licence"],
            SignatureRequired = true,
            CardNumberPrefix = "GRC-",
            NextCardNumber = 1,
            ReportingRequired = reportingRequired,
            ReportingAppliesTo = appliesTo,
            ReportingAuthority = "the local police station",
            ReportingDueHours = 24,
        };

        Db.Settings.Add(settings);
        await Db.SaveChangesAsync();
        return settings;
    }

    /// <summary>A stay to hang the desk's records on.</summary>
    /// <param name="arrival">When the guest arrived, or null for an unknown arrival.</param>
    /// <returns>The stored stay.</returns>
    public async Task<RoomStay> SeedStayAsync(DateTimeOffset? arrival = null)
    {
        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            PropertyId = Property,
            Origin = RecordOrigin.Staff,
            CreatedAt = Clock.GetUtcNow(),
            Version = 1,
        };

        var stay = new RoomStay
        {
            Id = Guid.CreateVersion7(),
            BookingId = booking.Id,
            PropertyId = Property,
            RoomTypeId = RoomType,
            CurrentRoomId = Room,
            Lifecycle = StayLifecycle.InHouse,
            ArrivalAt = arrival is { } at
                ? new StayTime(at, TimeBasis.Observed)
                : StayTime.None,
            DepartureAt = StayTime.None,
            Origin = RecordOrigin.Staff,
            CreatedAt = Clock.GetUtcNow(),
            Version = 1,
        };

        Db.Bookings.Add(booking);
        Db.Stays.Add(stay);
        await Db.SaveChangesAsync();
        return stay;
    }

    /// <summary>Close the context, then drop the database.</summary>
    /// <returns>When both are gone.</returns>
    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _scratch.DisposeAsync();
    }
}
