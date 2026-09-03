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
/// <b>Deliberately narrow.</b> Every question here is one this application must
/// ask and must not answer. It is not a general-purpose Master Data facade, and
/// it must not grow into one — every method added here is a fact this
/// application could start believing it owns.
/// </para>
/// <para>
/// <b>Serving is not storing</b> — ruled 2026-09-03, on the gap the widget
/// round found: four of Workforce's five widgets show a person's name and this
/// application had none to give. <see cref="FindNamesAsync"/> reads the display
/// name at the moment an answer is composed, and nothing keeps it — not a
/// column, not a cache, not a field that outlives the response. The proto's rule
/// (<i>this application can never become a second place somebody's name is
/// stored</i>) is about <b>storage</b>, and reading master data to answer a
/// question is what the constitution's <i>applications may read master data</i>
/// is for. A name never written down cannot go stale and cannot disagree with
/// Master Data, because there is no second copy to disagree with.
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

    /// <summary>The property's country, or <c>null</c> when it has none configured.</summary>
    /// <remarks>
    /// <para>
    /// What the leave-type seed template is keyed off — the country-seed ruling:
    /// <b>a template chosen by the property's own setting, never a literal</b>.
    /// The setting is <c>Property.Country</c>, which Master Data already carries
    /// (<c>Tenancy.cs:63</c>) and which is nullable there.
    /// </para>
    /// <para>
    /// Null is answered honestly rather than guessed. A property that has not
    /// said where it is has not said which vocabulary it uses, and inferring a
    /// region from a currency or a timezone would be the same country-in-the-
    /// product mistake wearing a different field.
    /// </para>
    /// </remarks>
    Task<string?> FindPropertyCountryAsync(Guid propertyId, CancellationToken cancellationToken);

    /// <summary>The display names for a set of staff, for one answer.</summary>
    /// <param name="propertyId">Whose property is asking.</param>
    /// <param name="staffIds">The people an answer is about.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>
    /// A name per id that Master Data knows. <b>An id that is absent from the
    /// result has no name here</b> — a caller renders what it was given rather
    /// than a placeholder, because "we could not find this person" and "this
    /// person has no name" are different facts and neither is "Unknown".
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>A set, not an id.</b> The shape is the round-trip decision: a widget
    /// resolves five names for one card, and a per-id port would make that five
    /// calls at every call site whether or not the adapter batched them. Taking
    /// the set means the call site costs one call and the adapter is free to
    /// answer it however Master Data's surface allows — today several small
    /// reads in parallel, tomorrow one filtered list if that RPC gains an id
    /// filter, with nothing above it changing.
    /// </para>
    /// <para>
    /// <b>Display name, and nothing else about the person.</b> Not the employee
    /// code, not the photograph, not the contact — a widget row and a rota cell
    /// need what a name badge shows, and every further field would be another
    /// fact this application could start believing it owns.
    /// </para>
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, string>> FindNamesAsync(
        Guid propertyId, IReadOnlyCollection<Guid> staffIds, CancellationToken cancellationToken);
}
