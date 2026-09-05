using System.Text.Json;
using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Catalogue;
using HotelOS.Jobs.Application.Settings;
using HotelOS.Platform;
using Microsoft.Extensions.DependencyInjection;

using static HotelOS.Platform.ModuleEnvelope;

namespace HotelOS.Jobs.Module;

/// <summary>
/// The two capabilities the jobs manager holds — <c>job.configure</c> (what
/// this property has decided) and <c>job.curate</c> (the catalogue every
/// property shares).
/// </summary>
/// <remarks>
/// They are separate because their blast radius is: a closing hour is one
/// property's, and a catalogue item is the organisation's. The manifest asked
/// for both and an administrator grants them together today, which does not
/// make them one permission.
/// </remarks>
public static class ConfigureCapabilities
{
    /// <summary><c>job.configure</c> — the six settings tabs' saves.</summary>
    public static async Task<object?> ConfigureAsync(
        IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var scope = request.Scope;

        switch (request.Method)
        {
            case "savePolicy":
                var policy = await services.GetRequiredService<ConcernPolicyService>().SaveAsync(
                    scope, Policy(body), cancellationToken);
                return new { id = policy.Id.ToString(), version = policy.Version };

            case "deletePolicy":
                await services.GetRequiredService<ConcernPolicyService>()
                    .DeleteAsync(scope, body.Id("id"), cancellationToken);
                return new { deleted = true };

            case "saveSubscriptions":
                var saved = await services.GetRequiredService<ClosingHoldService>().SaveSubscriptionsAsync(
                    scope, Subscriptions(body), cancellationToken);
                return new { count = saved.Count };

            case "saveClosing":
                var closing = await services.GetRequiredService<ClosingHoldService>().SaveClosingAsync(
                    scope,
                    new ClosingCommand(
                        body.OptionalText("department"),
                        body.Number("autoCloseHours", 4),
                        body.Flag("ratingOnClose", true)),
                    cancellationToken);
                return new { id = closing.Id.ToString() };

            case "saveHold":
                var hold = await services.GetRequiredService<ClosingHoldService>().SaveHoldAsync(
                    scope,
                    new HoldPolicyCommand(
                        body.Number("maxHoldDays", 30),
                        body.Number("warnDaysBefore", 1),
                        body.OptionalText("warnRole") ?? Domain.LadderRole.Supervisor,
                        body.Flag("warnAssigneeOnDay", true)),
                    cancellationToken);
                return new { id = hold.Id.ToString() };

            case "savePresence":
                var presence = await services.GetRequiredService<PresenceService>().SaveAsync(
                    scope,
                    new PresenceCommand(
                        body.Text("department"), body.Flag("enabled", true), body.Flag("followShifts", true)),
                    cancellationToken);
                return new { id = presence.Id.ToString() };

            case "saveHours":
                var hours = await services.GetRequiredService<PresenceService>().SaveHoursAsync(
                    scope,
                    new ServiceHoursCommand(
                        body.OptionalText("department"),
                        TimeOnly.Parse(body.Text("from")),
                        TimeOnly.Parse(body.Text("to"))),
                    cancellationToken);
                return new { id = hours.Id.ToString() };

            default:
                throw new InvalidRequestException($"job.configure has no method '{request.Method}'");
        }
    }

