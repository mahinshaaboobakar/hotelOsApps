using HotelOS.Contracts.MasterData.V1;
using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Platform;

namespace HotelOS.Jobs.Infrastructure;

/// <summary>
/// The directory, answered by Master Data's read surface — the property's code
/// and zone, its departments, a location. What Master Data cannot answer —
/// who is on shift (Workforce) and who holds a headship or the jobs-manager
/// grant (Workforce, Identity) — returns empty here: <b>no Workforce or
/// Identity client exists in the application SDK today</b> (build finding,
/// 2026-09-04). AUTO assignment then leaves a job "pending" and the ladder's
/// accountable user is recorded as unresolved, which the design allows (§6:
/// "nobody → one step up, reason recorded"). The Context question replaces
/// these two methods when it exists; the interface does not change.
/// </summary>
public class MasterDataPropertyDirectory(MasterDataService.MasterDataServiceClient masterData)
    : IPropertyDirectory
{
    private const string Service = "jobs";

    public async Task<string?> FindPropertyCodeAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await GetPropertyAsync(propertyId, cancellationToken);
        return string.IsNullOrWhiteSpace(property.Code) ? null : property.Code;
    }

    public async Task<string?> FindTimezoneAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await GetPropertyAsync(propertyId, cancellationToken);
        return string.IsNullOrWhiteSpace(property.Timezone) ? null : property.Timezone;
    }

    public async Task<Guid?> FindDepartmentIdAsync(Guid propertyId, string departmentCode, CancellationToken cancellationToken)
    {
        var departments = await masterData.ListDepartmentsAsync(
            new ListDepartmentsRequest { Context = RequestContextFactory.ForService(Service, propertyId) },
            cancellationToken: cancellationToken);
        var match = departments.Departments.FirstOrDefault(
            d => string.Equals(d.Code, departmentCode, StringComparison.OrdinalIgnoreCase));
        return match is not null && Guid.TryParse(match.Id, out var id) ? id : null;
    }

    public async Task<Guid?> FindOrganizationAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await GetPropertyAsync(propertyId, cancellationToken);
        return Guid.TryParse(property.OrganizationId, out var organization) ? organization : null;
    }

    public async Task<string?> FindLocationNameAsync(Guid propertyId, Guid locationId, CancellationToken cancellationToken)
    {
        try
        {
            var location = await masterData.GetLocationAsync(
                new GetLocationRequest
                {
                    Context = RequestContextFactory.ForService(Service, propertyId),
                    Id = locationId.ToString(),
                },
                cancellationToken: cancellationToken);
            return Guid.TryParse(location.PropertyId, out var owner) && owner == propertyId
                && !string.IsNullOrWhiteSpace(location.Name)
                    ? location.Name
                    : null;
        }
        catch (global::Grpc.Core.RpcException)
        {
            // Master Data down or the node gone: the screen says the place is
            // not named here rather than showing an id, and nothing fails.
            return null;
        }
    }

    public async Task<bool> LocationExistsAsync(Guid propertyId, Guid locationId, CancellationToken cancellationToken)
    {
        try
        {
            var location = await masterData.GetLocationAsync(
                new GetLocationRequest
                {
                    Context = RequestContextFactory.ForService(Service, propertyId),
                    Id = locationId.ToString(),
                },
                cancellationToken: cancellationToken);
            return Guid.TryParse(location.PropertyId, out var owner) && owner == propertyId;
        }
        catch (global::Grpc.Core.RpcException failure) when (failure.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    /// <remarks>Empty until a Workforce client exists — see the class remarks.</remarks>
    public Task<IReadOnlyList<OnShiftPerson>> OnShiftAsync(
        Guid propertyId, string departmentCode, DateOnly on, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OnShiftPerson>>([]);

    /// <inheritdoc />
    /// <remarks>Empty until Workforce headship and the Identity grant can be asked — see the class remarks.</remarks>
    public Task<IReadOnlyList<Guid>> ResolveRoleAsync(
        Guid propertyId, string departmentCode, string role, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(LadderRole.All.Contains(role) ? [] : []);

    private Task<Property> GetPropertyAsync(Guid propertyId, CancellationToken cancellationToken) =>
        masterData.GetPropertyAsync(
            new GetPropertyRequest { Context = RequestContextFactory.ForService(Service, propertyId) },
            cancellationToken: cancellationToken).ResponseAsync;
}
