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
/// <para>
/// <b>Membership is a subject partial, in <c>TeamService.Membership.cs</c>.</b>
/// <see cref="TeamMember"/> has its own type, its own table and its own
/// lifecycle, so the subject boundary is real — and it is taken as a second FILE
/// rather than a second SERVICE because <see cref="SetActiveAsync"/> loads a team
/// and closes its live memberships in ONE <c>SaveChangesAsync</c>. A membership
/// service would put a cross-service write inside that transaction; a partial
/// shares the change tracker by construction.
/// </para>
/// <para>
/// <b>The absence of a <c>team.*</c> event is NOT evidence about that
/// boundary</b>, which the paragraph above explains: it is the Kernel's stream
/// routing, and it is silent about the whole Teams subject rather than about
/// either half of it. ADR 0044's other markers — type, table, lifecycle — are
/// what carry the argument here.
/// </para>
/// </remarks>
public partial class TeamService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    IStaffDirectory directory,
    IOperatingDay days,
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

            // The day a membership closed, at the property — ADR 0211. It is
            // read back as "who was in this team in March", so the UTC day put
            // an evening stand-down in Guatemala on the following day. The
            // instant beside it stays UTC: `UpdatedAt` is when, not which day.
            var on = OperatingDay.OrUnavailable(
                await days.TodayAsync(scope, cancellationToken));

            foreach (var member in await Live(scope.PropertyId, team.Id)
                         .ToListAsync(cancellationToken))
            {
                Close(member, on, now);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return team;
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
