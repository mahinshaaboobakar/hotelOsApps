using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The desk's own settings, as the Setup screen draws them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The manifest declared <c>desk.configure</c>, the property approved it, the
/// Shell granted it, and nothing routed it.</b> The Setup screen called
/// <c>desk.configure/setup</c> against a <c>ModuleSurface</c> that mapped three
/// capabilities, so the envelope answered <c>404</c> and the screen fell back —
/// silently, while the fallback existed. It was the last row of the call/serve
/// diff and it was this application's defect, not the platform's: a method
/// absent from an application's surface is the application's to add.
/// </para>
/// <para>
/// <b>Read through <see cref="SettingsService"/> rather than the context.</b>
/// Unlike a stay, these settings are also written — <c>SettingsEdit</c> shares
/// the same service — and a view reading around it would be a second place the
/// shape of a setting is decided.
/// </para>
/// <para>
/// <b>Nothing here invents a value.</b> Where the entity holds no authority the
/// row says the filing has none rather than naming a plausible one, and the
/// card series shows the prefix and the next number the property will actually
/// print. Both are the gap rule: a settings screen is read by somebody about to
/// rely on it.
/// </para>
/// </remarks>
public sealed class SetupView(SettingsService settings)
{
    /// <summary>This property's desk settings.</summary>
    public async Task<object?> AnswerAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var it = await settings.GetAsync(scope, cancellationToken);

        return new
        {
            // The screen's own left rail. `on` marks where a person is; the
            // module owns that, so only the first is true here.
            // **The five the approved page draws, and three disabled with their
            // own sentence.** This sent `Registration · Card series · Reporting`
            // while `f17` draws `Registration · Guest reporting · Stop-sell · Stay
            // defaults` and `fV2` adds `Reasons` — so the harness rendered a strip
            // no property would, and `Card series` appears on no approved page at
            // all, because §2.8 puts the series inside registration.
            //
            // A `reason` means the tab is drawn with nothing behind it — ADR 0378,
            // which refuses both removing it and leaving it live and inert. The
            // sentences state what is ABSENT and promise nothing; Stay defaults'
            // is the owner's to approve through ADR 0235's temp page.
            sections = new object[]
            {
                new { label = "Registration", on = true },
                new { label = "Guest reporting", on = false },
                new
                {
                    label = "Stop-sell",
                    on = false,
                    reason = "Closing a room type for dates is not available from this screen yet.",
                },
                new
                {
                    label = "Reasons",
                    on = false,
                    reason = "Editing the reason lists is not available from this screen yet.",
                },
                new
                {
                    label = "Stay defaults",
                    on = false,
                    reason = "This tab holds no settings. Nothing about a stay is configured from here.",
                },
            },

            lead = Registration(it),
            pair = new[] { Series(it), Reporting(it) },

            card = new
            {
                title = "What the desk must capture",
                left = Fields("At home", it.RequiredForHomeCountry),
                right = Fields("Visiting", it.RequiredForVisitors),
                hint = "Two lists, because a guest from "
                     + it.HomeCountry
                     + " and a guest visiting it are asked for different papers.",
            },
        };
    }

    /// <summary>What registration asks for, and of whom.</summary>
    private static object Registration(GuestOpsSettings it)
        => new
        {
            title = "Registration",
            aside = (object?)null,
            rows = new object[]
            {
                Row("Home country", it.HomeCountry),
                Row("Accepted ID", Join(it.AcceptedIdTypes)),
                Row("Signature", it.SignatureRequired ? "required" : "not required"),
                Row("Print on check-in", it.PrintOnCheckIn ? "yes" : "no"),
            },
            blocks = Array.Empty<object>(),
        };

    /// <summary>The card series, shown as the next card the desk will print.</summary>
    /// <remarks>
    /// <b>This KEEPS the prospective number that the registration card lost, and
    /// the asymmetry is deliberate</b> — owner decision B, 2026-10-01. On an
    /// unconfigured property <c>RegistrationView.Series</c> now draws no number,
    /// because capture would be refused. <b>Here the number is true</b>: Setup is
    /// the screen that creates the settings row, the values shown are the declared
    /// defaults <c>Save</c> will store, and <c>{prefix}{NextCardNumber}</c> is
    /// exactly what the first card will carry once it is saved.
    /// <para>
    /// So <i>do not restore symmetry between these two surfaces.</i> One predicts
    /// what its own button is about to make true; the other predicted what a
    /// different screen's button would refuse.
    /// </para>
    /// </remarks>
    private static object Series(GuestOpsSettings it)
        => new
        {
            title = "Card series",
            aside = (object?)null,
            rows = new object[]
            {
                Row("Prefix", it.CardNumberPrefix),

                // The number itself, not a count of cards issued. A person
                // reading this is checking what the next card will say.
                Row("Next card", $"{it.CardNumberPrefix}{it.NextCardNumber}"),
            },
            blocks = Array.Empty<object>(),
        };

    /// <summary>Whether a filing is owed, to whom, and by when.</summary>
    private static object Reporting(GuestOpsSettings it)
        => new
        {
            title = "Reporting",
            aside = (object?)(it.ReportingRequired ? null : "not required here"),
            rows = it.ReportingRequired
                ? new object[]
                {
                    // **Absent rather than named.** A property that owes a filing
                    // and has not been told to whom is a real state, and printing
                    // an authority nobody configured would be a claim about who
                    // receives a guest's papers.
                    Row("Authority", it.ReportingAuthority ?? "not established"),
                    Row("Applies to", it.ReportingAppliesTo.ToString()),
                    Row("Due within", $"{it.ReportingDueHours} h of arrival"),
                }
                : Array.Empty<object>(),
            blocks = Array.Empty<object>(),
        };

    /// <summary>One required-fields column.</summary>
    private static object[] Fields(string label, IReadOnlyCollection<string> fields)
        => fields.Count == 0
            ? [Row(label, "nothing required")]
            : fields.Select(field => Row(label, field)).ToArray<object>();

    /// <summary>One row of a settings card.</summary>
    private static object Row(string label, string value)
        => new { label, value, tags = Array.Empty<object>(), note = (string?)null };

    /// <summary>A list in the property's own words, or the fact that it is empty.</summary>
    private static string Join(IReadOnlyCollection<string> values)
        => values.Count == 0 ? "none accepted" : string.Join(" · ", values);
}
