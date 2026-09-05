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
/// What a denied capability needs in order to reach the screen as a denial.
/// </summary>
/// <remarks>
/// <para>
/// <b>The envelope answers a denied capability with <c>Results.Forbid()</c></b>,
/// which asks ASP.NET's authentication stack to forbid — and an application
/// that registers no scheme gets <c>InvalidOperationException</c> instead: the
/// guard's own denial path arrives at a screen as <b>500</b>, telling a person
/// who simply lacks a permission that the system is broken.
/// </para>
/// <para>
/// Found by driving it in the wired round of 2026-09-05, and reported. It had
/// not been met before because the exemplar's proof stubbed the .NET side. The
/// scheme below <b>authenticates nobody</b> — the envelope has already
/// established the person from their token — and exists only so the framework
/// has somewhere to send a forbid.
/// </para>
/// <para>
/// The round's other envelope finding is closed at the source: the platform's
/// <c>755ee02</c> gave the envelope the interceptor's status table, so an
/// application's own refusals cross this hop as themselves. Jobs' mapping
/// middleware was deleted the day that landed rather than left to disagree with
/// it.
/// </para>
/// </remarks>
public static class ModuleRefusals
{
    private const string Scheme = "hotelos-module";

    /// <summary>Give the framework somewhere to send a forbid.</summary>
    public static IServiceCollection AddModuleRefusals(this IServiceCollection services)
    {
        services
            .AddAuthentication(Scheme)
            .AddScheme<AuthenticationSchemeOptions, NoScheme>(Scheme, _ => { });
        return services;
    }

    /// <summary>
    /// A token that does not verify, answered as a refusal rather than a fault.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The second half of the same envelope defect, and reported with it</b>
    /// (2026-09-05). <c>JwtCallerAuthenticator</c> <i>throws</i>
    /// <c>AuthenticationFailedException</c> for a token that is present and
    /// invalid — a wrong signature, a stale issuer — and the envelope's
    /// <c>catch (DomainException)</c> is around the <i>handler</i> only, not
    /// around authentication. So a bad token arrives at the bundle as
    /// <b>500</b>, where its own status table already says <c>401</c>.
    /// </para>
    /// <para>
    /// One exception, mapped to the status the platform itself chose for it.
    /// Deliberately not a general mapper: the envelope maps every other refusal
    /// now, and a second table beside it would be free to disagree.
    /// </para>
    /// </remarks>
    public static WebApplication UseModuleRefusals(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (AuthenticationFailedException)
            {
                // Nothing about why: an unauthenticated caller learns only that
                // they are not authenticated, which is the envelope's own rule
                // for the token that was never sent.
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
        });

        return app;
    }

    /// <summary>Authenticates nobody, and turns a forbid into a 403.</summary>
    private sealed class NoScheme(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}
