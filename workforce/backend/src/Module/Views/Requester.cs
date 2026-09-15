using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// Who is making a change — established from the request, never asked for.
/// </summary>
/// <remarks>
/// <para>
/// <b>ADR 0172.</b> A write whose subject is the caller derives the caller from
/// <see cref="RequestScope.UserId"/> and accepts no field naming them. Raising
/// leave used to take <c>staffId</c> from the bundle, which made *whose leave*
/// a parameter: any caller holding <c>leave.request</c> could have raised a
/// request in anybody's name, and the audit trail would have agreed with them.
/// </para>
/// <para>
/// <b>Not a convenience, and not the same question <c>MeView</c> asks.</b> That
/// read resolves the caller in order to <i>describe</i> them, and answers with
/// nulls where it cannot — a bar that refused to draw because a login has no
/// staff record would be refusing the screen a person asked for. A write cannot
/// do that: there is no partial answer to *who is this request from*, so this
/// one refuses instead, and the two behaviours are why the mechanism is shared
/// and the decision is not.
/// </para>
/// <para>
/// The refusal is an <see cref="InvalidRequestException"/> rather than a
/// permission failure, because no grant would fix it. ADR 0041 makes
/// <c>invalid</c> client-facing, so the sentence below is the one a person
/// actually reads — which is why it names the remedy rather than the mechanism.
/// </para>
/// </remarks>
internal static class Requester
{
    /// <summary>The caller, as a staff member of this property.</summary>
    /// <param name="call">The call, for its scope and its services.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Their staff id.</returns>
    /// <exception cref="InvalidRequestException">
    /// The request has no user behind it, or the user is not one of this
    /// property's people.
    /// </exception>
    public static async Task<Guid> RequiredAsync(
        ModuleCall call, CancellationToken cancellationToken)
    {
        // A service caller has no person behind it. Nothing it could send would
        // make it one, which is why this is not a permission failure.
        if (call.Scope.UserId is not { } userId)
        {
            throw new InvalidRequestException(
                "This can only be done by somebody signed in.");
        }

        var resolved = await call.Service<IStaffDirectory>()
            .FindStaffIdAsync(call.Scope.PropertyId, userId, cancellationToken);

        // Known to Identity and unknown to Master Data — the founding
        // administrator's own condition — or known and no longer active, which
        // `Scoped` filters out. The three collapse here for the same reason they
        // collapse on the bar: the remedy is identical and the differences are
        // somebody's employment status.
        if (resolved is not StaffResolution.Resolved found)
        {
            throw new InvalidRequestException(
                "There is no staff record for the signed-in account, so there is "
                + "nobody for this to be from.");
        }

        return found.StaffId;
    }
}
