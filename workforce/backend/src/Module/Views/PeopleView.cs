using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// People — the property's postings, and the two writes the screen offers.
/// </summary>
/// <remarks>
/// <para>
/// <b>The one list in this module that pages.</b> It is bounded by the
/// property's headcount rather than by a day, a week or a department, so
/// <c>CORE-Q13</c>'s paged pattern applies and the numbers the screen draws are
/// the ones this handler returns — the page served, the size applied and the
/// total counted, never the size the bundle asked for.
/// </para>
/// <para>
/// <b>The zone is a name this application does not have.</b> A posting carries
/// a <c>ZoneId</c>; zones belong to Room Care (ADR 0056), and a cross-domain
/// name comes from the Context Service, which is not built. So the zone reads
/// as null and the screen draws an em-dash. It is <i>not</i> rendered as
/// "Zone 1" from an index nobody assigned — a value nobody established must not
/// stand in for one somebody did.
/// </para>
/// </remarks>
public static class PeopleView
{
    /// <summary>The page a screen asked for.</summary>
    public static async Task<object?> Page(ModuleCall call, CancellationToken cancellationToken)
    {
        var postings = call.Service<PostingService>();
        var capabilities = call.Service<CapabilityService>();
        var directory = call.Service<IStaffDirectory>();
        var clock = call.Service<TimeProvider>();

        var page = await postings.ListPageAsync(
            call.Scope,
            new ListPostingsQuery
            {
                DepartmentCode = call.Optional("department")?.GetString(),
                Paging = new PagedQuery(call.Number("page", 0), call.Number("pageSize", 25)),
            },
            cancellationToken);

        // One person may hold two postings — WF-Q3 — and the screen draws one
        // row per PERSON with both department codes on it. Grouping here rather
        // than in the screen is deliberate: the row is what the service says a
        // person's posting situation is, and a UI that assembled it would be a
        // second implementation of the rule.
        var people = page.Postings.GroupBy(one => one.StaffId).ToList();
        var staffIds = people.Select(group => group.Key).ToList();

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, staffIds, cancellationToken);

        var register = await capabilities.ListAsync(
            call.Scope, new ListCapabilitiesQuery(), cancellationToken);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var held = register.ToLookup(one => one.StaffId);

        return new
        {
            postings = people.Select(group => Row(group, names, held, today)).ToList(),
            paging = new { page = page.Page, pageSize = page.Size, total = page.Total },
        };
    }

    /// <summary>The two writes: post somebody, and end a posting.</summary>
    public static Task<object?> Write(ModuleCall call, CancellationToken cancellationToken)
        => call.Method switch
        {
            "post" => Post(call, cancellationToken),
            "end" => End(call, cancellationToken),
            _ => throw new InvalidRequestException(call.Method + " is not a posting method"),
        };

    private static async Task<object?> Post(ModuleCall call, CancellationToken cancellationToken)
    {
        var posting = await call.Service<PostingService>().CreateAsync(
            call.Scope,
            new CreatePostingCommand
            {
                StaffId = call.Id("staffId"),
                DepartmentCode = call.Text("department"),
                JobRole = call.Text("role"),
                IsPrimary = call.Optional("primary")?.GetBoolean() ?? true,
                IsDepartmentHead = call.Optional("head")?.GetBoolean() ?? false,
                ZoneId = call.Optional("zoneId")?.GetGuid(),
                ReportingManagerStaffId = call.Optional("reportsTo")?.GetGuid(),
                EffectiveFrom = call.Date("from"),
            },
            cancellationToken);

        return new { id = posting.Id, version = posting.Version };
    }

    /// <summary>
    /// End a posting — and, in the same commit, the memberships it supported.
    /// </summary>
    /// <remarks>
    /// The consequence panel the screen draws before this button is
    /// <c>SupportedTeamsAsync</c>'s answer, so what is listed and what is closed
    /// are the same query. A screen that predicted the consequence would
    /// eventually be the one somebody read, and the wrong one.
    /// </remarks>
    private static async Task<object?> End(ModuleCall call, CancellationToken cancellationToken)
    {
        var posting = await call.Service<PostingService>().EndAsync(
            call.Scope,
            new EndPostingCommand
            {
                Id = call.Id("id"),
                ExpectedVersion = call.Required("version").GetInt64(),
                EffectiveTo = call.Date("lastDay"),
            },
            cancellationToken);

        return new { id = posting.Id, version = posting.Version, endedOn = posting.EffectiveTo };
    }

    /// <summary>One person's row, from every posting they hold on this page.</summary>
    private static object Row(
        IGrouping<Guid, Posting> held,
        IReadOnlyDictionary<Guid, string> names,
        ILookup<Guid, Capability> register,
        DateOnly today)
    {
        var postings = held.OrderByDescending(one => one.IsPrimary)
            .ThenBy(one => one.DepartmentCode)
            .ToList();

        var primary = postings[0];
        var (capability, tone) = Standing(register[held.Key], today);

        return new
        {
            // Absent from Master Data's answer means absent here. "Unknown" is
            // a name this application would be inventing — IStaffDirectory's
            // own contract says the two facts differ and neither is a
            // placeholder.
            who = names.TryGetValue(held.Key, out var name) ? name : null,
            since = Since(primary, postings.Count),
            departments = postings.Select(one => one.DepartmentCode).ToList(),
            zone = (string?)null,
            role = primary.JobRole,
            reportsTo = ReportsTo(primary, names),
            capability,
            tone,
        };
    }

    /// <summary>"Since 4 Jan 2025", and the second posting when there is one.</summary>
    private static string Since(Posting primary, int count)
    {
        var since = "Since " + primary.EffectiveFrom.ToString("d MMM yyyy");

        return count > 1 ? since + " · " + count + " postings" : since;
    }

    /// <summary>A manager's name, or the words that stand where a name would.</summary>
    private static string ReportsTo(Posting posting, IReadOnlyDictionary<Guid, string> names)
    {
        if (posting.IsDepartmentHead)
        {
            return "— department head";
        }

        return posting.ReportingManagerStaffId is { } manager
               && names.TryGetValue(manager, out var name)
            ? name
            : "—";
    }

    /// <summary>
    /// What the capability register says about this person, and how it reads.
    /// </summary>
    /// <remarks>
    /// The worst band wins, because a person with four valid certificates and
    /// one expired one is a person with an expired certificate. "none recorded"
    /// is neutral rather than bad: nothing recorded is not the same fact as
    /// something lapsed, and colouring it red would make an unstarted register
    /// look like a compliance failure.
    /// </remarks>
    private static (string Reads, string Tone) Standing(
        IEnumerable<Capability> held, DateOnly today)
    {
        var dated = held.Where(one => one.Lapses).ToList();

        if (dated.Count == 0)
        {
            return ("none recorded", "neu");
        }

        var bands = dated.Select(one => one.BandOn(today)).ToList();

        if (bands.Contains(ExpiryBand.Expired))
        {
            return (bands.Count(one => one == ExpiryBand.Expired) + " expired", "bad");
        }

        var expiring = bands.Count(one =>
            one is ExpiryBand.Within7Days or ExpiryBand.Within30Days or ExpiryBand.Within60Days);

        return expiring > 0
            ? (expiring + " expiring", "warn")
            : (dated.Count + " valid", "ok");
    }
}
