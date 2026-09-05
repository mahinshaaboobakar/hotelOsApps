using HotelOS.Contracts.Context.V1;
using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Infrastructure.Platform;

/// <summary>
/// Which sibling applications answer, read off Context's own resolution.
/// </summary>
/// <remarks>
/// <para>
/// <c>Resolution.sources</c> lists the domains that answered and its contract
/// says <i>"a domain not installed on this property is simply not listed"</i>,
/// with <c>degraded</c> reserved for a domain that <i>is</i> installed and
/// failed. So the three states a caller needs are all present on one message,
/// and this reads them rather than inferring an installation from whether a
/// neighbour has ever done anything.
/// </para>
/// <para>
/// <b>A degraded resolution answers unknown, not absent.</b> A domain that
/// should have answered and did not is exactly the case where saying <i>not
/// installed</i> would be wrong — and the caller's unknown branch draws the
/// installed variant, so a property whose Jobs is briefly down keeps its raise
/// button.
/// </para>
/// <para>
/// <b>A failure to reach Context answers unknown too.</b> This application has
/// no service certificate until an installed package is enrolled with one, so
/// today this call cannot succeed — and the honest answer to <i>is Jobs
/// installed</i> when the only authority is unreachable is <i>nobody
/// established it</i>. Failing closed here would dim tabs on every property.
/// </para>
/// </remarks>
public sealed class ContextNeighbours(ContextService.ContextServiceClient context)
    : INeighbours
{
    /// <inheritdoc />
    public async Task<bool?> InstalledAsync(
        RequestScope scope, string domain, CancellationToken cancellationToken)
    {
        PropertySummary summary;

        try
        {
            summary = await context.GetPropertySummaryAsync(
                new GetPropertySummaryRequest
                {
                    Context = RequestContextFactory.ToRequestContext(scope),
                },
                cancellationToken: cancellationToken);
        }
        // `global::`, because this assembly has its own `HotelOS.GuestOps.Grpc`
        // namespace and an unqualified `Grpc.Core` resolves into it.
        catch (global::Grpc.Core.RpcException)
        {
            // Unknown, deliberately. See the remarks: the alternative is a
            // desk that loses a capability every time Context is unreachable.
            return null;
        }

        if (summary.Resolution is not { } resolution)
        {
            return null;
        }

        if (resolution.Sources.Contains(domain))
        {
            return true;
        }

        // Degraded means a domain that should have answered did not, and the
        // message does not say which. Absence is therefore not evidence of
        // absence on this particular reply.
        return resolution.Degraded ? null : false;
    }
}
