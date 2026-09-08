using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Module;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// The three widget reads whose frames had to be amended before they could be
/// built — By Priority, Due Soon and Raised Today, owner-approved 2026-09-08.
/// </summary>
/// <remarks>
/// Each amendment was a figure the design could not produce being replaced by
/// one it can, so each test asserts the distinction that made the amendment
/// necessary rather than that the read returns something.
/// </remarks>
[Collection(JobsCollection.Name)]
public class WidgetProjectionTests(JobsFixture fixture)
{
    private static WidgetProjection Widgets(JobsHarness h) =>
        new(h.Db, h.Clock, h.Directory);

    [Fact]
    public async Task By_priority_counts_open_work_and_keeps_not_triaged_out_of_P3()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var scope = h.Scope();

        var urgent = await h.RaiseNotCoolingAsync(scope);
        var untriaged = await h.RaiseNotCoolingAsync(scope);
        var closed = await h.RaiseNotCoolingAsync(scope);

        urgent.Priority = Priority.P1;
        untriaged.Priority = Priority.NotTriaged;
        closed.Priority = Priority.P1;
        closed.JobStatus = JobStatus.Closed;
        await h.Db.SaveChangesAsync();

        var view = await Widgets(h).ByPriorityAsync(scope, default);

        // The closed P1 is not pressure, and the untriaged job is not a P3.
        Assert.Equal(1, view.P1);
        Assert.Equal(0, view.P3);
        Assert.Equal(1, view.NotTriaged);

        // The list is P1 and P2 only — an untriaged job is a queue, not a fire.
        Assert.All(view.Pressing, row => Assert.Contains(row.Priority, new[] { Priority.P1, Priority.P2 }));
        Assert.Contains(view.Pressing, row => row.Id == urgent.Id.ToString());
        Assert.DoesNotContain(view.Pressing, row => row.Id == untriaged.Id.ToString());
    }

    [Fact]
    public async Task Due_soon_splits_by_urgency_and_leaves_a_job_with_no_deadline_out_of_both()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var scope = h.Scope();
        var now = h.Clock.GetUtcNow();

        var late = await h.RaiseNotCoolingAsync(scope);
        var soon = await h.RaiseNotCoolingAsync(scope);
        var far = await h.RaiseNotCoolingAsync(scope);
        var undated = await h.RaiseNotCoolingAsync(scope);

        late.DueAt = now.AddMinutes(-12);
        soon.DueAt = now.AddHours(1);
        far.DueAt = now.AddHours(5);
        undated.DueAt = null;
        await h.Db.SaveChangesAsync();

        var view = await Widgets(h).DueSoonAsync(scope, default);

        Assert.Equal(1, view.Overdue);
        Assert.Equal(1, view.DueWithinTwoHours);

        // Neither figure counts the job with no due_at: nothing about it is late
        // and nothing is close, and saying otherwise would invent a deadline.
        Assert.DoesNotContain(view.Late.Concat(view.Soon), row => row.Id == undated.Id.ToString());
        Assert.DoesNotContain(view.Soon, row => row.Id == far.Id.ToString());

        // The corrected frame's reading: late first, and the mark says which
        // side of now it is on.
        Assert.Equal(late.Id.ToString(), Assert.Single(view.Late).Id);
        Assert.StartsWith("+", view.Late[0].Mark);
        Assert.StartsWith("in ", view.Soon[0].Mark);
    }

    [Fact]
    public async Task Raised_today_counts_by_category_and_uses_the_propertys_own_midnight()
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var scope = h.Scope();

        // The harness's clock is 09:31 UTC, which is 15:01 in Kochi and 12:31 in
        // Doha — the same instant, the same day, so what this test proves is
        // that the day is asked of the property rather than assumed.
        h.Directory.Timezone = "Asia/Kolkata";

        var today = await h.RaiseNotCoolingAsync(scope);
        var yesterday = await h.RaiseNotCoolingAsync(scope);
        yesterday.CreatedAt = h.Clock.GetUtcNow().AddDays(-1);
        await h.Db.SaveChangesAsync();

        var view = await Widgets(h).RaisedTodayAsync(scope, default);

        Assert.Equal(1, view.Raised);
        Assert.Equal(0, view.Closed);

        var line = Assert.Single(view.ByCategory);
        Assert.Equal("Air conditioning", line.Name);
        Assert.Equal(1, line.Count);
        Assert.NotEqual(0, today.CategoryId.GetHashCode());

        // "Everything else · n categories" counts categories, never their jobs.
        Assert.Equal(0, view.OtherCategories);
    }
}
