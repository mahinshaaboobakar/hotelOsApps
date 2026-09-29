using System.Globalization;

using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Grpc;
using HotelOS.Jobs.Module;
using Xunit;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// A day and a number leave Jobs as themselves, whatever culture the server is
/// configured to — ADR 0174 §<c>NUM-Q4</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The three are not a locale nicety.</b> <c>ar-SA</c> reads on the
/// Umm al-Qura calendar, <c>fa-IR</c> on the Solar Hijri and <c>th-TH</c> on the
/// Thai Buddhist, and a custom pattern such as <c>"yyyy-MM-dd"</c> takes
/// <see cref="CultureInfo.CurrentCulture"/>'s <b>calendar</b> and not merely its
/// separators. Measured against the unpinned code on 2026-09-29, these surfaces
/// wrote the 30th of September 2026 as <c>2569-09-30</c>, <c>1448-04-19</c> and
/// <c>1405-07-08</c> — each a well-formed ISO day that no consumer can tell from
/// a real one, which is why nothing downstream would ever have failed.
/// </para>
/// <para>
/// <b>Behaviour, beside <see cref="CultureSourceGuardTests"/>'s shapes.</b> The
/// guard reads the source and so cannot say what a projection produced; this
/// runs the real read under the real culture. Both are needed: the guard catches
/// the next site written, this one proves the four that exist.
/// </para>
/// <para>
/// <b>Why it builds the projections rather than calling <see cref="ModuleHarness"/>.</b>
/// The first draft of this file went through the module surface and <b>passed
/// against the unpinned code</b>: that harness serves a real <c>WebApplication</c>
/// over HTTP, so the handler runs on Kestrel's own execution context and a
/// culture set in the test's flow never reaches it. Three of four tests were
/// green for the reason they should have been red — found only by mutating the
/// four sites back and watching which failed. The whole-process alternative,
/// <see cref="CultureInfo.DefaultThreadCurrentCulture"/>, would have reached the
/// handler and every test running beside it.
/// </para>
/// <para>
/// <b>It also answers the question that started the round.</b> Jobs' six
/// <c>Parse</c> sites already name <see cref="CultureInfo.InvariantCulture"/>,
/// and an unpinned parse <i>throws</i> under <c>ar-SA</c> — so a read that
/// carries a day, run here under <c>ar-SA</c>, is the evidence that the reading
/// half was never exposed.
/// </para>
/// </remarks>
[Collection(JobsCollection.Name)]
public class ServerCultureTests(JobsFixture fixture)
{
    /// <summary>Three cultures whose default calendar is not Gregorian.</summary>
    public static TheoryData<string> Cultures => new() { "ar-SA", "fa-IR", "th-TH" };

    private static readonly DateOnly TheDay = new(2026, 9, 30);

    /// <summary>Runs one read with the server's culture set, and puts it back.</summary>
    private static async Task<T> UnderAsync<T>(string culture, Func<Task<T>> read)
    {
        var was = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            return await read();
        }
        finally
        {
            CultureInfo.CurrentCulture = was;
        }
    }

    private static BoardProjection Board(JobsHarness h) =>
        new(h.Db, h.Queries, new Naming(h.Directory), h.Clock, h.Directory);

    [Theory]
    [MemberData(nameof(Cultures))]
    public async Task The_scheduled_list_says_the_day_it_is(string culture)
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: TheDay);

        var page = await UnderAsync(culture, () => Board(h).ScheduledAsync(h.Scope(), 0, 24, default));

        Assert.Equal("2026-09-30", Assert.Single(page.Rows).ScheduledFor);
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public async Task One_jobs_record_says_the_day_it_is(string culture)
    {
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: TheDay);
        var projection = new JobProjection(h.Db, h.Queries, Board(h), h.Clock, h.Directory);

        var detail = await UnderAsync(culture, () => projection.DetailAsync(h.Scope(), job.Id, default));

        Assert.Equal("2026-09-30", detail.PriorityAndTime.Single(d => d.K == "Scheduled for").V);
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public async Task The_grpc_view_says_the_day_it_is(string culture)
    {
        // The other services' surface, which the screens' projections do not cover.
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: TheDay);

        var view = await UnderAsync(culture, async () =>
            Views.Detail(await h.Queries.DetailAsync(h.Scope(), job.Id, default), h.Queries.Now).Job);

        Assert.Equal("2026-09-30", view.ScheduledFor);
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public async Task The_average_rating_says_the_number_it_is(string culture)
    {
        // Four and a half stars: "4٫5" under ar-SA and fa-IR before the fix, and
        // the screen prints what arrives. **The th-TH row of this theory cannot
        // fail** — Thai writes a decimal point — so it is a companion to the other
        // two rather than evidence of its own; de-DE, which writes "4,5", is not
        // here because this file's subject is the calendar cultures.
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        foreach (var stars in new[] { 4, 5 })
        {
            h.Db.Ratings.Add(new JobRating
            {
                Id = Guid.CreateVersion7(),
                JobId = Guid.CreateVersion7(),
                PropertyId = h.PropertyId,
                StayId = Guid.CreateVersion7(),
                Stars = stars,
                RatedAt = h.Queries.Now,
            });
        }

        await h.Db.SaveChangesAsync();
        var settings = new SettingsProjection(h.Db, h.Queries, h.Clock, h.Directory);

        var view = await UnderAsync(culture, () => settings.SettingsAsync(h.Scope(), default));

        Assert.Equal("4.5", view.Rating.Single(d => d.K == "Average").V);
    }
}
