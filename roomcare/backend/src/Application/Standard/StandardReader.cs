using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Standard;

/// <summary>The property's standard as a decision reads it — the policy row, its windows and its service standards.</summary>
/// <remarks>
/// A property nobody has configured still has a standard: HosPilot's defaults
/// (S0), returned unsaved, so the day runs before Setup is ever opened and the
/// first save is the first version.
/// </remarks>
public sealed class StandardReader(RoomCareDbContext db)
{
    /// <summary>The morning and evening windows a property has before it sets its own.</summary>
    public static readonly (string Window, TimeOnly Starts, TimeOnly Ends, bool Enabled)[] DefaultWindows =
    [
        (ServiceWindowName.Morning, new TimeOnly(8, 0), new TimeOnly(16, 0), true),
        (ServiceWindowName.Evening, new TimeOnly(18, 0), new TimeOnly(22, 0), false),
    ];

    /// <summary>The policy row, or the default one unsaved.</summary>
    public async Task<PropertyPolicy> PolicyAsync(Guid propertyId, CancellationToken cancellationToken) =>
        await db.Policies.FirstOrDefaultAsync(p => p.PropertyId == propertyId, cancellationToken)
        ?? PropertyPolicy.DefaultFor(propertyId);

    /// <summary>The windows, filling any the property has not saved with the defaults.</summary>
    public async Task<IReadOnlyList<ServiceWindow>> WindowsAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var saved = await db.Windows.Where(w => w.PropertyId == propertyId).ToListAsync(cancellationToken);
        return DefaultWindows
            .Select(d => saved.FirstOrDefault(w => w.Window == d.Window) ?? new ServiceWindow
            {
                Id = Guid.Empty,
                PropertyId = propertyId,
                Window = d.Window,
                Starts = d.Starts,
                Ends = d.Ends,
                Enabled = d.Enabled,
            })
            .ToList();
    }

    /// <summary>What a service takes for a room type: the saved row, or the house default for that service.</summary>
    public async Task<ServiceStandard> StandardAsync(
        Guid propertyId, Guid? roomTypeId, string service, CancellationToken cancellationToken) =>
        await db.Standards.FirstOrDefaultAsync(
            s => s.PropertyId == propertyId && s.RoomTypeId == roomTypeId && s.Service == service, cancellationToken)
        ?? DefaultStandard(propertyId, roomTypeId, service);

    /// <summary>S0's defaults: minutes per service, and the phases each one walks.</summary>
    public static ServiceStandard DefaultStandard(Guid propertyId, Guid? roomTypeId, string service) => new()
    {
        PropertyId = propertyId,
        RoomTypeId = roomTypeId,
        Service = service,
        Minutes = service switch
        {
            Service.DepartureClean => 40,
            Service.DailyService => 20,
            Service.Turndown => 10,
            Service.Refresh => 10,
            _ => 20,
        },
        Credits = service == Service.DepartureClean ? 1m : 0.5m,
        InspectionRule = InspectionRule.None,
        Phases = service switch
        {
            Service.DepartureClean => [Phase.Strip, Phase.Clean, Phase.MakeUp, Phase.Done],
            Service.DailyService => [Phase.Clean, Phase.MakeUp, Phase.Done],
            _ => [Phase.Clean, Phase.Done],
        },
    };
}
