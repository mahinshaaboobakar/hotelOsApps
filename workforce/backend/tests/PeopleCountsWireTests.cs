using HotelOS.Platform;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The counts the People screen draws, and where they are counted.
/// </summary>
/// <remarks>
/// <para>
/// <b>Counted where the rows live, never from the page.</b> The header said
/// <i>"5 certifications expiring"</i> and the screen derived it from the rows it
/// had been sent — one page of a paged list — and from their tone, so it counted
/// PEOPLE rather than certificates and counted an expired one as expiring (app
/// surface audit, 2026-09-19). The total is the service's now, taken from the
/// register it already reads.
/// </para>
/// <para>
/// <b>A band and a number, never a sentence.</b> A row's standing arrived as
/// <c>"2 expiring"</c>, composed here in whatever culture the service runs
/// under (NUM-Q1, ADR 0174): the screen now writes the words and the digits.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class PeopleCountsWireTests(WorkforceFixture fixture)
{
    private static readonly DateOnly September = new(2026, 9, 1);

    [Fact]
    public async Task Certifications_expiring_is_counted_across_the_property_not_the_page()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

        var people = new List<Guid>();
        for (var index = 0; index < 7; index += 1)
        {
            people.Add(await Post(harness, scope, "Person " + index));
        }

        // Three certificates expiring, held by TWO people — so a count of people
        // says 2 and a count of certificates says 3 — plus one already expired,
        // which is not expiring and must not be counted as though it were.
        await Record(harness, scope, people[5], "Fire warden", today.AddDays(10));
        await Record(harness, scope, people[5], "First aid", today.AddDays(40));
        await Record(harness, scope, people[6], "Food hygiene", today.AddDays(3));
        await Record(harness, scope, people[0], "Lifeguard", today.AddDays(-2));
        await Record(harness, scope, people[1], "Forklift", today.AddYears(1));

        // A page of two: wherever the expiring holders fall, most of the
        // property is off this page.
        var page = await harness.CallAsync(
            PeopleView.Page, scope, "people", new { page = 0, pageSize = 2 });

        Assert.Equal(3, page.GetProperty("expiring").GetInt32());
    }

    [Fact]
    public async Task A_row_carries_its_standing_as_a_band_and_a_count()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

        var staff = await Post(harness, scope, "Anjali Menon");
        await Record(harness, scope, staff, "Fire warden", today.AddDays(10));
        await Record(harness, scope, staff, "First aid", today.AddDays(20));

        var row = (await harness.CallAsync(PeopleView.Page, scope, "people"))
            .GetProperty("postings")[0];

        Assert.Equal("expiring", row.GetProperty("standing").GetString());
        Assert.Equal(2, row.GetProperty("certificates").GetInt32());
        Assert.False(row.TryGetProperty("capability", out _),
            "the composed sentence is gone — the screen writes the words and the digits");
    }

    private static async Task<Guid> Post(ModuleHarness harness, RequestScope scope, string name)
    {
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, name);

        await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff,
            department = "FO",
            role = "Receptionist",
            from = September.ToString("yyyy-MM-dd"),
        });

        return staff;
    }

    private static Task Record(
        ModuleHarness harness, RequestScope scope, Guid staff, string name, DateOnly validUntil)
        => harness.Service<CapabilityService>().RecordAsync(
            scope,
            new RecordCapabilityCommand { StaffId = staff, Name = name, ValidUntil = validUntil },
            default);
}
