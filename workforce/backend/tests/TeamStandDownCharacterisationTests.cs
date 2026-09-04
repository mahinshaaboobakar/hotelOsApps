using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Teams;
using HotelOS.Workforce.Domain;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Standing a team down, and the memberships a posting is holding open.
/// </summary>
/// <remarks>
/// The two halves of a consequence. <b>Standing down keeps its people</b> — a
/// seasonal crew returns with the same crew, so the second position of frame 5's
/// toggle is a separate decision rather than what the verb means. And a posting
/// can be <b>asked</b> what it is holding open, which is what lets a screen
/// state the consequence before the button instead of reporting it afterwards.
/// <para>
/// The read exists because the write already did this: <see
/// cref="TeamCharacterisationTests"/> covers the closing itself. What is new is
/// that anybody can find out in advance.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class TeamStandDownCharacterisationTests(WorkforceFixture fixture)
{
    private static int slot = -1;

    /// <summary>A department this test owns, and a name nobody else uses.</summary>
    private static string Somewhere() => $"D{Interlocked.Increment(ref slot)}";

    [Fact]
    public async Task Standing_a_team_down_keeps_its_members()
    {
        var (teams, postings, _) = Build();
        var scope = fixture.Scope();
        var department = Somewhere();
        var staff = Guid.NewGuid();

        await postings.CreateAsync(scope, Post(staff, department), default);
        var team = await teams.FormAsync(scope, Form(department, "Season Crew"), default);
        await teams.AddMemberAsync(scope, Membership(team.Id, staff), default);

        var down = await teams.SetActiveAsync(scope, Amend(team), false, default);

        Assert.False(down.Active);

        // The default, and the whole reason the verb is Deactivate rather than
        // disband: the crew that comes back is the crew that left.
        //
        // **Asked about tomorrow, not today.** A membership closed on a day is
        // in force <i>through</i> that day — a last day is a day worked, the
        // same convention the posting uses — so today cannot tell the two
        // toggle positions apart, and a pair of tests that both passed on today
        // would be a pair that proved nothing.
        Assert.Equal(
            staff,
            Assert.Single(await teams.MembersAsync(scope, team.Id, Tomorrow, default)));
    }

    [Fact]
    public async Task Standing_a_team_down_can_end_its_memberships_when_asked()
    {
        var (teams, postings, _) = Build();
        var scope = fixture.Scope();
        var department = Somewhere();
        var staff = Guid.NewGuid();

        await postings.CreateAsync(scope, Post(staff, department), default);
        var team = await teams.FormAsync(scope, Form(department, "Season Crew"), default);
        await teams.AddMemberAsync(scope, Membership(team.Id, staff), default);

        await teams.SetActiveAsync(scope, Amend(team), false, default, keepMembers: false);

        Assert.Empty(await teams.MembersAsync(scope, team.Id, Tomorrow, default));

        // Closed, not deleted. <i>Who was in this team in March</i> is a
        // question a report asks, and a row removed cannot answer it — so the
        // membership ends on a day rather than ceasing to have existed, and the
        // last day it covers still answers with the person in it.
        Assert.Single(await teams.MembersAsync(scope, team.Id, Today, default));
    }

    [Fact]
    public async Task A_posting_can_be_asked_what_it_is_holding_open()
    {
        var (teams, postings, _) = Build();
        var scope = fixture.Scope();
        var department = Somewhere();
        var staff = Guid.NewGuid();

        await postings.CreateAsync(scope, Post(staff, department), default);
        var first = await teams.FormAsync(scope, Form(department, "Morning Crew"), default);
        var second = await teams.FormAsync(scope, Form(department, "Tower Block"), default);

        await teams.AddMemberAsync(scope, Membership(first.Id, staff), default);
        await teams.AddMemberAsync(scope, Membership(second.Id, staff), default);

        var supported = await teams.SupportedTeamsAsync(scope, staff, department, Today, default);

        // Both, and each with the day it began — the panel lists them under a
        // heading that promises exactly this, so a read that answered with
        // names alone would leave the screen inventing the rest.
        Assert.Equal(
            [first.Id, second.Id],
            supported.Select(one => one.Team.Id).OrderBy(id => id == first.Id ? 0 : 1));
        Assert.All(supported, one => Assert.Equal(Today, one.Since));
    }

    [Fact]
    public async Task A_posting_holds_open_nothing_in_another_department()
    {
        var (teams, postings, _) = Build();
        var scope = fixture.Scope();
        var here = Somewhere();
        var elsewhere = Somewhere();
        var staff = Guid.NewGuid();

        await postings.CreateAsync(scope, Post(staff, here), default);
        var team = await teams.FormAsync(scope, Form(here, "Morning Crew"), default);
        await teams.AddMemberAsync(scope, Membership(team.Id, staff), default);

        // Ending the posting that supports nothing must say so, or a dialog
        // warns about a consequence that does not exist — which teaches a
        // supervisor to stop reading the warning that matters.
        Assert.Empty(await teams.SupportedTeamsAsync(scope, staff, elsewhere, Today, default));
    }

    [Fact]
    public async Task Asking_what_a_posting_holds_open_is_a_read()
    {
        var (teams, _, _) = Build(out var authorizer);
        var scope = fixture.Scope();

        await teams.SupportedTeamsAsync(scope, Guid.NewGuid(), Somewhere(), Today, default);

        // A question, not a decision. Requiring `posting.assign` to find out
        // what ending a posting would do puts the warning behind the authority
        // to ignore it.
        Assert.Equal("roster.read", Assert.Single(authorizer.Checks).Permission);
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static DateOnly Tomorrow => Today.AddDays(1);

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
