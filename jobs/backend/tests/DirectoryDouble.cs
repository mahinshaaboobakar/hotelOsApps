using HotelOS.Jobs.Application.Abstractions;

namespace HotelOS.Jobs.Tests;

/// <summary>The directory as a test controls it: a property code, a zone, departments, a location set, who is on shift, who holds a role.</summary>
public sealed class DirectoryDouble : IPropertyDirectory
{
    public string? PropertyCode { get; set; } = "mrn";

    public string? Timezone { get; set; } = "Asia/Qatar";

    public HashSet<string> Departments { get; } = ["ENG", "HK", "FO"];

    public HashSet<Guid> Locations { get; } = [];

    public List<OnShiftPerson> OnShift { get; } = [];

    public Dictionary<string, List<Guid>> Roles { get; } = [];

    public List<string> RoleLookups { get; } = [];

    /// <summary>Properties Master Data cannot answer for — a real outage, for one property.</summary>
    /// <remarks>
    /// The sweep's tick is required to carry on past a property it cannot
    /// serve, and this is the failure that actually happens: Master Data
    /// unreachable while the row is fine.
    /// </remarks>
    public HashSet<Guid> Unreachable { get; } = [];

    public Task<string?> FindPropertyCodeAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult(PropertyCode);

    public Task<string?> FindTimezoneAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Unreachable.Contains(propertyId)
            ? throw new InvalidOperationException($"master data is unreachable for {propertyId}")
            : Task.FromResult(Timezone);

    public Task<Guid?> FindDepartmentIdAsync(Guid propertyId, string departmentCode, CancellationToken cancellationToken) =>
        Task.FromResult<Guid?>(Departments.Contains(departmentCode.ToUpperInvariant()) ? Guid.NewGuid() : null);

    public Task<bool> LocationExistsAsync(Guid propertyId, Guid locationId, CancellationToken cancellationToken) =>
        Task.FromResult(Locations.Count == 0 || Locations.Contains(locationId));

    /// <summary>The organisation the property belongs to, as Master Data would say.</summary>
    public Guid? Organization { get; set; }

    public Task<Guid?> FindOrganizationAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult(Organization);

    /// <summary>What the property calls a place — the test's own map, else null.</summary>
    public Dictionary<Guid, string> Places { get; } = [];

    public Task<string?> FindLocationNameAsync(Guid propertyId, Guid locationId, CancellationToken cancellationToken) =>
        Task.FromResult(Places.GetValueOrDefault(locationId));

    public Task<IReadOnlyList<OnShiftPerson>> OnShiftAsync(Guid propertyId, string departmentCode, DateOnly on, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OnShiftPerson>>(OnShift.ToList());

    public Task<IReadOnlyList<Guid>> ResolveRoleAsync(Guid propertyId, string departmentCode, string role, CancellationToken cancellationToken)
    {
        RoleLookups.Add(role);
        return Task.FromResult<IReadOnlyList<Guid>>(Roles.TryGetValue(role, out var users) ? users : []);
    }
}
