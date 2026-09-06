using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Infrastructure;

/// <summary>
/// <see cref="IStaffDirectory"/> over Master Data's canonical rows, read
/// through the platform-standard grant.
/// </summary>
/// <remarks>
/// <para>
/// <b>ADR 0092 §4 names this path</b> — <i>an application reads canonical
/// Master Data through the platform-standard grant</i> — and install step 4
/// issues it: <c>GRANT hotelos_masterdata_reader TO hotelos_app_workforce</c>.
/// </para>
/// <para>
/// <b>This replaced a gRPC client, and the live drive is why.</b> The client
/// reached Master Data and was refused — <i>`workforce` presented a certificate
/// but no access token; sign in first</i> — because an installed application's
/// certificate is <c>kind=application</c> and the authenticator admits only
/// <c>TransportPrincipalKind.Service</c> without a token. Neither door was ours
/// to open: admitting the application kind token-less puts application
/// capability in Master Data's authenticator, where ADR 0093's authority table
/// puts it in the Kernel; forwarding a bearer token rebuilds the second
/// identity channel ADR 0014 deleted <c>on_behalf_of</c> to remove.
/// </para>
/// <para>
/// <b>Serving is not storing.</b> Every method reads at answer time. Nothing
/// here is copied into <c>workforce</c> and nothing is cached — a department
/// deactivated a moment ago must not still accept a posting, and a stale
/// identity link would announce a tuple for a user who no longer exists. The
/// rows are keyless, so this application could not write them if it tried.
/// </para>
/// <para>
/// <b>Deleted and inactive are filtered here, not by the caller.</b> The gRPC
/// surface applied Master Data's own lifecycle rules before answering; reading
/// the tables directly means inheriting that obligation, and a caller that
/// forgot would silently post somebody to a department that had been stood
/// down. ADR 0062's three states, applied at every read.
/// </para>
/// </remarks>
public class MasterDataStaffDirectory(WorkforceDbContext database) : IStaffDirectory
{
    /// <inheritdoc />
    public async Task<StaffLink?> FindStaffAsync(
        Guid propertyId, Guid staffId, CancellationToken cancellationToken)
        // One row, and it carries both facts. Projecting to a record rather
        // than to `UserId` is what keeps "unknown here" distinguishable from
        // "known, no login": `FirstOrDefaultAsync` over a `Guid?` returns null
        // for both, which is the defect this shape removes.
        => await Scoped(propertyId)
            .Where(staff => staff.Id == staffId)
            .Select(staff => new StaffLink(staff.Id, staff.UserId))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Guid?> FindDepartmentIdAsync(
        Guid propertyId, string departmentCode, CancellationToken cancellationToken)
    {
        var code = departmentCode.ToUpperInvariant();

        var found = await Departments(propertyId)
            .Where(department => department.Code.ToUpper() == code)
            .Select(department => (Guid?)department.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return found;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> FindDepartmentNamesAsync(
        Guid propertyId, CancellationToken cancellationToken)
    {
        var departments = await Departments(propertyId)
            .Select(department => new { department.Code, department.Name })
            .ToListAsync(cancellationToken);

        // Keyed on the code and upper-cased once here, because a posting stores
        // the canon form and a lookup that differed in case would miss silently
        // — the caller would render a blank name and nothing would say why.
        //
        // `ToDictionary` throws on a duplicate key, which is the right failure:
        // two departments sharing a code after upper-casing is a Master Data
        // defect, and answering with whichever won would hide it.
        return departments.ToDictionary(
            department => department.Code.ToUpperInvariant(),
            department => department.Name);
    }

    /// <inheritdoc />
    public async Task<string?> FindPropertyCountryAsync(
        Guid propertyId, CancellationToken cancellationToken)
    {
        var country = await database.MasterDataProperties
            .AsNoTracking()
            .Where(property => property.Id == propertyId)
            .Select(property => property.Country)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(country) ? null : country;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>One query, where the gRPC surface needed one call per person.</b>
    /// That version's own comment looked forward to <i>the day `ListStaff`
    /// gains an id filter</i>; reading the rows directly is that day, and the
    /// fan-out it apologised for is gone rather than tuned.
    /// </remarks>
    public async Task<IReadOnlyDictionary<Guid, string>> FindNamesAsync(
        Guid propertyId, IReadOnlyCollection<Guid> staffIds, CancellationToken cancellationToken)
    {
        var wanted = staffIds.Distinct().ToList();

        if (wanted.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var people = await Scoped(propertyId)
            .Where(staff => wanted.Contains(staff.Id))
            .Select(staff => new { staff.Id, staff.DisplayName })
            .ToListAsync(cancellationToken);

        // An empty display name is left out rather than filled in. The caller
        // renders what it was given; a placeholder invented here would be this
        // application deciding what somebody is called.
        return people
            .Where(person => !string.IsNullOrWhiteSpace(person.DisplayName))
            .ToDictionary(person => person.Id, person => person.DisplayName!);
    }

    /// <summary>The property's live departments.</summary>
    private IQueryable<DepartmentRow> Departments(Guid propertyId)
        => database.MasterDataDepartments
            .AsNoTracking()
            .Where(department => department.PropertyId == propertyId
                                 && department.Active
                                 && department.DeletedAt == null);

    /// <summary>
    /// The people this property may see, live.
    /// </summary>
    /// <remarks>
    /// <c>masterdata.staff</c> is organization-scoped and carries no
    /// <c>property_id</c> — ADR 0052 left only <c>StaffPropertyScope</c> to
    /// narrow it. A read that skipped the join would answer across every
    /// property in the organization, which is the tenancy boundary crossed by
    /// omission rather than by intent, so the join is here and not at any
    /// caller's discretion.
    /// </remarks>
    private IQueryable<StaffRow> Scoped(Guid propertyId)
        => from staff in database.MasterDataStaff.AsNoTracking()
           join scope in database.MasterDataStaffScopes.AsNoTracking()
               on staff.Id equals scope.StaffId
           where scope.PropertyId == propertyId
                 && scope.Active && scope.DeletedAt == null
                 && staff.Active && staff.DeletedAt == null
           select staff;
}
