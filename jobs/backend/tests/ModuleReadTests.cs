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
        Assert.NotEmpty(me.Text("where"));
    }

    [Fact]
    public async Task Scheduled_lists_the_day_and_nothing_about_cycles()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: new DateOnly(2026, 9, 30));

        var answer = await module.CallAsync(Permissions.Read, "scheduled");

        Assert.Equal(200, answer.Status);
        var row = Assert.Single(answer.At().EnumerateArray().ToList());
        Assert.Equal("2026-09-30", row.GetProperty("scheduledFor").GetString());
        Assert.False(row.TryGetProperty("cycle", out _));
    }

    [Fact]
    public async Task A_method_the_capability_does_not_have_is_refused_by_name()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);

        var answer = await module.CallAsync(Permissions.Read, "everything");

        Assert.Equal(400, answer.Status);
    }
}
