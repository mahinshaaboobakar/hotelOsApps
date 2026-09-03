using HotelOS.Workforce.Application.Abstractions;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Master Data, as this suite needs it: recorded, and answering what a test says.
/// </summary>
/// <remarks>
/// <para>
/// The reason <see cref="IStaffDirectory"/> is an interface at all. A gRPC
/// channel here would make every posting test an integration test against a
/// second process, and the two facts this application asks Master Data for —
/// does this person have a login, is this department activated — are exactly the
/// two a characterisation test needs to vary.
/// </para>
/// <para>
/// <b>Activated by default, no identity link by default.</b> Both are the
/// ordinary case: a property has activated the departments it uses, and most
/// staff have no login — the platform's own proto calls that nullability *"the
/// whole point"*. A test that cares opts in by naming the exception, which keeps
/// the unusual case visible in the test that depends on it.
/// </para>
/// </remarks>
public sealed class StaffDirectoryDouble : IStaffDirectory
{
    private readonly Dictionary<Guid, Guid> _identities = [];
    private readonly Dictionary<string, Guid> _departments = [];

    /// <summary>Every department code this application asked about, in order.</summary>
    /// <remarks>
    /// Recorded because "was the department resolved before the posting was
    /// written" is a behaviour worth holding still: it is what makes a posting to
    /// an unactivated department a refusal rather than a row nothing can resolve.
    /// </remarks>
    public List<string> DepartmentLookups { get; } = [];

    /// <summary>Every staff id whose identity link was resolved, in order.</summary>
    public List<Guid> IdentityLookups { get; } = [];

    /// <summary>Codes to answer as not activated at this property.</summary>
    public HashSet<string> Unactivated { get; } = [];

    /// <summary>Give a staff member an identity link.</summary>
    /// <param name="staffId">The person.</param>
    /// <param name="userId">Their account.</param>
    public void WithLogin(Guid staffId, Guid userId) => _identities[staffId] = userId;

    /// <inheritdoc />
    public Task<Guid?> FindUserIdAsync(
        Guid propertyId, Guid staffId, CancellationToken cancellationToken)
    {
        IdentityLookups.Add(staffId);

        return Task.FromResult(
            _identities.TryGetValue(staffId, out var userId) ? userId : (Guid?)null);
    }

    /// <inheritdoc />
    public Task<Guid?> FindDepartmentIdAsync(
        Guid propertyId, string departmentCode, CancellationToken cancellationToken)
    {
        DepartmentLookups.Add(departmentCode);

        if (Unactivated.Contains(departmentCode))
        {
            return Task.FromResult((Guid?)null);
        }

        // Stable per code, so two postings to one department resolve to one id —
        // which is what an announcement's tuple would depend on.
        if (!_departments.TryGetValue(departmentCode, out var id))
        {
            id = Guid.NewGuid();
            _departments[departmentCode] = id;
        }

        return Task.FromResult((Guid?)id);
    }

    /// <summary>What the seed template will be keyed off. Null by default.</summary>
    /// <remarks>
    /// Null is the honest default: most tests do not care where the property is,
    /// and the neutral template is what a property that has not said gets.
    /// </remarks>
    public string? Country { get; set; }

    /// <inheritdoc />
    public Task<string?> FindPropertyCountryAsync(
        Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult(Country);

    private readonly Dictionary<Guid, string> _names = [];

    /// <summary>Each set of ids a name was asked for — one entry per call.</summary>
    /// <remarks>
    /// Recorded as sets rather than flattened, because what is worth holding
    /// still is that a card resolves its people in <b>one</b> call: a per-row
    /// lookup is the round trip the port's shape exists to prevent, and a flat
    /// list could not tell the two apart.
    /// </remarks>
    public List<IReadOnlyCollection<Guid>> NameLookups { get; } = [];

    /// <summary>Give a staff member a display name.</summary>
    /// <param name="staffId">The person.</param>
    /// <param name="displayName">What a name badge shows.</param>
    public void WithName(Guid staffId, string displayName) => _names[staffId] = displayName;

    /// <inheritdoc />
    /// <remarks>
    /// <b>Unknown by default.</b> A test that cares about a name says so, and one
    /// that does not gets the honest answer for somebody this directory has never
    /// heard of — which is the case a caller has to render without inventing a
    /// placeholder.
    /// </remarks>
    public Task<IReadOnlyDictionary<Guid, string>> FindNamesAsync(
        Guid propertyId, IReadOnlyCollection<Guid> staffIds, CancellationToken cancellationToken)
    {
        NameLookups.Add([.. staffIds]);

        IReadOnlyDictionary<Guid, string> found = staffIds
            .Distinct()
            .Where(_names.ContainsKey)
            .ToDictionary(id => id, id => _names[id]);

        return Task.FromResult(found);
    }
}
