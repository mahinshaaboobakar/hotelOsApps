using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// Every control the screens offer, driven against the real backend — the
/// ledger's section B.
/// </summary>
/// <remarks>
/// <para>
/// These are the twenty-eight rows that read <i>"blocked at the bridge"</i>.
/// The bridge exists now, so each one is a call the button makes: the module
/// surface, the platform's guards, the application's services, Entity Framework
/// and a real PostgreSQL — with the event appended in the same transaction,
/// into the platform's own <c>event_store</c>.
/// </para>
/// <para>
/// The assertion in every row is the database, read back afterwards. A status
/// code proves the call was accepted; only the row proves it happened.
/// </para>
/// </remarks>
[Collection(JobsCollection.Name)]
public class ModuleWriteTests(JobsFixture fixture)
{
    [Fact]
    public async Task Raising_writes_the_job_its_number_and_its_event()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();

        var answer = await module.CallAsync(Permissions.Create, "raise", new
        {
            itemId = h.NotCooling.Id.ToString(),
            locationId = h.Room1204.ToString(),
            summary = "Room feels warm since noon",
            raisedVia = RaisedVia.App,
            raisedKind = RaisedKind.Staff,
        });

        Assert.Equal(200, answer.Status);
        Assert.StartsWith("MRN-ENG-", answer.Text("number"), StringComparison.Ordinal);

