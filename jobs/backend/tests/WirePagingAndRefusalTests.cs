using HotelOS.Jobs.Domain;
using HotelOS.Platform;
using Grpc.Core;
using HotelOS.Jobs.Contracts.V1;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// The wire round's list, refusal and empty-state rows: pagination past the
/// first page and at its exact boundary, a list that grows while you are on
/// page two, a filter that matches nothing, and every refusal a screen can
/// meet — validation, conflict, not found, and a permission the caller does
/// not hold.
/// </summary>
[Collection(JobsCollection.Name)]
public class WirePagingAndRefusalTests(JobsFixture fixture)
{
    private static async Task<string> ItemAsync(WireHarness h)
    {
        var category = await h.Client.SaveCategoryAsync(new SaveCategoryRequest
        {
            Context = h.Context(), Code = "AC", Name = "Air conditioning", DepartmentCode = "ENG", Active = true,
        });
        var item = await h.Client.SaveItemAsync(new SaveItemRequest
        {
            Context = h.Context(), CategoryId = category.Id, Code = "AC_NOT_COOLING", Name = "Not cooling",
            DefaultPriority = "P2", DueWithinMinutes = 40, PhotoOnCompletion = "OPTIONAL", Active = true,
        });
        return item.Id;
    }

    private static async Task<JobView> RaiseAsync(WireHarness h, string itemId, string summary)
        => await h.Client.RaiseJobAsync(new RaiseJobRequest
        {
            Context = h.Context(), ItemId = itemId, LocationId = Guid.CreateVersion7().ToString(), Summary = summary,
            RaisedVia = RaisedVia.App, RaisedKind = RaisedKind.Staff, RaisedById = h.PropertyId.ToString(),
        });

    private static ListJobsRequest Page(WireHarness h, int page, int size) => new()
    {
        Context = h.Context(),
        // CORE-Q13: the platform's pair, not this service's own numbers.
        Paging = new HotelOS.Contracts.Common.V1.PagedRequest { Page = page, PageSize = size },
    };

    [Fact]
    public async Task An_empty_property_reads_as_empty_not_as_a_failure()
    {
        await using var h = await WireHarness.StartAsync(fixture);

        var first = await h.Client.ListJobsAsync(Page(h, 0, 12));

        Assert.Empty(first.Jobs);
        Assert.Equal(0, first.Paging.Total);
        // The reply echoes the size it applied, so a pager never divides by a guess.
        Assert.Equal(12, first.Paging.PageSize);
        Assert.Equal(0, first.Paging.Page);
    }

    [Fact(Skip = "Held on SHELL-Q37, the wall FF met: the platform's `event_store` tables are provisioned by deployment SQL, not by any migration or test convention an installed application can run, so the real event appender cannot write on a scratch database and every operation that announces an event stops at `relation \"event_store.events\" does not exist`. Reported, not worked around — a double here would be the green over an absent dependency ADR 0053 forbids.")]
    public async Task Pagination_walks_past_the_first_page_lands_on_the_boundary_and_ends_empty()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var itemId = await ItemAsync(h);
        for (var i = 1; i <= 25; i += 1) await RaiseAsync(h, itemId, $"job {i}");

        var first = await h.Client.ListJobsAsync(Page(h, 0, 12));
        var second = await h.Client.ListJobsAsync(Page(h, 1, 12));
        var boundary = await h.Client.ListJobsAsync(Page(h, 2, 12));
        var past = await h.Client.ListJobsAsync(Page(h, 3, 12));

