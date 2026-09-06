using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Infrastructure;

/// <summary>
/// The directory, answered from Master Data's tables through this application's
/// install grant — the property's code and timezone, its departments, a
/// location.
/// </summary>
/// <remarks>
/// <para>
/// <b>It was a <c>MasterDataServiceClient</c>, and that call could never have
/// succeeded.</b> An application presents a certificate and holds no access
/// token, so the wire refuses it: <b>an application is not a platform
/// service</b> — ADR 0093 §PKG-Q8. Nothing in this build had reached Master Data
/// yet, so the code looked correct while being unrunnable; Workforce met the
/// same refusal on 2026-09-06 and converted the same way. The ruled path is
/// ADR 0092 §4's install grant, and <c>hotelos_app_jobs</c> already holds
/// <c>hotelos_masterdata_reader</c>.
/// </para>
/// <para>
/// <b>The interface did not change.</b> <see cref="IPropertyDirectory"/> is the
/// question this application asks; where the answer comes from is this file's
/// business and nobody else's, which is why the conversion touched no caller.
/// </para>
/// <para>
/// What Master Data cannot answer — who is on shift (Workforce) and who holds a
/// headship or the jobs-manager grant (Workforce, Identity) — still returns
/// empty. AUTO assignment then leaves a job pending and the ladder's accountable
/// user is recorded as unresolved, which the design allows (§6: <i>"nobody → one
/// step up, reason recorded"</i>).
/// </para>
/// <para>
/// <b>Every query names the property.</b> These tables hold every property's
/// rows; a lookup by code or id alone would answer with another hotel's, which
/// is a cross-property leak and not a screen defect.
/// </para>
/// </remarks>
public class MasterDataPropertyDirectory(JobsDbContext db) : IPropertyDirectory
{
    public async Task<string?> FindPropertyCodeAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var code = await db.MasterDataProperties
            .Where(p => p.Id == propertyId)
            .Select(p => p.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(code) ? null : code;
    }

    public async Task<string?> FindTimezoneAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var timezone = await db.MasterDataProperties
            .Where(p => p.Id == propertyId)
            .Select(p => p.Timezone)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(timezone) ? null : timezone;
    }

    public async Task<Guid?> FindDepartmentIdAsync(
        Guid propertyId, string departmentCode, CancellationToken cancellationToken)
    {
        // `ILike` rather than lowering both sides in memory: the call this
        // replaced compared case-insensitively after fetching every department,
        // and the codes are written upper-case by configuration and in whatever
        // case by a policy author. Postgres does the comparison now, and one row
        // comes back instead of all of them.
        var match = await db.MasterDataDepartments
            .Where(d => d.PropertyId == propertyId && EF.Functions.ILike(d.Code, departmentCode))
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return match;
    }

    public async Task<Guid?> FindOrganizationAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var organization = await db.MasterDataProperties
            .Where(p => p.Id == propertyId)
            .Select(p => (Guid?)p.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        return organization == Guid.Empty ? null : organization;
    }

    public async Task<string?> FindLocationNameAsync(
        Guid propertyId, Guid locationId, CancellationToken cancellationToken)
    {
        // Absent, deleted, or another property's all answer the same: the screen
        // says the place is not named here rather than showing an id, and
        // nothing fails. That was the behaviour when Master Data being
        // unreachable produced it; it is now the behaviour when the row is not
        // this property's to read.
        var name = await db.MasterDataLocations
            .Where(l => l.Id == locationId && l.PropertyId == propertyId && l.DeletedAt == null)
            .Select(l => l.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    public Task<bool> LocationExistsAsync(
        Guid propertyId, Guid locationId, CancellationToken cancellationToken) =>
        db.MasterDataLocations.AnyAsync(
            l => l.Id == locationId && l.PropertyId == propertyId && l.DeletedAt == null,
            cancellationToken);

    /// <inheritdoc />
    /// <remarks>Empty until a Workforce read exists — see the class remarks.</remarks>
    public Task<IReadOnlyList<OnShiftPerson>> OnShiftAsync(
        Guid propertyId, string departmentCode, DateOnly on, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OnShiftPerson>>([]);

    /// <inheritdoc />
    /// <remarks>Empty until Workforce headship and the Identity grant can be asked — see the class remarks.</remarks>
    public Task<IReadOnlyList<Guid>> ResolveRoleAsync(
        Guid propertyId, string departmentCode, string role, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(LadderRole.All.Contains(role) ? [] : []);
}
