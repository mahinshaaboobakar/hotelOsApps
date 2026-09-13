using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Assignment;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.DeepCleans;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Application.Rooms;
using HotelOS.RoomCare.Application.Standard;
using HotelOS.RoomCare.Application.Supervision;
using HotelOS.RoomCare.Application.Tasks;
using HotelOS.RoomCare.Application.Tick;
using HotelOS.RoomCare.Application.Work;
using HotelOS.RoomCare.Events;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.RoomCare.Application;

/// <summary>The composition root of Room Care's services — every one scoped to a call or a tick pass.</summary>
/// <remarks>
/// One list for the host and the test harness alike, so a service added here is
/// wired in both, and a handler the manifest declares cannot be registered in
/// one and forgotten in the other.
/// </remarks>
public static class RoomCareApplication
{
    public static IServiceCollection AddRoomCareApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<Gate>();
        services.AddScoped<PropertyClock>();
        services.AddScoped<StandardReader>();
        services.AddScoped<ConditionWriter>();
        services.AddScoped<ObservationService>();
        services.AddScoped<DisagreementService>();
        services.AddScoped<RoomStatesService>();
        services.AddScoped<InspectionOutcome>();
        services.AddScoped<TaskWriter>();
        services.AddScoped<TaskMaker>();
        services.AddScoped<SupervisionLane>();
        services.AddScoped<ProposalService>();
        services.AddScoped<PrepareService>();
        services.AddScoped<AssignmentService>();
        services.AddScoped<DeepCleanService>();
        services.AddScoped<TaskEnding>();
        services.AddScoped<AttendantWork>();
        services.AddScoped<AmendService>();
        services.AddScoped<RoomActs>();
        services.AddScoped<SupervisionService>();
        services.AddScoped<StandardService>();
        services.AddScoped<HouseSetupService>();
        services.AddScoped<ManagerGrants>();
        services.AddScoped<WindowClose>();
        services.AddScoped<DayRoll>();
        services.AddScoped<TickPass>();
        return services;
    }
}
