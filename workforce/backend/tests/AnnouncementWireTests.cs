using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Workforce.Application.Postings;
using Xunit;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// The wire shape the Kernel actually reads.
/// </summary>
/// <remarks>
/// <para>
/// <b>Found before the pair check ran, not by it.</b> <c>EventAppender</c> calls
/// <c>JsonSerializer.SerializeToDocument</c> with no options, so a property named
/// <c>UserId</c> reaches the event store as <c>"UserId"</c> — while the Kernel
/// reads <c>uuid(body, "user_id")</c> at <c>plan.rs:248</c> and
/// <c>plan.rs:256</c>.
/// </para>
/// <para>
/// The failure has no symptom: the event is stored, relayed and acknowledged,
/// and no tuple is written. <b>It is CC's §2 defect arriving through a different
/// door</b> — that one was the subscription filter, this one is the body — and
/// the two together are why an announcement contract needs a test on the bytes
/// rather than on the type.
/// </para>
/// </remarks>
public class AnnouncementWireTests
{
    [Fact]
    public void The_payload_carries_the_names_the_kernel_reads()
    {
        var json = JsonSerializer.SerializeToDocument(Announcement()).RootElement;

        // Exactly the two the Kernel resolves a tuple from. Spelled out here
        // rather than referenced from a constant, because a test asserting on a
        // constant the code under test also uses is a tautology.
        Assert.Equal(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            json.GetProperty("user_id").GetGuid());

        Assert.Equal(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            json.GetProperty("department_id").GetGuid());
    }

    [Fact]
    public void Every_field_is_snake_case_so_none_can_drift_back()
    {
        var json = JsonSerializer.SerializeToDocument(Announcement()).RootElement;

        var names = json.EnumerateObject().Select(p => p.Name).ToList();

        // Derived from the object rather than listed, so a field added later is
        // covered the day it is written — and a PascalCase one fails here rather
        // than silently in a consumer.
        Assert.All(names, name => Assert.DoesNotContain(name, char.IsUpper));

        Assert.Equal(
            ["user_id", "staff_id", "department_id", "department_code",
             "posting_id", "property_id", "occurred_at"],
            names);
    }

    private static PostingAnnouncement Announcement() => new()
    {
        UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        StaffId = Guid.CreateVersion7(),
        DepartmentId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        DepartmentCode = "FO",
        PostingId = Guid.CreateVersion7(),
        PropertyId = Guid.CreateVersion7(),
        OccurredAt = DateTimeOffset.UtcNow,
    };
}
