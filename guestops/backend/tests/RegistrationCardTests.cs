using System.Text.Json;
using System.Text.RegularExpressions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The card the desk fills in — gold frame 15, and the <c>Save</c> that used to
/// do nothing.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is held here is the caption, not the layout.</b> Frame 15 says <i>the
/// fields are the design's proposal; which of them are required is the
/// property's setting</i> — so the tests that matter are the ones that would
/// fail if a required-field rule were ever written into the product.
/// </para>
/// <para>
/// <b>The round trip goes through the command, not around it.</b> The wire's
/// <c>snake_case</c> meets the record's properties in exactly one place —
/// twenty-eight named arguments — and a box added to
/// <see cref="RegistrationFields"/> and forgotten there would silently stop
/// saving with nothing in the build objecting. A test that built the edit
/// itself would be asserting its own mapping, so this one posts a body and
/// reads the stored card back.
/// </para>
/// </remarks>
public sealed class RegistrationCardTests
{
    /// <summary>
    /// Every box the card draws survives being saved, and lands in its own field.
    /// </summary>
    /// <remarks>
    /// Each box is given a value only it could have produced — and each date a
    /// different day, so a command that wrote the passport's expiry into the
    /// visa's would be caught rather than passing on a shared value.
    /// </remarks>
    [Fact]
    public async Task Every_box_the_card_draws_survives_the_save()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync();

        var sent = Typed();
        await Save(harness, stay.Id, sent);

        var stored = await Stored(harness, stay.Id);

        var lost = RegistrationFields.All
            .Where(box => RegistrationRule.ValueOf(stored, box.Name) != sent[box.Name])
            .Select(box => box.Name)
            .ToArray();

