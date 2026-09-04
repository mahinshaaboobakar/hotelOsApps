using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Duties;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// <c>WF-Q8</c> held still: the MOD duty is a span, and it has one holder at
/// every instant.
/// </summary>
/// <remarks>
/// The owner's sentence — <i>"MOD may run 8:00 pm to 8:00 am — it covers two
/// dates"</i> — is the first test. The rest are the consequence: what replaced
/// <i>one MOD per property per day</i> is an overlap check, and back-to-back
/// handovers must not trip it.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class DutyCharacterisationTests(WorkforceFixture fixture)
{
    /// <summary>Each test gets its own week, and this is a domain fact.</summary>
    /// <remarks>
    /// <para>
    /// A MOD duty is <b>property-wide</b> — that is the whole point of the
    /// register — so unlike a posting (isolated by staff member) or a capability
    /// (by staff member and name), two duties in one suite have no entity axis to
    /// separate them. <b>Time is the only one</b>, which is exactly what the
    /// overlap rule says.
    /// </para>
    /// <para>
    /// Discovered by the first run: every test used one Friday evening, and the
    /// first duty assigned refused all nine that followed. The shared fixture
    /// surfacing that is the design working — the alternative, a database per
    /// test, would have hidden the very rule this suite exists to hold still.
    /// </para>
    /// </remarks>
    private static int slot = -1;

    private static DateTimeOffset FridayEvening() =>
        new DateTimeOffset(2026, 1, 2, 20, 0, 0, TimeSpan.Zero)
            .AddDays(Interlocked.Increment(ref slot) * 7);

    [Fact]
    public async Task A_duty_crosses_midnight_and_covers_two_dates()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();

        var duty = await service.AssignAsync(
            scope, Assign(friday, friday.AddHours(12)), default);

        // The relationship, not two calendar numbers: the duty ends on the day
        // after it starts. Pinning literal day numbers made this test depend on
        // which week the suite handed it, which is a fact about the harness and
        // not about the duty.
        Assert.Equal(duty.StartsAt.Date.AddDays(1), duty.EndsAt.Date);

        // The owner's own scenario, and the reason a day cell cannot hold it.
        Assert.True(duty.CoversAt(friday.AddHours(6)));
    }

    [Fact]
    public async Task Two_duties_may_not_be_in_force_at_one_instant()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();

        await service.AssignAsync(scope, Assign(friday, friday.AddHours(12)), default);

        // Refused, not warned — WF-Q16. "Who is MOD now" with two answers is a
        // corrupt record, not a judgment call.
        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.AssignAsync(
                scope, Assign(friday.AddHours(6), friday.AddHours(18)), default));

        Assert.Contains("already holds part of that span", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Back_to_back_duties_do_not_overlap()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();

        var night = await service.AssignAsync(
            scope, Assign(friday, friday.AddHours(12)), default);

        // The ordinary handover: one ends at 08:00, the next begins at 08:00.
        // Half-open on both sides, so this is not a clash — and refusing it would
        // make the register unusable.
        var day = await service.AssignAsync(
            scope, Assign(night.EndsAt, night.EndsAt.AddHours(12)), default);

        Assert.NotEqual(night.Id, day.Id);
    }

    [Fact]
    public async Task Who_is_on_duty_is_the_clock_against_the_span()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();
        var holder = Guid.CreateVersion7();

        await service.AssignAsync(
            scope,
            Assign(friday, friday.AddHours(12)) with { StaffId = holder },
            default);

        var atMidnight = await service.HolderAtAsync(scope, friday.AddHours(4), default);
        var atNoon = await service.HolderAtAsync(scope, friday.AddHours(16), default);

        // Computed on the question. There is no is_current_mod flag to read, and
        // none to go stale at midnight.
        Assert.Equal(holder, atMidnight!.StaffId);
        Assert.Null(atNoon);
    }

    [Fact]
    public async Task The_end_instant_belongs_to_the_next_duty()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        var night = await service.AssignAsync(
            scope, Assign(friday, friday.AddHours(12)) with { StaffId = first }, default);

        await service.AssignAsync(
            scope,
            Assign(night.EndsAt, night.EndsAt.AddHours(12)) with { StaffId = second },
            default);

        var atHandover = await service.HolderAtAsync(scope, night.EndsAt, default);

        // Half-open: exactly one answer at every instant, including the one they
        // share. A closed interval would return two here — which is the corrupt
        // state the overlap rule exists to prevent, arriving through a boundary
        // rather than through an assignment.
        Assert.Equal(second, atHandover!.StaffId);
    }

    [Fact]
    public async Task Now_and_next_is_what_the_register_answers()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();
        var onNow = Guid.CreateVersion7();
        var onNext = Guid.CreateVersion7();

        var current = await service.AssignAsync(
            scope, Assign(friday, friday.AddHours(12)) with { StaffId = onNow }, default);

        await service.AssignAsync(
            scope,
            Assign(current.EndsAt.AddHours(2), current.EndsAt.AddHours(14)) with { StaffId = onNext },
            default);

        var holder = await service.HolderAtAsync(scope, friday.AddHours(3), default);
        var next = await service.NextAfterAsync(scope, friday.AddHours(3), default);

        Assert.Equal(onNow, holder!.StaffId);
        Assert.Equal(onNext, next!.StaffId);
    }

    [Fact]
    public async Task A_gap_between_duties_answers_nobody_rather_than_guessing()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();

        var duty = await service.AssignAsync(
            scope, Assign(friday, friday.AddHours(12)), default);

        var inTheGap = await service.HolderAtAsync(scope, duty.EndsAt.AddHours(3), default);

        // Saturday night with no MOD is a real state the register draws dashed.
        // Returning the nearest duty instead would be a guess presented as a fact.
        Assert.Null(inTheGap);
    }

    [Fact]
    public async Task A_span_that_ends_before_it_starts_is_refused()
    {
        var (service, _) = Build();
        var friday = FridayEvening();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.AssignAsync(
                fixture.Scope(), Assign(friday, friday.AddHours(-2)), default));
    }

    [Fact]
    public async Task Amending_a_span_re_checks_the_overlap()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();

        var night = await service.AssignAsync(
            scope, Assign(friday, friday.AddHours(12)), default);
        var day = await service.AssignAsync(
            scope, Assign(night.EndsAt, night.EndsAt.AddHours(12)), default);

        // Stretching the second backwards into the first is the same corruption
        // as assigning it there, and the check has to be on both paths.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.AmendAsync(
                scope,
                new AmendDutyCommand
                {
                    Id = day.Id,
                    ExpectedVersion = day.Version,
                    StartsAt = night.StartsAt.AddHours(4),
                },
                default));
    }

    [Fact]
    public async Task Amending_a_duty_without_moving_it_does_not_clash_with_itself()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();

        var duty = await service.AssignAsync(
            scope, Assign(friday, friday.AddHours(12)), default);

        var amended = await service.AmendAsync(
            scope,
            new AmendDutyCommand
            {
                Id = duty.Id,
                ExpectedVersion = duty.Version,
                HandoverNote = "Banquet finishes 23:30",
            },
            default);

        Assert.Equal("Banquet finishes 23:30", amended.HandoverNote);
    }

    [Fact]
    public async Task The_week_strip_carries_a_duty_that_began_before_it()
    {
        var (service, _) = Build();
        var friday = FridayEvening();
        var scope = fixture.Scope();

        var duty = await service.AssignAsync(
            scope, Assign(friday, friday.AddHours(12)), default);

        var saturday = await service.ListAsync(
            scope, friday.AddHours(4), friday.AddHours(28), default);

        // Overlapping, not contained — which is the whole reason the strip is a
        // timeline rather than a row of day cells.
        Assert.Contains(saturday, d => d.Id == duty.Id);
    }

    [Fact]
    public async Task The_handover_note_is_optional_and_blocks_nothing()
    {
        var (service, _) = Build();
        var friday = FridayEvening();

        var duty = await service.AssignAsync(
            fixture.Scope(), Assign(friday, friday.AddHours(12)), default);

        Assert.Equal(string.Empty, duty.HandoverNote);
    }

    [Fact]
    public async Task Assigning_asks_for_duty_assign_and_reading_for_workforce_read()
    {
        var (service, authorizer) = Build();
        var friday = FridayEvening();

        await service.AssignAsync(
            fixture.Scope(), Assign(friday, friday.AddHours(12)), default);
        await service.HolderAtAsync(fixture.Scope(), friday, default);

        Assert.Equal(
            ["duty.assign", "roster.read"],
            authorizer.Checks.Select(check => check.Permission));
    }

    private static AssignDutyCommand Assign(DateTimeOffset starts, DateTimeOffset ends) =>
        new() { StaffId = Guid.CreateVersion7(), StartsAt = starts, EndsAt = ends };

    private (DutyService Service, RecordingAuthorizer Authorizer) Build()
    {
        var authorizer = new RecordingAuthorizer();

        return (new DutyService(fixture.Context(), authorizer, TimeProvider.System), authorizer);
    }
}
