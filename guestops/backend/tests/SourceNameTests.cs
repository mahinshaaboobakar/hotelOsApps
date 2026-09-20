using System.Text.Json;
using System.Text.RegularExpressions;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// No vendor's or application's name is written into what GuestOps sends.
/// </summary>
/// <remarks>
/// <para>
/// <b>Found 2026-09-19</b> (developer-content sweep; ruled defects by the
/// architect): every booking the PMS sent carried an <c>"Opera"</c> chip, and
/// the stay, the booking, the group note and the cancel dialog each named Opera —
/// whatever PMS the property actually runs. The out-of-order chip named
/// <c>"EngineeringOps"</c> and the From the PMS widget showed the integration's
/// id. A property on Protel was told it was on Opera.
/// </para>
/// <para>
/// <b>What stands in their place, and why it is not the configured name yet.</b>
/// The integration's configured name is the Integration Hub's (<c>Integration.name</c>);
/// a fact arriving here carries only its <c>integration_id</c>, and the Hub's list
/// is authorized for a person — ADR 0210 keeps that refused to an application
/// until the delegated-context contract exists. So GuestOps says <i>the PMS</i>,
/// the function every such integration serves, until the route to the name is
/// ruled. Never a vendor's name, and never an id.
/// </para>
/// </remarks>
public sealed class SourceNameTests
{
    /// <summary>Names that are some vendor's or some application's, never GuestOps' to write.</summary>
    private static readonly Regex Named = new(
        """\b(Opera|OHIP|EngineeringOps|Protel|Mews|Cloudbeds)\b""", RegexOptions.Compiled);

    [Fact]
    public async Task A_booking_from_a_Protel_integration_is_not_called_Opera()
    {
        await using var harness = await DeskHarness.CreateAsync();
        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            PropertyId = DeskHarness.Property,
            Origin = RecordOrigin.Pms,
            CreatedAt = harness.Clock.GetUtcNow(),
            Version = 1,
        };
        booking.ExternalRefs.Add(new BookingExternalRef
        {
            Id = Guid.CreateVersion7(),
            BookingId = booking.Id,
            IntegrationId = "protel-cloud",
            IdentifierKind = "booking",
            ExternalId = "P-20260903-17",
        });
        harness.Db.Bookings.Add(booking);
        harness.Db.Stays.Add(new RoomStay
        {
            Id = Guid.CreateVersion7(),
            BookingId = booking.Id,
            PropertyId = DeskHarness.Property,
            RoomTypeId = DeskHarness.RoomType,
            Lifecycle = StayLifecycle.Booked,
            ArrivalAt = StayTime.Observed(new DateTimeOffset(2026, 9, 3, 14, 0, 0, TimeSpan.Zero)),
            DepartureAt = StayTime.Observed(new DateTimeOffset(2026, 9, 7, 11, 0, 0, TimeSpan.Zero)),
            Origin = RecordOrigin.Pms,
            CreatedAt = harness.Clock.GetUtcNow(),
            Version = 1,
        });
        await harness.Db.SaveChangesAsync();

        var answer = JsonSerializer.Serialize(await new BookingsView(
                new BookingReadService(harness.Db, harness.Authorizer, new StubBusinessDay(new DateOnly(2026, 9, 1))))
            .AnswerAsync(harness.Scope(), new Paging.Window(0, 25), CancellationToken.None));

        // The positive control: the booking is on the page, with its reference.
        Assert.Contains("P-20260903-17", answer);

        var names = Named.Matches(answer).Select(match => match.Value).Distinct().ToArray();
        Assert.True(names.Length == 0,
            $"a Protel booking was sent naming {string.Join(", ", names)} — the chip must name "
            + "the property's own integration, never a vendor written into the code");
    }

    [Fact]
    public void No_view_writes_a_vendor_or_an_application_name_in_code()
    {
        var module = Path.GetFullPath(Path.Combine(Here(), "..", "src", "Module"));
        var files = Directory.EnumerateFiles(module, "*.cs").ToList();
        Assert.True(files.Count >= 20, $"walked {files.Count} files under {module}");

        // Comments are records of what was sent — the ones naming Opera say why
        // it went — so only code is read.
        var found = files
            .SelectMany(file => File.ReadAllLines(file)
                .Select((line, index) => (file, index, code: Code(line))))
            .Where(line => Named.IsMatch(line.code))
            .Select(line => $"{Path.GetFileName(line.file)}:{line.index + 1}: {line.code.Trim()}")
            .ToList();

        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    [Fact]
    public void The_scan_finds_the_shapes_it_replaced()
    {
        Assert.Matches(Named, Code("""            chips.Add(new { text = "Opera", mark = "pms" });"""));
        Assert.Matches(Named, Code("""            outOfOrderBy = type.OutOfOrder > 0 ? "EngineeringOps" : null,"""));
        Assert.DoesNotMatch(Named, Code("""            // This chip said "Opera" until 2026-09-19."""));
        Assert.DoesNotMatch(Named, Code("""            "me" => services.GetRequiredService<OperatorView>()"""));
    }

    private static string Code(string line)
    {
        var at = line.IndexOf("//", StringComparison.Ordinal);
        return at < 0 ? line : line[..at];
    }

    private static string Here([System.Runtime.CompilerServices.CallerFilePath] string here = "")
        => Path.GetDirectoryName(here)!;
}
