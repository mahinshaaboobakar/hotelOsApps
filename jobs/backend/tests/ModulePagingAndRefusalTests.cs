using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// Pagination, filters, refusals and conflicts over the module surface — the
/// ledger's section C, and the rows a screen only meets when something is
/// wrong.
/// </summary>
/// <remarks>
/// The empty page, the stale version and the permission a person does not hold
/// are the three a build proves last and a property meets first. Each is
/// checked for what the <i>screen</i> receives, because a refusal that arrives
/// as a failure can only be drawn as "something went wrong".
/// </remarks>
[Collection(JobsCollection.Name)]
public class ModulePagingAndRefusalTests(JobsFixture fixture)
{
    [Fact]
    public async Task Paging_walks_the_boundary_and_runs_off_the_end_without_repeating_a_row()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        for (var i = 0; i < 25; i++) await h.RaiseNotCoolingAsync(h.Scope());

        var first = await module.CallAsync(Permissions.Read, "board", new { pageSize = 12, page = 0 });
        var second = await module.CallAsync(Permissions.Read, "board", new { pageSize = 12, page = 1 });
        var third = await module.CallAsync(Permissions.Read, "board", new { pageSize = 12, page = 2 });
        var past = await module.CallAsync(Permissions.Read, "board", new { pageSize = 12, page = 9 });

        Assert.Equal(12, first.Count("rows"));
        Assert.Equal(12, second.Count("rows"));
        Assert.Equal(1, third.Count("rows"));
        Assert.Equal(0, past.Count("rows"));

        // The total holds on every page, including the one past the end — the
        // pager divides by it, so a total that vanished would erase the pager
        // exactly when a person is trying to get back.
        foreach (var page in new[] { first, second, third, past })
        {
            Assert.Equal(25, page.Number("paging", "total"));
            Assert.Equal(12, page.Number("paging", "pageSize"));
        }

