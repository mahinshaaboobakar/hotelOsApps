using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Domain;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// What <see cref="CapabilityService"/> does, held still.
/// </summary>
/// <remarks>
/// Slice 2. The rules are: one optional date carries two concepts, the band is
/// computed rather than stored, the Attention audience resolves through
/// postings, and <b>nothing here blocks anything</b>.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class CapabilityCharacterisationTests(WorkforceFixture fixture)
{
    [Fact]
    public async Task A_capability_without_a_date_is_an_ability()
    {
        var (service, _) = Build();

        var capability = await service.RecordAsync(
            fixture.Scope(), Record(Uuid7.NewUuid7(), "Speaks Arabic"), default);

        Assert.False(capability.Lapses);
        Assert.Equal(ExpiryBand.DoesNotLapse, service.BandOf(capability));
    }

    [Fact]
    public async Task A_capability_with_a_date_is_a_certification()
    {
        var (service, _) = Build();

        var capability = await service.RecordAsync(
            fixture.Scope(),
            Record(Uuid7.NewUuid7(), "Fire warden", Today().AddYears(1)),
            default);

        // The date is the discriminator — there is no `kind` to set, and so no
        // way to record an ability that carries an expiry.
        Assert.True(capability.Lapses);
        Assert.Equal(ExpiryBand.Valid, service.BandOf(capability));
    }

    [Theory]
    [InlineData(-1, ExpiryBand.Expired)]
    [InlineData(0, ExpiryBand.Within7Days)]
    [InlineData(7, ExpiryBand.Within7Days)]
    [InlineData(8, ExpiryBand.Within30Days)]
    [InlineData(30, ExpiryBand.Within30Days)]
    [InlineData(31, ExpiryBand.Within60Days)]
    [InlineData(60, ExpiryBand.Within60Days)]
    [InlineData(61, ExpiryBand.Valid)]
    public void The_band_is_computed_from_the_day_it_is_asked_about(int days, ExpiryBand expected)
    {
        var capability = new Capability { ValidUntil = Today().AddDays(days) };

        // The boundaries are the ruling's 60 / 30 / 7, and the day of expiry
        // itself is still "within 7" rather than expired: a certificate valid
        // *until* the 12th is valid on the 12th.
        Assert.Equal(expected, capability.BandOn(Today()));
    }

    [Fact]
    public async Task Recording_the_same_capability_twice_is_refused()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        await service.RecordAsync(scope, Record(staff, "Food handling", Today()), default);

        // Two rows would be two expiry dates for one fact, and the register
        // would show the person as both current and lapsed.
        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.RecordAsync(
                scope, Record(staff, "Food handling", Today().AddYears(1)), default));

        Assert.Contains("renew", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Renewing_is_amending_the_date_on_the_row_that_exists()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        var capability = await service.RecordAsync(
            scope,
            Record(Uuid7.NewUuid7(), "Pool lifeguard", Today().AddDays(3)),
            default);

        Assert.Equal(ExpiryBand.Within7Days, service.BandOf(capability));

        var renewed = await service.AmendAsync(
            scope,
            new AmendCapabilityCommand
            {
                Id = capability.Id,
                ExpectedVersion = capability.Version,
                ValidUntil = Optional<DateOnly?>.Of(Today().AddYears(2)),
            },
            default);

        Assert.Equal(ExpiryBand.Valid, service.BandOf(renewed));
        Assert.Equal(2, renewed.Version);
    }

    [Fact]
    public async Task Clearing_the_date_turns_a_certification_back_into_an_ability()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();

        var capability = await service.RecordAsync(
            scope, Record(Uuid7.NewUuid7(), "Forklift", Today().AddDays(10)), default);

        var corrected = await service.AmendAsync(
            scope,
            new AmendCapabilityCommand
            {
                Id = capability.Id,
                ExpectedVersion = capability.Version,
                ValidUntil = Optional<DateOnly?>.Of(null),
            },
            default);

        Assert.False(corrected.Lapses);
    }

    [Fact]
    public async Task An_absent_date_on_an_amendment_leaves_the_expiry_alone()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();
        var expiry = Today().AddDays(10);

        var capability = await service.RecordAsync(
            scope, Record(Uuid7.NewUuid7(), "First aid", expiry), default);

        var renamed = await service.AmendAsync(
            scope,
            new AmendCapabilityCommand
            {
                Id = capability.Id,
                ExpectedVersion = capability.Version,
                Note = "Cert 4471",
            },
            default);

        // Absent and present-with-null are different requests. A nullable date
        // alone could not distinguish "rename it" from "it no longer expires".
        Assert.Equal(expiry, renamed.ValidUntil);
        Assert.Equal("Cert 4471", renamed.Note);
    }

    [Fact]
    public async Task Attention_lists_what_lapses_within_sixty_days_and_what_already_has()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        await service.RecordAsync(scope, Record(staff, "Lapsed card", Today().AddDays(-2)), default);
        await service.RecordAsync(scope, Record(staff, "Soon card", Today().AddDays(5)), default);
        await service.RecordAsync(scope, Record(staff, "Later card", Today().AddDays(200)), default);
        await service.RecordAsync(scope, Record(staff, "Speaks Tamil"), default);

        var attention = await service.AttentionAsync(scope, new AttentionQuery(), default);
        var mine = attention.Where(c => c.StaffId == staff).ToList();

        // The expired one is on the list: a certificate that has lapsed does not
        // stop needing attention because the date has passed — it needs it more.
        Assert.Equal(["Lapsed card", "Soon card"], mine.Select(c => c.Name));
    }

    [Fact]
    public async Task Attention_for_a_department_resolves_through_postings()
    {
        var (service, postings) = Build();
        var scope = fixture.Scope();
        var posted = Uuid7.NewUuid7();
        var unposted = Uuid7.NewUuid7();

        await postings.CreateAsync(
            scope,
            new CreatePostingCommand
            {
                StaffId = posted,
                DepartmentCode = "SPA",
                JobRole = "Therapist",
                EffectiveFrom = Today().AddDays(-30),
            },
            default);

        await service.RecordAsync(scope, Record(posted, "Spa card", Today().AddDays(5)), default);
        await service.RecordAsync(scope, Record(unposted, "Other card", Today().AddDays(5)), default);

        var spa = await service.AttentionAsync(
            scope, new AttentionQuery { DepartmentCode = "spa" }, default);

        // ADR 0116 §6: department membership derives from postings only, which is
        // why this application can answer "whose people are these" at all — and
        // the code is normalised on the way in, as everywhere else.
        Assert.Equal([posted], spa.Select(c => c.StaffId));
    }

    [Fact]
    public async Task The_register_carries_dated_capabilities_only()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        await service.RecordAsync(scope, Record(staff, "Boiler ticket", Today().AddDays(90)), default);
        await service.RecordAsync(scope, Record(staff, "Speaks Hindi"), default);

        var register = await service.RegisterAsync(scope, default);
        var mine = register.Where(c => c.StaffId == staff).ToList();

        // An inspector came to see certificates. Putting "speaks Hindi" on the
        // register would bury the rows they came for.
        Assert.Equal(["Boiler ticket"], mine.Select(c => c.Name));
    }

    [Fact]
    public async Task Nothing_here_blocks_anything()
    {
        var (service, postings) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        await service.RecordAsync(scope, Record(staff, "Expired warden", Today().AddDays(-30)), default);

        // WF-Q16: an expired certificate is a judgment, not an impossibility.
        // The person can physically work the shift, so the posting succeeds and
        // the expiry is reported rather than enforced.
        var posting = await postings.CreateAsync(
            scope,
            new CreatePostingCommand
            {
                StaffId = staff,
                DepartmentCode = "SEC",
                JobRole = "Security officer",
                EffectiveFrom = Today(),
            },
            default);

        Assert.NotEqual(Guid.Empty, posting.Id);
    }

    [Fact]
    public async Task Removing_takes_the_row_and_reading_it_again_is_not_found()
    {
        var (service, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        var capability = await service.RecordAsync(
            scope, Record(staff, "Recorded in error"), default);

        await service.RemoveAsync(
            scope,
            new RemoveCapabilityCommand
            {
                Id = capability.Id,
                ExpectedVersion = capability.Version,
            },
            default);

        var remaining = await service.ListAsync(
            scope, new ListCapabilitiesQuery { StaffId = staff }, default);

        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Writing_asks_for_capability_manage_and_reading_for_workforce_read()
    {
        var (service, _) = Build(out var authorizer);

        await service.RecordAsync(fixture.Scope(), Record(Uuid7.NewUuid7(), "Anything"), default);
        await service.RegisterAsync(fixture.Scope(), default);

        Assert.Equal(
            ["capability.manage", "workforce.read"],
            authorizer.Checks.Select(check => check.Permission));
    }

    [Fact]
    public async Task A_capability_at_another_property_is_not_found()
    {
        var (service, _) = Build();

        var capability = await service.RecordAsync(
            fixture.Scope(), Record(Uuid7.NewUuid7(), "Elsewhere"), default);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.AmendAsync(
                fixture.OtherPropertyScope(),
                new AmendCapabilityCommand
                {
                    Id = capability.Id,
                    ExpectedVersion = capability.Version,
                    Note = "reached across the boundary",
                },
                default));
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static RecordCapabilityCommand Record(
        Guid staff, string name, DateOnly? validUntil = null) =>
        new() { StaffId = staff, Name = name, ValidUntil = validUntil };

    private (CapabilityService Capabilities, PostingService Postings) Build() =>
        Build(out _);

    private (CapabilityService Capabilities, PostingService Postings) Build(
        out RecordingAuthorizer authorizer)
    {
        authorizer = new RecordingAuthorizer();
        var db = fixture.Context();

        return (
            new CapabilityService(db, authorizer, TimeProvider.System),
            new PostingService(
                db, authorizer, new StaffDirectoryDouble(),
                new PostingAnnouncer(new RecordingEventAppender(), new StaffDirectoryDouble()),
                TimeProvider.System));
    }
}
