namespace HotelOS.Jobs.Application.Abstractions;

/// <summary>
/// What this application asks the platform about a property — the property's
/// code and zone, its departments and their heads, who is on shift, who holds
/// the jobs-manager grant. Every answer is a Context question (design §1 rule
/// 3): Jobs holds none of these facts and never copies one.
/// </summary>
public interface IPropertyDirectory
{
    /// <summary>The property's code as Master Data has it — the job number's first part.</summary>
    Task<string?> FindPropertyCodeAsync(Guid propertyId, CancellationToken cancellationToken);

    /// <summary>The property's IANA zone — a scheduled job's day begins in it (S2 D3).</summary>
    Task<string?> FindTimezoneAsync(Guid propertyId, CancellationToken cancellationToken);

    /// <summary>The department's canonical id, or null when the property has not activated it.</summary>
    Task<Guid?> FindDepartmentIdAsync(Guid propertyId, string departmentCode, CancellationToken cancellationToken);

    /// <summary>Whether the location is a node of this property's tree.</summary>
    Task<bool> LocationExistsAsync(Guid propertyId, Guid locationId, CancellationToken cancellationToken);

    /// <summary>Which organisation this property belongs to, or null when Master Data cannot say.</summary>
    /// <remarks>
    /// The catalogue is the organisation's, and a call from a screen names only
    /// the property — the module envelope carries <c>property_id</c> and
    /// nothing else, by design. So curating asks Master Data which organisation
    /// the property is in rather than trusting a bundle to say.
    /// </remarks>
    Task<Guid?> FindOrganizationAsync(Guid propertyId, CancellationToken cancellationToken);

    /// <summary>What the property calls the place — "Room 1204" — or null when it cannot say.</summary>
    /// <remarks>
    /// A screen draws the name and the job row holds only the id. Null rather
    /// than the id: an identifier shown where a room number belongs is worse
    /// than an honest blank, because it reads as data.
    /// </remarks>
    Task<string?> FindLocationNameAsync(Guid propertyId, Guid locationId, CancellationToken cancellationToken);

    /// <summary>People of the department on shift on <paramref name="on"/> — the assignment list (S3 D1).</summary>
    Task<IReadOnlyList<OnShiftPerson>> OnShiftAsync(
        Guid propertyId, string departmentCode, DateOnly on, CancellationToken cancellationToken);

    /// <summary>The users a ladder role resolves to today — supervisor and manager from Workforce headship, jobs manager from Identity.</summary>
    Task<IReadOnlyList<Guid>> ResolveRoleAsync(
        Guid propertyId, string departmentCode, string role, CancellationToken cancellationToken);
}

/// <summary>Somebody who could take a job today.</summary>
public sealed record OnShiftPerson(Guid UserId, string Name, int OpenJobs);
