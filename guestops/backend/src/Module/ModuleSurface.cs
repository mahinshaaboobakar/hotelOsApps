using System.Text.Json;
using HotelOS.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// What this application's own bundles may ask it — <c>SHELL-Q37</c>.
/// </summary>
/// <remarks>
/// <para>
/// The composition root and nothing else: it names the capability and
/// dispatches on the method. Every projection lives in its own file
/// beside this one, because a screen's view model is its own subject and a
/// dispatcher that also built one would be the file everything accretes onto
/// (ADR 0038, ADR 0042).
/// </para>
/// <para>
/// <b>One capability, because the bundles ask for one.</b> Every screen and
/// every widget calls <c>reservation.read</c> — the manifest's <i>"show the
/// day's arrivals, the stay page and the guest record"</i>. A second
/// <c>MapModuleCapability</c> would be a permission this application does not
/// yet read anything under, which is the declared-and-never-used defect
/// <c>CORE-Q13</c> is named after.
/// </para>
/// <para>
/// <b>The token check and the capability guard are not here.</b> They are
/// inside <see cref="ModuleEnvelope.MapModuleCapability"/>, so an application
/// author cannot forget them — a UI that reached a capability by knowing its
/// path would be the failure with no error anywhere.
/// </para>
/// </remarks>
public static class ModuleSurface
{
    /// <summary>Serve this application's bundles.</summary>
    /// <param name="app">The application being built.</param>
    /// <remarks>
    /// <b>The provider comes from the request, not from a scope opened here</b>
    /// — <c>SHELL-Q38</c>. This used to build its own scope from
    /// <c>IServiceScopeFactory</c>, which worked but duplicated what the
    /// envelope now hands over; taking <c>request.Services</c> means the
    /// projections resolve in the same scope the guards authenticated in, and
    /// there is one answer to "which container is this call in" rather than
    /// two.
    /// </remarks>
    public static void MapGuestOpsModule(this WebApplication app)
        => app.MapModuleCapability(
            Application.Abstractions.Permissions.ReservationRead,
            (request, cancellationToken) =>
                AnswerAsync(request.Services, request, cancellationToken));

    /// <summary>The method, to the projection that answers it.</summary>
    /// <remarks>
    /// <b>An unknown method is refused by name.</b> Returning null would render
    /// as an empty screen, which reads to a receptionist as a quiet hotel
    /// rather than as a bundle asking for something this build does not serve.
    /// </remarks>
    private static Task<object?> AnswerAsync(
        IServiceProvider services,
        ModuleEnvelope.ModuleRequest request,
        CancellationToken cancellationToken)
        => request.Method switch
        {
            "today" => services.GetRequiredService<TodayView>()
                .AnswerAsync(request.Scope, Page(request.Body), cancellationToken),

            "attention" => services.GetRequiredService<AttentionView>()
                .AnswerAsync(request.Scope, cancellationToken),

            "occupancy" => services.GetRequiredService<OccupancyView>()
                .AnswerAsync(request.Scope, cancellationToken),

            "feed" => services.GetRequiredService<FeedView>()
                .AnswerAsync(request.Scope, cancellationToken),

            "mix" => services.GetRequiredService<MixView>()
                .AnswerAsync(request.Scope, cancellationToken),

            "watchlist" => services.GetRequiredService<WatchlistView>()
                .AnswerAsync(request.Scope, cancellationToken),

            // `stay` is declared by the bundle and not served here. It needs the
            // stay's id from the body and a projection of the whole page —
            // banner, timeline, six tabs — which is its own round. The bundle
            // falls back to its recorded facts and says so on screen, which is
            // the honest state; answering with a half-built page would put a
            // stay in front of a receptionist with pieces silently missing.
            _ => throw new InvalidRequestException(
                $"'{request.Method}' is not a method this application serves"),
        };

    /// <summary>The page the bundle asked for, clamped.</summary>
    /// <remarks>
    /// <para>
    /// <b>The body is the application's own JSON</b> — the platform defines the
    /// envelope and nothing inside it — so this reads two numbers and ignores
    /// anything else the bundle sends. A body that is absent, empty, or shaped
    /// differently is a caller asking for the beginning of the list, which is
    /// what every unpaged caller is.
    /// </para>
    /// <para>
    /// Clamped through <see cref="Paging.Of"/> rather than here, so the module
    /// route and the gRPC surface cannot disagree about what page 0 with size 0
    /// means.
    /// </para>
    /// </remarks>
    private static Paging.Window Page(JsonElement? body)
    {
        if (body is not { ValueKind: JsonValueKind.Object } element)
        {
            return Paging.Of(null);
        }

        return Paging.Of(new HotelOS.Contracts.Common.V1.PagedRequest
        {
            Page = Number(element, "page"),
            PageSize = Number(element, "pageSize"),
        });
    }

    /// <summary>One number from the bundle's body, or zero.</summary>
    /// <remarks>
    /// Zero for anything that is not a number, including a JSON string: proto3
    /// cannot tell an unset int from a zero one either, so zero already means
    /// "no preference" everywhere downstream and a refusal here would be a
    /// stricter contract than the wire's own.
    /// </remarks>
    private static int Number(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var parsed)
                ? parsed
                : 0;
}