    /// <summary><c>job.curate</c> — the catalogue, and one property's use of it.</summary>
    /// <remarks>
    /// <b>The organisation is asked for, not accepted.</b> A module call names a
    /// property and never an organisation — the envelope's scope carries
    /// <c>property_id</c> alone — and the catalogue is the organisation's. So
    /// Master Data is asked which organisation this property is in; a bundle
    /// that could name one would be choosing whose catalogue it edits.
    /// </remarks>
    public static async Task<object?> CurateAsync(
        IServiceProvider services, ModuleRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var organization = await services.GetRequiredService<IPropertyDirectory>()
            .FindOrganizationAsync(request.Scope.PropertyId, cancellationToken)
            ?? throw new InvalidRequestException(
                "Master Data does not say which organisation this property belongs to, and the "
                + "catalogue is the organisation's");
        var scope = request.Scope with { OrganizationId = organization };
        var catalogue = services.GetRequiredService<CatalogueService>();

        switch (request.Method)
        {
            case "saveCategory":
                var category = await catalogue.SaveCategoryAsync(
                    scope,
                    new CategoryCommand
                    {
                        Id = body.OptionalId("id"),
                        ExpectedVersion = body.OptionalId("id") is null ? null : body.Version(),
                        Code = body.Text("code"),
                        Name = body.Text("name"),
                        DepartmentCode = body.Text("department"),
                        Active = body.Flag("active", true),
                    },
                    cancellationToken);
                return new { id = category.Id.ToString(), version = category.Version };

            case "saveItem":
                var item = await catalogue.SaveItemAsync(
                    scope,
                    new ItemCommand
                    {
                        Id = body.OptionalId("id"),
                        ExpectedVersion = body.OptionalId("id") is null ? null : body.Version(),
                        CategoryId = body.Id("categoryId"),
                        Code = body.Text("code"),
                        Name = body.Text("name"),
                        DefaultPriority = body.OptionalText("defaultPriority") ?? Domain.Priority.P3,
                        DueWithinMinutes = body.Number("dueWithinMinutes") is var minutes and > 0 ? minutes : null,
                        RestrictedByDefault = body.Flag("restricted"),
                        GuestRequestable = body.Flag("guestRequestable", true),
                        Active = body.Flag("active", true),
                        Aliases = body.Texts("aliases"),
                    },
                    cancellationToken);
                return new { id = item.Id.ToString(), version = item.Version };

            case "addResolution":
                var resolution = await catalogue.AddResolutionAsync(
                    scope,
                    new ResolutionCommand
                    {
                        CategoryId = body.OptionalId("categoryId"),
                        ItemId = body.OptionalId("itemId"),
                        Name = body.Text("name"),
                        NoteRequired = body.Flag("noteRequired"),
                    },
                    cancellationToken);
                return new { id = resolution.Id.ToString() };

            case "saveItemPolicy":
                var used = await services.GetRequiredService<PropertyCatalogueService>().SaveAsync(
                    scope,
                    new ItemPolicyCommand
                    {
                        ItemId = body.Id("itemId"),
                        ActiveHere = body.Flag("activeHere", true),
                        DisplayName = body.OptionalText("displayName"),
                        DefaultPriority = body.OptionalText("defaultPriority"),
                        DueWithinMinutes = body.Number("dueWithinMinutes") is var within and > 0 ? within : null,
                        ConcernPolicyId = body.OptionalId("concernPolicyId"),
                    },
                    cancellationToken);
                return new { id = used.Id.ToString(), version = used.Version };

            default:
                throw new InvalidRequestException($"job.curate has no method '{request.Method}'");
        }
    }

    /// <summary>A policy with its rules and its ladder — the three-step flow of page 02 frames 8 to 10.</summary>
    private static ConcernPolicyCommand Policy(JsonElement? body) => new()
    {
        Id = body.OptionalId("id"),
        ExpectedVersion = body.OptionalId("id") is null ? null : body.Version(),
        Name = body.Text("name"),
        DepartmentCode = body.OptionalText("department"),
        CategoryId = body.OptionalId("categoryId"),
        ItemId = body.OptionalId("itemId"),
        UntriagedStuckMinutes = body.Number("untriagedStuckMinutes", 15),
        Rules = Rules(body),
        Ladder = Ladder(body),
    };

    private static IReadOnlyList<RuleCommand> Rules(JsonElement? body) =>
        Each(body, "rules").Select(rule => new RuleCommand(
            rule.Text("priority"),
            rule.Number("dueWithinMinutes") is var due and > 0 ? due : null,
            rule.Number("atRiskPercent", 75),
            rule.Number("notAcceptedMinutes") is var accepted and > 0 ? accepted : null,
            rule.Number("noSessionMinutes") is var session and > 0 ? session : null,
            rule.Flag("managerAtRisk"),
            rule.Flag("runsOutsidePresence"))).ToList();

    private static IReadOnlyList<LadderStepCommand> Ladder(JsonElement? body) =>
        Each(body, "ladder").Select(step => new LadderStepCommand(
            step.Text("priority"),
            step.Number("stepNo"),
            step.Text("role"),
            step.Text("trigger"),
            step.Number("delayMinutes"))).ToList();

    private static IReadOnlyList<SubscriptionCommand> Subscriptions(JsonElement? body) =>
        Each(body, "subscriptions").Select(row => new SubscriptionCommand(
            row.Text("role"),
            row.Text("concern"),
            row.OptionalText("onlyPriority"),
            row.OptionalText("department"),
            row.Number("repeatMinutes") is var repeat and > 0 ? repeat : null)).ToList();

    /// <summary>The objects in one array of the body, each readable by name.</summary>
    private static IEnumerable<JsonElement?> Each(JsonElement? body, string name)
    {
        if (body is not { ValueKind: JsonValueKind.Object } document
            || !document.TryGetProperty(name, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object).Select(item => (JsonElement?)item);
    }
}