        Assert.Equal(25, first.Paging.Total);
        Assert.Equal(12, first.Jobs.Count);
        Assert.Equal(12, second.Jobs.Count);
        // The exact boundary: 25 rows, twelve to a page, one left over.
        Assert.Single(boundary.Jobs);
        Assert.Empty(past.Jobs);
        Assert.Equal(25, past.Paging.Total);
        Assert.Equal(3, past.Paging.Page);
        // No row appears twice across the pages.
        var seen = first.Jobs.Concat(second.Jobs).Concat(boundary.Jobs).Select(j => j.Id).ToList();
        Assert.Equal(25, seen.Distinct().Count());
    }

    [Fact(Skip = "Held on SHELL-Q37, the wall FF met: the platform's `event_store` tables are provisioned by deployment SQL, not by any migration or test convention an installed application can run, so the real event appender cannot write on a scratch database and every operation that announces an event stops at `relation \"event_store.events\" does not exist`. Reported, not worked around — a double here would be the green over an absent dependency ADR 0053 forbids.")]
    public async Task A_list_that_grows_while_you_are_on_page_two_reports_the_new_total()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var itemId = await ItemAsync(h);
        for (var i = 1; i <= 14; i += 1) await RaiseAsync(h, itemId, $"job {i}");

        var second = await h.Client.ListJobsAsync(Page(h, 1, 12));
        Assert.Equal((14, 2), (second.Paging.Total, second.Jobs.Count));

        await RaiseAsync(h, itemId, "raised while you were reading page two");

        var again = await h.Client.ListJobsAsync(Page(h, 1, 12));
        Assert.Equal(15, again.Paging.Total);
        Assert.Equal(3, again.Jobs.Count);
    }

    [Fact(Skip = "Held on SHELL-Q37, the wall FF met: the platform's `event_store` tables are provisioned by deployment SQL, not by any migration or test convention an installed application can run, so the real event appender cannot write on a scratch database and every operation that announces an event stops at `relation \"event_store.events\" does not exist`. Reported, not worked around — a double here would be the green over an absent dependency ADR 0053 forbids.")]
    public async Task A_filter_that_matches_nothing_returns_nothing_and_says_so_in_the_total()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var itemId = await ItemAsync(h);
        await RaiseAsync(h, itemId, "one engineering job");

        var engineering = await h.Client.ListJobsAsync(new ListJobsRequest { Context = h.Context(), DepartmentCode = "ENG" });
        var housekeeping = await h.Client.ListJobsAsync(new ListJobsRequest { Context = h.Context(), DepartmentCode = "HK" });
        var closed = await h.Client.ListJobsAsync(new ListJobsRequest { Context = h.Context(), Statuses = { "CLOSED" } });
        var scheduled = await h.Client.ListJobsAsync(new ListJobsRequest { Context = h.Context(), ScheduledOnly = true });

        Assert.Single(engineering.Jobs);
        Assert.Empty(housekeeping.Jobs);
        Assert.Equal(0, housekeeping.Paging.Total);
        Assert.Empty(closed.Jobs);
        Assert.Empty(scheduled.Jobs);
    }

    [Fact]
    public async Task A_validation_failure_crosses_the_wire_as_the_service_own_sentence()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var itemId = await ItemAsync(h);

        var refusal = await Assert.ThrowsAsync<RpcException>(() => h.Client.RaiseJobAsync(new RaiseJobRequest
        {
            Context = h.Context(), ItemId = itemId, LocationId = Guid.CreateVersion7().ToString(), Summary = "  ",
            RaisedVia = RaisedVia.App, RaisedKind = RaisedKind.Staff,
        }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, refusal.StatusCode);
        Assert.Contains("summary is required", refusal.Status.Detail, StringComparison.Ordinal);

        var guest = await Assert.ThrowsAsync<RpcException>(() => h.Client.RaiseJobAsync(new RaiseJobRequest
        {
            Context = h.Context(), ItemId = itemId, LocationId = Guid.CreateVersion7().ToString(), Summary = "warm",
            RaisedVia = RaisedVia.GuestApp, RaisedKind = RaisedKind.Guest,
        }).ResponseAsync);
        Assert.Contains("stay_id", guest.Status.Detail, StringComparison.Ordinal);
    }

    [Fact(Skip = "Held on SHELL-Q37, the wall FF met: the platform's `event_store` tables are provisioned by deployment SQL, not by any migration or test convention an installed application can run, so the real event appender cannot write on a scratch database and every operation that announces an event stops at `relation \"event_store.events\" does not exist`. Reported, not worked around — a double here would be the green over an absent dependency ADR 0053 forbids.")]
    public async Task Two_edits_to_one_job_and_the_second_surfaces_the_conflict()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var itemId = await ItemAsync(h);
        var job = await RaiseAsync(h, itemId, "one job, two editors");
        var asBothSaw = job.Version;

        var first = await h.Client.AmendJobAsync(new AmendJobRequest
        {
            Context = h.Context(), Id = job.Id, ExpectedVersion = asBothSaw, Priority = "P1",
        });
        Assert.Equal("P1", first.Priority);

        var second = await Assert.ThrowsAsync<RpcException>(() => h.Client.AmendJobAsync(new AmendJobRequest
        {
            Context = h.Context(), Id = job.Id, ExpectedVersion = asBothSaw, Priority = "P3",
        }).ResponseAsync);

        // The second edit is refused, not silently applied over the first.
        Assert.Equal(StatusCode.Aborted, second.StatusCode);
        var after = await h.Client.GetJobAsync(new GetJobRequest { Context = h.Context(), Id = job.Id });
        Assert.Equal("P1", after.Job.Priority);
    }

    [Fact(Skip = "Held on SHELL-Q37, the wall FF met: the platform's `event_store` tables are provisioned by deployment SQL, not by any migration or test convention an installed application can run, so the real event appender cannot write on a scratch database and every operation that announces an event stops at `relation \"event_store.events\" does not exist`. Reported, not worked around — a double here would be the green over an absent dependency ADR 0053 forbids.")]
    public async Task A_job_that_does_not_exist_and_one_of_another_property_are_both_not_found()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var itemId = await ItemAsync(h);
        var mine = await RaiseAsync(h, itemId, "this property's job");

        var missing = await Assert.ThrowsAsync<RpcException>(() => h.Client.GetJobAsync(new GetJobRequest
        {
            Context = h.Context(), Id = Guid.CreateVersion7().ToString(),
        }).ResponseAsync);
        Assert.Equal(StatusCode.NotFound, missing.StatusCode);

        var elsewhere = await Assert.ThrowsAsync<RpcException>(() => h.Client.GetJobAsync(new GetJobRequest
        {
            Context = h.OtherProperty(), Id = mine.Id,
        }).ResponseAsync);
        Assert.Equal(StatusCode.NotFound, elsewhere.StatusCode);
    }

    [Fact(Skip = "Held on SHELL-Q37, the wall FF met: the platform's `event_store` tables are provisioned by deployment SQL, not by any migration or test convention an installed application can run, so the real event appender cannot write on a scratch database and every operation that announces an event stops at `relation \"event_store.events\" does not exist`. Reported, not worked around — a double here would be the green over an absent dependency ADR 0053 forbids.")]
    public async Task A_permission_the_caller_does_not_hold_is_refused_by_the_service()
    {
        await using var h = await WireHarness.StartAsync(fixture);
        var itemId = await ItemAsync(h);
        h.Authorizer.Deny.Add("job.create");

        var refusal = await Assert.ThrowsAsync<RpcException>(() => h.Client.RaiseJobAsync(new RaiseJobRequest
        {
            Context = h.Context(), ItemId = itemId, LocationId = Guid.CreateVersion7().ToString(), Summary = "denied",
            RaisedVia = RaisedVia.App, RaisedKind = RaisedKind.Staff,
        }).ResponseAsync);

        Assert.Equal(StatusCode.PermissionDenied, refusal.StatusCode);
        Assert.Contains("job.create", refusal.Status.Detail, StringComparison.Ordinal);
    }

    [Fact(Skip = "Held on SHELL-Q37, the wall FF met: the platform's `event_store` tables are provisioned by deployment SQL, not by any migration or test convention an installed application can run, so the real event appender cannot write on a scratch database and every operation that announces an event stops at `relation \"event_store.events\" does not exist`. Reported, not worked around — a double here would be the green over an absent dependency ADR 0053 forbids.")]
    public async Task A_restart_loses_nothing_the_backend_owned()
    {
        var propertyJobs = new List<string>();
        string itemId;
        string number;
        await using (var before = await WireHarness.StartAsync(fixture))
        {
            itemId = await ItemAsync(before);
            var job = await RaiseAsync(before, itemId, "raised before the restart");
            number = job.JobNumber;
            propertyJobs.Add(job.Id);

            // The next number comes from the property's counter, not memory.
            var second = await RaiseAsync(before, itemId, "and another");
            propertyJobs.Add(second.Id);
        }

        // A second host, a new channel, a new connection pool — the same rows.
        await using var after = await WireHarness.StartAsync(fixture);
        foreach (var id in propertyJobs)
        {
            var refused = await Assert.ThrowsAsync<RpcException>(() => after.Client.GetJobAsync(new GetJobRequest
            {
                Context = after.Context(), Id = id,
            }).ResponseAsync);
            // A new harness is a new property: the rows survive, and they belong
            // to the property that raised them.
            Assert.Equal(StatusCode.NotFound, refused.StatusCode);
        }

        await using var db = after.Db();
        Assert.Equal(2, await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.Jobs, j => propertyJobs.Contains(j.Id.ToString())));
        Assert.StartsWith("MRN-ENG-", number, StringComparison.Ordinal);
    }
}
