using HotelOS.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using HotelOS.Workforce.Module.Views;

namespace HotelOS.Workforce.Module;

/// <summary>
/// The surface this application serves to its own packaged UI — design page
/// 63 §3, and the one line per capability the envelope asks for.
/// </summary>
/// <remarks>
/// <para>
/// A composition root and nothing else: it names the capabilities, routes a
/// method to the view that owns it, and translates a domain refusal into the
/// status the platform's own mapping reads. No projection is written here.
/// </para>
/// <para>
/// <b>Every capability the screens call, and no more.</b> The manifest declares
/// twelve permissions; the module maps the ones a screen actually invokes, so a
/// capability nobody calls has no route to be reached by.
/// </para>
/// </remarks>
public static class WorkforceModule
{
    /// <summary>Serve this application's UI.</summary>
    /// <param name="app">The host.</param>
    public static void MapWorkforceModule(this WebApplication app)
    {
        Capability(app, "roster.read", ReadViews.Answer);
        // Teams are written under the same permission as postings — the
        // manifest declares no team.* and should not: forming a crew and
        // posting somebody are one administrative decision, and a second
        // permission would ask a property twice about one thing. Routing
        // by method is the composition root's job, which is why it is here
        // and not inside either view.
        Capability(app, "posting.assign", (call, token) =>
            call.Method is "post" or "end"
                ? PeopleView.Write(call, token)
                : TeamsView.Write(call, token));
        Capability(app, "roster.plan", RotaView.Write);
        Capability(app, "leave.request", LeaveView.Request);
        Capability(app, "leave.approve", LeaveView.Decide);
        Capability(app, "duty.assign", DutyView.Write);
        Capability(app, "swap.propose", LeaveView.Propose);
        Capability(app, "swap.approve", LeaveView.DecideSwap);
        Capability(app, "attendance.record", AttendanceView.Record);
        Capability(app, "attendance.amend", AttendanceView.Amend);
        Capability(app, "roster.configure", PolicyView.Write);
    }

    /// <summary>
    /// One capability, in a scope of its own, with domain refusals translated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The scope.</b> The envelope passes no service provider, so the root
    /// is captured and a scope opened per call — see <see cref="ModuleCall"/>
    /// for why that is reported rather than merely done.
    /// </para>
    /// <para>
    /// <b>The translation.</b> gRPC has <c>DomainExceptionInterceptor</c>; this
    /// hop has nothing equivalent, so an <c>InvalidRequestException</c> would
    /// leave as a 500 and reach the bundle as <c>internal</c> — a kind ADR 0041
    /// forbids showing a person. The mapping below is the same vocabulary that
    /// interceptor uses, against the status codes
    /// <c>src-tauri/src/commands/module_call.rs</c> actually reads.
    /// </para>
    /// </remarks>
    private static void Capability(
        WebApplication app,
        string capability,
        Func<ModuleCall, CancellationToken, Task<object?>> answer)
        => app.MapModuleCapability(capability, async (request, cancellationToken) =>
        {
            await using var scope = app.Services.CreateAsyncScope();

            var call = new ModuleCall(
                request.Method, request.Body, request.Scope, scope.ServiceProvider);

            return await answer(call, cancellationToken);
        })
        .AddEndpointFilter(async (context, next) =>
        {
            try
            {
                return await next(context);
            }
            catch (DomainException refusal)
            {
                return Refusal(refusal);
            }
        });

    /// <summary>
    /// The status a refusal leaves as, chosen for what the bundle is told.
    /// </summary>
    /// <remarks>
    /// <c>module_call.rs</c> reads 400 as <c>invalid</c>, and 404/409/422 as
    /// <c>rejected</c>. Both are client-facing under ADR 0041; everything else
    /// is a diagnostic and must not carry this application's words across into
    /// a third party's realm.
    /// </remarks>
    private static IResult Refusal(DomainException refusal) => refusal switch
    {
        InvalidRequestException => Results.BadRequest(),
        NotFoundException => Results.NotFound(),
        ConcurrencyException => Results.Conflict(),
        InUseException => Results.Conflict(),
        PermissionDeniedException => Results.Forbid(),
        _ => Results.UnprocessableEntity(),
    };
}
