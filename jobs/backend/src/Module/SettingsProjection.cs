using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

using static HotelOS.Jobs.Module.ModuleSettingsViews;

namespace HotelOS.Jobs.Module;

/// <summary>
/// The catalogue screen and the six settings tabs — mockup page 02.
/// </summary>
/// <remarks>
/// One read serves the whole settings screen. The tabs are one subject — what
/// this property has decided — and six calls that could disagree with each
/// other by a second is the shape a person would notice and nobody could
/// reproduce.
/// </remarks>
public sealed class SettingsProjection(JobsDbContext db, JobQueries queries, TimeProvider clock)
{
    /// <summary>The catalogue as this property sees it — frame 7.</summary>
    public async Task<CatalogueView> CatalogueAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var rows = await queries.CatalogueAsync(scope, cancellationToken);
        var off = await db.ItemPolicies
            .Where(p => p.PropertyId == scope.PropertyId && !p.ActiveHere)
            .Select(p => p.ItemId)
            .ToListAsync(cancellationToken);

        var categories = rows.Categories.Select(c => new CategoryView(
            c.Id.ToString(),
            c.Name,
            c.DepartmentCode,
            rows.Items.Count(i => i.CategoryId == c.Id),
            true)).ToList();

        var items = rows.Items.Select(i => new ItemView(
            i.Id.ToString(),
            i.CategoryId.ToString(),
            i.Name,
            rows.Categories.FirstOrDefault(c => c.Id == i.CategoryId)?.DepartmentCode ?? string.Empty,
            i.DefaultPriority,
            i.DueWithinMinutes,
            i.RestrictedByDefault,
            rows.Aliases.Where(a => a.ItemId == i.Id).Select(a => a.Alias).ToList(),
            [new ItemPropertyView("this property", !off.Contains(i.Id))],
            rows.Resolutions
                .Where(r => r.CategoryId == i.CategoryId || r.ItemId == i.Id || (r.CategoryId is null && r.ItemId is null))
                .Select(r => new ResolutionChoiceView(r.Id.ToString(), r.Name, r.NoteRequired))
                .ToList())).ToList();

