using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Teams;
using HotelOS.Workforce.Domain;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Teams — Workforce's, whole, and a membership that cannot outlive its posting.
/// </summary>
/// <remarks>
/// Ruled 2026-09-04 on Jobs' <c>S3-D1</c>. The rules worth holding still are the
/// three invariants: a member is assignable in the team's own department, one
/// live membership per person per team, and <b>ending a posting ends the
/// membership</b> — the third being the one that would otherwise route work to
/// somebody who left the department last month.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class TeamCharacterisationTests(WorkforceFixture fixture)
{
    private static int slot = -1;

    /// <summary>A department this test owns, and a name nobody else uses.</summary>
    /// <remarks>
    /// The suite shares one property in one scratch database, and a team's name
    /// is unique per department — so two tests forming "Team A" in <c>HK</c>
    /// would fail on each other rather than on anything they assert.
    /// </remarks>
    private static string Somewhere() => $"T{Interlocked.Increment(ref slot)}";

    [Fact]
    public async Task A_team_belongs_to_a_department_this_property_has_activated()
    {
        var (teams, _, directory) = Build();
        var scope = fixture.Scope();
        var department = Somewhere();

        directory.Unactivated.Add(department);

        // Resolved rather than trusted, exactly as a posting's department is: a
        // team nothing can route work to is a team that should not exist.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => teams.FormAsync(scope, Form(department, "Team A"), default));
    }

    [Fact]
    public async Task Two_live_teams_in_one_department_cannot_share_a_name()
    {
        var (teams, _, _) = Build();
        var scope = fixture.Scope();
        var department = Somewhere();

        await teams.FormAsync(scope, Form(department, "Morning Crew"), default);

        // A supervisor picking "Morning Crew" from two identical entries is
        // choosing at random, and the job goes to whichever the list ordered
        // first.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => teams.FormAsync(scope, Form(department, "Morning Crew"), default));
    }

    [Fact]
    public async Task A_member_must_hold_a_posting_in_the_teams_own_department()
    {
        var (teams, postings, _) = Build();
        var scope = fixture.Scope();
        var here = Somewhere();
        var elsewhere = Somewhere();

        var team = await teams.FormAsync(scope, Form(here, "Team A"), default);
        var person = Guid.CreateVersion7();

        await postings.CreateAsync(scope, Post(person, elsewhere), default);

        // A team exists to receive work in its department. A member who cannot
        // be assigned there is a row that lies.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => teams.AddMemberAsync(scope, Membership(team.Id, person), default));
    }

    [Fact]
    public async Task The_posting_is_checked_on_the_day_the_membership_starts()
    {
        var (teams, postings, _) = Build();
        var scope = fixture.Scope();
        var department = Somewhere();

        var team = await teams.FormAsync(scope, Form(department, "Next Week"), default);
        var person = Guid.CreateVersion7();

        await postings.CreateAsync(
            scope, Post(person, department) with { EffectiveFrom = Today.AddDays(7) }, default);

        // Not against today: a supervisor forming next week's crew on Friday is
        // describing next week, and checking today would refuse the person who
        // starts on Monday.
        var member = await teams.AddMemberAsync(
            scope,
            Membership(team.Id, person) with { On = Today.AddDays(7) },
            default);

        Assert.Equal(Today.AddDays(7), member.JoinedOn);
        Assert.Null(member.LeftOn);
    }

    [Fact]
    public async Task Somebody_is_in_a_team_once()
    {
        var (teams, postings, _) = Build();
        var scope = fixture.Scope();
        var department = Somewhere();

        var team = await teams.FormAsync(scope, Form(department, "Team A"), default);
        var person = Guid.CreateVersion7();

        await postings.CreateAsync(scope, Post(person, department), default);
        await teams.AddMemberAsync(scope, Membership(team.Id, person), default);

        // A second live row would let a removal close one and leave the other
        // standing, and the person would still be in the team.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => teams.AddMemberAsync(scope, Membership(team.Id, person), default));
    }

    [Fact]
    public async Task Ending_a_posting_ends_the_membership_it_supported()
    {
        var (teams, postings, _) = Build();
        var scope = fixture.Scope();
        var department = Somewhere();

        var team = await teams.FormAsync(scope, Form(department, "Team A"), default);
        var person = Guid.CreateVersion7();

        var posting = await postings.CreateAsync(scope, Post(person, department), default);
        await teams.AddMemberAsync(scope, Membership(team.Id, person), default);

        Assert.Single(await teams.MembersAsync(scope, team.Id, Today, default));

        await postings.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = posting.Version,
                EffectiveTo = Today,
            },
            default);

        // **The invariant that would otherwise route work to somebody who left.**
        // Closed in the posting's own transaction, not by a nightly job — the
        // two facts commit together or neither does.
        Assert.Empty(await teams.MembersAsync(scope, team.Id, Today.AddDays(1), default));

        // And the row survives, because "who was in this team in March" is a
        // question a report asks.
        Assert.Single(await teams.MembersAsync(scope, team.Id, Today, default));
    }

    [Fact]
    public async Task A_posting_ending_elsewhere_leaves_the_membership_alone()
    {
        var (teams, postings, _) = Build();
        var scope = fixture.Scope();
        var here = Somewhere();
        var elsewhere = Somewhere();

        var team = await teams.FormAsync(scope, Form(here, "Team A"), default);
        var person = Guid.CreateVersion7();

        await postings.CreateAsync(scope, Post(person, here), default);
        var second = await postings.CreateAsync(scope, Post(person, elsewhere), default);
        await teams.AddMemberAsync(scope, Membership(team.Id, person), default);

        await postings.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = second.Id,
                ExpectedVersion = second.Version,
                EffectiveTo = Today,
            },
            default);

        // `WF-Q3` allows a second posting. Ending one must not empty a team in
        // the department the person still works in.
        Assert.Single(await teams.MembersAsync(scope, team.Id, Today.AddDays(1), default));
    }

    [Fact]
    public async Task A_team_stands_down_and_back_up()
    {
        var (teams, _, _) = Build();
        var scope = fixture.Scope();
        var department = Somewhere();

        var team = await teams.FormAsync(scope, Form(department, "Season Crew"), default);

        var down = await teams.SetActiveAsync(scope, Amend(team), false, default);
        Assert.False(down.Active);
        Assert.Empty(await teams.ListAsync(scope, department, includeInactive: false, default));
        Assert.Single(await teams.ListAsync(scope, department, includeInactive: true, default));

        // ADR 0062 §22 · 2: a deactivate with no counterpart states a capability
        // in the schema and withholds it from the service.
        var up = await teams.SetActiveAsync(scope, Amend(down), true, default);
        Assert.True(up.Active);
        Assert.Single(await teams.ListAsync(scope, department, includeInactive: false, default));
    }

    [Fact]
    public async Task Forming_a_team_asks_for_posting_assign()
    {
        var (teams, _, _) = Build(out var authorizer);
        var scope = fixture.Scope();

        await teams.FormAsync(scope, Form(Somewhere(), "Team A"), default);

        // The same authority a posting needs, and no thirteenth permission: who
        // works where, and with whom, is one question.
        Assert.Equal("posting.assign", Assert.Single(authorizer.Checks).Permission);
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static FormTeamCommand Form(string department, string name) =>
        new() { DepartmentCode = department, Name = name };

    private static AmendTeamCommand Amend(Team team) =>
        new() { Id = team.Id, ExpectedVersion = team.Version };

    private static TeamMembershipCommand Membership(Guid teamId, Guid staffId) =>
        new() { TeamId = teamId, StaffId = staffId, On = Today };

    private static CreatePostingCommand Post(Guid staff, string department) =>
        new()
        {
            StaffId = staff,
            DepartmentCode = department,
            JobRole = "Attendant",

            // Relative to today, because these tests assert against the real
            // clock: a fixed year in the future is a posting that is in force
            // in no test that asks about now.
            EffectiveFrom = Today.AddYears(-1),
        };

    private (TeamService Teams, PostingService Postings, StaffDirectoryDouble Directory)
        Build() => Build(out _);

    private (TeamService Teams, PostingService Postings, StaffDirectoryDouble Directory)
        Build(out RecordingAuthorizer authorizer)
    {
        authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();
        var db = fixture.Context();
        var teams = new TeamService(db, authorizer, directory, TimeProvider.System);

        return (
            teams,
            new PostingService(
                db, authorizer, directory,
                new PostingAnnouncer(new RecordingEventAppender(), directory),
                teams,
                TimeProvider.System),
            directory);
    }
}
