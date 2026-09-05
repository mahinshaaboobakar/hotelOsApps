using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Queries;
using HotelOS.Platform;
using Microsoft.Extensions.DependencyInjection;

using static HotelOS.Platform.ModuleEnvelope;

namespace HotelOS.Jobs.Module;

/// <summary>
/// <c>job.read</c> — everything a Jobs screen shows.
/// </summary>
/// <remarks>
/// <para>
/// Nine methods behind one capability, because they are one permission: a
/// person who may see the board may see a job, the catalogue and the settings
/// they are governed by. Splitting them would invent permissions the manifest
/// never asked for and an administrator never decided.
/// </para>
/// <para>
/// The envelope has already checked the token and that this person holds
/// <c>job.read</c> at this property. What remains is per-object, and the query
/// layer does it with the scope it is handed.
/// </para>
/// </remarks>
public static class ReadCapability
{
    /// <summary>Serve one read.</summary>
    public static async Task<object?> HandleAsync(
        IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var scope = request.Scope;

        return request.Method switch
        {
            "today" => await services.GetRequiredService<BoardProjection>()
                .TodayAsync(scope, body.OptionalText("department"), cancellationToken),

            "board" => await services.GetRequiredService<BoardProjection>()
                .PageAsync(scope, Filter(request), cancellationToken),

            "scheduled" => await services.GetRequiredService<BoardProjection>()
                .ScheduledAsync(scope, cancellationToken),

            "job" => await services.GetRequiredService<JobProjection>()
                .DetailAsync(scope, body.Id("id"), cancellationToken),

            "catalogue" => await services.GetRequiredService<SettingsProjection>()
                .CatalogueAsync(scope, cancellationToken),

            "settings" => await services.GetRequiredService<SettingsProjection>()
                .SettingsAsync(scope, cancellationToken),

            "live" => await services.GetRequiredService<LiveProjection>()
                .LiveAsync(scope, cancellationToken),

            "jobsNow" => await services.GetRequiredService<LiveProjection>()
                .NowAsync(scope, body.OptionalText("department"), cancellationToken),

            // The dock widgets, one read each — a widget answers one question
            // whole, and one fed from a screen's payload would show whatever
            // that screen happened to be holding (SHELL-Q35).
            "widgetBoard" => await services.GetRequiredService<WidgetProjection>()
                .BoardAsync(scope, cancellationToken),

            "widgetBlocked" => await services.GetRequiredService<WidgetProjection>()
                .BlockedAsync(scope, cancellationToken),

            "me" => await MeAsync(services, request, cancellationToken),

            _ => throw new InvalidRequestException($"job.read has no method '{request.Method}'"),
        };
    }

    /// <summary>
    /// The board's filters, as <see cref="JobFilter"/> — the one contract the
    /// screens and the query layer share.
    /// </summary>
    /// <remarks>
    /// <b>Assignee is "mine", resolved here from the caller</b> rather than
    /// taken from the body: a screen that could name whose jobs to show would
    /// let a person filter to somebody else's by editing a request, and the
    /// board is not a place to learn who holds what.
    /// </remarks>
    private static JobFilter Filter(ModuleRequest request)
    {
        var body = request.Body;
        return new JobFilter(
            body.OptionalText("department"),
            body.Texts("statuses"),
            body.Flag("scheduledOnly"),
            body.Flag("mine") ? request.Caller.UserId : null,
            body.Number("pageSize", JobQueries.DefaultPageSize),
            body.Number("page"))
        {
            RaisedKind = body.OptionalText("raisedKind"),
            RestrictedOnly = body.Flag("restricted"),
        };
    }

    /// <summary>
    /// Who is signed in, as the service knows them.
    /// </summary>
    /// <remarks>
    /// The caller is established — the envelope validated their token — so the
    /// person's id is a fact. Their <i>name</i> is Workforce's and there is no
    /// client, so the header says where they are rather than inventing who they
    /// are: the audit's finding about a fabricated operator name is why.
    /// </remarks>
    private static async Task<object?> MeAsync(
        IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var directory = services.GetRequiredService<IPropertyDirectory>();
        var code = await directory.FindPropertyCodeAsync(request.Scope.PropertyId, cancellationToken);
        return new ModuleViews.OperatorView("Signed in", code is null ? "this property" : code.ToUpperInvariant());
    }
}
