namespace PmsOracle.Vocabularies;

/// <summary>
/// The outcome of reading one source value against a declared vocabulary:
/// either a meaning this connector recognises, or the unrecognised value itself.
/// </summary>
/// <typeparam name="T">
/// The vocabulary's meaning. A value type — an enum where the meaning is one
/// choice, a record struct where reading also yields something about the
/// message, as the on-site vocabulary's two halves do.
/// </typeparam>
/// <remarks>
/// <para>
/// This type exists to make one specific defect impossible to write. Every
/// status parser in the legacy reference ended
/// <c>default: return null</c> — six of them, across five vendors — so an
/// unrecognised status became an absent status, indistinguishable from a field
/// the PMS had not sent. It is why nobody can say today what those systems
/// actually emit, and it is requirement R5's whole subject.
/// </para>
/// <para>
/// There is no null here to return. A reader either recognises the value and
/// carries a meaning, or does not and <b>carries the value</b> — which is what
/// lets the Hub reject the record naming the string it could not read, and what
/// lets an operator see <c>"NO SHOW"</c> in Operations Center rather than a
/// silence. Adding a meaning to a vocabulary is then a visible act with a
/// rejected record behind it, instead of a value that has been quietly
/// discarded since the day the PMS was upgraded.
/// </para>
/// </remarks>
public readonly record struct Reading<T>
    where T : struct
{
    private readonly T _meaning;
    private readonly string? _unrecognised;

    private Reading(T meaning, string? unrecognised)
    {
        _meaning = meaning;
        _unrecognised = unrecognised;
    }

    /// <summary>Whether the source value carried a meaning this connector declares.</summary>
    public bool Recognised => _unrecognised is null;

    /// <summary>
    /// The source value that no declared meaning covers, or <c>null</c> when
    /// <see cref="Recognised"/>.
    /// </summary>
    /// <remarks>
    /// Named for what it is rather than for an error, because an unrecognised
    /// status is usually a PMS that gained a value — not a fault. The record is
    /// rejected and the value is reported; someone then decides whether the
    /// vocabulary should grow.
    /// </remarks>
    public string? UnrecognisedValue => _unrecognised;

    /// <summary>A value this connector recognises.</summary>
    /// <param name="meaning">What the source value means here.</param>
    /// <returns>A recognised reading.</returns>
    public static Reading<T> Of(T meaning) => new(meaning, null);

    /// <summary>A value no declared meaning covers.</summary>
    /// <param name="sourceValue">Exactly what the source sent, carried forward verbatim.</param>
    /// <returns>An unrecognised reading holding <paramref name="sourceValue"/>.</returns>
    public static Reading<T> Unrecognised(string sourceValue) =>
        new(default, sourceValue);

    /// <summary>Take the meaning, if this reading has one.</summary>
    /// <param name="meaning">The meaning, when this returns <c>true</c>.</param>
    /// <returns><c>true</c> when the value was recognised.</returns>
    /// <remarks>
    /// The only way to the meaning is through a check. A caller that wants to
    /// ignore the failure has to write the ignoring down.
    /// </remarks>
    public bool TryGet(out T meaning)
    {
        meaning = _meaning;
        return _unrecognised is null;
    }
}
