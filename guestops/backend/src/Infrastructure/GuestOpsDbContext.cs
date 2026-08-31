using HotelOS.GuestOps.Domain;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Infrastructure;

/// <summary>
/// This application's own schema, and nothing else's.
/// </summary>
/// <remarks>
/// <para>
/// <b>The schema is <c>reservations</c></b> — CLAUDE.md's canonical list names
/// it, and it is the domain ADR 0089 §CTX-Q2 assigned to this application. It
/// is not named after the application's display name for the same reason the
/// permission registry is not: the domain outlives whatever the product calls
/// its screens.
/// </para>
/// <para>
/// <b>It writes here and reads Master Data.</b> An installed application holds
/// DML on its own schema and <c>SELECT</c> on <c>masterdata</c>, and no DDL at
/// all — so it could not alter its own schema if its code tried. Rooms, room
/// types and properties are referenced by id and never copied.
/// </para>
/// <para>
/// <b>The event store is the platform's</b> and is excluded from this
/// application's migrations: <c>AddPlatformEventStore</c> maps the two tables
/// the shared appender writes, so a stay change and its announcement commit in
/// one transaction. Generating a <c>CREATE TABLE events</c> from here would put
/// two components in charge of one table.
/// </para>
/// </remarks>
public class GuestOpsDbContext(DbContextOptions<GuestOpsDbContext> options) : DbContext(options)
{
    /// <summary>The schema this application owns — ADR 0029.</summary>
    public const string Schema = "reservations";

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingExternalRef> BookingExternalRefs => Set<BookingExternalRef>();

    public DbSet<RoomStay> Stays => Set<RoomStay>();

    public DbSet<StayExternalRef> StayExternalRefs => Set<StayExternalRef>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<StayGuest> Party => Set<StayGuest>();

    public DbSet<GuestIdentity> Guests => Set<GuestIdentity>();

    public DbSet<ContactPoint> Contacts => Set<ContactPoint>();

    public DbSet<StayAbsence> Absences => Set<StayAbsence>();

    public DbSet<CommercialTerms> Terms => Set<CommercialTerms>();

    public DbSet<StaySource> Sources => Set<StaySource>();

    public DbSet<StaySourceDetail> SourceDetail => Set<StaySourceDetail>();

    public DbSet<StayDisagreement> Disagreements => Set<StayDisagreement>();

    public DbSet<StayLinkCandidate> LinkCandidates => Set<StayLinkCandidate>();

    public DbSet<StopSell> StopSells => Set<StopSell>();

    public DbSet<RoomOutOfOrder> RoomsOutOfOrder => Set<RoomOutOfOrder>();

    public DbSet<Registration> Registrations => Set<Registration>();

    public DbSet<StayReporting> Reporting => Set<StayReporting>();

    public DbSet<StayRequest> Requests => Set<StayRequest>();

    public DbSet<StayNote> Notes => Set<StayNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GuestOpsDbContext).Assembly);

        // The shared event store — one appender, one table, one transaction.
        modelBuilder.AddPlatformEventStore();
    }
}
