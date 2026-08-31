using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Postings;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// What <see cref="PostingService"/> does, held still.
/// </summary>
/// <remarks>
/// ADR 0054: a service suite owns <b>behavioural</b> coverage — the rules — and
/// the E2E suite owns the boundary classes. So there is nothing here about the
/// listener, the certificate or the Kernel's real decision; there is everything
/// about what a posting is allowed to be.
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class PostingCharacterisationTests(WorkforceFixture fixture)
{
    private static readonly DateOnly September = new(2026, 9, 1);

    [Fact]
    public async Task Create_records_the_posting_and_asks_for_posting_manage()
    {
        var (service, authorizer, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        var posting = await service.CreateAsync(scope, Command(staff), default);

        Assert.Equal(scope.PropertyId, posting.PropertyId);
        Assert.Equal(staff, posting.StaffId);
        Assert.Equal("FO", posting.DepartmentCode);
        Assert.Equal(1, posting.Version);
        Assert.Null(posting.EffectiveTo);

        Assert.Equal(
            ("posting.manage", "property", scope.PropertyId),
            Assert.Single(authorizer.Checks));
    }

    [Fact]
    public async Task Create_normalises_the_department_code()
    {
        var (service, _, directory) = Build();

        var posting = await service.CreateAsync(
            fixture.Scope(), Command(Uuid7.NewUuid7(), department: "  fo  "), default);

        // The canon code is the identity — ADR 0119 — so it is stored in one
        // form. Two postings written `FO` and `fo` would be two departments to
        // every report that groups on the code.
        Assert.Equal("FO", posting.DepartmentCode);
        Assert.Equal("FO", Assert.Single(directory.DepartmentLookups));
    }

    [Fact]
    public async Task Create_refuses_a_department_the_property_has_not_activated()
    {
        var (service, _, directory) = Build();
        directory.Unactivated.Add("CASINO");

        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.CreateAsync(
                fixture.Scope(), Command(Uuid7.NewUuid7(), department: "CASINO"), default));

        Assert.Contains("CASINO", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_refuses_an_overlapping_open_posting_in_the_same_department()
    {
        var (service, _, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        await service.CreateAsync(scope, Command(staff), default);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.CreateAsync(scope, Command(staff), default));
    }

    [Fact]
    public async Task Create_allows_the_same_posting_again_after_the_first_has_ended()
    {
        var (service, _, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        var first = await service.CreateAsync(scope, Command(staff), default);
        await service.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = first.Id,
                ExpectedVersion = first.Version,
                EffectiveTo = September.AddMonths(3),
            },
            default);

        // Re-hiring somebody into the department they used to work in. The window
        // is what distinguishes the two postings, which is why uniqueness is not
        // an index on (property, staff, department).
        var second = await service.CreateAsync(
            scope, Command(staff, from: September.AddMonths(6)), default);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Create_refuses_a_blank_job_role()
    {
        var (service, _, _) = Build();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.CreateAsync(
                fixture.Scope(), Command(Uuid7.NewUuid7(), role: "   "), default));
    }

    [Fact]
    public async Task Create_resolves_the_identity_link_and_tolerates_its_absence()
    {
        var (service, _, directory) = Build();
        var staff = Uuid7.NewUuid7();

        await service.CreateAsync(fixture.Scope(), Command(staff), default);

        // Most staff have no login, and a posting for such a person is a
        // complete posting. What must hold is that the question was asked — it
        // is what the announcement will depend on when AUTHZ-Q20's contract
        // lands.
        Assert.Equal(staff, Assert.Single(directory.IdentityLookups));
    }

    [Fact]
    public async Task End_closes_the_window_and_keeps_the_row()
    {
        var (service, _, _) = Build();
        var scope = fixture.Scope();

        var posting = await service.CreateAsync(scope, Command(Uuid7.NewUuid7()), default);
        var ended = await service.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = posting.Version,
                EffectiveTo = September.AddMonths(2),
            },
            default);

        Assert.Equal(September.AddMonths(2), ended.EffectiveTo);
        Assert.Equal(2, ended.Version);

        // The row survives: a rota worked under this posting was worked under it,
        // and deleting it to revoke access would take the history with it.
        await using var db = fixture.Context();
        Assert.NotNull(await db.Postings.FindAsync(posting.Id));
    }

    [Fact]
    public async Task End_refuses_a_date_before_the_posting_started()
    {
        var (service, _, _) = Build();
        var scope = fixture.Scope();

        var posting = await service.CreateAsync(scope, Command(Uuid7.NewUuid7()), default);

        // WF-Q16: a window that ends before it starts cannot be true, so it is
        // refused rather than warned.
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.EndAsync(
                scope,
                new EndPostingCommand
                {
                    Id = posting.Id,
                    ExpectedVersion = posting.Version,
                    EffectiveTo = September.AddDays(-1),
                },
                default));
    }

    [Fact]
    public async Task Update_refuses_a_stale_version()
    {
        var (service, _, _) = Build();
        var scope = fixture.Scope();

        var posting = await service.CreateAsync(scope, Command(Uuid7.NewUuid7()), default);

        await Assert.ThrowsAsync<ConcurrencyException>(
            () => service.UpdateAsync(
                scope,
                new UpdatePostingCommand
                {
                    Id = posting.Id,
                    ExpectedVersion = posting.Version + 7,
                    JobRole = "Duty Manager",
                },
                default));
    }

    [Fact]
    public async Task Update_distinguishes_clearing_the_zone_from_leaving_it_alone()
    {
        var (service, _, _) = Build();
        var scope = fixture.Scope();
        var zone = Uuid7.NewUuid7();

        var posting = await service.CreateAsync(
            scope, Command(Uuid7.NewUuid7()) with { ZoneId = zone }, default);

        // Absent: the zone survives an unrelated edit.
        var renamed = await service.UpdateAsync(
            scope,
            new UpdatePostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = posting.Version,
                JobRole = "Duty Manager",
            },
            default);

        Assert.Equal(zone, renamed.ZoneId);

        // Present and null: the zone is removed. A nullable field alone could not
        // express the difference, which is why the command carries `Optional<T>`.
        var cleared = await service.UpdateAsync(
            scope,
            new UpdatePostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = renamed.Version,
                ZoneId = Optional<Guid?>.Of(null),
            },
            default);

        Assert.Null(cleared.ZoneId);
    }

    [Fact]
    public async Task A_posting_at_another_property_is_not_found_rather_than_denied()
    {
        var (service, _, _) = Build();

        var posting = await service.CreateAsync(
            fixture.Scope(), Command(Uuid7.NewUuid7()), default);

        // NotFound, not PermissionDenied: a cross-property read must not confirm
        // that the id exists.
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetAsync(fixture.OtherPropertyScope(), posting.Id, default));
    }

    [Fact]
    public async Task List_excludes_ended_postings_unless_asked()
    {
        var (service, _, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        var posting = await service.CreateAsync(scope, Command(staff), default);
        await service.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = posting.Version,
                EffectiveTo = new DateOnly(2026, 9, 2),
            },
            default);

        var current = await service.ListAsync(
            scope, new ListPostingsQuery { StaffId = staff }, default);
        var all = await service.ListAsync(
            scope, new ListPostingsQuery { StaffId = staff, IncludeEnded = true }, default);

        Assert.Empty(current);
        Assert.Single(all);
    }

    [Fact]
    public async Task Reading_asks_for_workforce_read_rather_than_posting_manage()
    {
        var (service, authorizer, _) = Build();

        await service.ListAsync(fixture.Scope(), new ListPostingsQuery(), default);

        Assert.Equal("workforce.read", Assert.Single(authorizer.Checks).Permission);
    }

    [Fact]
    public async Task A_refused_permission_stops_the_write()
    {
        var (service, authorizer, _) = Build();
        authorizer.Deny.Add("posting.manage");

        await Assert.ThrowsAsync<PermissionDeniedException>(
            () => service.CreateAsync(fixture.Scope(), Command(Uuid7.NewUuid7()), default));

        // And nothing was written. The authorization is at the top of the method
        // for exactly this reason.
        await using var db = fixture.Context();
        Assert.Empty(await db.Postings.Where(p => p.DepartmentCode == "FO").ToListAsync());
    }

    private static CreatePostingCommand Command(
        Guid staff, string department = "FO", string role = "Receptionist", DateOnly? from = null) =>
        new()
        {
            StaffId = staff,
            DepartmentCode = department,
            JobRole = role,
            EffectiveFrom = from ?? September,
        };

    private (PostingService Service, RecordingAuthorizer Authorizer, StaffDirectoryDouble Directory)
        Build()
    {
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();

        return (
            new PostingService(
                fixture.Context(), authorizer, directory, TimeProvider.System),
            authorizer,
            directory);
    }
}
