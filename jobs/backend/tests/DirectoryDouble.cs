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

    public Task<string?> FindPropertyCodeAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult(PropertyCode);

    public Task<string?> FindTimezoneAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult(Timezone);

    public Task<Guid?> FindDepartmentIdAsync(Guid propertyId, string departmentCode, CancellationToken cancellationToken) =>
        Task.FromResult<Guid?>(Departments.Contains(departmentCode.ToUpperInvariant()) ? Guid.NewGuid() : null);

    public Task<bool> LocationExistsAsync(Guid propertyId, Guid locationId, CancellationToken cancellationToken) =>
        Task.FromResult(Locations.Count == 0 || Locations.Contains(locationId));

    public Task<IReadOnlyList<OnShiftPerson>> OnShiftAsync(Guid propertyId, string departmentCode, DateOnly on, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OnShiftPerson>>(OnShift.ToList());

    public Task<IReadOnlyList<Guid>> ResolveRoleAsync(Guid propertyId, string departmentCode, string role, CancellationToken cancellationToken)
    {
        RoleLookups.Add(role);
        return Task.FromResult<IReadOnlyList<Guid>>(Roles.TryGetValue(role, out var users) ? users : []);
    }
}
