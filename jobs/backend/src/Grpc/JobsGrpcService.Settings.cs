using Grpc.Core;
using HotelOS.Jobs.Application.Catalogue;
using HotelOS.Jobs.Application.Settings;
using HotelOS.Jobs.Contracts.V1;
using HotelOS.Platform;

namespace HotelOS.Jobs.Grpc;

/// <summary>Curating the catalogue and configuring the property.</summary>
public partial class JobsGrpcService
{
    public override async Task<CategoryView> SaveCategory(SaveCategoryRequest r, ServerCallContext context) =>
        Views.Category(await catalogue.SaveCategoryAsync(r.Context.ToScope(CallerContext.Get(context)), new CategoryCommand
        {
            Id = ParseOptionalId(r.Id, "id"), ExpectedVersion = string.IsNullOrWhiteSpace(r.Id) ? null : r.ExpectedVersion,
            Code = r.Code, Name = r.Name, DepartmentCode = r.DepartmentCode, Active = r.Active,
        }, context.CancellationToken));

    public override async Task<ItemView> SaveItem(SaveItemRequest r, ServerCallContext context)
    {
        var item = await catalogue.SaveItemAsync(r.Context.ToScope(CallerContext.Get(context)), new ItemCommand
        {
            Id = ParseOptionalId(r.Id, "id"), ExpectedVersion = string.IsNullOrWhiteSpace(r.Id) ? null : r.ExpectedVersion,
            CategoryId = ParseId(r.CategoryId, "category_id"), Code = r.Code, Name = r.Name,
            DefaultPriority = string.IsNullOrWhiteSpace(r.DefaultPriority) ? "P3" : r.DefaultPriority,
            DueWithinMinutes = r.DueWithinMinutes > 0 ? r.DueWithinMinutes : null,
            RestrictedByDefault = r.RestrictedByDefault, GuestRequestable = r.GuestRequestable,
            PhotoOnCompletion = string.IsNullOrWhiteSpace(r.PhotoOnCompletion) ? "OPTIONAL" : r.PhotoOnCompletion,
            Active = r.Active, Aliases = r.ReplaceAliases ? r.Aliases.ToList() : null,
        }, context.CancellationToken);
        return Views.Item(item, r.ReplaceAliases ? r.Aliases : []);
    }

    public override async Task<CatalogueResolutionView> AddResolution(AddResolutionRequest r, ServerCallContext context) =>
        Views.Resolution(await catalogue.AddResolutionAsync(r.Context.ToScope(CallerContext.Get(context)), new ResolutionCommand
        {
            CategoryId = ParseOptionalId(r.CategoryId, "category_id"), ItemId = ParseOptionalId(r.ItemId, "item_id"),
            Name = r.Name, NoteRequired = r.NoteRequired,
        }, context.CancellationToken));

    public override async Task<SaveItemPolicyResponse> SaveItemPolicy(SaveItemPolicyRequest r, ServerCallContext context)
    {
        var policy = await propertyCatalogue.SaveAsync(r.Context.ToScope(CallerContext.Get(context)), new ItemPolicyCommand
        {
            ItemId = ParseId(r.ItemId, "item_id"), ActiveHere = r.ActiveHere, DisplayName = Blank(r.DisplayName),
            DefaultPriority = Blank(r.DefaultPriority), DueWithinMinutes = r.DueWithinMinutes > 0 ? r.DueWithinMinutes : null,
            ConcernPolicyId = ParseOptionalId(r.ConcernPolicyId, "concern_policy_id"),
            AutoAssign = string.IsNullOrWhiteSpace(r.AutoAssign) ? "USER" : r.AutoAssign,
            AutoAssignTeamId = ParseOptionalId(r.AutoAssignTeamId, "auto_assign_team_id"),
        }, context.CancellationToken);
        return new SaveItemPolicyResponse { Id = policy.Id.ToString(), Version = policy.Version };
    }

    public override async Task<SaveConcernPolicyResponse> SaveConcernPolicy(SaveConcernPolicyRequest r, ServerCallContext context)
    {
        var policy = await concernPolicies.SaveAsync(r.Context.ToScope(CallerContext.Get(context)), new ConcernPolicyCommand
        {
            Id = ParseOptionalId(r.Id, "id"), ExpectedVersion = string.IsNullOrWhiteSpace(r.Id) ? null : r.ExpectedVersion,
            Name = r.Name, DepartmentCode = Blank(r.DepartmentCode),
            CategoryId = ParseOptionalId(r.CategoryId, "category_id"), ItemId = ParseOptionalId(r.ItemId, "item_id"),
            UntriagedStuckMinutes = r.UntriagedStuckMinutes > 0 ? r.UntriagedStuckMinutes : 15,
            Rules = r.Rules.Select(x => new RuleCommand(
                x.Priority, x.DueWithinMinutes > 0 ? x.DueWithinMinutes : null, x.AtRiskPercent,
                x.NotAcceptedMinutes > 0 ? x.NotAcceptedMinutes : null, x.NoSessionMinutes > 0 ? x.NoSessionMinutes : null,
                x.ManagerAtRisk, x.RunsOutsidePresence)).ToList(),
            Ladder = r.Ladder.Select(x => new LadderStepCommand(x.Priority, x.StepNo, x.Role, x.Trigger, x.DelayMinutes)).ToList(),
        }, context.CancellationToken);
        return new SaveConcernPolicyResponse { Id = policy.Id.ToString(), Version = policy.Version };
    }

    public override async Task<PresenceView> SavePresence(SavePresenceRequest r, ServerCallContext context) =>
        Views.Presence(await presence.SaveAsync(
            r.Context.ToScope(CallerContext.Get(context)), new PresenceCommand(r.DepartmentCode, r.Enabled, r.FollowShifts), context.CancellationToken));
}
