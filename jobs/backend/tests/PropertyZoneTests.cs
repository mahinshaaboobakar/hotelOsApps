using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// Every day and every wall-clock time Jobs uses is the property's, in two zones
/// either side of UTC: Asia/Kolkata (+05:30) and America/Guatemala (−06:00).
/// </summary>
/// <remarks>
/// ADR 0174; the owner, 2026-09-19. Each instant is chosen so the UTC reading and
/// the property's reading disagree — the defect classes GG found in Workforce
/// (b5c5ffc), where a 07:00 shift's announcement fired at 12:30 at +05:30.
/// <b>Red before the fixes</b>, each on the line the source guard names.
/// </remarks>
[Collection(JobsCollection.Name)]
public class PropertyZoneTests(JobsFixture fixture)
{
    public static TheoryData<string> Zones => new() { "Asia/Kolkata", "America/Guatemala" };

    private static TimeZoneInfo Zone(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);

    [Theory]
    [MemberData(nameof(Zones))]
    public async Task Service_hours_are_read_on_the_property_clock(string zone)
    {
        // Service hours 07:00–23:00 at the property. Each instant is inside them at
        // the property and outside them in UTC, or the other way round.
        var h = new JobsHarness(fixture);
        h.Directory.Timezone = zone;
        h.Db.ServiceHours.Add(new Domain.Policy.ServiceHours
        {
            Id = Guid.CreateVersion7(), PropertyId = h.PropertyId, DepartmentCode = "ENG",
            From = new TimeOnly(7, 0), To = new TimeOnly(23, 0),
        });
        await h.Db.SaveChangesAsync();

        var (inside, outside) = zone == "Asia/Kolkata"
            ? (new DateTimeOffset(2026, 9, 19, 2, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 19, 18, 0, 0, TimeSpan.Zero))
            : (new DateTimeOffset(2026, 9, 20, 4, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 19, 12, 30, 0, TimeSpan.Zero));

        Assert.True(await h.Presence.InsideHoursAsync(h.PropertyId, "ENG", inside, default), $"{inside:o} is {TimeZoneInfo.ConvertTime(inside, Zone(zone)):HH:mm} at the property");
        Assert.False(await h.Presence.InsideHoursAsync(h.PropertyId, "ENG", outside, default), $"{outside:o} is {TimeZoneInfo.ConvertTime(outside, Zone(zone)):HH:mm} at the property");
    }

    [Theory]
    [MemberData(nameof(Zones))]
    public async Task A_scheduled_jobs_clock_starts_at_the_propertys_midnight(string zone)
    {
        var h = new JobsHarness(fixture);
        h.Directory.Timezone = zone;
        await h.SeedCatalogueAsync();
        var day = new DateOnly(2026, 9, 20);

        var job = await h.RaiseNotCoolingAsync(h.Scope(), scheduledFor: day);

        var local = TimeZoneInfo.ConvertTime(job.DueAt!.Value, Zone(zone));
        Assert.Equal(day, DateOnly.FromDateTime(local.DateTime));
        Assert.True(local.TimeOfDay <= TimeSpan.FromHours(2), $"due {local:yyyy-MM-dd HH:mm} at the property — minutes after its midnight, not UTC's");
    }

    [Theory]
    [MemberData(nameof(Zones))]
    public async Task Auto_assignment_asks_for_todays_roster_at_the_property(string zone)
    {
        // 20:00 UTC is the 20th in Kolkata; 03:00 UTC on the 20th is still the 19th in Guatemala.
        var now = zone == "Asia/Kolkata"
            ? new DateTimeOffset(2026, 9, 19, 20, 0, 0, TimeSpan.Zero)
            : new DateTimeOffset(2026, 9, 20, 3, 0, 0, TimeSpan.Zero);
        var h = new JobsHarness(fixture, now);
        h.Directory.Timezone = zone;
        await h.SeedCatalogueAsync();

        await h.RaiseNotCoolingAsync(h.Scope());

        Assert.Equal(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Zone(zone)).DateTime), h.Directory.OnShiftDays[^1]);
    }

    [Theory]
    [MemberData(nameof(Zones))]
    public async Task Closed_today_and_done_today_count_from_the_propertys_midnight(string zone)
    {
        // One job closed late on the property's yesterday (inside the last 24 hours),
        // one closed half an hour ago. "Today" is one, not two.
        var now = zone == "Asia/Kolkata"
            ? new DateTimeOffset(2026, 9, 19, 20, 0, 0, TimeSpan.Zero)
            : new DateTimeOffset(2026, 9, 20, 3, 0, 0, TimeSpan.Zero);
        var yesterday = zone == "Asia/Kolkata"
            ? new DateTimeOffset(2026, 9, 19, 17, 0, 0, TimeSpan.Zero)
            : new DateTimeOffset(2026, 9, 19, 5, 0, 0, TimeSpan.Zero);

        await using var module = await ModuleHarness.StartAsync(fixture);
        var h = module.Data;
        h.Clock.Set(now);
        h.Directory.Timezone = zone;
        await h.SeedCatalogueAsync();
        foreach (var closedAt in new[] { yesterday, now.AddMinutes(-30) })
        {
            var job = await h.RaiseNotCoolingAsync(h.Scope());
            job.JobStatus = JobStatus.Closed;
            job.UpdatedAt = closedAt;
            await h.Db.SaveChangesAsync();
        }

        var today = await module.CallAsync(Permissions.Read, "today");
        var widget = await module.CallAsync(Permissions.Read, "widgetBoard");

        Assert.Equal(1, today.Number("closedToday"));
        Assert.Equal(1, widget.Number("doneToday"));
    }

    [Fact]
    public async Task The_departure_note_carries_no_utc_time_in_its_words()
    {
        // The note is read by a person on the job; its own timestamp is drawn in the
        // property's form. Its words said "Guest departed 2026-09-19 06:35 UTC".
        var h = new JobsHarness(fixture);
        await h.SeedCatalogueAsync();
        var stay = Guid.CreateVersion7();
        var job = await h.RaiseNotCoolingAsync(h.Scope(), stay: stay);
        var handler = new Events.StayDepartedHandler(h.Db, h.Clock);

        await handler.HandleAsync(h.Sweeping, new Events.StayDeparted(stay), new HotelOS.Platform.EventEnvelope
        {
            EventId = Guid.CreateVersion7(), EventType = "stay.departed", AggregateType = "stay", AggregateId = stay,
            OccurredAt = new DateTimeOffset(2026, 9, 19, 6, 35, 0, TimeSpan.Zero),
        }, default);

        var note = await h.Db.Notes.SingleAsync(n => n.JobId == job.Id && n.Text.StartsWith("Guest departed"));
        Assert.DoesNotContain("UTC", note.Text);
        Assert.DoesNotMatch(@"\d{4}-\d{2}-\d{2}", note.Text);
    }
}
