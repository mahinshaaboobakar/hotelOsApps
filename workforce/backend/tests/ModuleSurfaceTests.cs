using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Module.Views;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// Every capability and method this application serves to its own UI, executed.
/// </summary>
/// <remarks>
/// <para>
/// This is the wired ledger's application half. Each test is one row: a read
/// that answers with the shape a screen destructures, or a write that changes
/// the database and can be seen to have changed it on the next read.
/// </para>
/// <para>
/// <b>Every assertion is against the JSON, not the object.</b> What reaches a
/// bundle is text — a field named in the wrong case, or one whose type
/// serialises to nothing, is invisible to an assertion on the anonymous object
/// and plain against the parsed result.
/// </para>
/// <para>
/// <b>What is deliberately absent.</b> Nothing here touches the envelope: the
/// token, the capability guard and the status mapping are the platform's, and
/// they cannot execute in any application today (see <see cref="ModuleHarness"/>).
/// A test that stood those up itself would be testing a second implementation
/// of them.
/// </para>
/// <para>
/// <b>This part holds the arrangement and no test.</b> The rows live in
/// <c>.Reads.cs</c> and <c>.Writes.cs</c>, one file per subject, and the five
/// helpers below are what both of them build on — ADR 0042's composition root,
/// and its rule that a helper used by more than one topic belongs to the shared
/// surface rather than to whichever subject happened to declare it first.
/// </para>
/// </remarks>
[Collection(WorkforceCollection.Name)]
public partial class ModuleSurfaceTests(WorkforceFixture fixture)
{
    private static readonly DateOnly September = new(2026, 9, 1);

    private static IEnumerable<string?> Names(JsonElement page)
        => page.GetProperty("postings").EnumerateArray()
            .Select(one => one.GetProperty("who").GetString());

    private async Task<Guid> Post(
        ModuleHarness harness, RequestScope scope, string name, string department, string role)
    {
        var staff = Guid.CreateVersion7();
        harness.Directory.WithName(staff, name);

        await harness.CallAsync(PeopleView.Write, scope, "post", new
        {
            staffId = staff,
            department,
            role,
            from = September.ToString("yyyy-MM-dd"),
        });

        return staff;
    }

    private async Task<Guid> Form(
        ModuleHarness harness, RequestScope scope, string department, string name)
    {
        var formed = await harness.CallAsync(
            TeamsView.Write, scope, "form", new { department, name });

        return formed.GetProperty("id").GetGuid();
    }

    private async Task<Guid> DefineShift(
        ModuleHarness harness, RequestScope scope, string name, string code)
    {
        var shift = await harness.CallAsync(PolicyView.Write, scope, "defineShift", new
        {
            name,
            code,
            colour = "Cyan",
            startsAt = "07:00",
            endsAt = "15:00",
            from = new DateOnly(2026, 1, 1).ToString("yyyy-MM-dd"),
        });

        return shift.GetProperty("id").GetGuid();
    }

    private async Task<Posting> Posting(
        ModuleHarness harness, RequestScope scope, Guid staffId)
    {
        var service = harness.Service<PostingService>();

        var held = await service.ListAsync(
            scope, new ListPostingsQuery { StaffId = staffId }, default);

        return held[0];
    }
}
