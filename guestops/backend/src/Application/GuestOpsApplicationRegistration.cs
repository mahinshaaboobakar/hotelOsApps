using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Application.Inbound;
using HotelOS.GuestOps.Application.Reconciliation;
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
        services.AddScoped<AvailabilityService>();

        // The inbound half — the Hub's deferred facts, and the two flows a
        // person resolves. The transport that delivers them is not here: no
        // .NET subscription surface exists yet, so these are driven by a
        // recorded fact until that lands.
        services.AddScoped<StayMatcher>();
        services.AddScoped<InboundFactService>();
        services.AddScoped<ReconciliationService>();

        return services;
    }
}
