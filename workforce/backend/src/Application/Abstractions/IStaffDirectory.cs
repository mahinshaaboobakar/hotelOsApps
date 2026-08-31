namespace HotelOS.Workforce.Application.Abstractions;

/// <summary>
/// What this application needs to know from Master Data, and nothing more.
/// </summary>
/// <remarks>
/// <para>
/// CLAUDE.md's non-negotiable list: <i>"Applications may read master data."</i>
/// Master Data is the platform, not another application, so this is a sanctioned
/// read — unlike reading Room Care's or Jobs' tables, which is the one rule
/// modularity rests on and which the Context Service exists to replace.
/// </para>
/// <para>
/// An interface rather than a client at the call site, for the reason ADR 0054
/// gives: a characterisation suite constructs the service with doubles and is
/// exhaustive and fast <i>because</i> it stands up nothing. A hard dependency on
/// a gRPC channel here would make every posting test an integration test.
/// </para>
/// <para>
/// <b>Deliberately narrow.</b> Two questions, both of which have a
/// authorization consequence. This is not a general-purpose Master Data facade,
/// and it must not grow into one — every method added here is a fact this
/// application could start believing it owns.
/// </para>
/// </remarks>
public interface IStaffDirectory
{
    /// <summary>
    /// The identity link for a staff member, or <c>null</c> when they have none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>masterdata.staff.user_id</c> is nullable and the platform's own proto
    /// says <i>"that nullability is the whole point"</i> — most room attendants
    /// have no login and never will.
    /// </para>
    /// <para>
    /// <b>Null is the ordinary answer, not an error.</b> A posting for somebody
    /// with no account is a complete, correct posting that announces nothing:
    /// there is no principal for a tuple to grant anything to, and writing one
    /// would be inventing an account. A department folder grant means something
    /// only for somebody who can open a folder.
    /// </para>
    /// </remarks>
    Task<Guid?> FindUserIdAsync(Guid propertyId, Guid staffId, CancellationToken cancellationToken);

    /// <summary>
    /// The row id of an activated department, by its canon code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two identities, and both are needed. ADR 0119 makes the <b>code</b> the
    /// canonical business identity — immutable, identical in every installation,
    /// what reports group on — and it is what a posting stores. The
    /// authorization graph addresses a department by its <b>row id</b>:
    /// <c>department:{uuid}</c>, registered from
    /// <c>department.created</c> like every other registrable type.
    /// </para>
    /// <para>
    /// So an announcement has to carry the id as well as the code, and this is
    /// where the id comes from. Resolving it here rather than storing it on the
    /// posting is deliberate: the id is Master Data's and can only be stale here.
    /// </para>
    /// <para>
    /// Returns <c>null</c> when the property has not activated that code — which
    /// is a refusal, not an announcement with a missing field.
    /// </para>
    /// </remarks>
    Task<Guid?> FindDepartmentIdAsync(
        Guid propertyId, string departmentCode, CancellationToken cancellationToken);
}
