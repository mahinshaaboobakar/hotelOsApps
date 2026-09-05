using HotelOS.Contracts.MasterData.V1;
using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;

namespace HotelOS.Workforce.Infrastructure;

/// <summary>
/// <see cref="IStaffDirectory"/> over Master Data's own gRPC surface.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a synchronous call is right here, stated where somebody would
/// challenge it.</b> <c>EVT-Q3</c> rules that between <i>applications</i> a
/// reply is an event carrying a correlation id, never a blocking call — and it
/// preserves request/reply for platform-internal <i>questions</i>. Master Data
/// is the platform: CLAUDE.md's non-negotiable list says applications may read
/// master data, and both calls below are questions, never commands. A
/// <c>CreatePosting</c> in an installable application calling out to a
/// neighbouring <i>application</i> would be the violation; this is not that.
/// </para>
/// <para>
/// <b>And it is not the absent-neighbour case either.</b> <c>APPS-Q2</c> says an
/// application's own flow is never gated on another <i>application</i> being
/// installed. Master Data cannot be absent — it is platform, always present —
/// so failing when it is unreachable is a genuine outage rather than a missing
/// neighbour, and is surfaced as one.
/// </para>
/// <para>
/// <b>Nothing is cached.</b> A department deactivated a moment ago must not
/// still accept a posting, and a stale identity link would announce a tuple for
/// a user who no longer exists. This is the same reason the platform refuses a
/// second cache in front of the authorization authority.
/// </para>
/// </remarks>
public class MasterDataStaffDirectory(
    MasterDataService.MasterDataServiceClient masterData)
    : IStaffDirectory
{
    /// <inheritdoc />
    public async Task<Guid?> FindUserIdAsync(
        Guid propertyId, Guid staffId, CancellationToken cancellationToken)
    {
        var staff = await masterData.GetStaffAsync(
            new GetStaffRequest
            {
                Context = RequestContextFactory.ForService("workforce", propertyId),
                Id = staffId.ToString(),
            },
            cancellationToken: cancellationToken);

        // Empty is the ordinary answer — most staff have no login, and the
        // platform's own proto calls that nullability "the whole point".
        return Guid.TryParse(staff.UserId, out var userId) ? userId : null;
    }

    /// <inheritdoc />
    public async Task<Guid?> FindDepartmentIdAsync(
        Guid propertyId, string departmentCode, CancellationToken cancellationToken)
    {
        var departments = await masterData.ListDepartmentsAsync(
            new ListDepartmentsRequest
            {
                Context = RequestContextFactory.ForService("workforce", propertyId),
            },
            cancellationToken: cancellationToken);

        // Matched on the **code**, which ADR 0119 makes the canonical identity —
        // immutable, identical in every installation, and what a posting stores.
        // The row id is what the authorization graph addresses
        // (`department:{uuid}`), which is the whole reason this lookup exists.
        var match = departments.Departments.FirstOrDefault(
            department => string.Equals(
                department.Code, departmentCode, StringComparison.OrdinalIgnoreCase));

        return match is not null && Guid.TryParse(match.Id, out var id) ? id : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> FindDepartmentNamesAsync(
        Guid propertyId, CancellationToken cancellationToken)
    {
        var departments = await masterData.ListDepartmentsAsync(
            new ListDepartmentsRequest
            {
                Context = RequestContextFactory.ForService("workforce", propertyId),
            },
            cancellationToken: cancellationToken);

        // Keyed on the code and upper-cased once here, because a posting stores
        // the canon form and a lookup that differed in case would miss silently
        // — the caller would render a blank name and nothing would say why.
        return departments.Departments.ToDictionary(
            department => department.Code.ToUpperInvariant(),
            department => department.Name);
    }

    /// <inheritdoc />
    public async Task<string?> FindPropertyCountryAsync(
        Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await masterData.GetPropertyAsync(
            // No id on the request: the property *is* the scope, which is
            // Master Data expressing that a property cannot be read from another
            // property's context. The tenancy boundary is the request envelope.
            new GetPropertyRequest
            {
                Context = RequestContextFactory.ForService("workforce", propertyId),
            },
            cancellationToken: cancellationToken);

        return string.IsNullOrWhiteSpace(property.Country) ? null : property.Country;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Several small reads, run together.</b> Master Data's <c>ListStaff</c>
    /// filters by property, a search string and a page — it has no id filter —
    /// so listing to find five people would pull and page everybody who works
    /// there to answer a question about five. The gets are issued concurrently,
    /// so the cost is one round trip's latency rather than five.
    /// </para>
    /// <para>
    /// The port takes a set precisely so this stays the adapter's decision: the
    /// day <c>ListStaff</c> gains an id filter, this becomes one call and
    /// nothing above it changes.
    /// </para>
    /// <para>
    /// <b>Nothing is kept.</b> Serving is not storing — the dictionary lives as
    /// long as the answer being composed. A cache here would be the second copy
    /// the ruling exists to avoid, and it would be wrong the first time somebody
    /// was renamed.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyDictionary<Guid, string>> FindNamesAsync(
        Guid propertyId, IReadOnlyCollection<Guid> staffIds, CancellationToken cancellationToken)
    {
        var wanted = staffIds.Distinct().ToList();

        if (wanted.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var context = RequestContextFactory.ForService("workforce", propertyId);

        var people = await Task.WhenAll(wanted.Select(id =>
            masterData.GetStaffAsync(
                new GetStaffRequest { Context = context, Id = id.ToString() },
                cancellationToken: cancellationToken).ResponseAsync));

        var names = new Dictionary<Guid, string>(wanted.Count);

        foreach (var person in people)
        {
            // An empty display name is left out rather than filled in. The
            // caller renders what it was given; a placeholder invented here
            // would be this application deciding what somebody is called.
            if (Guid.TryParse(person.Id, out var id)
                && !string.IsNullOrWhiteSpace(person.DisplayName))
            {
                names[id] = person.DisplayName;
            }
        }

        return names;
    }
}
