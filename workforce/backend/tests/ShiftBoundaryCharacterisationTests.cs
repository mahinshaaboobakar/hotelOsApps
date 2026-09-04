using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The shift fan-out — the announcement that is its own fact, and the count that
/// removes the handover hazard.
/// </summary>
/// <remarks>
/// <para>
/// Ruled 2026-09-04 on Jobs' <c>S5-D13</c>. Two things are worth holding still
/// above everything else here: that a tick which runs twice announces once, and
/// that a consumer reading <c>on_now_after</c> is right whichever of a
/// handover's two events reaches it last.
/// </para>
/// <para>
/// The clock is frozen, because every assertion is about a moment.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class ShiftBoundaryCharacterisationTests(WorkforceFixture fixture)
{
    private static int slot = -1;
    private static int code = -1;

    private static DateOnly SomeDay() =>
        new DateOnly(2032, 1, 5).AddDays(Interlocked.Increment(ref slot) * 14);

    private static string Somewhere() => $"B{Interlocked.Increment(ref code)}";

    [Fact]
    public async Task A_tick_run_twice_announces_once()
    {
        var day = SomeDay();
        var clock = At(day, 12, 0);
        var (announcer, rota, shifts, events) = Build(clock);
        var scope = fixture.Scope();
        var department = Somewhere();

        var morning = await Shift(shifts, scope, 7, 15);
        await rota.AssignAsync(scope, Assign(Uuid7.NewUuid7(), day, morning, department), default);

        var first = await announcer.AnnounceDueAsync(scope, default);
        var second = await announcer.AnnounceDueAsync(scope, default);

        // **The whole mechanism.** The announcement row and the event are one
        // transaction under a unique key, so a retry, a restart or two
        // schedulers announce nothing twice — which is what lets the tick be an
        // ordinary at-least-once one.
        Assert.Equal(1, first);
        Assert.Equal(0, second);

        Assert.Single(events.Events, e => e.EventType == ShiftAnnouncements.Started);
    }

    [Fact]
    public async Task A_boundary_that_has_not_fallen_yet_is_not_announced()
    {
        var day = SomeDay();
        var clock = At(day, 12, 0);
        var (announcer, rota, shifts, events) = Build(clock);
        var scope = fixture.Scope();
        var department = Somewhere();

        var morning = await Shift(shifts, scope, 7, 15);
        await rota.AssignAsync(scope, Assign(Uuid7.NewUuid7(), day, morning, department), default);

        await announcer.AnnounceDueAsync(scope, default);

        // Noon: the shift has started and has not ended.
        Assert.Single(events.Events, e => e.EventType == ShiftAnnouncements.Started);
        Assert.DoesNotContain(events.Events, e => e.EventType == ShiftAnnouncements.Ended);

        // The tick that runs after fifteen hundred announces the end, and does
        // not re-announce the start.
        clock.Now = At(day, 15, 30).Now;
        Assert.Equal(1, await announcer.AnnounceDueAsync(scope, default));
        Assert.Single(events.Events, e => e.EventType == ShiftAnnouncements.Ended);
    }

    [Fact]
    public async Task At_a_handover_both_events_carry_the_same_count()
    {
        var day = SomeDay();
        var clock = At(day, 15, 30);
        var (announcer, rota, shifts, events) = Build(clock);
        var scope = fixture.Scope();
        var department = Somewhere();

        var morning = await Shift(shifts, scope, 7, 15);
        var afternoon = await Shift(shifts, scope, 15, 23);

        await rota.AssignAsync(scope, Assign(Uuid7.NewUuid7(), day, morning, department), default);
        await rota.AssignAsync(scope, Assign(Uuid7.NewUuid7(), day, afternoon, department), default);

        await announcer.AnnounceDueAsync(scope, default);

        var ended = Payload(events, ShiftAnnouncements.Ended);
        var startedAfternoon = events.Events
            .Where(e => e.EventType == ShiftAnnouncements.Started)
            .Select(e => (ShiftBoundaryAnnouncement)e.Payload!)
            .Single(p => p.At.Hour == 15);

        // **The hazard removed rather than mitigated.** Morning's end and
        // Afternoon's start fall at one instant; a consumer setting presence
        // from the verb lands on whichever arrived last, and the wrong order
        // reads unstaffed all afternoon with nothing looking broken. Both carry
        // the same count, so the boolean is right either way.
        Assert.Equal(1, ended.OnNowAfter);
        Assert.Equal(1, startedAfternoon.OnNowAfter);
    }

    [Fact]
    public async Task The_last_shift_of_the_day_ends_with_nobody_on()
    {
        var day = SomeDay();
        var clock = At(day, 16, 0);
        var (announcer, rota, shifts, events) = Build(clock);
        var scope = fixture.Scope();
        var department = Somewhere();

        var morning = await Shift(shifts, scope, 7, 15);
        await rota.AssignAsync(scope, Assign(Uuid7.NewUuid7(), day, morning, department), default);

        await announcer.AnnounceDueAsync(scope, default);

        // Which is the fact a presence row exists to hold: the department is
        // now closed, and the number says so without the consumer inferring it.
        Assert.Equal(0, Payload(events, ShiftAnnouncements.Ended).OnNowAfter);
    }

    [Fact]
    public async Task A_night_shift_ends_on_the_next_day_and_carries_its_own_business_date()
    {
        var day = SomeDay();
        var clock = At(day.AddDays(1), 8, 0);
        var (announcer, rota, shifts, events) = Build(clock);
        var scope = fixture.Scope();
        var department = Somewhere();

        var night = await Shift(shifts, scope, 23, 7);
        await rota.AssignAsync(scope, Assign(Uuid7.NewUuid7(), day, night, department), default);

        await announcer.AnnounceDueAsync(scope, default);

        var ended = Payload(events, ShiftAnnouncements.Ended);

        // The end falls at 07:00 on the following calendar day, and carries the
        // rota's own date. A consumer reconciling against a roster needs the
        // date the roster used, not the one the clock shows — and the unique key
        // needs it too, or a night shift's end collides with the next morning's.
        Assert.Equal(day.ToString("yyyy-MM-dd"), ended.BusinessDate);
        Assert.Equal(day.AddDays(1).ToDateTime(new TimeOnly(7, 0)), ended.At.UtcDateTime);
    }

    [Fact]
    public async Task A_split_shift_announces_its_second_half_too()
    {
        var day = SomeDay();
        var clock = At(day, 23, 0);
        var (announcer, rota, shifts, events) = Build(clock);
        var scope = fixture.Scope();
        var department = Somewhere();

        var split = await Split(shifts, scope);
        await rota.AssignAsync(scope, Assign(Uuid7.NewUuid7(), day, split, department), default);

        await announcer.AnnounceDueAsync(scope, default);

        // 10–14 and 18–22 is four boundaries — but one row per (department,
        // shift, date, kind), so the day yields one started and one ended.
        // Announcing four would be four presence flips for one person's day.
        Assert.Single(events.Events, e => e.EventType == ShiftAnnouncements.Started);
        Assert.Single(events.Events, e => e.EventType == ShiftAnnouncements.Ended);
    }

    [Fact]
    public async Task Nine_people_on_one_shift_is_one_announcement()
    {
        var day = SomeDay();
        var clock = At(day, 12, 0);
        var (announcer, rota, shifts, events) = Build(clock);
        var scope = fixture.Scope();
        var department = Somewhere();

        var morning = await Shift(shifts, scope, 7, 15);

        for (var i = 0; i < 9; i++)
        {
            await rota.AssignAsync(
                scope, Assign(Uuid7.NewUuid7(), day, morning, department), default);
        }

        Assert.Equal(1, await announcer.AnnounceDueAsync(scope, default));

        // One fact about the department, not nine about people — and the count
        // is what carries the nine.
        Assert.Equal(9, Payload(events, ShiftAnnouncements.Started).OnNowAfter);
    }

    [Fact]
    public async Task The_announcement_is_its_own_aggregate()
    {
        var day = SomeDay();
        var (announcer, rota, shifts, events) = Build(At(day, 12, 0));
        var scope = fixture.Scope();
        var department = Somewhere();

        var morning = await Shift(shifts, scope, 7, 15);
        await rota.AssignAsync(scope, Assign(Uuid7.NewUuid7(), day, morning, department), default);
        await announcer.AnnounceDueAsync(scope, default);

        var appended = Assert.Single(events.Events);
        var row = await fixture.Context().ShiftBoundaries
            .SingleAsync(b => b.Id == appended.AggregateId);

        // *Announce against what you own.* Not the catalogue entry, whose
        // version never moves when a shift starts: two announcements would then
        // collide on (aggregate, version) and a consumer deduping on the pair
        // would drop the second.
        Assert.Equal(ShiftAnnouncements.Aggregate, appended.AggregateType);
        Assert.Equal(1, appended.EntityVersion);
        Assert.Equal(department, row.DepartmentCode);
    }

    private static ShiftBoundaryAnnouncement Payload(
        RecordingEventAppender events, string eventType) =>
        (ShiftBoundaryAnnouncement)events.Events.Single(e => e.EventType == eventType).Payload!;

    private static FrozenClock At(DateOnly day, int hour, int minute) =>
        new(new DateTimeOffset(day.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero));

    private static AssignShiftCommand Assign(
        Guid staff, DateOnly date, Guid entry, string department) =>
        new()
        {
            StaffId = staff,
            Date = date,
            CatalogueEntryId = entry,
            DepartmentCode = department,
        };

    private static async Task<Guid> Shift(
        ShiftCatalogueService shifts, RequestScope scope, int from, int to)
    {
        var entry = await shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = $"Shift {from}",
                ShortCode = $"Y{Interlocked.Increment(ref code)}",
                Colour = "cyan",
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(from, 0),
                    EndsAt = new TimeOnly(to, 0),
                },
                EffectiveFrom = new DateOnly(2031, 1, 1),
            },
            default);

        return entry.Id;
    }

    private static async Task<Guid> Split(ShiftCatalogueService shifts, RequestScope scope)
    {
        var entry = await shifts.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = "Split — Banquet",
                ShortCode = $"YS{Interlocked.Increment(ref code)}",
                Colour = "amber",
                Hours = new ShiftHoursCommand
                {
                    StartsAt = new TimeOnly(10, 0),
                    EndsAt = new TimeOnly(14, 0),
                    SecondStartsAt = new TimeOnly(18, 0),
                    SecondEndsAt = new TimeOnly(22, 0),
                },
                EffectiveFrom = new DateOnly(2031, 1, 1),
            },
            default);

        return entry.Id;
    }

    private (ShiftBoundaryAnnouncer Announcer, RotaService Rota, ShiftCatalogueService Shifts,
        RecordingEventAppender Events) Build(TimeProvider clock)
    {
        var authorizer = new RecordingAuthorizer();
        var events = new RecordingEventAppender();
        var db = fixture.Context();

        return (
            new ShiftBoundaryAnnouncer(db, events, clock),
            new RotaService(db, authorizer, clock),
            new ShiftCatalogueService(db, authorizer, clock),
            events);
    }
}
