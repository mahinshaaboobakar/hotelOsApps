using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Application.Inbound;
using HotelOS.GuestOps.Application.Reconciliation;
using HotelOS.GuestOps.Application.Registrations;
using HotelOS.GuestOps.Application.Reporting;
using HotelOS.GuestOps.Application.Requests;
using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.GuestOps.Application;

/// <summary>This application's own services, registered in one place.</summary>
/// <remarks>
/// A composition root holds no subject (ADR 0042). It is here rather than in
/// <c>Program.cs</c> so the test harness can construct the same graph without a
/// web host — the alternative is a second registration list that drifts from
/// the one that ships.
/// </remarks>
public static class GuestOpsApplicationRegistration
{
    /// <param name="services">The container this registers into.</param>
    public static IServiceCollection AddGuestOpsApplication(this IServiceCollection services)
    {
        services.AddScoped<IRoomInventory, RoomInventory>();

        services.AddScoped<BookingService>();
        services.AddScoped<StayLifecycleService>();
        services.AddScoped<StayAssignmentService>();
        services.AddScoped<StayListService>();
        services.AddScoped<BookingReadService>();
        services.AddScoped<Module.TodayView>();
        services.AddScoped<Module.AttentionView>();
        services.AddScoped<Module.OccupancyView>();
        services.AddScoped<Module.FeedView>();
        services.AddScoped<Module.MixView>();
        services.AddScoped<Module.WatchlistView>();
        services.AddScoped<Module.BookingsView>();
        services.AddScoped<Module.BookingView>();
        services.AddScoped<Module.AvailabilityView>();
        services.AddScoped<Module.CancelPlanView>();
        services.AddScoped<Module.WalkInCommand>();
        services.AddScoped<Module.CancelCommand>();
        services.AddScoped<Module.ActivityView>();
        services.AddScoped<Module.RequestsView>();
        services.AddScoped<Module.ServicingView>();
        services.AddScoped<Module.PaymentView>();
        services.AddScoped<AvailabilityService>();

        // The inbound half — the Hub's deferred facts, and the two flows a
        // person resolves. The transport that delivers them is not here: no
        // .NET subscription surface exists yet, so these are driven by a
        // recorded fact until that lands.
        services.AddScoped<StayMatcher>();
        services.AddScoped<InboundFactService>();

        // The event handlers — `EVT-Q4`. Scoped, because each resolves its own
        // DbContext: the host creates a scope per fact so two events never
        // share a unit of work.
        services.AddScoped<Events.JobCreatedHandler>();
        services.AddScoped<Events.ReservationFactHandler>();
        services.AddScoped<ReconciliationService>();

        // The desk's own records — the card, the filing obligation it creates,
        // guest requests and notes, over this application's configuration.
        services.AddScoped<SettingsService>();
        services.AddScoped<RegistrationService>();
        services.AddScoped<ReportingService>();
        services.AddScoped<StayRequestService>();

        return services;
    }
}
