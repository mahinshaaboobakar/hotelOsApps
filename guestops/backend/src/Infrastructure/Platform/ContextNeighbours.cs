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
/// failed. That is the design, and it is the only place in the platform where
/// the fact could live.
/// </para>
/// <para>
/// <b>It cannot answer yet, and this returns unknown rather than false.</b>
/// Measured 2026-09-05: Context's <c>Domains</c> class has exactly one member,
/// <c>masterdata</c>, and every resolver in the service hardcodes
/// <c>.Answered(Domains.MasterData)</c>. No code path anywhere records
/// <c>job</c>, <c>roomcare</c> or <c>guestops</c>. So <c>sources</c> in v1 is
/// always the single value <c>masterdata</c>, and <i>not listed</i> is a fact
/// about Context's ledger rather than about the property.
/// </para>
/// <para>
/// The first draft of this file read that absence as <b>false</b>, which would
/// have told <b>every</b> property that Jobs and Room Care were not installed —
/// renaming a tab and taking away a button on every desk in the estate. It is
/// the platform's recurring failure in a new place: a value that would read the
/// same if the world were otherwise.
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

        // **Unknown, never false.** Two independent reasons, and either alone
        // would be enough:
        //
        // 1. Context records only `masterdata` today, so this list cannot say
        //    anything about a neighbour — see the remarks.
        // 2. Even once it can, `degraded` means a domain that should have
        //    answered did not, and the message does not say which; absence on
        //    that reply is not evidence of absence.
        //
        // The day Context's ledger records installed domains, the `false`
        // branch belongs here and this comment is what says so. Returning it
        // now would be reading a fact off a field that does not carry it.
        return null;
    }
}
