using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// Every screen's read, over the module surface — the ledger's section A.
/// </summary>
/// <remarks>
/// These are the rows that said <i>"no message answers this"</i> in the held
/// ledger. They were true of the gRPC contract and are not true of the module
/// surface: the envelope carries the application's own JSON, so a screen's
/// question is answered by the application rather than by a protobuf that had
/// to be minted for it first.
/// </remarks>
[Collection(JobsCollection.Name)]
public class ModuleReadTests(JobsFixture fixture)
{
    [Fact]
    public async Task The_board_arrives_with_its_rows_its_paging_and_its_place_names()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        h.Directory.Places[h.Room1204] = "Room 1204";
        await h.RaiseNotCoolingAsync(h.Scope());
        await h.RaiseNotCoolingAsync(h.Scope());

        var answer = await module.CallAsync(Permissions.Read, "board");

        Assert.Equal(200, answer.Status);
        Assert.Equal(2, answer.Count("rows"));
        Assert.Equal("Room 1204", answer.Text("rows", "0", "where"));
        Assert.Equal(2, answer.Number("paging", "total"));
        Assert.Equal(24, answer.Number("paging", "pageSize"));
        Assert.Equal(0, answer.Number("paging", "page"));
    }

    [Fact]
    public async Task A_property_with_nothing_in_it_reads_as_empty_rather_than_as_an_error()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);

        var answer = await module.CallAsync(Permissions.Read, "board");

        Assert.Equal(200, answer.Status);
        Assert.Equal(0, answer.Count("rows"));
        Assert.Equal(0, answer.Number("paging", "total"));
    }

    [Fact]
    public async Task Todays_strip_counts_what_is_open_running_and_escalated()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        var arjun = module.Caller;
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        await h.Work.StartAsync(h.Scope(arjun), job.Id, default);

        var answer = await module.CallAsync(Permissions.Read, "today", new { department = "ENG" });

        Assert.Equal(1, answer.Number("open"));
        Assert.Equal(1, answer.Number("running"));
        Assert.Equal(0, answer.Number("breached"));
        Assert.Equal("ENG", answer.Text("department"));
    }

    [Fact]
    public async Task One_job_arrives_with_every_tab_and_the_services_own_running_seconds()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        h.Directory.Places[h.Room1204] = "Room 1204";
        var arjun = module.Caller;
        var job = await h.RaiseNotCoolingAsync(h.Scope(), assignTo: arjun);
        job = await h.Assignment.AcceptAsync(h.Scope(arjun), job.Id, job.Version, default);
        await h.Work.StartAsync(h.Scope(arjun), job.Id, default);

        var answer = await module.CallAsync(Permissions.Read, "job", new { id = job.Id.ToString() });

        Assert.Equal(200, answer.Status);
        Assert.Equal(job.JobNumber, answer.Text("row", "number"));
        Assert.Equal("Room 1204", answer.Text("row", "where"));
        Assert.Equal(JobStatus.InProgress, answer.Text("row", "status"));
        Assert.True(answer.At("row", "viewerIsAssignee").GetBoolean());
        Assert.Equal(RaisedVia.GuestApp, answer.Text("raised", "via"));
        Assert.Single(answer.At("sessions").EnumerateArray());
        Assert.True(answer.At("runningSeconds").GetInt64() >= 0);

        // The audit's finding, closed on the wire: the figure is the service's,
        // so a desktop with a wrong clock cannot invent one.
        Assert.True(answer.At("runningSeconds").GetInt64() < 60);
        Assert.NotEmpty(answer.At("history").EnumerateArray());
        Assert.NotEmpty(answer.At("record").EnumerateArray());
    }

    [Fact]
    public async Task The_catalogue_the_settings_the_live_tab_and_the_widget_all_answer()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        await h.SeedEngineeringPolicyAsync();
        await h.RaiseNotCoolingAsync(h.Scope());

        var catalogue = await module.CallAsync(Permissions.Read, "catalogue");
        Assert.Equal(200, catalogue.Status);
        // The catalogue is the organisation's, and this suite's database holds
        // every test's, so the assertion is that mine came back — never a count.
        Assert.Contains(
            catalogue.At("categories").EnumerateArray(),
            category => category.GetProperty("name").GetString() == "Air conditioning");
        Assert.Contains(
            catalogue.At("items").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "Not cooling");

        var settings = await module.CallAsync(Permissions.Read, "settings");
        Assert.Equal(200, settings.Status);
        Assert.Contains(
            settings.At("policies").EnumerateArray(),
            policy => policy.GetProperty("name").GetString() == "Engineering");
        Assert.NotEmpty(settings.At("access").EnumerateArray());
        Assert.NotEmpty(settings.Text("numbering"));

        var live = await module.CallAsync(Permissions.Read, "live");
        Assert.Equal(200, live.Status);
        Assert.NotEmpty(live.Text("sweptAt"));

        var widget = await module.CallAsync(Permissions.Read, "jobsNow", new { department = "ENG" });
        Assert.Equal(200, widget.Status);
        Assert.Equal(1, widget.Number("open"));

        var me = await module.CallAsync(Permissions.Read, "me");
        Assert.Equal(200, me.Status);
        Assert.NotEmpty(me.Text("property"));
    }

    [Fact]
    public async Task Scheduled_lists_the_day_and_nothing_about_cycles()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: new DateOnly(2026, 9, 30));

        var answer = await module.CallAsync(Permissions.Read, "scheduled");

        // **Rewritten 2026-09-19** (ADR 0034): this read a bare array. Scheduled is
        // now paged like the board — `rows` and `paging` — because the array was
        // ONE page at the maximum size with the total dropped, while the screen
        // drew "1–n of n" as though it were the whole list (checklist G1).
        Assert.Equal(200, answer.Status);
        var row = Assert.Single(answer.At("rows").EnumerateArray().ToList());
        Assert.Equal("2026-09-30", row.GetProperty("scheduledFor").GetString());
        Assert.False(row.TryGetProperty("cycle", out _));
    }

    [Fact]
    public async Task Scheduled_is_paged_and_says_how_many_there_are_in_all()
    {
        // Standard §6 / CORE-Q13 (checklist G1): a bounded operational list is
        // paged — page / page_size → total — and no application defines a third
        // pattern. Scheduled returned a bare first page with the total dropped, so
        // a property with more scheduled jobs than one page silently lost the
        // rest while the pager said the list in front of the reader was whole.
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        foreach (var day in new[] { 28, 29, 30 })
        {
            await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: new DateOnly(2026, 9, day));
        }

        var answer = await module.CallAsync(Permissions.Read, "scheduled", new { page = 0, pageSize = 2 });

        Assert.Equal(200, answer.Status);
        Assert.Equal(2, answer.At("rows").GetArrayLength());
        Assert.Equal(3, answer.At("paging").GetProperty("total").GetInt32());
        Assert.Equal(2, answer.At("paging").GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Me_names_the_person_and_the_property_and_leaves_the_department_unestablished()
    {
        // Page 64 §3: `name · department · property`. Room Care's reading is the
        // precedent (the architect, 2026-09-19): the name is Master Data's staff
        // row for the signed-in login, the property its name before its code.
        // The department is ADR 0203's to establish — Jobs spans departments and
        // has no posting to read — so it is absent, never a stand-in.
        await using var module = await ModuleHarness.StartAsync(fixture);
        module.Data.Directory.PropertyName = "Marina Bay";
        module.Data.Directory.Staff[module.Caller] = "Priya Nair";

        var me = await module.CallAsync(Permissions.Read, "me");

        Assert.Equal(200, me.Status);
        Assert.Equal("Priya Nair", me.Text("name"));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, me.At("department").ValueKind);
        Assert.Equal("Marina Bay", me.Text("property"));
    }

    [Fact]
    public async Task Me_says_nothing_it_does_not_know_and_falls_back_to_the_code()
    {
        // No staff row for this login and no property name: no name at all —
        // never "Signed in", which read as one — and the code, as Room Care does.
        await using var module = await ModuleHarness.StartAsync(fixture);
        module.Data.Directory.PropertyName = null;

        var me = await module.CallAsync(Permissions.Read, "me");

        Assert.Equal(System.Text.Json.JsonValueKind.Null, me.At("name").ValueKind);
        Assert.Equal("MRN", me.Text("property"));
    }

    [Fact]
    public async Task Me_names_the_role_in_the_department_clause_for_a_jobs_manager()
    {
        // Owner, 2026-09-19: a person whose role spans every department shows the
        // role there — "Rohan Desai · Jobs manager · The Marina Bay". Jobs holds
        // the jobs-manager grant itself, so this is established, not guessed.
        await using var module = await ModuleHarness.StartAsync(fixture);
        await module.Data.Grants.GrantAsync(module.Data.Scope(Guid.CreateVersion7()), module.Caller, default);

        var me = await module.CallAsync(Permissions.Read, "me");

        Assert.Equal("Jobs manager", me.Text("department"));
    }

    [Fact]
    public async Task The_record_tab_shows_the_job_number_and_the_property_name_and_no_raw_id()
    {
        // Owner, 2026-09-19 (c): the job number and the property's name, never raw ids.
        await using var module = await ModuleHarness.StartAsync(fixture);
        module.Data.Directory.PropertyName = "Marina Bay";
        await module.Data.SeedCatalogueAsync();
        var raised = await module.Data.RaiseNotCoolingAsync(module.Data.Scope(module.Caller));

        var job = await module.CallAsync(Permissions.Read, "job", new { id = raised.Id.ToString() });
        var record = job.At("record").EnumerateArray()
            .ToDictionary(r => r.GetProperty("k").GetString()!, r => r.GetProperty("v").GetString()!);

        Assert.Equal(raised.JobNumber, record["Number"]);
        Assert.Equal("Marina Bay", record["Property"]);
        Assert.DoesNotContain(record, r => Guid.TryParse(r.Value, out _));
    }

    [Fact]
    public async Task A_method_the_capability_does_not_have_is_refused_by_name()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);

        var answer = await module.CallAsync(Permissions.Read, "everything");

        Assert.Equal(400, answer.Status);
    }
}
