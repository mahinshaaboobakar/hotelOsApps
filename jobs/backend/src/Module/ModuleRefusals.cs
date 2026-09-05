using System.Text.Encodings.Web;
using HotelOS.Platform;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelOS.Jobs.Module;

/// <summary>
/// A refusal reaching the screen as a refusal — the status and the sentence the
/// service itself gave.
/// </summary>
/// <remarks>
/// <para>
/// The gRPC surface has this already: <c>DomainExceptionInterceptor</c> turns
/// the application's own failures into a status and a message a caller can act
/// on. The module envelope carries no equivalent, so without this every
/// refusal — a missing summary, a stale version, a job at another property —
/// arrives at the bundle as <c>500</c>, which a screen can only render as
/// "something went wrong".
/// </para>
/// <para>
/// <b>Reported to the platform as well as fixed here</b> (2026-09-05): the
/// envelope's own documentation says the status vocabulary is the platform's,
/// and a vocabulary each application spells for itself is one that will differ
/// between them. This mapping is deliberately the interceptor's, verb for verb,
/// so that adopting a platform one later changes nothing a screen sees.
/// </para>
/// <para>
/// <b>An unexpected failure says nothing.</b> Its message could name a table, a
/// column or a connection string, and the person reading it is on a shift, not
/// on the team. It is logged whole and answered as <c>500</c> with no body.
/// </para>
/// </remarks>
public static class ModuleRefusals
{
    /// <summary>
    /// What <c>Results.Forbid()</c> needs in order to be a 403.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A defect in the envelope, worked around here and reported</b>
    /// (2026-09-05). <c>MapModuleCapability</c> answers a denied capability with
    /// <c>Results.Forbid()</c>, which asks ASP.NET's authentication stack to
    /// forbid — and an application that registers none (neither the SDK nor
    /// <c>hello-hotel</c> does) gets <c>InvalidOperationException</c> instead:
    /// the guard's denial path arrives at the screen as <b>500</b>, and a
    /// person who lacks a permission is told the system is broken.
    /// </para>
    /// <para>
    /// It had not been met before because the exemplar's proof stubbed the .NET
    /// side; this round drove it. The scheme below authenticates nobody — the
    /// envelope has already established the person from the token — and exists
    /// only so the framework has somewhere to send a forbid.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddModuleRefusals(this IServiceCollection services)
    {
        services
            .AddAuthentication(Scheme)
            .AddScheme<AuthenticationSchemeOptions, NoScheme>(Scheme, _ => { });
        return services;
    }

    private const string Scheme = "hotelos-module";

    /// <summary>Authenticates nobody, and turns a forbid into a 403.</summary>
    private sealed class NoScheme(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }

    /// <summary>Answer a module call's failure the way the gRPC surface answers one.</summary>
    public static void UseModuleRefusals(this WebApplication app) =>
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/module"))
            {
                await next(context);
                return;
            }

            try
            {
                await next(context);
            }
            catch (DomainException refusal)
            {
                var status = refusal switch
                {
                    NotFoundException => StatusCodes.Status404NotFound,
                    PermissionDeniedException => StatusCodes.Status403Forbidden,
                    ConcurrencyException => StatusCodes.Status409Conflict,
                    InUseException => StatusCodes.Status409Conflict,
                    AuthenticationFailedException => StatusCodes.Status401Unauthorized,
                    _ => StatusCodes.Status400BadRequest,
                };

                context.Response.StatusCode = status;

                // A denial says "permission denied" and never which permission
                // or which resource — the gRPC interceptor withholds both for
                // the same reason, that they are what an attacker probes for.
                // Every other refusal is the service's own sentence, which is
                // what a screen shows the person who has to act on it.
                await context.Response.WriteAsJsonAsync(new
                {
                    refused = refusal switch
                    {
                        PermissionDeniedException => "permission denied",
                        AuthenticationFailedException => "authentication failed",
                        _ => refusal.Message,
                    },
                });
            }
            catch (Exception failure)
            {
                context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(ModuleRefusals))
                    .LogError(failure, "a module call failed: {Path}", context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        });
}
