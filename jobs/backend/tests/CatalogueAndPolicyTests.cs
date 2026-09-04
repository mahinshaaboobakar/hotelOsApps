using HotelOS.Jobs.Application.Catalogue;
using HotelOS.Jobs.Application.Policies;
using HotelOS.Jobs.Application.Settings;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>Curate, configure, and the chain — item → category → department → property (settings frame 11).</summary>
[Collection(JobsCollection.Name)]
public class CatalogueAndPolicyTests(JobsFixture fixture)
{
    [Fact]
    public async Task Curating_is_organisation_scoped_and_asks_job_curate_on_the_organisation()
    {
        var h = new JobsHarness(fixture);
        var scope = h.Scope();

        var category = await h.Catalogue.SaveCategoryAsync(scope, new CategoryCommand { Code = " ac ", Name = "AC not working", DepartmentCode = "eng" }, default);
        var item = await h.Catalogue.SaveItemAsync(scope, new ItemCommand
        {
            CategoryId = category.Id, Code = "AC_WATER", Name = "Water dropping from unit", DefaultPriority = Priority.P2,
            DueWithinMinutes = 20, Aliases = ["AC leaking", "water from AC", " AC leaking "],
        }, default);
        var resolution = await h.Catalogue.AddResolutionAsync(scope, new ResolutionCommand { ItemId = item.Id, Name = "Drain cleared" }, default);

        Assert.Equal(("AC", "ENG", h.OrganizationId), (category.Code, category.DepartmentCode, category.OrganizationId));
        Assert.Equal(("job.curate", "organization", h.OrganizationId), h.Authorizer.Checks[0]);
        Assert.Equal(2, await h.Db.ItemAliases.CountAsync(a => a.ItemId == item.Id));
        Assert.Equal(category.Id, resolution.CategoryId);

        await Assert.ThrowsAsync<InvalidRequestException>(() => h.Catalogue.SaveCategoryAsync(scope,
            new CategoryCommand { Code = "AC", Name = "Again", DepartmentCode = "ENG" }, default));
        await Assert.ThrowsAsync<InvalidRequestException>(() => h.Catalogue.SaveCategoryAsync(
            scope with { OrganizationId = null }, new CategoryCommand { Code = "X", Name = "X", DepartmentCode = "ENG" }, default));
    }

    [Fact]
    public async Task The_chain_resolves_item_then_category_then_department_then_property()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var scope = h.Scope();
        var property = await h.Policies.SaveAsync(scope, Policy("Property default", null, null, null, 60), default);
        var department = await h.Policies.SaveAsync(scope, Policy("Housekeeping", "HK", null, null, 45), default);
        var category = await h.Policies.SaveAsync(scope, Policy("Water — 10 minutes", "HK", h.StillWater.CategoryId, null, 10), default);
        var resolver = new JobPolicyResolver(h.Db);

        var water = await resolver.ResolveAsync(h.PropertyId, h.StillWater, default);
        Assert.Equal((category.Id, 10), (water.ConcernPolicyId, water.DueWithinMinutes));

        // Not cooling has no HK anything: Engineering has no department policy either, so the property default holds it.
        var cooling = await resolver.ResolveAsync(h.PropertyId, h.NotCooling, default);
        Assert.Equal(property.Id, cooling.ConcernPolicyId);
        Assert.Equal(40, cooling.DueWithinMinutes); // the item's own due-within wins over the policy's

        var itemPolicy = await h.Policies.SaveAsync(scope, Policy("Still water — VIP", "HK", h.StillWater.CategoryId, h.StillWater.Id, 5), default);
        Assert.Equal(itemPolicy.Id, (await resolver.ResolveAsync(h.PropertyId, h.StillWater, default)).ConcernPolicyId);
        Assert.Equal(("job.configure", "property", h.PropertyId), h.Authorizer.Checks[^1]);
        _ = department;
    }

    [Fact]
    public async Task A_property_override_beats_the_item_and_a_second_policy_for_one_scope_is_refused()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var scope = h.Scope();
        await h.Policies.SaveAsync(scope, Policy("Housekeeping", "HK", null, null, 45), default);
        await h.PropertyCatalogue.SaveAsync(scope, new ItemPolicyCommand { ItemId = h.StillWater.Id, DefaultPriority = Priority.P2, DueWithinMinutes = 7, DisplayName = "Water, still" }, default);

        var resolved = await new JobPolicyResolver(h.Db).ResolveAsync(h.PropertyId, h.StillWater, default);
        Assert.Equal((Priority.P2, 7), (resolved.Priority, resolved.DueWithinMinutes));

        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(() => h.Policies.SaveAsync(scope, Policy("Housekeeping again", "HK", null, null, 30), default));
        Assert.Contains("already exists", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_policy_in_use_by_an_item_cannot_be_deleted()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var scope = h.Scope();
        var policy = await h.Policies.SaveAsync(scope, Policy("Engineering", "ENG", null, null, 40), default);
        await h.PropertyCatalogue.SaveAsync(scope, new ItemPolicyCommand { ItemId = h.NotCooling.Id, ConcernPolicyId = policy.Id }, default);

        await Assert.ThrowsAsync<InUseException>(() => h.Policies.DeleteAsync(scope, policy.Id, default));
    }

    private static ConcernPolicyCommand Policy(string name, string? department, Guid? category, Guid? item, int p1Due) => new()
    {
        Name = name, DepartmentCode = department, CategoryId = category, ItemId = item,
        Rules =
        [
            new RuleCommand(Priority.P1, p1Due, 75, 8, 15, false, true),
            new RuleCommand(Priority.P2, p1Due * 3, 75, 20, 45, false, false),
            new RuleCommand(Priority.P3, null, 80, 60, null, false, false),
        ],
        Ladder =
        [
            new LadderStepCommand(Priority.P1, 1, LadderRole.Assignee, Concern.AtRisk, 0),
            new LadderStepCommand(Priority.P1, 2, LadderRole.Supervisor, Concern.Breached, 0),
        ],
    };
}
