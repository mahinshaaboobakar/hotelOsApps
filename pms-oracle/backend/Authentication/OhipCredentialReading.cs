namespace PmsOracle.Authentication;

/// <summary>
/// The outcome of reading a stored configuration as a credential set: either a
/// complete set, or exactly which names are absent.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deliberately not <c>Reading&lt;T&gt;</c>.</b> That type answers a
/// different question — a source sent a status string this connector may not
/// recognise, and it carries the value forward so nobody has to guess what the
/// PMS emitted. Here nothing was sent and nothing is unrecognised; a
/// configuration is simply unfinished, and what the caller needs is the list of
/// names still to fill. Forcing one type to serve both would make
/// <c>UnrecognisedValue</c> mean "a missing key" in half its uses.
/// </para>
/// <para>
/// <b>There is no way to the credentials except through a check</b>, which is
/// the property <c>Reading&lt;T&gt;</c> does share and the reason both exist.
/// A caller that wants to proceed without one has to write the ignoring down.
/// </para>
/// <para>
/// The absent names are the useful half. <c>TestConnection</c> reports them
/// straight to the operator, so an unfinished configuration is answered with
/// <i>"application-key and pms-password are not set"</i> rather than with a
/// vendor 401 half an hour later at poll time.
/// </para>
/// </remarks>
public readonly record struct OhipCredentialReading
{
    private readonly OhipCredentials? _credentials;
    private readonly IReadOnlyList<string>? _missing;

    private OhipCredentialReading(
        OhipCredentials? credentials, IReadOnlyList<string>? missing)
    {
        _credentials = credentials;
        _missing = missing;
    }

    /// <summary>Whether every name this connector needs carries a value.</summary>
    public bool Complete => _credentials is not null;

    /// <summary>
    /// The names with no value, in the order they are drawn — empty when
    /// <see cref="Complete"/>.
    /// </summary>
    /// <remarks>
    /// Settings before secrets, matching the form, so an operator reading the
    /// list works down the screen rather than hunting.
    /// </remarks>
    public IReadOnlyList<string> Missing => _missing ?? [];

    /// <summary>A complete credential set.</summary>
    /// <param name="credentials">The set.</param>
    /// <returns>A complete reading.</returns>
    public static OhipCredentialReading Of(OhipCredentials credentials) =>
        new(credentials, null);

    /// <summary>A configuration that is not finished.</summary>
    /// <param name="missing">The names carrying no value. Never empty.</param>
    /// <returns>An incomplete reading naming <paramref name="missing"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="missing"/> is empty.</exception>
    public static OhipCredentialReading Incomplete(IReadOnlyList<string> missing)
    {
        ArgumentNullException.ThrowIfNull(missing);

        // An "incomplete" reading naming nothing would report a complete
        // configuration as broken with no way to see why.
        if (missing.Count == 0)
        {
            throw new ArgumentException(
                "An incomplete reading must name at least one absent value.",
                nameof(missing));
        }

        return new OhipCredentialReading(null, missing);
    }

    /// <summary>Take the credential set, if this reading has one.</summary>
    /// <param name="credentials">The set, when this returns <c>true</c>.</param>
    /// <returns><c>true</c> when the configuration is complete.</returns>
    public bool TryGet(out OhipCredentials credentials)
    {
        credentials = _credentials!;
        return _credentials is not null;
    }
}
