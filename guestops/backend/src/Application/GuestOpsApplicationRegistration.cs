using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Application.Bookings;
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

        return services;
    }
}
