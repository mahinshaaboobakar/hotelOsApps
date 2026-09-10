using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Postings;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// Who is signed in, and where they are posted — the app bar's one line.
/// </summary>
/// <remarks>
/// <para>
/// <b>The module asks its own backend, because its own backend is the only
/// thing that knows.</b> The bundle's <c>host.identity</c> names the
/// application and <c>host.property</c> carries a timezone and a locale;
/// neither says who is at the desk. <c>SHELL-Q52</c> refused to widen either —
/// what the shell presents to a module is not identity and not enforcement
/// authority — and design page 63 §3 already had the answer: a bundle calls
/// only its own backend, everything else its backend does. This backend
/// validated the session token, so the person is a fact it holds.
/// </para>
/// <para>
/// <b>What it replaces.</b> The module carried
/// <c>const OPERATOR = { name: "Priya Thomas", … }</c> with the comment
/// <i>"who is signed in"</i>, drawn unconditionally on every screen. On a real
/// property that named somebody who is not the signed-in user, under a hotel
/// that may not be the hotel — the gap rule's plainest breach, because a name
/// on a line is an attribution claim.
/// </para>
/// <para>
/// <b>Three legitimate unknowns, each stated rather than filled in.</b> None of
/// them is an error, and none may be papered over with a placeholder:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>A service caller.</b> Not every request carries a person —
///     <c>RequestScope.UserId</c> is nullable precisely because a background
///     sweep or an event consumer has none. Answered as nobody signed in.
///   </description></item>
///   <item><description>
///     <b>A login with no staff record.</b> The founding administrator signs in
///     before any staff row exists (ADR 0088), and a user may be linked later.
///     Answered as a person Workforce does not yet know.
///   </description></item>
///   <item><description>
///     <b>A person deactivated or soft-deleted.</b> ADR 0062's lifecycle:
///     <c>active</c> false or <c>deleted_at</c> set means they have left the
///     usable universe. The scoped read already excludes them, so this arrives
///     as the same answer as the case above — deliberately, because the bar has
///     no business explaining an employment status to whoever is looking at it.
///   </description></item>
/// </list>
/// <para>
/// <b>Every field is nullable and none is defaulted.</b> A missing name renders
/// as nothing; it does not render as "Unknown", which would be this application
/// asserting a fact about a person it could not find.
/// </para>
/// </remarks>
public static class MeView
{
    /// <summary>The signed-in person, as far as this property can say.</summary>
    public static async Task<object?> Read(ModuleCall call, CancellationToken cancellationToken)
    {
        var directory = call.Service<IStaffDirectory>();
        var postings = call.Service<PostingService>();

        var property = await directory.FindPropertyNameAsync(
            call.Scope.PropertyId, cancellationToken);

        // A request with no person behind it. The property is still named,
        // because the property is a fact about the request rather than about a
        // caller who is not there.
        if (call.Scope.UserId is not { } userId)
        {
            return Answer(null, null, null, property);
        }

        var resolved = await directory.FindStaffIdAsync(
            call.Scope.PropertyId, userId, cancellationToken);

        // Known to Identity, unknown to Master Data — or known and no longer
        // active, which `Scoped` filters out. Either way Workforce cannot name
        // them, and says so by naming nobody.
        if (resolved is not StaffResolution.Resolved found)
        {
            return Answer(null, null, null, property);
        }

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, [found.StaffId], cancellationToken);

        var name = names.TryGetValue(found.StaffId, out var display) ? display : null;

        // Where they are posted is this application's own — Master Data owns
        // who a person is, Workforce owns what they are doing (ADR 0051).
        // `IncludeEnded` left false: a posting that ended yesterday is not
        // where this person is now, and the bar answers the present tense.
        var held = await postings.ListAsync(
            call.Scope,
            new ListPostingsQuery { StaffId = found.StaffId },
            cancellationToken);

        // The primary posting, or the only one, or none. `FirstOrDefault` over
        // an ordering rather than `Single`: a person may hold several, and the
        // bar shows where they mainly are rather than refusing to draw.
        var primary = held.OrderByDescending(one => one.IsPrimary).FirstOrDefault();

        var departments = await directory.FindDepartmentNamesAsync(
            call.Scope.PropertyId, cancellationToken);

        var department = primary is null
            ? null
            : departments.TryGetValue(primary.DepartmentCode, out var known)
                ? known
                : primary.DepartmentCode;

        return Answer(name, department, primary?.JobRole, property);
    }

    /// <summary>One shape, so every path answers the same question.</summary>
    /// <remarks>
    /// Built here rather than at each return so the unknowns cannot diverge:
    /// three of the four paths above differ only in how much they could
    /// establish, and a caller reading the answer should not be able to tell
    /// which one it came from except by the nulls.
    /// </remarks>
    private static object Answer(
        string? name, string? department, string? role, string? property)
        => new { name, department, role, property };
}