        var seen = new[] { first, second, third }
            .SelectMany(page => page.At("rows").EnumerateArray().Select(row => row.GetProperty("id").GetString()!))
            .ToList();
        Assert.Equal(25, seen.Count);
        Assert.Equal(25, seen.Distinct().Count());
    }

    [Fact]
    public async Task A_page_size_beyond_the_ceiling_is_answered_with_the_size_that_was_applied()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        await h.RaiseNotCoolingAsync(h.Scope());

        var answer = await module.CallAsync(Permissions.Read, "board", new { pageSize = 500 });

        // CORE-Q13: the size the service applied, never the one asked for. A
        // pager dividing by 500 would draw one page for a list that has forty.
        Assert.Equal(100, answer.Number("paging", "pageSize"));
    }

    [Fact]
    public async Task A_list_that_grows_while_you_are_on_page_two_gains_the_row_and_the_total()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        for (var i = 0; i < 13; i++) await h.RaiseNotCoolingAsync(h.Scope());

        var before = await module.CallAsync(Permissions.Read, "board", new { pageSize = 12, page = 1 });
        Assert.Equal(1, before.Count("rows"));
        Assert.Equal(13, before.Number("paging", "total"));

        await h.RaiseNotCoolingAsync(h.Scope());

        var after = await module.CallAsync(Permissions.Read, "board", new { pageSize = 12, page = 1 });
        Assert.Equal(2, after.Count("rows"));
        Assert.Equal(14, after.Number("paging", "total"));
    }

    [Fact]
    public async Task The_filters_narrow_the_list_and_one_that_matches_nothing_is_empty()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        await h.RaiseNotCoolingAsync(h.Scope());

        var engineering = await module.CallAsync(Permissions.Read, "board", new { department = "ENG" });
        Assert.Equal(1, engineering.Number("paging", "total"));

        var housekeeping = await module.CallAsync(Permissions.Read, "board", new { department = "HK" });
        Assert.Equal(0, housekeeping.Number("paging", "total"));
        Assert.Equal(0, housekeeping.Count("rows"));

        var raised = await module.CallAsync(Permissions.Read, "board", new { statuses = new[] { JobStatus.Raised } });
        Assert.Equal(1, raised.Number("paging", "total"));

        var resolved = await module.CallAsync(Permissions.Read, "board", new { statuses = new[] { JobStatus.Resolved } });
        Assert.Equal(0, resolved.Number("paging", "total"));
    }

    [Fact]
    public async Task Mine_shows_only_what_the_person_asking_holds()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        await h.RaiseNotCoolingAsync(h.Scope(), assignTo: module.Caller);
        await h.RaiseNotCoolingAsync(h.Scope(), assignTo: Guid.CreateVersion7());

        var all = await module.CallAsync(Permissions.Read, "board");
        var mine = await module.CallAsync(Permissions.Read, "board", new { mine = true });

        Assert.Equal(2, all.Count("rows"));
        Assert.Equal(1, mine.Count("rows"));
        Assert.True(mine.At("rows", "0", "viewerIsAssignee").GetBoolean());
    }

    [Fact]
    public async Task A_permission_the_caller_does_not_hold_is_refused_before_anything_happens()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        module.Granted.Remove(Permissions.Create);

        var answer = await module.CallAsync(Permissions.Create, "raise", new
        {
            itemId = h.NotCooling.Id.ToString(),
            locationId = h.Room1204.ToString(),
            summary = "Room feels warm since noon",
        });

        Assert.Equal(403, answer.Status);
        Assert.Contains(Permissions.Create, module.Kernel.Asked);

        // Nothing was written: the guard is before the handler, which is what
        // makes it a gate rather than an audit note.
        var board = await module.CallAsync(Permissions.Read, "board");
        Assert.Equal(0, board.Number("paging", "total"));
    }

    [Fact]
    public async Task A_token_that_is_not_there_or_not_ours_gets_nothing()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);

        Assert.Equal(401, await module.StatusWithoutTokenAsync(Permissions.Read, "board"));
        Assert.Equal(401, await module.StatusWithForeignTokenAsync(Permissions.Read, "board"));

        // And a call that names no property is a bad request rather than a
        // guess: a property guessed here is a call authorized somewhere the
        // person may not be.
        Assert.Equal(400, await module.StatusWithoutPropertyAsync(Permissions.Read, "board"));
    }

    [Fact]
    public async Task Two_edits_to_one_row_and_the_second_is_refused()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        var raised = await module.CallAsync(Permissions.Create, "raise", new
        {
            itemId = h.NotCooling.Id.ToString(),
            locationId = h.Room1204.ToString(),
            summary = "Room feels warm since noon",
        });
        var id = raised.Text("id");
        var stale = raised.At("version").GetInt64();

        var first = await module.CallAsync(Permissions.Assign, "assign", new
        {
            id, version = stale, userId = module.Caller.ToString(),
        });
        Assert.Equal(200, first.Status);

        var second = await module.CallAsync(Permissions.Assign, "assign", new
        {
            id, version = stale, userId = Guid.CreateVersion7().ToString(),
        });

        Assert.Equal(409, second.Status);

        // The first edit stands. A silent win for the second is the defect this
        // row exists to catch — it looks like success to both people.
        var job = await module.CallAsync(Permissions.Read, "job", new { id });
        Assert.Equal(JobStatus.Assigned, job.Text("row", "status"));
        Assert.True(job.At("row", "viewerIsAssignee").GetBoolean());
    }

    [Fact]
    public async Task A_job_that_is_not_there_and_one_of_another_property_are_both_not_found()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();

        var missing = await module.CallAsync(Permissions.Read, "job", new { id = Guid.CreateVersion7().ToString() });
        Assert.Equal(404, missing.Status);

        // Another property's job, raised through a second harness on the same
        // database. Not found rather than forbidden: a property learns nothing
        // about another property's rows, including that they exist.
        var elsewhere = new JobsHarness(fixture);
        await elsewhere.SeedCatalogueAsync();
        var theirs = await elsewhere.RaiseNotCoolingAsync(elsewhere.Scope());

        var refused = await module.CallAsync(Permissions.Read, "job", new { id = theirs.Id.ToString() });
        Assert.Equal(404, refused.Status);
    }

    [Fact]
    public async Task The_widget_and_the_board_agree_because_they_are_counted_from_one_place()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        for (var i = 0; i < 3; i++) await h.RaiseNotCoolingAsync(h.Scope());

        var board = await module.CallAsync(Permissions.Read, "board", new { department = "ENG" });
        var strip = await module.CallAsync(Permissions.Read, "today", new { department = "ENG" });
        var widget = await module.CallAsync(Permissions.Read, "jobsNow", new { department = "ENG" });

        Assert.Equal(3, board.Number("paging", "total"));
        Assert.Equal(3, strip.Number("open"));
        Assert.Equal(3, widget.Number("open"));
    }
}
