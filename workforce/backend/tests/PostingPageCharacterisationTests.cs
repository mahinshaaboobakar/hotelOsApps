using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Teams;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// One page of postings, and the numbers the pager draws itself from.
/// </summary>
/// <remarks>
/// <para>
/// People is the one Workforce list whose length is the property's headcount
/// rather than a day, a week or a department — <c>CORE-Q13</c> and the app
/// surface standard §6 — so it is the one list that pages, and paged rather
/// than cursor because the count is a fact.
/// </para>
/// <para>
/// What is held still here is the part a client cannot check: the size the
/// service <b>applied</b> after its clamp, and a total that counts the query
/// rather than the page. A pager numbered from what the caller asked for is
/// wrong on every button while the list underneath looks perfect.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class PostingPageCharacterisationTests(WorkforceFixture fixture)
{
    private static readonly DateOnly September = new(2026, 9, 1);

    [Fact]
    public async Task An_absent_page_is_the_first_page_at_the_default_size()
    {
        var service = Build();
        var scope = Property();
        await Post(service, scope, 30);

        var page = await service.ListPageAsync(scope, new ListPostingsQuery(), default);

        // A caller that does not page still works, and gets told what it got.
        Assert.Equal(0, page.Page);
        Assert.Equal(25, page.Size);
        Assert.Equal(30, page.Total);
        Assert.Equal(25, page.Postings.Count);
    }

    [Fact]
    public async Task The_total_counts_the_query_and_not_the_page()
    {
        var service = Build();
        var scope = Property();
        await Post(service, scope, 30);

        var page = await service.ListPageAsync(
            scope, new ListPostingsQuery { Paging = new PagedQuery(1, 25) }, default);

        // Five rows on the last page, and the total still says thirty. This is
        // the number the pager renders as "of 30"; taken from the rows in front
        // of it, the last page would claim the property had five people.
        Assert.Equal(5, page.Postings.Count);
        Assert.Equal(30, page.Total);
    }

    [Fact]
    public async Task An_oversized_request_is_clamped_and_the_applied_size_is_returned()
    {
        var service = Build();
        var scope = Property();
        await Post(service, scope, 4);

        var page = await service.ListPageAsync(
            scope, new ListPostingsQuery { Paging = new PagedQuery(0, 5_000) }, default);

        // The size is a REQUEST, not an instruction. Echoing back the clamped
        // value is what keeps the client's page count right: a pager that
        // divided the total by the size it asked for would compute one page
        // where the server will serve fifty.
        Assert.Equal(100, page.Size);
        Assert.Equal(4, page.Postings.Count);
    }

    [Fact]
    public async Task A_page_beyond_the_end_serves_the_last_one()
    {
        var service = Build();
        var scope = Property();
        await Post(service, scope, 12);

        var page = await service.ListPageAsync(
            scope, new ListPostingsQuery { Paging = new PagedQuery(9, 5) }, default);

        // Not an empty page and not a refusal: a stale link, a deleted posting
        // or a filter applied while somebody was on page nine all land here, and
        // an empty list under a pager saying "of 12" reads as data loss.
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.Postings.Count);
    }

    [Fact]
    public async Task An_empty_property_is_page_zero_of_nothing()
    {
        var service = Build();

        var page = await service.ListPageAsync(Property(), new ListPostingsQuery(), default);

        // Zero total, page zero, and the SIZE still stated — the screen decides
        // to draw no pager at all from these numbers, so they have to be real
        // rather than a special case the client has to recognise.
        Assert.Equal(0, page.Total);
        Assert.Equal(0, page.Page);
        Assert.Equal(25, page.Size);
        Assert.Empty(page.Postings);
    }

    [Fact]
    public async Task The_page_holds_the_same_rows_the_unpaged_list_would()
    {
        var service = Build();
        var scope = Property();
        await Post(service, scope, 9);

        var all = await service.ListAsync(scope, new ListPostingsQuery(), default);
        var first = await service.ListPageAsync(
            scope, new ListPostingsQuery { Paging = new PagedQuery(0, 4) }, default);
        var second = await service.ListPageAsync(
            scope, new ListPostingsQuery { Paging = new PagedQuery(1, 4) }, default);

        // Paging is a window on one ordering, not a second query with an order
        // of its own. Two orderings would let a row appear on both pages and
        // another appear on neither, which nothing on the screen could reveal.
        Assert.Equal(
            all.Take(8).Select(one => one.Id),
            first.Postings.Concat(second.Postings).Select(one => one.Id));
    }

    /// <summary>A property of this test's own, with nothing else posted in it.</summary>
    /// <remarks>
    /// The fixture's <c>Scope()</c> is one property shared by the whole
    /// collection, which every other suite here can use because it asserts on
    /// the rows it created. These assert on a <b>total</b>, so a posting made by
    /// any other test in the collection changes the answer — and the failure
    /// would arrive as an off-by-a-few page count in whichever suite happened to
    /// run second.
    /// </remarks>
    private static RequestScope Property() => new()
    {
        Caller = CallerKind.User,
        PropertyId = Guid.CreateVersion7(),
        UserId = Guid.CreateVersion7(),
    };

    private async Task Post(PostingService service, RequestScope scope, int count)
    {
        for (var index = 0; index < count; index += 1)
        {
            await service.CreateAsync(
                scope,
                new CreatePostingCommand
                {
                    StaffId = Guid.CreateVersion7(),
                    DepartmentCode = "FO",
                    JobRole = "Receptionist",
                    EffectiveFrom = September,
                },
                default);
        }
    }

    private PostingService Build()
    {
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();

        return new PostingService(
            fixture.Context(), authorizer, directory,
            new PostingAnnouncer(new RecordingEventAppender(), directory),
            new TeamService(fixture.Context(), authorizer, directory, TimeProvider.System),
            TimeProvider.System);
    }
}
