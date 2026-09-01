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
        var (service, authorizer, _, _) = Build();
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
        var (service, _, directory, _) = Build();

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
        var (service, _, directory, _) = Build();
        directory.Unactivated.Add("CASINO");

        var refusal = await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.CreateAsync(
                fixture.Scope(), Command(Uuid7.NewUuid7(), department: "CASINO"), default));

        Assert.Contains("CASINO", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_refuses_an_overlapping_open_posting_in_the_same_department()
    {
        var (service, _, _, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        await service.CreateAsync(scope, Command(staff), default);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.CreateAsync(scope, Command(staff), default));
    }

    [Fact]
    public async Task Create_allows_the_same_posting_again_after_the_first_has_ended()
    {
        var (service, _, _, _) = Build();
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
        var (service, _, _, _) = Build();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.CreateAsync(
                fixture.Scope(), Command(Uuid7.NewUuid7(), role: "   "), default));
    }

    [Fact]
    public async Task Create_resolves_the_identity_link_and_tolerates_its_absence()
    {
        var (service, _, directory, _) = Build();
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
        var (service, _, _, _) = Build();
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
        var (service, _, _, _) = Build();
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
        var (service, _, _, _) = Build();
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
        var (service, _, _, _) = Build();
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
        var (service, _, _, _) = Build();

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
        var (service, _, _, _) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();

        // Started and ended in the past. An end date in the *future* leaves the
        // posting in force until it arrives — which is the service being right
        // and was this test being wrong: "ended" means the window has closed,
        // not that somebody has typed a closing date.
        var posting = await service.CreateAsync(
            scope, Command(staff, from: new DateOnly(2026, 1, 1)), default);
        await service.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = posting.Version,
                EffectiveTo = new DateOnly(2026, 6, 30),
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
        var (service, authorizer, _, _) = Build();

        await service.ListAsync(fixture.Scope(), new ListPostingsQuery(), default);

        Assert.Equal("workforce.read", Assert.Single(authorizer.Checks).Permission);
    }

    [Fact]
    public async Task A_refused_permission_stops_the_write()
    {
        var (service, authorizer, _, _) = Build();
        var staff = Uuid7.NewUuid7();
        authorizer.Deny.Add("posting.manage");

        await Assert.ThrowsAsync<PermissionDeniedException>(
            () => service.CreateAsync(fixture.Scope(), Command(staff), default));

        // And nothing was written. The authorization is at the top of the method
        // for exactly this reason.
        //
        // Scoped to this test's own staff id, not to the department: the fixture
        // is shared, so every sibling test's Front Office posting is in this
        // table too. An assertion that reads other tests' rows is an assertion
        // that fails when somebody adds a test.
        await using var db = fixture.Context();
        Assert.Empty(await db.Postings.Where(p => p.StaffId == staff).ToListAsync());
    }

    /// <summary>A department code no other test in this suite or its neighbours is using.</summary>
    /// <remarks>
    /// A department has <b>one</b> current head, so any test that makes one needs
    /// a department of its own — the same isolation the swap suite needed, and
    /// for the same reason. Found twice now by the invariant refusing a second
    /// head that another suite had already created in ENG and SPA, which is the
    /// rule working rather than the harness failing.
    /// </remarks>
    private static string OwnDepartment() => $"HD{Interlocked.Increment(ref headSlot)}";

    private static int headSlot = -1;

    private static CreatePostingCommand Command(
        Guid staff, string department = "FO", string role = "Receptionist", DateOnly? from = null) =>
        new()
        {
            StaffId = staff,
            DepartmentCode = department,
            JobRole = role,
            EffectiveFrom = from ?? September,
        };

    [Fact]
    public async Task Posting_somebody_with_a_login_announces_user_posted()
    {
        var (service, _, directory, events) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();
        directory.WithLogin(staff, Uuid7.NewUuid7());

        var posting = await service.CreateAsync(scope, Command(staff, "FO"), default);

        var announced = Assert.Single(events.Events);

        // The ratified AUTHZ-Q20 contract: domain `user`, aggregate `posting`.
        // The fact is about a person; the record that establishes it is a
        // posting, and it is the only row that can lend a version.
        Assert.Equal("user.posted", announced.EventType);
        Assert.Equal("posting", announced.AggregateType);
        Assert.Equal(posting.Id, announced.AggregateId);
        Assert.Equal(posting.Version, announced.EntityVersion);
    }

    [Fact]
    public async Task The_payload_carries_both_department_identifiers()
    {
        var (service, _, directory, events) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();
        var user = Uuid7.NewUuid7();
        directory.WithLogin(staff, user);

        await service.CreateAsync(scope, Command(staff, "HK"), default);

        var payload = Assert.IsType<PostingAnnouncement>(events.Events.Single().Payload);

        // The id is what department:{uuid} addresses; the code is what the fact
        // means and what survives a database being rebuilt. Only the id makes it
        // unreadable to a human debugging it; only the code makes it unusable to
        // the consumer that must write a tuple.
        Assert.Equal(user, payload.UserId);
        Assert.Equal("HK", payload.DepartmentCode);
        Assert.NotEqual(Guid.Empty, payload.DepartmentId);
        Assert.Equal(scope.PropertyId, payload.PropertyId);
    }

    [Fact]
    public async Task A_posting_for_somebody_with_no_login_announces_nothing()
    {
        var (service, _, _, events) = Build();

        await service.CreateAsync(fixture.Scope(), Command(Uuid7.NewUuid7(), "FO"), default);

        // Most of the workforce has no account. The posting is complete and
        // correct, and there is no principal for a tuple to name — writing one
        // would be inventing an account.
        Assert.Empty(events.Events);
    }

    [Fact]
    public async Task Posting_a_department_head_announces_both_facts()
    {
        var (service, _, directory, events) = Build();
        var staff = Uuid7.NewUuid7();
        directory.WithLogin(staff, Uuid7.NewUuid7());

        await service.CreateAsync(
            fixture.Scope(), Command(staff, OwnDepartment()) with { IsDepartmentHead = true }, default);

        // Two events from one operation. Headship is its own grant kind, so it is
        // its own announcement — folding it into user.posted would have widened
        // every kind in the Kernel's table to serve one.
        Assert.Equal(["user.posted", "user.headship_started"], events.Types);
    }

    [Fact]
    public async Task Ending_a_head_posting_withdraws_both()
    {
        var (service, _, directory, events) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();
        directory.WithLogin(staff, Uuid7.NewUuid7());

        var posting = await service.CreateAsync(
            scope, Command(staff, OwnDepartment()) with { IsDepartmentHead = true }, default);

        await service.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = posting.Version,
                EffectiveTo = posting.EffectiveFrom.AddDays(30),
            },
            default);

        // posting_ended withdraws #posted, headship_ended withdraws #manager.
        // Two relations, two tuples, two announcements — and both directions land
        // together or neither does.
        Assert.Equal(
            ["user.posted", "user.headship_started", "user.posting_ended", "user.headship_ended"],
            events.Types);
    }

    [Fact]
    public async Task Granting_headship_on_an_amendment_announces_it_alone()
    {
        var (service, _, directory, events) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();
        directory.WithLogin(staff, Uuid7.NewUuid7());

        var posting = await service.CreateAsync(scope, Command(staff, OwnDepartment()), default);

        var promoted = await service.UpdateAsync(
            scope,
            new UpdatePostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = posting.Version,
                IsDepartmentHead = true,
            },
            default);

        // The third trigger. Headship changes without the posting starting or
        // finishing, which is exactly why it is a second grant kind — and why
        // UpdateAsync, which announced nothing until the contract landed,
        // announces now.
        Assert.Equal(["user.posted", "user.headship_started"], events.Types);

        await service.UpdateAsync(
            scope,
            new UpdatePostingCommand
            {
                Id = promoted.Id,
                ExpectedVersion = promoted.Version,
                IsDepartmentHead = false,
            },
            default);

        // And the fourth.
        Assert.Equal("user.headship_ended", events.Types[^1]);
    }

    [Fact]
    public async Task Amending_without_changing_headship_announces_nothing()
    {
        var (service, _, directory, events) = Build();
        var scope = fixture.Scope();
        var staff = Uuid7.NewUuid7();
        directory.WithLogin(staff, Uuid7.NewUuid7());

        var posting = await service.CreateAsync(scope, Command(staff, "FO"), default);

        await service.UpdateAsync(
            scope,
            new UpdatePostingCommand
            {
                Id = posting.Id,
                ExpectedVersion = posting.Version,
                JobRole = "Senior Receptionist",
                IsDepartmentHead = false,
            },
            default);

        // Re-stating a flag at the value it already holds is not a change, and a
        // headship_ended for somebody who was never head would delete a tuple
        // that never existed — noise the Kernel would have to be tolerant of.
        Assert.Equal(["user.posted"], events.Types);
    }

    private (PostingService Service, RecordingAuthorizer Authorizer, StaffDirectoryDouble Directory,
        RecordingEventAppender Events) Build()
    {
        var authorizer = new RecordingAuthorizer();
        var directory = new StaffDirectoryDouble();

        var events = new RecordingEventAppender();

        return (
            new PostingService(
                fixture.Context(), authorizer, directory, events, TimeProvider.System),
            authorizer,
            directory,
            events);
    }
}