        Assert.Empty(lost);
    }

    /// <summary>
    /// Every field the record holds has a box on the card.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived from the entity, so a new column is covered the day it is
    /// added.</b> A property may configure any card field as required; a column
    /// the rule understands and the card does not draw would be reported
    /// missing on a screen offering no way to enter it — and a hand-kept list
    /// of names here would agree with itself forever.
    /// </para>
    /// <para>
    /// The five exclusions are the record's own bookkeeping and are named with
    /// their reason, so the walk is a declaration rather than a filter that
    /// quietly grows.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_field_of_the_record_has_a_box_on_the_card()
    {
        var drawn = RegistrationFields.All.Select(box => box.Name).ToHashSet();
        var undrawn = CardColumns().Where(name => !drawn.Contains(name)).ToArray();

        Assert.Empty(undrawn);
    }

    /// <summary>
    /// Every box the card draws is a field the rule can read.
    /// </summary>
    /// <remarks>
    /// The other direction. A box whose name
    /// <see cref="RegistrationRule.ValueOf"/> did not know would read as
    /// permanently empty and — if a property required it — permanently missing.
    /// </remarks>
    [Fact]
    public void Every_box_on_the_card_is_a_field_the_rule_can_read()
    {
        var columns = CardColumns().ToHashSet();

        var unreadable = RegistrationFields.All
            .Select(box => box.Name)
            .Where(name => !columns.Contains(name))
            .ToArray();

        Assert.Empty(unreadable);
    }

    /// <summary>
    /// The card holds no required-ness of its own — frame 15's caption.
    /// </summary>
    /// <remarks>
    /// <b>The assertion the ruling asks for.</b> A box carrying *"surely
    /// everyone needs a name"* would compile one jurisdiction's practice into
    /// every property's build. <see cref="CardBox"/>'s shape is what makes that
    /// inexpressible; this says so out loud rather than leaving it to whoever
    /// next reads a record declaration.
    /// </remarks>
    [Fact]
    public void No_box_declares_itself_required()
    {
        var declared = typeof(CardBox).GetProperties().Select(property => property.Name);

        Assert.DoesNotContain("Required", declared);
        Assert.DoesNotContain("Mandatory", declared);
    }

    /// <summary>
    /// Two properties, the same guest, different required sets.
    /// </summary>
    /// <remarks>
    /// <b>The fixture is chosen so the two answers disagree.</b> A pair of
    /// settings that happened to require the same fields would pass under
    /// either rule — the product's or the property's — and prove nothing.
    /// </remarks>
    [Fact]
    public void What_is_required_is_the_propertys_answer_and_not_the_products()
    {
        var resort = new GuestOpsSettings
        {
            HomeCountry = "IN",
            RequiredForHomeCountry = ["name_as_on_id"],
        };

        var cityHotel = new GuestOpsSettings
        {
            HomeCountry = "IN",
            RequiredForHomeCountry = ["name_as_on_id", "id_type", "id_number", "purpose_of_visit"],
        };

        Assert.Equal(["name_as_on_id"], RegistrationRule.RequiredFor(resort, "IN"));

        Assert.Equal(
            ["name_as_on_id", "id_type", "id_number", "purpose_of_visit"],
            RegistrationRule.RequiredFor(cityHotel, "IN"));
    }

    /// <summary>
    /// The same guest is domestic at one property and visiting at another.
    /// </summary>
    /// <remarks>
    /// <b>Two properties, each the other's foreign country.</b> The whole
    /// no-country-in-the-product rule, expressed as an assertion rather than as
    /// a paragraph.
    /// </remarks>
    [Fact]
    public void The_same_guest_is_domestic_at_one_property_and_visiting_at_another()
    {
        var kochi = new GuestOpsSettings
        {
            HomeCountry = "IN",
            RequiredForHomeCountry = ["name_as_on_id"],
            RequiredForVisitors = ["passport_number"],
        };

        var dubai = new GuestOpsSettings
        {
            HomeCountry = "AE",
            RequiredForHomeCountry = ["name_as_on_id"],
            RequiredForVisitors = ["passport_number"],
        };

        Assert.Equal(["passport_number"], RegistrationRule.RequiredFor(kochi, "AE"));
        Assert.Equal(["name_as_on_id"], RegistrationRule.RequiredFor(dubai, "AE"));
    }

    /// <summary>
    /// Scanned documents satisfy a property that requires them.
    /// </summary>
    /// <remarks>
    /// <b>A behaviour change, asserted rather than mentioned.</b> The rule had
    /// no arm for <c>documents</c>, so its unknown-name branch answered for a
    /// name that is not unknown: a property requiring scans was told they were
    /// missing however many it held.
    /// </remarks>
    [Fact]
    public void Scanned_documents_satisfy_a_property_that_asks_for_them()
    {
        var settings = new GuestOpsSettings
        {
            HomeCountry = "IN",
            RequiredForHomeCountry = ["documents"],
        };

        var blank = new Registration { Nationality = "IN" };
        var scanned = new Registration { Nationality = "IN", DocumentRefs = "media:passport-page" };

        Assert.Equal(["documents"], RegistrationRule.Missing(settings, blank));
        Assert.Empty(RegistrationRule.Missing(settings, scanned));
    }

    /// <summary>
    /// A blank box clears the field rather than storing an empty string.
    /// </summary>
    /// <remarks>
    /// The commonest correction there is: a desk clearing a mistyped passport
    /// number. Stored blank it would be a value that is not a value, which the
    /// rule would then have to treat as missing in a second place.
    /// </remarks>
    [Fact]
    public async Task A_cleared_box_clears_the_field()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync();

        await Save(harness, stay.Id, Typed());
        await Save(harness, stay.Id, new Dictionary<string, string> { ["id_number"] = "   " });

        Assert.Null((await Stored(harness, stay.Id)).IdNumber);
    }

    /// <summary>
    /// A date the card cannot read leaves the field empty rather than refusing the card.
    /// </summary>
    /// <remarks>
    /// <b>The property's required set is what says the field is wanted.</b>
    /// Refusing the whole card for a half-typed birthday would lose the address
    /// somebody had just entered; the answer reports the field missing instead.
    /// </remarks>
    [Fact]
    public async Task A_date_that_cannot_be_read_leaves_the_field_empty()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync();

        await Save(harness, stay.Id, new Dictionary<string, string>
        {
            ["name_as_on_id"] = "Fatima Sheikh",
            ["date_of_birth"] = "14/03/86",
        });

        var stored = await Stored(harness, stay.Id);

        Assert.Equal("Fatima Sheikh", stored.NameAsOnId);
        Assert.Null(stored.DateOfBirth);
    }

    /// <summary>
    /// The save answers with what the property still wants, and saves anyway.
    /// </summary>
    /// <remarks>
    /// <b>A prompt, never a gate</b> — S19b. A guest at the desk at midnight is
    /// served and the card is completed after, so an incomplete card is stored
    /// and the answer names what is blank.
    /// </remarks>
    [Fact]
    public async Task An_incomplete_card_is_stored_and_says_what_is_missing()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync();

        var answer = await Save(harness, stay.Id, new Dictionary<string, string>
        {
            ["name_as_on_id"] = "Fatima Sheikh",
        });

        var missing = Read<string[]>(answer, "missing");

        // The harness configures a home-country set of three; two are blank.
        Assert.NotNull(missing);
        Assert.Equal(["id_type", "id_number"], missing);
        Assert.Equal("Fatima Sheikh", (await Stored(harness, stay.Id)).NameAsOnId);
    }

    /// <summary>
    /// A card refuses a body with no values rather than reading it as an empty card.
    /// </summary>
    /// <remarks>
    /// An absent <c>values</c> and a card whose every box is blank are different
    /// things; treating the first as the second would let a malformed request
    /// erase a completed registration.
    /// </remarks>
    [Fact]
    public async Task A_body_with_no_values_is_refused()
    {
        await using var harness = await DeskHarness.CreateAsync();
        await harness.ConfigureAsync();
        var stay = await harness.SeedStayAsync();

        var command = new RegistrationCommand(harness.Registrations);
        var body = JsonSerializer.SerializeToElement(new { stayId = stay.Id.ToString() });

        await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => command.RunAsync(harness.Scope(), body, CancellationToken.None));
    }

    /// <summary>Post a card through the module's own command.</summary>
    private static async Task<object?> Save(
        DeskHarness harness, Guid stayId, Dictionary<string, string> values)
    {
        var command = new RegistrationCommand(harness.Registrations);

        var body = JsonSerializer.SerializeToElement(new
        {
            stayId = stayId.ToString(),
            values,
        });

        return await command.RunAsync(harness.Scope(), body, CancellationToken.None);
    }

    /// <summary>The card as the database now holds it, read fresh.</summary>
    /// <remarks>
    /// Detached from the context's tracking first, so what is asserted is what
    /// was written rather than the instance the write happened to leave behind.
    /// </remarks>
    private static async Task<Registration> Stored(DeskHarness harness, Guid stayId)
    {
        harness.Db.ChangeTracker.Clear();

        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstAsync(harness.Db.Registrations.Where(card => card.StayId == stayId));
    }

    /// <summary>One property of the command's anonymous answer.</summary>
    private static T? Read<T>(object? answer, string name)
    {
        var json = JsonSerializer.SerializeToElement(answer);
        return json.TryGetProperty(name, out var value) ? value.Deserialize<T>() : default;
    }

    /// <summary>A distinct value per box, and a distinct day per date box.</summary>
    /// <remarks>
    /// <b>Chosen so the answers can disagree.</b> One date used everywhere would
    /// pass even if the command wrote the passport's expiry into the visa's, and
    /// one string everywhere would pass if it wrote the city into the state.
    /// </remarks>
    private static Dictionary<string, string> Typed()
    {
        var day = new DateOnly(2021, 1, 1);
        var values = new Dictionary<string, string>();

        foreach (var box in RegistrationFields.All)
        {
            if (box.Kind == "date")
            {
                day = day.AddDays(37);
                values[box.Name] = day.ToString("yyyy-MM-dd");
            }
            else if (Codes.TryGetValue(box.Name, out var code))
            {
                values[box.Name] = code;
            }
            else
            {
                values[box.Name] = $"typed-{box.Name}";
            }
        }

        return values;
    }

    /// <summary>
    /// The two boxes whose column holds two characters.
    /// </summary>
    /// <remarks>
    /// <b>Found by the database rather than assumed.</b> `nationality` and
    /// `country` are <c>varchar(2)</c> — they hold ISO 3166-1 alpha-2 and the
    /// schema says so — so a round trip that sent <c>typed-nationality</c> was
    /// refused by PostgreSQL before it proved anything. Two different codes, so
    /// a command writing one into the other is still caught.
    /// </remarks>
    private static readonly Dictionary<string, string> Codes = new()
    {
        ["nationality"] = "AE",
        ["country"] = "IN",
    };

    /// <summary>
    /// The record's own columns, as wire names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Derived from <see cref="Registration"/> rather than typed out, so a
    /// column added tomorrow is walked without anybody remembering to add it
    /// here.
    /// </para>
    /// <para>
    /// <b>The exclusions are the record's bookkeeping, and each says why.</b>
    /// Two columns are drawn under a shorter name than they are stored by, so
    /// the alias is declared rather than derived — a rename that dropped either
    /// would fail both walks above.
    /// </para>
    /// </remarks>
    private static IEnumerable<string> CardColumns()
        => typeof(Registration)
            .GetProperties()
            .Select(property => property.Name)
            .Where(name => !Bookkeeping.Contains(name))
            .Select(name => Aliases.TryGetValue(name, out var wire) ? wire : Snake(name));

    /// <summary>Columns that are not boxes on the card, and why.</summary>
    private static readonly HashSet<string> Bookkeeping =
    [
        "Id",          // the row's identity
        "StayId",      // whose card it is, carried by the request
        "CardNumber",  // minted from the property's series, never sent
        "CapturedBy",  // the authenticated caller
        "SignedAt",    // the server's clock
        "Stay",        // the navigation property
    ];

    /// <summary>The two columns drawn under a shorter name than they are stored by.</summary>
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["DocumentRefs"] = "documents",
        ["SignatureRef"] = "signature",
    };

    private static string Snake(string name)
        => Regex.Replace(name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
}
