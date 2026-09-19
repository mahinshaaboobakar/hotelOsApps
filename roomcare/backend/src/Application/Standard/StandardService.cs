using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Standard;

/// <summary>Setup — the property's rules, windows and services per room type (roomcare.configure, frames 7a–7c).</summary>
/// <remarks>
/// Every save is versioned and refuses a stale edit. An inspection rule other
/// than none is refused while no inspection application is installed — Room
/// Care's screens say "not installed" rather than pretending (chapter 03 §9).
/// </remarks>
public sealed class StandardService(RoomCareDbContext db, Gate gate, TimeProvider clock)
{
    /// <summary>The words shown when a rule needs an application this property does not have.</summary>
    public const string NoInspectionApplication =
        "no inspection application is installed, so a service can only be set to no inspection";

    public async Task<PropertyPolicy> SavePolicyAsync(
        RequestScope scope, PolicyEdit edit, long expectedVersion, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        var saved = await db.Policies.FirstOrDefaultAsync(p => p.PropertyId == scope.PropertyId, cancellationToken);
        if (saved is null)
        {
            saved = PropertyPolicy.DefaultFor(scope.PropertyId);
            db.Policies.Add(saved);
        }

        if (saved.Version != expectedVersion)
        {
            throw new ConcurrencyException("property_policy", scope.PropertyId, expectedVersion);
        }

        edit.ApplyTo(saved);
        saved.Version += 1;
        saved.ChangedAt = clock.GetUtcNow();
        saved.ChangedBy = Actor.PersonOf(scope, "changing the standard");
        await db.SaveChangesAsync(cancellationToken);
        return saved;
    }

    public async Task<ServiceWindow> SaveWindowAsync(
        RequestScope scope, string window, TimeOnly starts, TimeOnly ends, bool enabled, bool outside, long expectedVersion,
        CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        if (!ServiceWindowName.All.Contains(window))
        {
            throw new InvalidRequestException("that is not a service window");
        }

        if (starts == ends)
        {
            throw new InvalidRequestException("a window cannot start and end at the same time; one that ends earlier than it starts crosses midnight");
        }

        var saved = await db.Windows.FirstOrDefaultAsync(w => w.PropertyId == scope.PropertyId && w.Window == window, cancellationToken);
        if (saved is null)
        {
            saved = new ServiceWindow { Id = Guid.CreateVersion7(), PropertyId = scope.PropertyId, Window = window };
            db.Windows.Add(saved);
        }

        if (saved.Version != expectedVersion)
        {
            throw new ConcurrencyException("service_window", saved.Id, expectedVersion);
        }

        saved.Starts = starts;
        saved.Ends = ends;
        saved.Enabled = enabled;
        saved.AllowAssignmentOutside = outside;
        saved.Version += 1;
        await db.SaveChangesAsync(cancellationToken);
        return saved;
    }

    public async Task<ServiceStandard> SaveServiceAsync(
        RequestScope scope, ServiceEdit edit, long expectedVersion, CancellationToken cancellationToken)
    {
        await gate.PropertyAsync(scope, Permissions.Configure, cancellationToken);
        edit.Check();
        var saved = await db.Standards.FirstOrDefaultAsync(
            s => s.PropertyId == scope.PropertyId && s.RoomTypeId == edit.RoomTypeId && s.Service == edit.Service, cancellationToken);
        if (saved is null)
        {
            saved = new ServiceStandard { Id = Guid.CreateVersion7(), PropertyId = scope.PropertyId, RoomTypeId = edit.RoomTypeId, Service = edit.Service };
            db.Standards.Add(saved);
        }

        if (saved.Version != expectedVersion)
        {
            throw new ConcurrencyException("service_standard", saved.Id, expectedVersion);
        }

        saved.Minutes = edit.Minutes;
        saved.Credits = edit.Credits;
        saved.InspectionRule = edit.InspectionRule;
        saved.ChecklistRef = null;
        saved.Phases = edit.Phases.ToList();
        saved.Version += 1;
        await db.SaveChangesAsync(cancellationToken);
        return saved;
    }
}

