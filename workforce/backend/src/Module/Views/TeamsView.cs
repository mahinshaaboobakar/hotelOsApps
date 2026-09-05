using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Teams;
using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// Teams — the property's teams, one team's roll for a day, and the five writes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Membership is effective-dated, so every count is "on a day".</b> The
/// header carries the date and the roll is the members live on it — which is
/// why the screen's day picker changes the numbers rather than only the list.
/// A count taken without a date would be right on the day it was written and
/// quietly wrong afterwards.
/// </para>
/// <para>
/// <b>The candidate list carries the service's refusal, not the screen's.</b>
/// Somebody with no posting in the team's department cannot receive its work,
/// and the sentence saying so is computed here from the postings live on the
/// day — so the screen renders a refusal it did not decide.
/// </para>
/// </remarks>
public static class TeamsView
{
    /// <summary>Every team, and the open one's roll when a team is named.</summary>
    public static async Task<object?> List(ModuleCall call, CancellationToken cancellationToken)
    {
        var teams = call.Service<TeamService>();
        var directory = call.Service<IStaffDirectory>();
        var clock = call.Service<TimeProvider>();

        var on = call.Optional("on") is { } day
            ? DateOnly.Parse(day.GetString()!)
            : DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var all = await teams.ListAsync(
            call.Scope,
            call.Optional("department")?.GetString(),
            includeInactive: call.Optional("includeStoodDown")?.GetBoolean() ?? true,
            cancellationToken);

        var names = await directory.FindDepartmentNamesAsync(
            call.Scope.PropertyId, cancellationToken);

        var rows = new List<object>();

        foreach (var team in all)
        {
            var members = await teams.MembersAsync(call.Scope, team.Id, on, cancellationToken);
            rows.Add(Row(team, names, members.Count));
        }

        var open = call.Optional("team") is { } named
            ? await Detail(call, Guid.Parse(named.GetString()!), on, names, cancellationToken)
            : null;

        return new
        {
            property = call.Optional("property")?.GetString(),
            teams = rows,
            on = on.ToString("ddd d MMM"),
            detail = open,
        };
    }

    /// <summary>Form, rename, stand down or back up, add and remove.</summary>
    public static Task<object?> Write(ModuleCall call, CancellationToken cancellationToken)
        => call.Method switch
        {
            "form" => Form(call, cancellationToken),
            "rename" => Rename(call, cancellationToken),
            "standing" => Standing(call, cancellationToken),
            "addMember" => AddMember(call, cancellationToken),
            "removeMember" => RemoveMember(call, cancellationToken),
            _ => throw new InvalidRequestException(call.Method + " is not a team method"),
        };

    private static async Task<object?> Form(ModuleCall call, CancellationToken cancellationToken)
    {
        var team = await call.Service<TeamService>().FormAsync(
            call.Scope,
            new FormTeamCommand
            {
                DepartmentCode = call.Text("department"),
                Name = call.Text("name"),
            },
            cancellationToken);

        return new { id = team.Id, version = team.Version };
    }

    private static async Task<object?> Rename(ModuleCall call, CancellationToken cancellationToken)
    {
        var team = await call.Service<TeamService>().RenameAsync(
            call.Scope,
            new AmendTeamCommand
            {
                Id = call.Id("id"),
                ExpectedVersion = call.Required("version").GetInt64(),
                Name = call.Text("name"),
            },
            cancellationToken);

        return new { id = team.Id, version = team.Version, name = team.Name };
    }

    /// <summary>
    /// Stand a team down, or bring it back up.
    /// </summary>
    /// <remarks>
    /// <c>keepMembers</c> is the dialog's toggle and reaches the service
    /// unchanged: standing a team down with its members kept is a different
    /// decision from emptying it, and the screen must not be the place that
    /// picks one.
    /// </remarks>
    private static async Task<object?> Standing(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var team = await call.Service<TeamService>().SetActiveAsync(
            call.Scope,
            new AmendTeamCommand
            {
                Id = call.Id("id"),
                ExpectedVersion = call.Required("version").GetInt64(),
            },
            active: call.Required("active").GetBoolean(),
            cancellationToken,
            keepMembers: call.Optional("keepMembers")?.GetBoolean() ?? true);

        return new { id = team.Id, version = team.Version, active = team.Active };
    }

