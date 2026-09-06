using HotelOS.Jobs.Application.Configuration;
using HotelOS.Jobs.Events;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// The general manager's action — design §4.2. What Jobs must get right is the
/// <b>announcement</b>: the Kernel folds it into <c>property#jobs_manager</c>,
/// and this application never writes the tuple, so a wrong body is a grant that
/// silently lands on the wrong person or the wrong hotel.
/// </summary>
[Collection(JobsCollection.Name)]
public class JobsManagerGrantTests(JobsFixture fixture)
{
    [Fact]
    public async Task Granting_announces_the_person_in_the_body_and_never_the_administrator()
    {
        var h = new JobsHarness(fixture);
        var generalManager = Guid.CreateVersion7();
        var newManager = Guid.CreateVersion7();
        var scope = h.Scope(generalManager);

        var granted = await h.Grants.GrantAsync(scope, newManager, default);

        Assert.Equal(newManager, granted.UserId);
        Assert.Equal(generalManager, granted.GrantedBy);
        Assert.True(granted.IsLive);

        var announced = Assert.Single(h.Events.Events);
        Assert.Equal(EventTypes.JobsManagerGranted, announced.EventType);
        Assert.Equal(EventTypes.PropertyAggregate, announced.AggregateType);
        Assert.Equal(h.PropertyId, announced.AggregateId);

        // The body carries both ends, and the person is the one being granted.
        // Announcing the envelope's actor instead would grant the general
        // manager themself — the mistake the Kernel's own note says "would look
        // correct in every test where the two coincide", which is why this test
        // uses two different ids.
        var body = Assert.IsType<JobsManagerAnnouncement>(announced.Payload);
        Assert.Equal(newManager, body.UserId);
        Assert.Equal(h.PropertyId, body.PropertyId);
    }

    [Fact]
    public async Task Granting_twice_leaves_one_grant_and_announces_once()
    {
        var h = new JobsHarness(fixture);
        var scope = h.Scope();
        var person = Guid.CreateVersion7();

        var first = await h.Grants.GrantAsync(scope, person, default);
        var second = await h.Grants.GrantAsync(scope, person, default);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(h.Events.Events);
        Assert.Single(await h.Db.JobsManagerGrants.Where(g => g.PropertyId == h.PropertyId).ToListAsync());
    }

    [Fact]
    public async Task Revoking_keeps_the_row_and_lets_the_grant_be_made_again()
    {
        var h = new JobsHarness(fixture);
        var scope = h.Scope();
        var person = Guid.CreateVersion7();

        await h.Grants.GrantAsync(scope, person, default);
        var revoked = await h.Grants.RevokeAsync(scope, person, default);

        Assert.NotNull(revoked);
        Assert.NotNull(revoked!.RevokedAt);
        Assert.Equal(scope.UserId, revoked.RevokedBy);
        Assert.Empty(await h.Grants.LiveAsync(scope, default));

        // Revoking what is already gone succeeds and says nothing happened —
        // a double-click is not a support question.
        Assert.Null(await h.Grants.RevokeAsync(scope, person, default));

        // And the history is still there to be granted over: the unique index
        // is partial, so grant → revoke → grant is an ordinary sequence.
        var again = await h.Grants.GrantAsync(scope, person, default);
        Assert.NotEqual(revoked.Id, again.Id);
        Assert.Equal(2, await h.Db.JobsManagerGrants.CountAsync(g => g.PropertyId == h.PropertyId && g.UserId == person));

        Assert.Equal(
            [EventTypes.JobsManagerGranted, EventTypes.JobsManagerRevoked, EventTypes.JobsManagerGranted],
            h.Events.Types);
    }

    [Fact]
    public async Task A_grant_is_a_persons_action_and_a_background_scope_cannot_make_one()
    {
        var h = new JobsHarness(fixture);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => h.Grants.GrantAsync(h.Sweeping, Guid.CreateVersion7(), default));
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => h.Grants.GrantAsync(h.Scope(), Guid.Empty, default));

        Assert.Empty(h.Events.Events);
    }

    [Fact]
    public async Task One_propertys_grant_is_not_anothers()
    {
        var h = new JobsHarness(fixture);
        var elsewhere = new JobsHarness(fixture);
        var person = Guid.CreateVersion7();

        await h.Grants.GrantAsync(h.Scope(), person, default);

        Assert.Single(await h.Grants.LiveAsync(h.Scope(), default));
        Assert.Empty(await elsewhere.Grants.LiveAsync(elsewhere.Scope(), default));
    }
}
