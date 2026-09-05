using HotelOS.Platform;
using Microsoft.AspNetCore.Routing;
using HotelOS.Workforce.Module.Views;

namespace HotelOS.Workforce.Module;

/// <summary>
/// The surface this application serves to its own packaged UI — design page
/// 63 §3, and the one line per capability the envelope asks for.
/// </summary>
/// <remarks>
/// <para>
/// A composition root and nothing else: it names the capabilities and routes a
/// method to the view that owns it. No projection is written here.
/// </para>
/// <para>
/// <b>Every capability the screens call, and no more.</b> The manifest declares
/// twelve permissions; the module maps the ones a screen actually invokes, so a
/// capability nobody calls has no route to be reached by.
/// </para>
/// <para>
/// <b>It carried two workarounds until <c>SHELL-Q40</c>, and both are gone.</b>
/// It opened a DI scope per call, because the envelope passed no service
/// provider and resolving a scoped <c>DbContext</c> from the root works on the
/// desk it was written at; and it mapped <c>DomainException</c> to a status
/// itself, because this hop had no equivalent of the gRPC interceptor and a
/// refusal would otherwise have left as a 500. The envelope now hands over the
/// request-scoped provider and maps refusals with their message intact, so both
/// are the platform's again — and deleting them is the point, because a
/// workaround left standing after its cause is fixed becomes a second
/// implementation nobody knows to look at.
/// </para>
/// </remarks>
public static class WorkforceModule
{
    /// <summary>Serve this application's UI, on the door the Kernel dials.</summary>
    /// <param name="door">The loopback endpoint builder — <c>SHELL-Q40</c> §4.</param>
    public static void MapWorkforceModule(this IEndpointRouteBuilder door)
    {
        Capability(door, "roster.read", ReadViews.Answer);

        // Teams are written under the same permission as postings — the
        // manifest declares no team.* and should not: forming a crew and
        // posting somebody are one administrative decision, and a second
        // permission would ask a property twice about one thing. Routing by
        // method is the composition root's job, which is why it is here and not
        // inside either view.
        Capability(door, "posting.assign", (call, token) =>
            call.Method is "post" or "end"
                ? PeopleView.Write(call, token)
                : TeamsView.Write(call, token));

        Capability(door, "roster.plan", RotaView.Write);
        Capability(door, "leave.request", LeaveView.Request);
        Capability(door, "leave.approve", LeaveView.Decide);
        Capability(door, "duty.assign", DutyView.Write);
        Capability(door, "swap.propose", LeaveView.Propose);
        Capability(door, "swap.approve", LeaveView.DecideSwap);
        Capability(door, "attendance.record", AttendanceView.Record);
        Capability(door, "attendance.amend", AttendanceView.Amend);
        Capability(door, "roster.configure", PolicyView.Write);
    }

    /// <summary>One capability, served in the request's own scope.</summary>
    /// <remarks>
    /// The handler is handed the envelope's request-scoped provider directly.
    /// There is nothing to arrange: a scoped <c>DbContext</c> resolves from the
    /// scope the request already has, and a refusal a view raises reaches the
    /// bundle carrying the sentence the service wrote.
    /// </remarks>
    private static void Capability(
        IEndpointRouteBuilder door,
        string capability,
        Func<ModuleCall, CancellationToken, Task<object?>> answer)
        => door.MapModuleCapability(capability, (request, cancellationToken) => answer(
            new ModuleCall(request.Method, request.Body, request.Scope, request.Services),
            cancellationToken));
}