        return new CatalogueView("this organisation", categories, items);
    }

    /// <summary>Everything the settings tabs draw, read once.</summary>
    public async Task<SettingsView> SettingsAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var property = scope.PropertyId;
        var policies = await db.ConcernPolicies.Where(p => p.PropertyId == property && p.DeletedAt == null)
            .OrderBy(p => p.Name).ToListAsync(cancellationToken);
        var ids = policies.Select(p => p.Id).ToList();
        var rules = await db.ConcernRules.Where(r => ids.Contains(r.PolicyId)).ToListAsync(cancellationToken);
        var ladder = await db.LadderSteps.Where(s => ids.Contains(s.PolicyId)).ToListAsync(cancellationToken);
        var categories = await db.Categories.Where(c => c.DeletedAt == null).ToDictionaryAsync(c => c.Id, cancellationToken);
        var items = await db.Items.Where(i => i.DeletedAt == null).ToDictionaryAsync(i => i.Id, cancellationToken);

        return new SettingsView(
            Scopes(policies),
            policies.Select(p => Row(p, rules, ladder, categories, items)).ToList(),
            Rules(policies.FirstOrDefault(p => p.DepartmentCode is not null), rules, ladder),
            await PresenceAsync(scope, cancellationToken),
            await ToldAsync(property, cancellationToken),
            await HoldsAsync(property, cancellationToken),
            await HoldWarningsAsync(property, cancellationToken),
            await ClosingAsync(property, cancellationToken),
            await RatingAsync(property, cancellationToken),
            Access(),
            "PROPERTY-DEPT-n, from the property's code and the job's department");
    }

    private static IReadOnlyList<ScopeLineView> Scopes(IReadOnlyList<ConcernPolicy> policies)
    {
        var lines = new List<ScopeLineView>
        {
            new("This property", policies.Any(p => p.DepartmentCode is null && p.CategoryId is null) ? "set" : "not set", 0),
        };
        foreach (var department in policies.Where(p => p.DepartmentCode is not null).Select(p => p.DepartmentCode!).Distinct().Order())
        {
            lines.Add(new ScopeLineView(department, "set", 1));
            foreach (var category in policies.Where(p => p.DepartmentCode == department && p.CategoryId is not null))
            {
                lines.Add(new ScopeLineView(category.Name, "set", 2));
            }
        }

        return lines;
    }

    private static PolicyRowView Row(
        ConcernPolicy policy,
        IReadOnlyList<ConcernPolicyRule> rules,
        IReadOnlyList<ConcernLadderStep> ladder,
        IReadOnlyDictionary<Guid, Domain.Catalogue.Category> categories,
        IReadOnlyDictionary<Guid, Domain.Catalogue.Item> items)
    {
        var mine = rules.Where(r => r.PolicyId == policy.Id).ToList();
        var steps = ladder.Count(s => s.PolicyId == policy.Id);
        var (scope, label) = policy switch
        {
            { ItemId: { } item } => ("item", items.TryGetValue(item, out var found) ? found.Name : "an item"),
            { CategoryId: { } category } => ("category", categories.TryGetValue(category, out var found) ? found.Name : "a category"),
            { DepartmentCode: { } department } => ("department", department),
            _ => ("property", "This property"),
        };

        return new PolicyRowView(
            scope,
            label,
            policy.Name,
            mine.Count == 0 ? "from the item" : string.Join(" · ", mine.Select(r => $"{r.Priority} {r.DueWithinMinutes?.ToString() ?? "item"}m")),
            mine.Count == 0 ? "75%" : $"{mine[0].AtRiskPercent}%",
            steps == 0 ? "no ladder" : $"{steps} steps",
            scope == "property" ? "everything not covered below" : label);
    }

    private static IReadOnlyList<PolicyRuleView> Rules(
        ConcernPolicy? policy, IReadOnlyList<ConcernPolicyRule> rules, IReadOnlyList<ConcernLadderStep> ladder)
    {
        if (policy is null) return [];
        return rules.Where(r => r.PolicyId == policy.Id).OrderBy(r => r.Priority).Select(r => new PolicyRuleView(
            r.Priority,
            r.DueWithinMinutes is { } due ? $"{due}m" : "from the item",
            $"{r.AtRiskPercent}%",
            r.NotAcceptedMinutes is { } accept ? $"{accept}m" : "—",
            r.NoSessionMinutes is { } session ? $"{session}m" : "—",
            string.Join(" → ", ladder.Where(s => s.PolicyId == policy.Id && s.Priority == r.Priority)
                .OrderBy(s => s.StepNo).Select(s => $"{s.Role} +{s.DelayMinutes}m")),
            r.ManagerAtRisk)).ToList();
    }

    private async Task<IReadOnlyList<PresenceRowView>> PresenceAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var rows = await queries.PresenceAsync(scope, cancellationToken);
        var hours = await db.ServiceHours.Where(h => h.PropertyId == scope.PropertyId).ToListAsync(cancellationToken);
        return rows.Select(p => new PresenceRowView(
            p.DepartmentCode,
            p.Enabled,
            p.FollowShifts,
            hours.FirstOrDefault(h => h.DepartmentCode == p.DepartmentCode) is { } window
                ? $"{window.From:HH\\:mm}–{window.To:HH\\:mm}"
                : "all day",
            p.Staffed ? $"staffed · {p.OnShift} on shift" : "nobody on shift")).ToList();
    }

    private async Task<IReadOnlyList<ToldView>> ToldAsync(Guid property, CancellationToken cancellationToken)
    {
        var subscriptions = await db.Subscriptions.Where(s => s.PropertyId == property).ToListAsync(cancellationToken);
        return subscriptions.GroupBy(s => s.Role).Select(group => new ToldView(
            group.Key,
            group.Any(s => s.Concern == Domain.Concern.AtRisk),
            group.Any(s => s.Concern == Domain.Concern.Breached) ? "yes" : "no",
            group.Any(s => s.Concern == Domain.Concern.Stuck) ? "yes" : "no",
            group.Any(s => s.OnlyPriority == Priority.NotTriaged),
            group.FirstOrDefault(s => s.RepeatMinutes is not null)?.RepeatMinutes is { } repeat ? $"every {repeat}m" : "once",
            string.Join(", ", group.Select(s => s.DepartmentCode ?? "all").Distinct()))).ToList();
    }

    private async Task<IReadOnlyList<ModuleViews.DetailView>> HoldsAsync(Guid property, CancellationToken cancellationToken)
    {
        var policy = await db.HoldPolicies.FirstOrDefaultAsync(h => h.PropertyId == property, cancellationToken);
        return policy is null
            ? [new ModuleViews.DetailView("Longest hold", "not set — 30 days applies")]
            :
            [
                new ModuleViews.DetailView("Longest hold", $"{policy.MaxHoldDays} days"),
                new ModuleViews.DetailView("Warn", $"{policy.WarnDaysBefore} day(s) before, {policy.WarnRole}"),
                new ModuleViews.DetailView("Warn the assignee on the day", policy.WarnAssigneeOnDay ? "yes" : "no"),
            ];
    }

    private async Task<IReadOnlyList<HoldWarningView>> HoldWarningsAsync(Guid property, CancellationToken cancellationToken)
    {
        var holding = await db.Jobs
            .Where(j => j.PropertyId == property && j.JobStatus == JobStatus.OnHold && j.HoldUntil != null && j.DeletedAt == null)
            .OrderBy(j => j.HoldUntil)
            .Take(10)
            .ToListAsync(cancellationToken);
        return holding.Select(j => new HoldWarningView(j.HoldUntil!.Value.ToString("o"), j.JobNumber)).ToList();
    }

    private async Task<IReadOnlyList<ClosingView>> ClosingAsync(Guid property, CancellationToken cancellationToken)
    {
        var policies = await db.ClosingPolicies.Where(c => c.PropertyId == property).ToListAsync(cancellationToken);
        return policies.Count == 0
            ? [new ClosingView("This property", "4 hours — the default, not set here")]
            : policies.Select(c => new ClosingView(c.DepartmentCode ?? "This property", $"{c.AutoCloseHours} hours")).ToList();
    }

    private async Task<IReadOnlyList<ModuleViews.DetailView>> RatingAsync(Guid property, CancellationToken cancellationToken)
    {
        var policy = await db.ClosingPolicies.FirstOrDefaultAsync(c => c.PropertyId == property && c.DepartmentCode == null, cancellationToken);
        var rated = await db.Ratings.CountAsync(r => r.PropertyId == property, cancellationToken);
        var average = rated == 0 ? 0 : await db.Ratings.Where(r => r.PropertyId == property).AverageAsync(r => (double)r.Stars, cancellationToken);
        return
        [
            new ModuleViews.DetailView("Ask on close", policy?.RatingOnClose ?? true ? "yes" : "no"),
            new ModuleViews.DetailView("Ratings", rated.ToString()),
            new ModuleViews.DetailView("Average", rated == 0 ? "—" : average.ToString("0.0")),
            new ModuleViews.DetailView("Window", "24 hours after closing"),
        ];
    }

    /// <summary>
    /// Where the grants come from — stated, not read.
    /// </summary>
    /// <remarks>
    /// Jobs writes no authorization tuple and holds no copy of who has what: it
    /// declares one grant kind in its manifest and the Kernel materialises it
    /// (design §4, ruling 4). The tab therefore says where to look rather than
    /// showing a list this service would have to keep in step with the Kernel's.
    /// </remarks>
    private static IReadOnlyList<AccessView> Access() =>
    [
        new("job.read · create · assign · complete · cancel · amend", "everyone the property grants them to", "the Kernel"),
        new("job.configure · curate", "the jobs manager", "property#jobs_manager, granted by the GM"),
        new("Who holds each", "asked of the Kernel by the shell", "never stored here"),
    ];

    /// <summary>Now, so a settings read can say what is true at this instant.</summary>
    public DateTimeOffset Now => clock.GetUtcNow();
}
