using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The one line naming who is signed in, and every way it can be unknown.
/// </summary>
/// <remarks>
/// <para>
/// The module used to draw a constant here — a name, a department, a role and a
/// hotel, written into <c>application.ts</c> and rendered on every screen. These
/// tests exist because the replacement has four paths and three of them answer
/// with nulls: without a test per path, the easiest repair for a blank bar is a
/// default, and the default is the defect coming back.
/// </para>
/// <para>
/// <b>Each unknown is asserted as null, not as a word.</b> "Unknown", "—" or
/// "Not posted" would each be this application stating something about a person
/// it could not find; the screen decides how to render an absence, and it can
/// only do that if the absence reaches it.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public class MeCharacterisationTests(WorkforceFixture fixture)
{
    private static readonly DateOnly September = new(2026, 9, 1);

    [Fact]
    public async Task Me_names_the_signed_in_person_their_posting_and_the_property()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        harness.Directory.PropertyName = "Kochi Beach Resort";
        harness.Directory.WithDepartmentName("FO", "Front Office");

        await Sign(harness, scope, "Anjali Menon", "FO", "Receptionist");

        var me = await harness.CallAsync(MeView.Read, scope, "me");

        Assert.Equal("Anjali Menon", me.GetProperty("name").GetString());
        Assert.Equal("Front Office", me.GetProperty("department").GetString());
        Assert.Equal("Receptionist", me.GetProperty("role").GetString());
        Assert.Equal("Kochi Beach Resort", me.GetProperty("property").GetString());
    }

    [Fact]
    public async Task Me_is_reachable_through_the_read_dispatch()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        harness.Directory.PropertyName = "Kochi Beach Resort";

        // Through `ReadViews.Answer` rather than `MeView.Read` directly, because
        // the bundle reaches this by name over `roster.read` — and a view nobody
        // mapped is the 404 the module would draw as a failure screen.
        var me = await harness.CallAsync(ReadViews.Answer, scope, "me");

        Assert.Equal("Kochi Beach Resort", me.GetProperty("property").GetString());
    }

    [Fact]
    public async Task A_service_caller_is_nobody_and_the_property_is_still_named()
    {
        var harness = new ModuleHarness(fixture);
        var scope = new RequestScope
        {
            Caller = CallerKind.Service,
            PropertyId = Guid.CreateVersion7(),
            UserId = null,
        };
        harness.Directory.PropertyName = "Kochi Beach Resort";

        var me = await harness.CallAsync(MeView.Read, scope, "me");

        // Nobody is signed in, and that is not an error — a sweep or an event
        // consumer legitimately has no person behind it.
        Assert.Equal(JsonValueKind.Null, me.GetProperty("name").ValueKind);
        Assert.Equal(JsonValueKind.Null, me.GetProperty("department").ValueKind);
        Assert.Equal(JsonValueKind.Null, me.GetProperty("role").ValueKind);

        // The property is a fact about the request, not about the caller.
        Assert.Equal("Kochi Beach Resort", me.GetProperty("property").GetString());
    }

    [Fact]
    public async Task A_login_with_no_staff_record_is_named_by_nobody()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        harness.Directory.PropertyName = "Kochi Beach Resort";

        // No `WithLogin`, so Master Data holds no staff row for this user — the
        // founding administrator's own condition, and anyone not yet linked.
        var me = await harness.CallAsync(MeView.Read, scope, "me");

        Assert.Equal(JsonValueKind.Null, me.GetProperty("name").ValueKind);
        Assert.Equal(JsonValueKind.Null, me.GetProperty("role").ValueKind);
        Assert.Equal("Kochi Beach Resort", me.GetProperty("property").GetString());
    }

    [Fact]
    public async Task A_person_with_no_posting_is_named_with_nowhere_to_be()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, "Anjali Menon");
        harness.Directory.WithLogin(staff, scope.UserId!.Value);

        var me = await harness.CallAsync(MeView.Read, scope, "me");

        // Known to Master Data, posted nowhere by Workforce. The two halves are
        // separately answerable, so the name survives the missing posting.
        Assert.Equal("Anjali Menon", me.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, me.GetProperty("department").ValueKind);
        Assert.Equal(JsonValueKind.Null, me.GetProperty("role").ValueKind);
    }

    [Fact]
    public async Task An_unnamed_property_is_absent_rather_than_filled_in()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        harness.Directory.PropertyName = null;

        var me = await harness.CallAsync(MeView.Read, scope, "me");

        // Master Data holds no name for it. The bar draws a blank; it does not
        // draw "Unknown", and it does not draw the property's id.
        Assert.Equal(JsonValueKind.Null, me.GetProperty("property").ValueKind);
    }

    [Fact]
    public async Task A_department_Master_Data_has_not_named_falls_back_to_its_code()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();

        // No `WithDepartmentName`. The code is not a guess — it is what this
        // application actually holds, and `FO` is what the posting says.
        await Sign(harness, scope, "Anjali Menon", "FO", "Receptionist");

        var me = await harness.CallAsync(MeView.Read, scope, "me");

        Assert.Equal("FO", me.GetProperty("department").GetString());
    }

    [Fact]
    public async Task The_primary_posting_is_the_one_drawn()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var staff = await Sign(harness, scope, "Anjali Menon", "FO", "Receptionist");
        harness.Directory.WithDepartmentName("HK", "Housekeeping");

        await harness.Service<PostingService>().CreateAsync(scope, new CreatePostingCommand
        {
            StaffId = staff,
            DepartmentCode = "HK",
            JobRole = "Floor Supervisor",
            IsPrimary = true,
            EffectiveFrom = September,
        }, default);

        var me = await harness.CallAsync(MeView.Read, scope, "me");

        // Two open postings, and the bar shows one. `FirstOrDefault` over an
        // ordering rather than `Single`: a person holding two is ordinary, and
        // refusing to draw would be worse than drawing the main one.
        Assert.Equal("Housekeeping", me.GetProperty("department").GetString());
        Assert.Equal("Floor Supervisor", me.GetProperty("role").GetString());
    }

    [Fact]
    public async Task An_ended_posting_is_not_where_this_person_is_now()
    {
        var harness = new ModuleHarness(fixture);
        var scope = ModuleHarness.Property();
        var staff = await Sign(harness, scope, "Anjali Menon", "FO", "Receptionist");

        var postings = harness.Service<PostingService>();
        var held = await postings.ListAsync(
            scope, new ListPostingsQuery { StaffId = staff }, default);

        await postings.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = held[0].Id,
                ExpectedVersion = held[0].Version,
                EffectiveTo = September,
            },
            default);

        var me = await harness.CallAsync(MeView.Read, scope, "me");

        // The name is still theirs; the posting is in the past. Drawing a role
        // somebody no longer holds is the same class of claim as drawing a name
        // that was never theirs.
        Assert.Equal("Anjali Menon", me.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, me.GetProperty("role").ValueKind);
    }

    /// <summary>A staff member, signed in as this scope's user, and posted.</summary>
    private static async Task<Guid> Sign(
        ModuleHarness harness, RequestScope scope, string name, string department, string role)
    {
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, name);
        harness.Directory.WithLogin(staff, scope.UserId!.Value);

        await harness.Service<PostingService>().CreateAsync(scope, new CreatePostingCommand
        {
            StaffId = staff,
            DepartmentCode = department,
            JobRole = role,
            EffectiveFrom = September,
        }, default);

        return staff;
    }
}
