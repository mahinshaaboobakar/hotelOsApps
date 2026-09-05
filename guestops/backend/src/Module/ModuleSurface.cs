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
/// The composition root and nothing else: it names each capability and
/// dispatches on the method. Every projection and every command lives in its
/// own file beside this one, because a screen's view model is its own subject
/// and a dispatcher that also built one would be the file everything accretes
/// onto (ADR 0038, ADR 0042).
/// </para>
/// <para>
/// <b>Three capabilities, because the bundles now ask for three.</b> Reads go
/// under <c>reservation.read</c>; taking a walk-in is <c>stay.create</c>;
/// cancelling is <c>stay.override</c>, which is the same permission that makes
/// an override and clears a disagreement (GUEST-Q3). Each is a permission the
/// manifest declares and screens actually exercise — a fourth mapped
/// speculatively would be the declared-and-never-used defect that
/// <c>CORE-Q13</c> is named after.
/// </para>
/// <para>
/// <b>The split is by what a caller is allowed to do, not by what the code
/// finds convenient.</b> A read and a write of the same aggregate sit in
/// different maps here so that a property granting only <c>reservation.read</c>
/// gets a desk that lists and refuses, rather than one that lists and quietly
/// cancels.
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
    /// <param name="app">
    /// The door these routes belong on — <c>IEndpointRouteBuilder</c> rather
    /// than <c>WebApplication</c>, because `SHELL-Q40` §4·3 binds each surface
    /// to its own listener's pipeline. Taking the whole application would map
    /// the bundle route onto <b>both</b> doors, which is the mutual-TLS one
    /// answering a route only the Shell's plaintext hop should reach.
    /// </param>
    /// <remarks>
    /// <b>The provider comes from the request, not from a scope opened here</b>
    /// — <c>SHELL-Q38</c>. This used to build its own scope from
    /// <c>IServiceScopeFactory</c>, which worked but duplicated what the
    /// envelope now hands over; taking <c>request.Services</c> means the
    /// projections resolve in the same scope the guards authenticated in, and
    /// there is one answer to "which container is this call in" rather than
    /// two.
    /// </remarks>
    public static void MapGuestOpsModule(this IEndpointRouteBuilder app)
    {
        app.MapModuleCapability(
            Application.Abstractions.Permissions.ReservationRead,
            (request, cancellationToken) =>
                ReadAsync(request.Services, request, cancellationToken));

        app.MapModuleCapability(
            Application.Abstractions.Permissions.StayCreate,
            (request, cancellationToken) =>
                CreateAsync(request.Services, request, cancellationToken));

        app.MapModuleCapability(
            Application.Abstractions.Permissions.StayOverride,
            (request, cancellationToken) =>
                OverrideAsync(request.Services, request, cancellationToken));
    }

    /// <summary>The reads — every list, every page, every plan.</summary>
    /// <remarks>
    /// <b>A cancellation <i>plan</i> is a read.</b> It computes penalties and
    /// names consequences and writes nothing, so a person who may look at a
    /// booking may see what cancelling it would do. Only the button needs
    /// <c>stay.override</c>.
    /// </remarks>
    private static Task<object?> ReadAsync(
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

            "bookings" => services.GetRequiredService<BookingsView>()
                .AnswerAsync(request.Scope, Page(request.Body), cancellationToken),

            "booking" => services.GetRequiredService<BookingView>()
                .AnswerAsync(request.Scope, Booking(request.Body), cancellationToken),

            "cancelPlan" => services.GetRequiredService<CancelPlanView>()
                .AnswerAsync(request.Scope, Booking(request.Body), cancellationToken),

            "availability" => Availability(services, request, cancellationToken),

            "activity" => services.GetRequiredService<ActivityView>()
                .AnswerAsync(request.Scope, Stay(request.Body), cancellationToken),

            "requests" => services.GetRequiredService<RequestsView>()
                .AnswerAsync(request.Scope, Stay(request.Body), cancellationToken),

            "payment" => services.GetRequiredService<PaymentView>()
                .AnswerAsync(request.Scope, Stay(request.Body), cancellationToken),

            // The only one of the stay's tabs that needs no stay: what it
            // answers is whether Room Care is here at all, which is a fact
            // about the property.
            "servicing" => services.GetRequiredService<ServicingView>()
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

    /// <summary>Taking a booking — the walk-in, for now.</summary>
    private static Task<object?> CreateAsync(
        IServiceProvider services,
        ModuleEnvelope.ModuleRequest request,
        CancellationToken cancellationToken)
        => request.Method switch
        {
            "walkIn" => services.GetRequiredService<WalkInCommand>()
                .RunAsync(request.Scope, request.Body, cancellationToken),

            _ => throw new InvalidRequestException(
                $"'{request.Method}' is not a method this application serves"),
        };

    /// <summary>The lifecycle writes a person makes over a source.</summary>
    private static Task<object?> OverrideAsync(
        IServiceProvider services,
        ModuleEnvelope.ModuleRequest request,
        CancellationToken cancellationToken)
        => request.Method switch
        {
            "cancel" => services.GetRequiredService<CancelCommand>()
                .RunAsync(request.Scope, request.Body, cancellationToken),

            _ => throw new InvalidRequestException(
                $"'{request.Method}' is not a method this application serves"),
        };

    /// <summary>The dates availability was asked about.</summary>
    /// <remarks>
    /// <b>Refused rather than defaulted.</b> A missing arrival could be read as
    /// *today*, and the answer would then be a availability for dates the desk
    /// never asked about — shown in the column a guest is quoted from. The
    /// screen always knows the dates it is asking for; a request without them
    /// is a caller that has gone wrong.
    /// </remarks>
    private static Task<object?> Availability(
        IServiceProvider services,
        ModuleEnvelope.ModuleRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Body is not { ValueKind: JsonValueKind.Object } body)
        {
            throw new InvalidRequestException("availability needs an arrival and a departure");
        }

        var from = Date(body, "arrive")
            ?? throw new InvalidRequestException("availability needs an arrival date");

        var to = Date(body, "depart")
            ?? throw new InvalidRequestException("availability needs a departure date");

        return services.GetRequiredService<AvailabilityView>()
            .AnswerAsync(request.Scope, from, to, cancellationToken);
    }

    /// <summary>Which stay the bundle is asking about.</summary>
    private static Guid Stay(JsonElement? body)
    {
        if (body is not { ValueKind: JsonValueKind.Object } request
            || !request.TryGetProperty("stayId", out var value)
            || value.ValueKind != JsonValueKind.String
            || !Guid.TryParse(value.GetString(), out var id))
        {
            throw new InvalidRequestException("this method needs a stay");
        }

        return id;
    }

    /// <summary>Which booking the bundle is asking about.</summary>
    private static Guid Booking(JsonElement? body)
    {
        if (body is not { ValueKind: JsonValueKind.Object } request
            || !request.TryGetProperty("bookingId", out var value)
            || value.ValueKind != JsonValueKind.String
            || !Guid.TryParse(value.GetString(), out var id))
        {
            throw new InvalidRequestException("this method needs a booking");
        }

        return id;
    }

    /// <summary>A date from the bundle's body, where it sent one.</summary>
    private static DateOnly? Date(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && DateOnly.TryParse(value.GetString(), out var date)
                ? date
                : null;

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
