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

        // ADR 0211 — the day a team's membership is read on.
        var on = call.Optional("on") is { } day
            ? DateOnly.Parse(day.GetString()!)
            : await PropertyDay.TodayAsync(call, cancellationToken);

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

            // **The day, once, as the wire carries it** — ADR 0175. This
            // carried a rendering (`ddd d MMM`) beside the ISO value for one
            // commit, which was one commit of two spellings of one fact. The
            // rendering used the process's current culture, so a hotel's own
            // week label depended on the account the service runs under; the
            // module renders this through the SDK's `formatDay` against the
            // property's locale, which is where the reader's locale is.
            onDate = on.ToString("yyyy-MM-dd"),
            detail = open,

            // **The departments a team could be formed in, which the screen had
            // no way to ask for.** Forming a team needs a department code, and
            // this read carried only the departments that already HAVE a team —
            // so a property's first team in Housekeeping was unformable from
            // the screen, and the sheet's picker drew a literal.
            //
            // **Ordered by CODE, ordinally — and the order a person reads is
            // not this layer's to choose.** The first version sorted by name
            // with <c>CurrentCultureIgnoreCase</c>, which reads like the right
            // intent and is not: nothing sets a culture in this service, in the
            // SDK, or in the Kernel that launches the process, and
            // <c>InvariantGlobalization</c> is off — so "current culture" is
            // the account the service happens to run under, and the order of a
            // hotel's departments would be a property of the machine. This
            // platform is sold into India and the GCC and writes no country
            // into code.
            //
            // The code is what this service owns and it sorts the same
            // everywhere. The reader's order belongs where the reader's locale
            // is, which is the module: <c>PropertyEnvironment.locale</c> is on
            // the bundle's side of the bridge and on no side of this one.
            departments = names
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new { code = entry.Key, name = entry.Value })
                .ToArray(),
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

            // **The row a write has to quote back.** Standing a team down and
            // renaming one both take `ExpectedVersion`, and this read sent no
            // version at all - so neither write could be made from the screen
            // that offers it, whatever the button did. Optimistic concurrency
            // needs the reader to say which row it was looking at.
            version = team.Version,
            name = team.Name,
            department = team.DepartmentCode,
            departmentName = names.TryGetValue(team.DepartmentCode, out var named)
                ? named
                : team.DepartmentCode,
            note = (string?)null,
            members,
            // ISO, and rendered by the module: a formation date is read months
            // later, so it carries its year - `date-year` on the other side.
            formed = team.CreatedAt.ToString("O"),
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

        var roll = await teams.MembersAsync(call.Scope, teamId, on, cancellationToken);

        var postings = await call.Service<PostingService>().ListAsync(
            call.Scope, new ListPostingsQuery(), cancellationToken);

        var everybody = postings.Select(one => one.StaffId)
            .Concat(roll.Select(one => one.StaffId))
            .Distinct()
            .ToList();

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, everybody, cancellationToken);

        return new
        {
            team = Row(team, departments, roll.Count),
            onDate = on.ToString("yyyy-MM-dd"),
            members = roll.Select(one => Member(one, names)).ToList(),
            candidates = Candidates(
                postings, [.. roll.Select(one => one.StaffId)], names, team.DepartmentCode),
        };
    }

    /// <summary>One person on the roll.</summary>
    private static object Member(TeamMember member, IReadOnlyDictionary<Guid, string> names)
    {
        var staffId = member.StaffId;
        var name = names.TryGetValue(staffId, out var found) ? found : null;

        return new
        {
            staffId,
            name,
            // Initials from the name when there is one, and from nothing when
            // there is not — a two-letter stand-in derived from a UUID would be
            // an identity this application invented for somebody.
            initials = name is null ? "" : Wording.Initials(name),

            // **When they joined, which this answer used to withhold.** It sent
            // `null` unconditionally while the UI typed it as a string and the
            // harness's fixture supplied one — so every capture showed a join
            // date and every real property showed nothing, and the two could
            // not be told apart from either side.
            //
            // ISO, per ADR 0175: the screen renders it against the property's
            // locale.
            since = member.JoinedOn.ToString("yyyy-MM-dd"),
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
