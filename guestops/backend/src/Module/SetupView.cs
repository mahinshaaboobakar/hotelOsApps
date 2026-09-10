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
            sections = new object[]
            {
                new { label = "Registration", on = true },
                new { label = "Card series", on = false },
                new { label = "Reporting", on = false },
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