/// <summary>One service for one room type, as Setup's services tab edits it (frame 7b).</summary>
public sealed record ServiceEdit(Guid? RoomTypeId, string Service, int Minutes, decimal Credits, string InspectionRule, IReadOnlyList<string> Phases)
{
    public void Check()
    {
        if (!Domain.Service.All.Contains(Service))
        {
            throw new InvalidRequestException("that is not a service");
        }

        if (Minutes is < 1 or > 600 || Credits < 0)
        {
            throw new InvalidRequestException("minutes are between 1 and 600, and credits are not negative");
        }

        if (InspectionRule != Domain.InspectionRule.None)
        {
            throw new InvalidRequestException(StandardService.NoInspectionApplication);
        }

        if (Phases.Count == 0 || Phases.Any(p => !Phase.All.Contains(p) || p == Phase.Inspect) || Phases[^1] != Phase.Done)
        {
            throw new InvalidRequestException("a service walks one or more phases and ends when it is done; inspection is added by its rule");
        }
    }
}

/// <summary>The property's rules as Setup edits them (frames 7a, 7c, 7d); a null field is left as it is.</summary>
public sealed record PolicyEdit
{
    public string? TriggerMode { get; init; }
    public string? WhoLeads { get; init; }
    public string? StaySource { get; init; }
    public string? BoardDefaultView { get; init; }
    public string? StatesDefaultView { get; init; }
    public string? LinenRuleKind { get; init; }
    public int? LinenEveryDays { get; init; }
    public string? Towels { get; init; }
    public bool? TurndownEnabled { get; init; }
    public int? RefreshAfterDays { get; init; }
    public int? DndRecheckMinutes { get; init; }
    public int? SupervisorAfterDays { get; init; }
    public IReadOnlyList<string>? PriorityLadder { get; init; }
    public string? AssignmentStrategy { get; init; }
    public string? UnsoldDeparture { get; init; }

    /// <summary>Copy what was sent onto the saved row, refusing any word the vocabulary does not have.</summary>
    public void ApplyTo(PropertyPolicy policy)
    {
        policy.TriggerMode = Word(TriggerMode, Domain.TriggerMode.All, "trigger") ?? policy.TriggerMode;
        policy.WhoLeads = Word(WhoLeads, Domain.WhoLeads.All, "who leads") ?? policy.WhoLeads;
        policy.StaySource = Word(StaySource, Domain.StaySource.All, "stay source") ?? policy.StaySource;
        policy.BoardDefaultView = Word(BoardDefaultView, BoardView.All, "board view") ?? policy.BoardDefaultView;
        policy.StatesDefaultView = Word(StatesDefaultView, StatesView.All, "room states view") ?? policy.StatesDefaultView;
        policy.LinenRuleKind = Word(LinenRuleKind, Domain.LinenRuleKind.All, "linen rule") ?? policy.LinenRuleKind;
        policy.Towels = Word(Towels, TowelRule.All, "towel rule") ?? policy.Towels;
        policy.AssignmentStrategy = Word(AssignmentStrategy, Domain.AssignmentStrategy.All, "strategy") ?? policy.AssignmentStrategy;
        policy.UnsoldDeparture = Word(UnsoldDeparture, Domain.UnsoldDeparture.All, "unsold departure") ?? policy.UnsoldDeparture;
        policy.TurndownEnabled = TurndownEnabled ?? policy.TurndownEnabled;
        policy.LinenEveryDays = Positive(LinenEveryDays, 30, "linen days") ?? policy.LinenEveryDays;
        policy.RefreshAfterDays = Positive(RefreshAfterDays, 60, "refresh days") ?? policy.RefreshAfterDays;
        policy.DndRecheckMinutes = Positive(DndRecheckMinutes, 480, "DND re-check minutes") ?? policy.DndRecheckMinutes;
        policy.SupervisorAfterDays = Positive(SupervisorAfterDays, 14, "days before the supervisor decides") ?? policy.SupervisorAfterDays;
        if (PriorityLadder is { } ladder)
        {
            if (ladder.Count != PriorityBand.All.Count || ladder.Except(PriorityBand.All).Any() || ladder.Distinct().Count() != ladder.Count)
            {
                throw new InvalidRequestException("the priority ladder orders each of the four bands exactly once");
            }

            policy.PriorityLadder = ladder.ToList();
        }
    }

    private static string? Word(string? value, IReadOnlyList<string> words, string what) =>
        value is null || words.Contains(value) ? value : throw new InvalidRequestException($"that is not a {what}");

    private static int? Positive(int? value, int max, string what) =>
        value is null || (value >= 1 && value <= max) ? value : throw new InvalidRequestException($"{what} is between 1 and {max}");
}
