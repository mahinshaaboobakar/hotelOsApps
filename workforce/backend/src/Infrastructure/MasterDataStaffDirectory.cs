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
}
