using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Shifts;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// <c>WF-Q15</c> held still: an edited shift never rewrites history.
/// </summary>
/// <remarks>
/// The ruling this suite exists for is one sentence — <i>effective-forward from
/// a manager-chosen date</i> — and the tests that matter are the ones that ask
/// what a <b>past</b> date resolves to after an edit.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class ShiftCatalogueCharacterisationTests(WorkforceFixture fixture)
{
    private static readonly DateOnly March = new(2026, 3, 1);
    private static readonly DateOnly November = new(2026, 11, 1);

    [Fact]
    public async Task An_edit_leaves_a_past_rota_reading_the_hours_it_was_worked_under()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        var morning = await service.CreateAsync(
            scope, Create("Morning", "M", new TimeOnly(7, 0), new TimeOnly(15, 0), March), default);

        await service.RescheduleAsync(
            scope,
            new RescheduleShiftCommand
            {
                Id = morning.Id,
                ExpectedVersion = morning.Version,
                Hours = Hours(new TimeOnly(6, 30), new TimeOnly(14, 30)),
                EffectiveFrom = November,
            },
            default);

        var inMarch = await service.HoursOnAsync(scope, morning.Id, March.AddDays(20), default);
        var inNovember = await service.HoursOnAsync(scope, morning.Id, November.AddDays(3), default);

        // The whole of WF-Q15: a property that moves Morning from 07:00 to 06:30
        // in November must not turn last March into a rota of 06:30 starts.
        Assert.Equal(new TimeOnly(7, 0), inMarch!.StartsAt);
        Assert.Equal(new TimeOnly(6, 30), inNovember!.StartsAt);
    }

    [Fact]
    public async Task The_series_has_no_gap_and_no_overlap()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        var shift = await service.CreateAsync(
            scope, Create("Evening", "EV", new TimeOnly(15, 0), new TimeOnly(23, 0), March), default);

        await service.RescheduleAsync(
            scope,
            new RescheduleShiftCommand
            {
                Id = shift.Id,
                ExpectedVersion = shift.Version,
                Hours = Hours(new TimeOnly(16, 0), new TimeOnly(0, 0)),
                EffectiveFrom = November,
            },
            default);

        var history = await service.HistoryAsync(scope, shift.Id, default);

        // The predecessor is closed the day before the successor starts, so every
        // date from the entry's first day onward resolves to exactly one row.
        Assert.Equal(2, history.Count);
        Assert.Equal(November.AddDays(-1), history[0].EffectiveTo);
        Assert.Equal(November, history[1].EffectiveFrom);
        Assert.Null(history[1].EffectiveTo);
    }

    [Fact]
    public async Task Rescheduling_backwards_over_the_hours_in_force_is_refused()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        var shift = await service.CreateAsync(
            scope, Create("Late", "LT", new TimeOnly(14, 0), new TimeOnly(22, 0), November), default);

        // The caller means either "correct the current hours" — which is not this
        // operation — or "start a new period". Guessing between them is how a
        // rota quietly changes under somebody.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.RescheduleAsync(
                scope,
                new RescheduleShiftCommand
                {
                    Id = shift.Id,
                    ExpectedVersion = shift.Version,
                    Hours = Hours(new TimeOnly(13, 0), new TimeOnly(21, 0)),
                    EffectiveFrom = March,
                },
                default));
    }

    [Fact]
    public async Task Renaming_changes_every_rota_because_a_name_is_not_history()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        var shift = await service.CreateAsync(
            scope, Create("Morning", "AM", new TimeOnly(7, 0), new TimeOnly(15, 0), March), default);

        var renamed = await service.RenameAsync(
            scope,
            new RenameShiftCommand
            {
                Id = shift.Id,
                ExpectedVersion = shift.Version,
                Name = "Early",
            },
            default);

        // Deliberate, and the counterpart to the first test: versioning the name
        // would make one shift appear under two names in one week's history,
        // which is worse than the problem it would solve. What WF-Q15 protects is
        // what was *worked*.
        Assert.Equal("Early", renamed.Name);
        Assert.Single(await service.HistoryAsync(scope, shift.Id, default));
    }

    [Fact]
    public async Task An_off_shift_states_no_times_and_is_not_working()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        var off = await service.CreateAsync(
            scope,
            new CreateShiftCommand
            {
                Name = "Week-off",
                ShortCode = "OFF",
                Colour = "none",
                Hours = new ShiftHoursCommand(),
                EffectiveFrom = March,
            },
            default);

        var hours = await service.HoursOnAsync(scope, off.Id, March, default);

        // WF-Q12: Week-off is a rota marker, not a leave type. Expressed by the
        // absence of times rather than by a flag, so an off shift carrying hours
        // cannot be written.
        Assert.False(hours!.IsWorking);
    }

    [Theory]
    [InlineData(7, null, "a shift states both a start and an end")]
    [InlineData(null, 15, "a shift states both a start and an end")]
    public async Task A_half_stated_span_is_refused(int? start, int? end, string because)
    {
        var (service, _) = Build();

        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.CreateAsync(
                fixture.Scope(),
                new CreateShiftCommand
                {
                    Name = "Broken",
                    ShortCode = $"B{start}{end}",
                    Colour = "grey",
                    Hours = new ShiftHoursCommand
                    {
                        StartsAt = start is { } s ? new TimeOnly(s, 0) : null,
                        EndsAt = end is { } e ? new TimeOnly(e, 0) : null,
                    },
                    EffectiveFrom = March,
                },
                default));

        Assert.Contains(because, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_second_span_without_a_first_is_refused()
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.CreateAsync(
                fixture.Scope(),
                new CreateShiftCommand
                {
                    Name = "Halves",
                    ShortCode = "HV",
                    Colour = "grey",
                    Hours = new ShiftHoursCommand
                    {
                        SecondStartsAt = new TimeOnly(18, 0),
                        SecondEndsAt = new TimeOnly(22, 0),
                    },
                    EffectiveFrom = March,
                },
                default));
    }

    [Fact]
    public async Task A_night_shift_may_end_before_it_starts()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        var night = await service.CreateAsync(
            scope, Create("Night", "N", new TimeOnly(23, 0), new TimeOnly(7, 0), March), default);

        var hours = await service.HoursOnAsync(scope, night.Id, March, default);

        // The times within a day may run backwards — that is how a night shift is
        // written — while the *effective window* may not. Two different orderings,
        // and only one of them is a contradiction.
        Assert.True(hours!.IsWorking);
        Assert.True(hours.EndsAt < hours.StartsAt);
    }

    [Fact]
    public async Task Two_live_shifts_cannot_share_a_short_code()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        await service.CreateAsync(
            scope, Create("Morning", "MM", new TimeOnly(7, 0), new TimeOnly(15, 0), March), default);

        // Two shifts sharing a code look identical in a rota cell and on paper —
        // the failure the typed-not-derived rule exists to prevent, reached by a
        // different route.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.CreateAsync(
                scope,
                Create("Mid-shift", "MM", new TimeOnly(11, 0), new TimeOnly(19, 0), March),
                default));
    }

    [Fact]
    public async Task A_retired_shift_keeps_its_hours_and_leaves_the_picker()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        var shift = await service.CreateAsync(
            scope, Create("Old", "OLD", new TimeOnly(9, 0), new TimeOnly(17, 0), March), default);

        await service.RetireAsync(
            scope,
            new RetireShiftCommand { Id = shift.Id, ExpectedVersion = shift.Version },
            default);

        var offered = await service.ListAsync(scope, includeRetired: false, default);
        var all = await service.ListAsync(scope, includeRetired: true, default);

        Assert.DoesNotContain(offered, e => e.Id == shift.Id);
        Assert.Contains(all, e => e.Id == shift.Id);

        // Not a delete: rotas were worked under it, and removing the row would
        // leave every one of them pointing at nothing.
        Assert.NotNull(await service.HoursOnAsync(scope, shift.Id, March, default));
    }

    [Fact]
    public async Task Writing_the_catalogue_asks_for_policy_manage()
    {
        var (service, authorizer) = Build();

        await service.CreateAsync(
            fixture.Scope(),
            Create("Perm", "PRM", new TimeOnly(8, 0), new TimeOnly(16, 0), March),
            default);

        Assert.Equal("policy.manage", Assert.Single(authorizer.Checks).Permission);
    }

    private static ShiftHoursCommand Hours(TimeOnly starts, TimeOnly ends) =>
        new() { StartsAt = starts, EndsAt = ends };

    private static CreateShiftCommand Create(
        string name, string code, TimeOnly starts, TimeOnly ends, DateOnly from) =>
        new()
        {
            Name = name,
            ShortCode = code,
            Colour = "cyan",
            Hours = Hours(starts, ends),
            EffectiveFrom = from,
        };

    private (ShiftCatalogueService Service, RecordingAuthorizer Authorizer) Build()
    {
        var authorizer = new RecordingAuthorizer();

        return (new ShiftCatalogueService(fixture.Context(), authorizer, TimeProvider.System),
            authorizer);
    }
}
