using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Teams;

/// <summary>
/// Teams — a named group of posted staff in one department, formed to be
/// assigned work.
/// </summary>
/// <remarks>
/// <para>
/// Ruled 2026-09-04 on Jobs' <c>S3-D1</c>: the object is <b>Workforce's,
/// whole</b>. The reasoning lives on <see cref="Team"/>, beside the type, since
/// that is where somebody re-deriving it against ADR 0063 §Q4's Zone precedent
/// will be standing.
/// </para>
/// <para>
/// <b><c>posting.assign</c>, and no thirteenth permission.</b> Forming a team is
/// the same authority over the same question a posting answers — who works
/// where, and with whom — and a permission per noun is how a registry acquires
/// forty of them.
/// </para>
/// <para>
/// <b>No events in v1</b>, and that is a platform fact rather than a preference:
/// the Kernel's stream routing pre-names <c>shift</c>, <c>leave</c>,
/// <c>duty</c>, <c>attendance</c> and <c>user</c>, and a subject outside that
/// list is <i>acked, matches nothing, and dead-letters silently</i>. Nothing
/// subscribes to teams yet; when something does, the route arrives by
/// <c>PKG-Q39</c>'s mechanism — manifest-declared domains materialised at
/// install — and never as a sixth pre-name.
/// </para>
/// </remarks>
public class TeamService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    IStaffDirectory directory,
    TimeProvider clock)
{
    /// <summary>Form a team.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="command">Which department, and what it is called.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The team.</returns>
    public async Task<Team> FormAsync(
        RequestScope scope, FormTeamCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PostingAssign, "property", scope.PropertyId, cancellationToken);

        var code = Normalise(command.DepartmentCode);
        var name = command.Name?.Trim() ?? string.Empty;

        if (code.Length == 0)
        {
            throw new InvalidRequestException("department_code is required");
        }

        if (name.Length == 0)
        {
            throw new InvalidRequestException("name is required — a team is known by it");
        }

        // Resolved rather than trusted, exactly as a posting's is: a team in a
        // department this property has not activated is a team nothing can ever
        // route work to.
        _ = await directory.FindDepartmentIdAsync(scope.PropertyId, code, cancellationToken)
            ?? throw new InvalidRequestException(
                $"department {code} is not activated at this property");

        await RefuseDuplicateNameAsync(scope.PropertyId, code, name, null, cancellationToken);

        var now = clock.GetUtcNow();
        var team = new Team
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            DepartmentCode = code,
            Name = name,
            Active = true,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync(cancellationToken);

        return team;
    }

    /// <summary>Rename a team.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="command">The team, its version, and the new name.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The team.</returns>
    /// <remarks>
    /// <b>The department is not amendable.</b> Moving a team to another
    /// department would move every member with it, and a member holds a posting
    /// in the department the team was in — so the operation is <i>form a team
    /// there and move the people</i>, which is two decisions somebody should
    /// make deliberately rather than one field they can edit.
    /// </remarks>
    public async Task<Team> RenameAsync(
        RequestScope scope, AmendTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await ForWriteAsync(scope, command.Id, command.ExpectedVersion, cancellationToken);
        var name = command.Name?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            throw new InvalidRequestException("name is required — a team is known by it");
        }

        await RefuseDuplicateNameAsync(
            scope.PropertyId, team.DepartmentCode, name, team.Id, cancellationToken);

        team.Name = name;
        Touch(team);

        await db.SaveChangesAsync(cancellationToken);
        return team;
    }

    /// <summary>Stand a team down, or back up — ADR 0062's verbs.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="command">The team and its version.</param>
    /// <param name="active">Whether it is offered.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <param name="keepMembers">
    /// Whether a stand-down leaves the memberships alone. True by default,
    /// because that is what a seasonal crew means; false ends them on the day.
    /// </param>
    /// <returns>The team.</returns>
    /// <remarks>
    /// <b>Reactivation is required, not optional.</b> ADR 0062 §22 · 2: a
    /// deactivate with no counterpart states a capability in the schema and
    /// withholds it from the service. A crew stood down for the low season comes
    /// back.
    /// </remarks>
    public async Task<Team> SetActiveAsync(
        RequestScope scope,
        AmendTeamCommand command,
        bool active,
        CancellationToken cancellationToken,
        bool keepMembers = true)
    {
        var team = await ForWriteAsync(scope, command.Id, command.ExpectedVersion, cancellationToken);

        team.Active = active;
        Touch(team);

        // **Frame 5's toggle, and it defaults to keeping them.** A crew stood
        // down for the low season comes back with the same people, which is why
        // the switch is on by default and why standing down is not a disband. A
        // property that means the other thing says so, once, at the moment it
        // decides — and the memberships close on that day rather than being
        // deleted, because who was in this team in March is still a question.
        if (!active && !keepMembers)
        {
            var now = clock.GetUtcNow();
            var on = DateOnly.FromDateTime(now.UtcDateTime);

            foreach (var member in await Live(scope.PropertyId, team.Id)
                         .ToListAsync(cancellationToken))
            {
                Close(member, on, now);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return team;
    }

    /// <summary>The teams a posting is holding open for somebody.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="staffId">Whose posting is about to end.</param>
    /// <param name="departmentCode">The department it is in.</param>
    /// <param name="on">The day it would end.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The teams, and when the person joined each.</returns>
    /// <remarks>
    /// <para>
    /// <b>The read that lets an interface say what a write is about to do.</b>
    /// Ending a posting closes these memberships — that has always been true and
    /// is tested — and until this existed there was no way for a screen to tell
    /// anybody: a supervisor ended a posting, two teams quietly emptied, and
    /// nothing said so.
    /// </para>
    /// <para>
    /// It is the same query <see cref="EndMembershipsForPostingAsync"/> makes,
    /// which is the point: a screen that predicted the consequence with its own
    /// logic would eventually predict it wrongly, and the version that disagreed
    /// would be the one a person read.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<(Team Team, DateOnly Since)>> SupportedTeamsAsync(
        RequestScope scope,
        Guid staffId,
        string departmentCode,
        DateOnly on,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var code = Normalise(departmentCode);

        var rows = await db.TeamMembers
            .Where(m => m.PropertyId == scope.PropertyId
                        && m.StaffId == staffId
                        && m.LeftOn == null)
            .Join(
                db.Teams.Where(t => t.PropertyId == scope.PropertyId
                                    && t.DepartmentCode == code
                                    && t.DeletedAt == null),
                member => member.TeamId,
                team => team.Id,
                (member, team) => new { team, member.JoinedOn })
            .OrderBy(row => row.JoinedOn)
            .ToListAsync(cancellationToken);

        return [.. rows.Where(row => row.JoinedOn <= on).Select(row => (row.team, row.JoinedOn))];
    }

    /// <summary>Put somebody in a team.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="command">The team, the person, and the day.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The membership.</returns>
    /// <remarks>
    /// <b>A member holds a posting in force in the team's department.</b> A team
    /// exists to receive work there, so a member who cannot be assigned it is a
    /// row that lies — and the check is against the day the membership starts,
    /// not against today, so next week's crew is formed against next week's
    /// postings.
    /// </remarks>
    public async Task<TeamMember> AddMemberAsync(
        RequestScope scope, TeamMembershipCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PostingAssign, "property", scope.PropertyId, cancellationToken);

        var team = await FindAsync(scope, command.TeamId, cancellationToken)
            ?? throw new NotFoundException("team", command.TeamId);

        var posted = await db.Postings.AnyAsync(
            p => p.PropertyId == scope.PropertyId
                 && p.StaffId == command.StaffId
                 && p.DepartmentCode == team.DepartmentCode
                 && p.EffectiveFrom <= command.On
                 && (p.EffectiveTo == null || p.EffectiveTo >= command.On),
            cancellationToken);

        if (!posted)
        {
            throw new InvalidRequestException(
                $"this person holds no posting in {team.DepartmentCode} on {command.On:yyyy-MM-dd}, "
                + "and a team member has to be assignable in the team's own department");
        }

        var already = await Live(scope.PropertyId, command.TeamId)
            .AnyAsync(m => m.StaffId == command.StaffId, cancellationToken);

        if (already)
        {
            throw new InvalidRequestException("this person is already in this team");
        }

        var now = clock.GetUtcNow();
        var member = new TeamMember
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            TeamId = command.TeamId,
            StaffId = command.StaffId,
            JoinedOn = command.On,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        db.TeamMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);

        return member;
    }

    /// <summary>Take somebody out of a team.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="command">The team, the person, and the day.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The closed membership.</returns>
    /// <remarks>
    /// The row is <b>closed, never deleted</b>: <i>who was in this team in
    /// March</i> is a question a report asks, and a deleted row cannot answer it.
    /// </remarks>
    public async Task<TeamMember> RemoveMemberAsync(
        RequestScope scope, TeamMembershipCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PostingAssign, "property", scope.PropertyId, cancellationToken);

        var member = await Live(scope.PropertyId, command.TeamId)
            .FirstOrDefaultAsync(m => m.StaffId == command.StaffId, cancellationToken)
            ?? throw new NotFoundException("team membership", command.StaffId);

        Close(member, command.On, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return member;
    }

    /// <summary>This property's teams, and who is in them.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="departmentCode">One department, or null for all.</param>
    /// <param name="includeInactive">Whether teams stood down are listed.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The teams.</returns>
    public async Task<IReadOnlyList<Team>> ListAsync(
        RequestScope scope,
        string? departmentCode,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var teams = db.Teams.Where(t => t.PropertyId == scope.PropertyId && t.DeletedAt == null);

        if (!includeInactive)
        {
            teams = teams.Where(t => t.Active);
        }

        if (!string.IsNullOrWhiteSpace(departmentCode))
        {
            var code = Normalise(departmentCode);
            teams = teams.Where(t => t.DepartmentCode == code);
        }

        return await teams
            .OrderBy(t => t.DepartmentCode)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Who is in a team on a given day.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="teamId">Which team.</param>
    /// <param name="on">Which day.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The staff ids — this application holds no name.</returns>
    public async Task<IReadOnlyList<Guid>> MembersAsync(
        RequestScope scope, Guid teamId, DateOnly on, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var members = await db.TeamMembers
            .Where(m => m.PropertyId == scope.PropertyId && m.TeamId == teamId)
            .ToListAsync(cancellationToken);

        return [.. members.Where(m => m.IsInForceOn(on)).Select(m => m.StaffId)];
    }

    /// <summary>End every membership a person holds in one department.</summary>
    /// <param name="propertyId">The property.</param>
    /// <param name="staffId">The person.</param>
    /// <param name="departmentCode">The department their posting ended in.</param>
    /// <param name="on">The day it ended.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>How many memberships were closed.</returns>
    /// <remarks>
    /// <para>
    /// <b>Called from inside <see cref="Postings.PostingService"/>'s own
    /// transaction, and takes no scope of its own.</b> A team routes work to its
    /// members; a member whose posting has ended cannot be assigned in that
    /// department, so leaving the membership open would route work to somebody
    /// who left last month with nothing anywhere saying so.
    /// </para>
    /// <para>
    /// It authorizes nothing, deliberately: the caller has already been
    /// authorized to end the posting, and this is a consequence of that decision
    /// rather than a second one. A second check here would be a second place for
    /// the two to disagree — and the one that failed would leave the posting
    /// ended and the membership standing.
    /// </para>
    /// </remarks>
    public async Task<int> EndMembershipsForPostingAsync(
        Guid propertyId,
        Guid staffId,
        string departmentCode,
        DateOnly on,
        CancellationToken cancellationToken)
    {
        var teams = await db.Teams
            .Where(t => t.PropertyId == propertyId && t.DepartmentCode == departmentCode)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (teams.Count == 0)
        {
            return 0;
        }

        var memberships = await db.TeamMembers
            .Where(m => m.PropertyId == propertyId
                        && m.StaffId == staffId
                        && teams.Contains(m.TeamId)
                        && m.LeftOn == null)
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();

        foreach (var membership in memberships)
        {
            Close(membership, on, now);
        }

        // No SaveChanges: this runs inside the caller's transaction, and saving
        // here would commit the posting's half early.
        return memberships.Count;
    }

    /// <summary>The live memberships of one team.</summary>
    private IQueryable<TeamMember> Live(Guid propertyId, Guid teamId) =>
        db.TeamMembers.Where(
            m => m.PropertyId == propertyId && m.TeamId == teamId && m.LeftOn == null);

    /// <summary>Close a membership, never before it began.</summary>
    /// <remarks>
    /// A person removed on the day they joined leaves on that day rather than
    /// the day before it — a window that ends before it starts is a record that
    /// cannot be true, and the database refuses one.
    /// </remarks>
    private static void Close(TeamMember member, DateOnly on, DateTimeOffset now)
    {
        member.LeftOn = on < member.JoinedOn ? member.JoinedOn : on;
        member.UpdatedAt = now;
        member.Version += 1;
    }

    private async Task<Team> ForWriteAsync(
        RequestScope scope, Guid id, long expectedVersion, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.PostingAssign, "property", scope.PropertyId, cancellationToken);

        var team = await FindAsync(scope, id, cancellationToken)
            ?? throw new NotFoundException("team", id);

        if (team.Version != expectedVersion)
        {
            throw new ConcurrencyException("team", id, expectedVersion);
        }

        return team;
    }

    private async Task<Team?> FindAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken) =>
        await db.Teams.FirstOrDefaultAsync(
            t => t.Id == id && t.PropertyId == scope.PropertyId && t.DeletedAt == null,
            cancellationToken);

    /// <summary>Two live teams in one department may not share a name.</summary>
    /// <remarks>
    /// A supervisor picking "Team A" from a list of two identical entries is
    /// choosing at random, and the job goes to whichever the dropdown ordered
    /// first.
    /// </remarks>
    private async Task RefuseDuplicateNameAsync(
        Guid propertyId,
        string departmentCode,
        string name,
        Guid? excluding,
        CancellationToken cancellationToken)
    {
        var taken = await db.Teams.AnyAsync(
            t => t.PropertyId == propertyId
                 && t.DepartmentCode == departmentCode
                 && t.DeletedAt == null
                 && t.Name == name
                 && (excluding == null || t.Id != excluding),
            cancellationToken);

        if (taken)
        {
            throw new InvalidRequestException(
                $"{departmentCode} already has a team called \"{name}\"");
        }
    }

    /// <summary>Mark a team changed, on the injected clock.</summary>
    /// <remarks>
    /// <c>DateTimeOffset.UtcNow</c> would work and is wrong: every timestamp in
    /// this application comes from the injected <see cref="TimeProvider"/>, so a
    /// test can assert on the value rather than on "roughly now".
    /// </remarks>
    private void Touch(Team team)
    {
        team.UpdatedAt = clock.GetUtcNow();
        team.Version += 1;
    }

    private static string Normalise(string code) => code.Trim().ToUpperInvariant();
}