    private static async Task<object?> AddMember(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var member = await call.Service<TeamService>().AddMemberAsync(
            call.Scope,
            new TeamMembershipCommand
            {
                TeamId = call.Id("teamId"),
                StaffId = call.Id("staffId"),
                On = call.Date("on"),
            },
            cancellationToken);

        return new { id = member.Id, version = member.Version, since = member.JoinedOn };
    }

    private static async Task<object?> RemoveMember(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var member = await call.Service<TeamService>().RemoveMemberAsync(
            call.Scope,
            new TeamMembershipCommand
            {
                TeamId = call.Id("teamId"),
                StaffId = call.Id("staffId"),
                On = call.Date("on"),
            },
            cancellationToken);

        return new { id = member.Id, version = member.Version, until = member.LeftOn };
    }

    /// <summary>One team, as the list draws it.</summary>
    private static object Row(Team team, IReadOnlyDictionary<string, string> names, int members)
        => new
        {
            id = team.Id,
            name = team.Name,
            department = team.DepartmentCode,
            departmentName = names.TryGetValue(team.DepartmentCode, out var named)
                ? named
                : team.DepartmentCode,
            note = (string?)null,
            members,
            formed = team.CreatedAt.ToString("d MMM yyyy"),
            active = team.Active,
        };

    /// <summary>The open team: its roll for the day, and who could join it.</summary>
    private static async Task<object> Detail(
        ModuleCall call,
        Guid teamId,
        DateOnly on,
        IReadOnlyDictionary<string, string> departments,
        CancellationToken cancellationToken)
    {
        var teams = call.Service<TeamService>();
        var directory = call.Service<IStaffDirectory>();

        var all = await teams.ListAsync(call.Scope, null, true, cancellationToken);
        var team = all.FirstOrDefault(one => one.Id == teamId)
                   ?? throw new NotFoundException("team", teamId);

        var memberIds = await teams.MembersAsync(call.Scope, teamId, on, cancellationToken);

        var postings = await call.Service<PostingService>().ListAsync(
            call.Scope, new ListPostingsQuery(), cancellationToken);

        var everybody = postings.Select(one => one.StaffId)
            .Concat(memberIds)
            .Distinct()
            .ToList();

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, everybody, cancellationToken);

        return new
        {
            team = Row(team, departments, memberIds.Count),
            on = on.ToString("ddd d MMM"),
            members = memberIds.Select(id => Member(id, names)).ToList(),
            candidates = Candidates(postings, memberIds, names, team.DepartmentCode),
        };
    }

    /// <summary>One person on the roll.</summary>
    private static object Member(Guid staffId, IReadOnlyDictionary<Guid, string> names)
    {
        var name = names.TryGetValue(staffId, out var found) ? found : null;

        return new
        {
            staffId,
            name,
            // Initials from the name when there is one, and from nothing when
            // there is not — a two-letter stand-in derived from a UUID would be
            // an identity this application invented for somebody.
            initials = name is null ? "" : Wording.Initials(name),
            since = (string?)null,
        };
    }

    /// <summary>
    /// Who may join, and the sentence for whoever may not.
    /// </summary>
    /// <remarks>
    /// The refused candidate is listed rather than hidden: a supervisor looking
    /// for a colleague who is not there learns nothing from an absence, and the
    /// reason — no posting in this department — is the thing they need in order
    /// to fix it.
    /// </remarks>
    private static List<object> Candidates(
        IReadOnlyList<Posting> postings,
        IReadOnlyList<Guid> members,
        IReadOnlyDictionary<Guid, string> names,
        string departmentCode)
    {
        var already = members.ToHashSet();

        return postings
            .Where(one => !already.Contains(one.StaffId))
            .GroupBy(one => one.StaffId)
            .Select(group =>
            {
                var posting = group.First();
                var here = group.Any(one => string.Equals(
                    one.DepartmentCode, departmentCode, StringComparison.OrdinalIgnoreCase));

                return (object)new
                {
                    staffId = group.Key,
                    name = names.TryGetValue(group.Key, out var found) ? found : null,
                    role = posting.JobRole,
                    department = posting.DepartmentCode,
                    refused = here ? null : "Not posted here",
                };
            })
            .ToList();
    }
}