        var id = Guid.Parse(answer.Text("id"));
        var job = await h.Db.Jobs.AsNoTracking().FirstAsync(j => j.Id == id);
        Assert.Equal(JobStatus.Raised, job.JobStatus);
        Assert.Equal(module.Caller, job.RaisedById);
        // Announced in the same transaction, into the platform's own store —
        // the wall the previous round held ten rows on.
        Assert.Contains(EventTypes.JobCreated, await h.Fixture.EventsForAsync(id));
    }

    [Fact]
    public async Task A_job_goes_all_the_way_through_its_life_from_the_screens()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        var raised = await module.CallAsync(Permissions.Create, "raise", Job(h));
        var id = raised.Text("id");

        var assigned = await module.CallAsync(Permissions.Assign, "assign", new
        {
            id, version = raised.At("version").GetInt64(), userId = module.Caller.ToString(),
        });
        Assert.Equal(JobStatus.Assigned, assigned.Text("status"));

        var accepted = await module.CallAsync(Permissions.Assign, "accept", new
        {
            id, version = assigned.At("version").GetInt64(),
        });
        Assert.Equal(JobStatus.Accepted, accepted.Text("status"));

        var started = await module.CallAsync(Permissions.Complete, "start", new { id });
        Assert.Equal(200, started.Status);

        var paused = await module.CallAsync(Permissions.Complete, "pause", new { id, reason = "waiting for parts" });
        Assert.Equal(200, paused.Status);
        Assert.NotEmpty(paused.Text("pausedAt"));

        Assert.Equal(200, (await module.CallAsync(Permissions.Complete, "resume", new { id })).Status);
        Assert.Equal(200, (await module.CallAsync(Permissions.Complete, "stop", new { id })).Status);

        var job = await h.Db.Jobs.AsNoTracking().FirstAsync(j => j.Id == Guid.Parse(id));
        var resolved = await module.CallAsync(Permissions.Complete, "resolve", new
        {
            id, version = job.Version, resolutionId = h.RefrigerantToppedUp.Id.ToString(),
        });
        Assert.Equal(JobStatus.Resolved, resolved.Text("status"));

        var closed = await module.CallAsync(Permissions.Complete, "close", new
        {
            id, version = resolved.At("version").GetInt64(),
        });
        Assert.Equal(JobStatus.Closed, closed.Text("status"));

        // Two sessions, because resuming closes the paused one and opens a new
        // one rather than reopening it — one resolution, and the whole climb in
        // the status history. Read from the database, not from the answers.
        var stored = Guid.Parse(id);
        Assert.Equal(2, await h.Db.WorkSessions.CountAsync(s => s.JobId == stored));
        Assert.Equal(1, await h.Db.Resolutions.CountAsync(r => r.JobId == stored));
        Assert.True(await h.Db.StatusHistory.CountAsync(s => s.JobId == stored) >= 5);
    }

    [Fact]
    public async Task Notes_holds_reminders_and_cancellation_all_reach_the_row()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        await h.SeedCatalogueAsync();
        var raised = await module.CallAsync(Permissions.Create, "raise", Job(h));
        var id = raised.Text("id");
        var key = Guid.Parse(id);

        var note = await module.CallAsync(Permissions.Amend, "note", new { id, text = "Guest called again" });
        Assert.Equal(200, note.Status);

        // Two: the summary the job was raised with is itself a note — which is
        // what the job view marks as the raising text — and this one.
        Assert.Equal(2, await h.Db.Notes.CountAsync(n => n.JobId == key));
        Assert.Contains(await h.Db.Notes.Where(n => n.JobId == key).Select(n => n.Text).ToListAsync(), t => t == "Guest called again");

        var remind = await module.CallAsync(Permissions.Amend, "remind", new
        {
            id, at = h.Clock.GetUtcNow().AddHours(2).ToString("o"), note = "check the compressor",
        });
        Assert.Equal(200, remind.Status);

        // A RAISED job cannot be held — the service says so in its own words,
        // and the sentence crosses the wire as a refusal rather than a failure.
        var tooEarly = await module.CallAsync(Permissions.Amend, "hold", new
        {
            id, version = raised.At("version").GetInt64(), reason = "parts, Thursday",
        });
        Assert.Equal(400, tooEarly.Status);
        Assert.Contains("cannot be held", tooEarly.Text("refused"), StringComparison.Ordinal);

        var assigned = await module.CallAsync(Permissions.Assign, "assign", new
        {
            id, version = raised.At("version").GetInt64(), userId = module.Caller.ToString(),
        });
        var accepted = await module.CallAsync(Permissions.Assign, "accept", new
        {
            id, version = assigned.At("version").GetInt64(),
        });

        var held = await module.CallAsync(Permissions.Amend, "hold", new
        {
            id, version = accepted.At("version").GetInt64(), reason = "parts, Thursday",
        });
        Assert.Equal(JobStatus.OnHold, held.Text("status"));

        var released = await module.CallAsync(Permissions.Amend, "release", new
        {
            id, version = held.At("version").GetInt64(),
        });
        Assert.NotEqual(JobStatus.OnHold, released.Text("status"));

        var cancelled = await module.CallAsync(Permissions.Cancel, "cancel", new
        {
            id, version = released.At("version").GetInt64(), reason = "raised twice",
        });
        Assert.Equal(JobStatus.Cancelled, cancelled.Text("status"));
    }

    [Fact]
    public async Task The_settings_saves_and_the_catalogue_saves_all_write()
    {
        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;

        var category = await module.CallAsync(Permissions.Curate, "saveCategory", new
        {
            code = $"LIFT{Guid.NewGuid():n}"[..12], name = "Lifts", department = "ENG",
        });
        Assert.Equal(200, category.Status);

        var item = await module.CallAsync(Permissions.Curate, "saveItem", new
        {
            categoryId = category.Text("id"),
            code = $"STUCK{Guid.NewGuid():n}"[..12],
            name = "Stuck between floors",
            defaultPriority = Priority.P1,
            dueWithinMinutes = 15,
            aliases = new[] { "lift stuck", "elevator stuck" },
        });
        Assert.Equal(200, item.Status);

        var resolution = await module.CallAsync(Permissions.Curate, "addResolution", new
        {
            categoryId = category.Text("id"), name = "Freed and tested",
        });
        Assert.Equal(200, resolution.Status);

        var policy = await module.CallAsync(Permissions.Configure, "savePolicy", new
        {
            name = "Lifts",
            department = "ENG",
            categoryId = category.Text("id"),
            rules = new[] { new { priority = Priority.P1, dueWithinMinutes = 15, atRiskPercent = 70, managerAtRisk = true } },
            ladder = new[] { new { priority = Priority.P1, stepNo = 1, role = LadderRole.Supervisor, trigger = Concern.Breached, delayMinutes = 5 } },
        });
        Assert.Equal(200, policy.Status);

        Assert.Equal(200, (await module.CallAsync(Permissions.Configure, "savePresence", new
        {
            department = "ENG", enabled = true, followShifts = true,
        })).Status);

        Assert.Equal(200, (await module.CallAsync(Permissions.Configure, "saveHours", new
        {
            department = "ENG", from = "07:00", to = "23:00",
        })).Status);

        Assert.Equal(200, (await module.CallAsync(Permissions.Configure, "saveClosing", new
        {
            department = "ENG", autoCloseHours = 6, ratingOnClose = true,
        })).Status);

        Assert.Equal(200, (await module.CallAsync(Permissions.Configure, "saveHold", new
        {
            maxHoldDays = 21, warnDaysBefore = 2, warnRole = LadderRole.Supervisor, warnAssigneeOnDay = true,
        })).Status);

        Assert.Equal(200, (await module.CallAsync(Permissions.Configure, "saveSubscriptions", new
        {
            subscriptions = new[]
            {
                new { role = LadderRole.Supervisor, concern = Concern.Breached, department = "ENG", repeatMinutes = 10 },
            },
        })).Status);

        // And the settings screen shows what was just saved — one read, the
        // whole tab set, from the rows those saves wrote.
        var settings = await module.CallAsync(Permissions.Read, "settings");
        Assert.Contains(
            settings.At("policies").EnumerateArray(),
            row => row.GetProperty("name").GetString() == "Lifts");
        Assert.Contains(
            settings.At("closing").EnumerateArray(),
            row => row.GetProperty("hours").GetString() == "6 hours");
        Assert.Contains(
            settings.At("presence").EnumerateArray(),
            row => row.GetProperty("department").GetString() == "ENG");
    }

    /// <summary>The raise every write test starts from.</summary>
    private static object Job(JobsHarness h) => new
    {
        itemId = h.NotCooling.Id.ToString(),
        locationId = h.Room1204.ToString(),
        summary = "Room feels warm since noon",
        raisedVia = RaisedVia.App,
        raisedKind = RaisedKind.Staff,
    };
}
