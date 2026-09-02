namespace PmsOracle.Authentication;

/// <summary>
/// Everything this connector needs to reach one property's OHIP tenancy.
/// </summary>
/// <remarks>
/// <para>
/// <b>The names here are the package's single configuration vocabulary</b>, and
/// the UI's <c>configuration.ts</c> mirrors them. That duplication across the
/// two halves of one package is the one ADR 0128 §7 accepts deliberately: the
/// Hub's <c>settings</c> map is opaque to it, because a Hub that knew these
/// names would need a schema per connector — the "second programming language"
/// <c>CONN-Q9</c> refused when it chose a signed module over a form engine.
/// </para>
/// <para>
/// <b>Two spellings, and the split is the vault boundary rather than an
/// oversight.</b> Settings are camelCase and secrets are kebab-case, because a
/// secret's name is a path segment in the Token Vault
/// (<c>connector/{integration}/{property}/{name}</c>) and a setting's is a key
/// in a stored map. Frame 3 draws the same split as masked versus legible, and
/// <c>CONN-Q12</c> ruled that drawing to be the vault boundary — which is why
/// <see cref="ClientIdSetting"/> is a setting and not a secret.
/// </para>
/// <para>
/// <b>The lean set this replaces was package-wide.</b> Before <c>CONN-Q12</c>
/// neither half of this package held an application key, an external system
/// code or a PMS username anywhere — the backend and the form implemented a
/// simplified OAuth consistently, which is why the gap read as a form gap until
/// the backend was checked.
/// </para>
/// </remarks>
public sealed record OhipCredentials
{
    /// <summary>The OHIP host this property's tenancy answers on.</summary>
    public const string EndpointSetting = "endpoint";

    /// <summary>What OPERA calls this property — sent as <c>x-hotelid</c>.</summary>
    public const string HotelCodeSetting = "hotelCode";

    /// <summary>The integration's registered code at the tenancy.</summary>
    public const string ExternalSystemCodeSetting = "externalSystemCode";

    /// <summary>The OAuth client, legible because it identifies rather than proves.</summary>
    public const string ClientIdSetting = "clientId";

    /// <summary>The OPERA user the password grant authenticates as.</summary>
    public const string PmsUsernameSetting = "pmsUsername";

    /// <summary>The tenancy's application key — sent as <c>x-app-key</c>.</summary>
    public const string ApplicationKeySecret = "application-key";

    /// <summary>The OAuth client's secret half.</summary>
    public const string ClientSecretSecret = "client-secret";

    /// <summary>The OPERA user's password.</summary>
    public const string PmsPasswordSecret = "pms-password";

    /// <summary>Every setting name this connector reads, in the order drawn.</summary>
    public static IReadOnlyList<string> SettingNames { get; } =
    [
        EndpointSetting,
        HotelCodeSetting,
        ExternalSystemCodeSetting,
        ClientIdSetting,
        PmsUsernameSetting,
    ];

    /// <summary>Every secret name this connector reads, in the order drawn.</summary>
    public static IReadOnlyList<string> SecretNames { get; } =
    [
        ApplicationKeySecret,
        PmsPasswordSecret,
        ClientSecretSecret,
    ];

    /// <summary>The OHIP host, without a trailing slash.</summary>
    public required string Endpoint { get; init; }

    /// <summary>The property code OPERA knows this hotel by.</summary>
    public required string HotelCode { get; init; }

    /// <summary>This integration's registered external system code.</summary>
    public required string ExternalSystemCode { get; init; }

    /// <summary>The OAuth client id.</summary>
    public required string ClientId { get; init; }

    /// <summary>The OPERA username.</summary>
    public required string PmsUsername { get; init; }

    /// <summary>The tenancy's application key.</summary>
    public required string ApplicationKey { get; init; }

    /// <summary>The OAuth client secret.</summary>
    public required string ClientSecret { get; init; }

    /// <summary>The OPERA user's password.</summary>
    public required string PmsPassword { get; init; }

    /// <summary>
    /// Read a credential set from what the Hub stored for one property.
    /// </summary>
    /// <param name="settings">The stored settings map.</param>
    /// <param name="secrets">
    /// The secrets, fetched from the Token Vault by the names in
    /// <see cref="SecretNames"/>. Never stored by the Hub and never returned to
    /// the form.
    /// </param>
    /// <returns>
    /// A reading that either carries a complete set or names exactly what is
    /// absent.
    /// </returns>
    /// <remarks>
    /// A blank value is absent. A configuration holding <c>pmsUsername = ""</c>
    /// is not configured for a password grant, and treating the empty string as
    /// present would produce a 401 at poll time instead of a sentence at
    /// configuration time.
    /// </remarks>
    public static OhipCredentialReading Read(
        IReadOnlyDictionary<string, string> settings,
        IReadOnlyDictionary<string, string> secrets)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(secrets);

        // **Built by `MissingFrom`, not beside it.** The two answers must
        // agree — a completeness test and a credential read disagreeing about
        // what is absent would be a screen saying "ready" over a poll that
        // 401s — and two implementations of one list is how they drift. So
        // this reduces its values to the names that carry one, and asks the
        // same question the test asks.
        var configured = SecretNames.Where(name => !Blank(secrets, name)).ToArray();
        var missing = MissingFrom(settings, configured);

        return missing.Count > 0
            ? OhipCredentialReading.Incomplete(missing)
            : OhipCredentialReading.Of(new OhipCredentials
            {
                Endpoint = settings[EndpointSetting].TrimEnd('/'),
                HotelCode = settings[HotelCodeSetting],
                ExternalSystemCode = settings[ExternalSystemCodeSetting],
                ClientId = settings[ClientIdSetting],
                PmsUsername = settings[PmsUsernameSetting],
                ApplicationKey = secrets[ApplicationKeySecret],
                ClientSecret = secrets[ClientSecretSecret],
                PmsPassword = secrets[PmsPasswordSecret],
            });
    }

    /// <summary>
    /// Which names carry no value, judged from the settings and the *names* of
    /// the stored credentials.
    /// </summary>
    /// <param name="settings">The stored settings map.</param>
    /// <param name="configuredSecrets">
    /// The credential names this property has stored — <b>names, never
    /// values</b>, which is all a connection test is given.
    /// </param>
    /// <returns>The absent names, settings first, in the order drawn.</returns>
    /// <remarks>
    /// <b>The list <see cref="Read"/> builds</b> — it calls this rather than
    /// repeating it, so a completeness answer and a credential read cannot
    /// disagree about what is missing. The
    /// difference is only what each is given: this one is answerable without a
    /// single secret value, which is why <c>TestConnection</c> can use it
    /// while nothing in the Hub reads a credential back out of the Vault.
    /// </remarks>
    public static IReadOnlyList<string> MissingFrom(
        IReadOnlyDictionary<string, string> settings,
        IReadOnlyCollection<string> configuredSecrets)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(configuredSecrets);

        return
        [
            .. SettingNames.Where(name => Blank(settings, name)),
            .. SecretNames.Where(name => !configuredSecrets.Contains(name)),
        ];
    }

    private static bool Blank(IReadOnlyDictionary<string, string> from, string name) =>
        !from.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value);
}
