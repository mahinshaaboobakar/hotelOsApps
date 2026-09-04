using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Teams;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Chapter 04 §6: the login that arrives after the posting.
/// </summary>
/// <remarks>
/// The last part of the ratified <c>AUTHZ-Q20</c> contract, and the case that
/// makes the announcement complete rather than merely correct: a posting for
/// somebody with no account announces nothing, so without this the tuple never
/// arrives when the account does.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class StaffChangeCharacterisationTests(WorkforceFixture fixture)
{
    private static int slot = -1;

    private static string OwnDepartment() => $"SC{Interlocked.Increment(ref slot)}";

    [Fact]
    public async Task A_login_granted_later_announces_every_open_posting()
    {
        var world = Build();
        var scope = fixture.Scope();
        var staff = Guid.CreateVersion7();

        // Two postings, made while the person had no account. Both are complete
        // and correct, and neither announced anything.
        await world.Postings.CreateAsync(scope, Post(staff, OwnDepartment()), default);
        await world.Postings.CreateAsync(scope, Post(staff, OwnDepartment()), default);

        Assert.Empty(world.Events.Events);

        world.Directory.WithLogin(staff, Guid.CreateVersion7());
        var announced = await world.Consumer.IdentityLinkGainedAsync(scope, staff, default);

        // Nothing about the postings changed. What was missing was only ever the
        // announcement.
        Assert.Equal(2, announced);
        Assert.Equal(["user.posted", "user.posted"], world.Events.Types);
    }

    [Fact]
    public async Task A_headship_held_since_before_the_login_is_announced_too()
    {
        var world = Build();
        var scope = fixture.Scope();
        var staff = Guid.CreateVersion7();

        await world.Postings.CreateAsync(
            scope, Post(staff, OwnDepartment()) with { IsDepartmentHead = true }, default);

        world.Directory.WithLogin(staff, Guid.CreateVersion7());
        await world.Consumer.IdentityLinkGainedAsync(scope, staff, default);

        // Both grant kinds, because both were true before the account existed.
        Assert.Equal(["user.posted", "user.headship_started"], world.Events.Types);
    }

    [Fact]
    public async Task A_posting_that_already_ended_is_not_announced()
    {
        var world = Build();
        var scope = fixture.Scope();
        var staff = Guid.CreateVersion7();

        var posting = await world.Postings.CreateAsync(
            scope, Post(staff, OwnDepartment()), default);

        await world.Postings.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = posting.Version,
                EffectiveTo = posting.EffectiveFrom.AddDays(10),
            },
            default);

        world.Directory.WithLogin(staff, Guid.CreateVersion7());
        var announced = await world.Consumer.IdentityLinkGainedAsync(scope, staff, default);

        // It grants nothing, and announcing it would write a tuple whose
        // withdrawal already happened and will not come again.
        Assert.Equal(0, announced);
        Assert.Empty(world.Events.Events);
    }

    [Fact]
    public async Task A_posting_that_starts_next_week_is_announced_now()
    {
        var world = Build();
        var scope = fixture.Scope();
        var staff = Guid.CreateVersion7();

        await world.Postings.CreateAsync(
            scope,
            Post(staff, OwnDepartment()) with
            {
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
            },
            default);

        world.Directory.WithLogin(staff, Guid.CreateVersion7());

        // Open, not started. The tuple is what makes next Monday work, and
        // waiting for Monday needs a scheduler nobody has asked for.
        Assert.Equal(1, await world.Consumer.IdentityLinkGainedAsync(scope, staff, default));
    }

    [Fact]
    public async Task Losing_a_login_withdraws_what_gaining_it_granted()
    {
        var world = Build();
        var scope = fixture.Scope();
        var staff = Guid.CreateVersion7();

        await world.Postings.CreateAsync(
            scope, Post(staff, OwnDepartment()) with { IsDepartmentHead = true }, default);

        world.Directory.WithLogin(staff, Guid.CreateVersion7());
        await world.Consumer.IdentityLinkGainedAsync(scope, staff, default);
        await world.Consumer.IdentityLinkRemovedAsync(scope, staff, default);

        // Invariant 2: both directions, or the contract is not done. An account
        // removed while its tuples stand is somebody keeping departmental access
        // they no longer have a login for.
        Assert.Equal(
            ["user.posted", "user.headship_started", "user.posting_ended", "user.headship_ended"],
            world.Events.Types);
    }

    [Fact]
    public async Task A_staff_exit_ends_the_postings_and_announces_through_the_ordinary_path()
    {
        var world = Build();
        var scope = fixture.Scope();
        var staff = Guid.CreateVersion7();

        world.Directory.WithLogin(staff, Guid.CreateVersion7());

        var posting = await world.Postings.CreateAsync(
            scope, Post(staff, OwnDepartment()), default);
        var lastDay = posting.EffectiveFrom.AddDays(200);

        var ended = await world.Consumer.StaffExitedAsync(scope, staff, lastDay, default);

        Assert.Equal(1, ended);
        Assert.Equal(["user.posted", "user.posting_ended"], world.Events.Types);

        // Ended, never deleted: a rota worked last March was worked under this
        // posting, and a person leaving does not unmake the months they were here.
        var kept = await world.Postings.GetAsync(scope, posting.Id, default);
        Assert.Equal(lastDay, kept.EffectiveTo);
    }

    [Fact]
    public async Task An_exit_dated_before_a_posting_began_ends_it_at_its_own_start()
    {
        var world = Build();
        var scope = fixture.Scope();
        var staff = Guid.CreateVersion7();

        var posting = await world.Postings.CreateAsync(
            scope, Post(staff, OwnDepartment()), default);

        await world.Consumer.StaffExitedAsync(
            scope, staff, posting.EffectiveFrom.AddDays(-30), default);

        // A person whose exit predates a posting is a record that cannot be true,
        // and the posting's own start is the earliest honest end. Refusing
        // outright would leave the posting open for somebody who has left.
        var kept = await world.Postings.GetAsync(scope, posting.Id, default);
        Assert.Equal(posting.EffectiveFrom, kept.EffectiveTo);
    }

    [Fact]
    public async Task Reconciling_somebody_with_no_login_still_announces_nothing()
    {
        var world = Build();
        var scope = fixture.Scope();
        var staff = Guid.CreateVersion7();

        await world.Postings.CreateAsync(scope, Post(staff, OwnDepartment()), default);

        // The gate is in the announcer, so it holds on every path into it — a
        // consumer told about the wrong person cannot invent an account.
        await world.Consumer.IdentityLinkGainedAsync(scope, staff, default);

        Assert.Empty(world.Events.Events);
    }

    private static CreatePostingCommand Post(Guid staff, string department) => new()
    {
        StaffId = staff,
        DepartmentCode = department,
        JobRole = "Attendant",
        EffectiveFrom = new DateOnly(2026, 6, 1),
    };

    private sealed record World(
        PostingService Postings,
        StaffChangeConsumer Consumer,
        StaffDirectoryDouble Directory,
        RecordingEventAppender Events);

    private World Build()
    {
        var db = fixture.Context();
        var directory = new StaffDirectoryDouble();
        var events = new RecordingEventAppender();
        var announcer = new PostingAnnouncer(events, directory);

        return new World(
            new PostingService(
                db, new RecordingAuthorizer(), directory, announcer,
                new TeamService(
                    db, new RecordingAuthorizer(), directory, TimeProvider.System),
                TimeProvider.System),
            new StaffChangeConsumer(db, announcer, TimeProvider.System),
            directory,
            events);
    }
}
