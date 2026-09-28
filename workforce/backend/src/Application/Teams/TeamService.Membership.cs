using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Teams;

/// <summary>
/// Who is in a team, and when they were — the membership half of Teams.
/// </summary>
/// <remarks>
/// A subject partial of <see cref="TeamService"/>, whose primary part carries
/// this service's contract. **One class, two files, on purpose**: these members
/// and the primary part's write to the SAME scoped <c>WorkforceDbContext</c> in
/// ONE <c>SaveChangesAsync</c>, so a second service here would put a
/// cross-service write inside that transaction. The subject boundary is real;
/// the transaction is what decides it is taken as files rather than as services.
/// </remarks>
public partial class TeamService
{
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

    /// <summary>Who is in a team on a given day.</summary>
    /// <param name="scope">The caller.</param>
    /// <param name="teamId">Which team.</param>
    /// <param name="on">Which day.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The staff ids — this application holds no name.</returns>
    public async Task<IReadOnlyList<TeamMember>> MembersAsync(
        RequestScope scope, Guid teamId, DateOnly on, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var members = await db.TeamMembers
            .Where(m => m.PropertyId == scope.PropertyId && m.TeamId == teamId)
            .ToListAsync(cancellationToken);

        // **The memberships, not their staff ids.** This returned ids alone, so
        // the roll had nothing to say when a person joined and the view sent
        // `since = null` for every member — while the value sat on the row it
        // had just discarded. One caller wants the count and one wants the
        // dates; a second method for the same query would be the same rows
        // fetched twice.
        //
        // Ordered by when they joined, then by id. The order was previously
        // whatever the database returned, which is not an order — two runs
        // could draw one roll two ways and nothing would be wrong.
        return [.. members
            .Where(m => m.IsInForceOn(on))
            .OrderBy(m => m.JoinedOn)
            .ThenBy(m => m.StaffId)];
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
}
