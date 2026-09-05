namespace HotelOS.Jobs.Module;

/// <summary>
/// The shapes the catalogue and the settings tabs are given — mirroring
/// <c>ui/board/model.ts</c>, and separate from <see cref="ModuleViews"/>
/// because a screen that lists what a hotel <i>configures</i> is not the same
/// subject as one that lists what it is <i>doing</i>.
/// </summary>
public static class ModuleSettingsViews
{
    /// <summary>A catalogue category, and whether this property uses it.</summary>
    public sealed record CategoryView(string Id, string Name, string Department, int Items, bool ActiveHere);

    /// <summary>A catalogue item as this property sees it.</summary>
    public sealed record ItemView(
        string Id,
        string CategoryId,
        string Name,
        string Department,
        string DefaultPriority,
        int? DueWithinMinutes,
        bool Restricted,
        IReadOnlyList<string> Aliases,
        IReadOnlyList<ItemPropertyView> ActiveAt,
        IReadOnlyList<string> Resolutions);

    /// <summary>Whether one property has this item switched on.</summary>
    public sealed record ItemPropertyView(string Property, bool On);

    /// <summary>The catalogue screen — frame 7.</summary>
    public sealed record CatalogueView(
        string Organisation,
        IReadOnlyList<CategoryView> Categories,
        IReadOnlyList<ItemView> Items);

    /// <summary>One policy on the settings list — page 02 frame 7.</summary>
    public sealed record PolicyRowView(
        string Scope,
        string ScopeLabel,
        string Name,
        string Due,
        string AtRisk,
        string Ladder,
        string UsedBy);

    /// <summary>One priority's row inside a policy.</summary>
    public sealed record PolicyRuleView(
        string Priority,
        string Due,
        string AtRisk,
        string NotAccepted,
        string NoSession,
        string Ladder,
        bool ManagerAtRisk);

    /// <summary>A department's presence switches.</summary>
    public sealed record PresenceRowView(
        string Department,
        bool Enabled,
        bool FollowShifts,
        string Hours,
        string Now);

    /// <summary>A line of the scope tree at the head of the policies tab.</summary>
    public sealed record ScopeLineView(string Label, string State, int Indent);

    /// <summary>One subscription row — who is told, and when.</summary>
    public sealed record ToldView(
        string Role,
        bool AtRisk,
        string Breached,
        string Stuck,
        bool Untriaged,
        string Repeat,
        string Departments);

    /// <summary>A hold that will expire, and who put it there.</summary>
    public sealed record HoldWarningView(string When, string Who);

    /// <summary>A closing rule — the scope and its hours.</summary>
    public sealed record ClosingView(string Scope, string Hours);

    /// <summary>An access line — the grant, who holds it, and where it comes from.</summary>
    public sealed record AccessView(string Label, string Who, string From);

    /// <summary>Everything the six settings tabs draw.</summary>
    public sealed record SettingsView(
        IReadOnlyList<ScopeLineView> Scopes,
        IReadOnlyList<PolicyRowView> Policies,
        IReadOnlyList<PolicyRuleView> EngineeringRules,
        IReadOnlyList<PresenceRowView> Presence,
        IReadOnlyList<ToldView> WhoIsTold,
        IReadOnlyList<ModuleViews.DetailView> Holds,
        IReadOnlyList<HoldWarningView> HoldWarnings,
        IReadOnlyList<ClosingView> Closing,
        IReadOnlyList<ModuleViews.DetailView> Rating,
        IReadOnlyList<AccessView> Access,
        string Numbering);
}
