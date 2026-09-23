using System.Text.Json;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// Giving a stay a room — gold frame 3's <i>Move room</i>, and the day list's
/// <c>＋ assign</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The reason is derived and the request has nowhere to put one.</b> A stay
/// with no room is being given its first; one that has a room is being moved.
/// A client able to send a reason could record a move as a first assignment,
/// and the history is what a property is asked about.
/// </para>
/// <para>
/// <b>A conflict is part of the answer.</b> GUEST-Q5 made a double-booked room
/// a possible truth, so the service warns and never forbids — and this command
/// turns that warning into something a screen can act on rather than an error
/// that would reach the desk as <i>nothing was changed</i>.
/// </para>
/// </remarks>
public sealed class AssignCommandTests
{
    private static readonly Guid Spare = Guid.Parse("bbbb0000-0000-4000-8000-000000000305");
    private static readonly Guid Taken = Guid.Parse("bbbb0000-0000-4000-8000-000000000306");

    private static readonly DateOnly Arrive = new(2026, 8, 31);
    private static readonly DateOnly Depart = new(2026, 9, 1);

    [Fact]
    public async Task A_stay_with_no_room_records_its_first_assignment()
    {
        await using var harness = await Ready();
        var stay = await Waiting(harness);

        var answer = await Assign(harness, stay.Id, Spare, stay.Version);

        Assert.True(Read<bool>(answer, "assigned"));
        Assert.False(Read<bool>(answer, "moved"));
        Assert.Equal(AssignmentReason.Initial, await ReasonOf(harness, stay.Id));
    }

    /// <summary>
    /// A stay that already has a room records a move, without being told to.
    /// </summary>
    /// <remarks>
    /// The same call, the same body, a different record — because the service
    /// read which room the stay had rather than believing a client.
    /// </remarks>
    [Fact]
    public async Task A_stay_that_has_a_room_records_a_move()
    {
        await using var harness = await Ready();
        var stay = await InRoom(harness, Spare);

        var answer = await Assign(harness, stay.Id, Taken, stay.Version, accept: true);

        Assert.True(Read<bool>(answer, "assigned"));
        Assert.True(Read<bool>(answer, "moved"));
        Assert.Equal(AssignmentReason.Move, await ReasonOf(harness, stay.Id));
    }

    /// <summary>
    /// A held room comes back as a conflict, and nothing is written.
    /// </summary>
    /// <remarks>
    /// <b>Not an exception reaching the screen.</b> A refusal would arrive as
    /// *nothing was changed* with nothing to do about it; this is a question
    /// the desk can answer, which is what GUEST-Q5 requires of it.
    /// </remarks>
    [Fact]
    public async Task A_room_another_stay_holds_comes_back_as_a_conflict()
    {
        await using var harness = await Ready();
        await InRoom(harness, Taken);
        var waiting = await Waiting(harness);

        var answer = await Assign(harness, waiting.Id, Taken, waiting.Version);

        Assert.False(Read<bool>(answer, "assigned"));
        Assert.True(Read<bool>(answer, "conflict"));

        harness.Db.ChangeTracker.Clear();
        var after = await harness.Db.Stays.FirstAsync(s => s.Id == waiting.Id);
        Assert.Null(after.CurrentRoomId);
    }

    /// <summary>
    /// The same call again, with the desk's agreement, is taken.
    /// </summary>
    /// <remarks>
    /// The double-booked room is the ruled outcome, reached deliberately — and
    /// the conflict had to be shown before it could be meant.
    /// </remarks>
    [Fact]
    public async Task The_same_assignment_is_taken_once_the_desk_means_it()
    {
        await using var harness = await Ready();
        await InRoom(harness, Taken);
        var waiting = await Waiting(harness);

        var refused = await Assign(harness, waiting.Id, Taken, waiting.Version);
        Assert.False(Read<bool>(refused, "assigned"));

        var taken = await Assign(harness, waiting.Id, Taken, waiting.Version, accept: true);

        Assert.True(Read<bool>(taken, "assigned"));

        harness.Db.ChangeTracker.Clear();
        var after = await harness.Db.Stays.FirstAsync(s => s.Id == waiting.Id);
        Assert.Equal(Taken, after.CurrentRoomId);
    }

    /// <summary>
    /// A body with no version is refused, rather than failing as a stale one.
    /// </summary>
    /// <remarks>
    /// Zero would reach the concurrency check and come back saying somebody
    /// else had changed the stay — a claim about the world, when what happened
    /// is that the caller never said which stay it read.
    /// </remarks>
    [Fact]
    public async Task A_body_with_no_version_is_refused_by_name()
    {
        await using var harness = await Ready();
        var stay = await Waiting(harness);

        var body = JsonSerializer.SerializeToElement(new
        {
            stayId = stay.Id.ToString(),
            roomId = Spare.ToString(),
        });

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Command(harness).RunAsync(harness.Scope(), body, CancellationToken.None));

        Assert.Contains("version", refused.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<object?> Assign(
        DeskHarness harness, Guid stayId, Guid roomId, long version, bool accept = false)
    {
        var body = accept
            ? JsonSerializer.SerializeToElement(new
            {
                stayId = stayId.ToString(),
                roomId = roomId.ToString(),
                version,
                acceptConflict = true,
            })
            : JsonSerializer.SerializeToElement(new
            {
                stayId = stayId.ToString(),
                roomId = roomId.ToString(),
                version,
            });

        return await Command(harness).RunAsync(harness.Scope(), body, CancellationToken.None);
    }

    private static AssignCommand Command(DeskHarness harness)
        => new(harness.Db, new StayAssignmentService(
            harness.Db,
            harness.Authorizer,
            harness.Events,
            new StubBusinessDay(Arrive),
            harness.Clock));

    /// <summary>What was actually recorded, read back from the open row.</summary>
    private static async Task<AssignmentReason> ReasonOf(DeskHarness harness, Guid stayId)
    {
        harness.Db.ChangeTracker.Clear();

        return await harness.Db.Assignments
            .Where(a => a.StayId == stayId && a.ReleasedAt == null)
            .Select(a => a.Reason)
            .FirstAsync();
    }

    /// <summary>A stay with no room — the one that gets a first assignment.</summary>
    private static async Task<RoomStay> Waiting(DeskHarness harness)
    {
        var stay = await Seed(harness);
        stay.CurrentRoomId = null;
        await harness.Db.SaveChangesAsync();
        return stay;
    }

    /// <summary>A stay already holding a room, over the dates asked about.</summary>
    private static async Task<RoomStay> InRoom(DeskHarness harness, Guid roomId)
    {
        var stay = await Seed(harness);
        stay.CurrentRoomId = roomId;

        harness.Db.Assignments.Add(new Assignment
        {
            Id = Guid.CreateVersion7(),
            StayId = stay.Id,
            RoomId = roomId,
            AssignedAt = harness.Clock.GetUtcNow(),
            AssignedBy = Guid.NewGuid(),
            Reason = AssignmentReason.Initial,
        });

        await harness.Db.SaveChangesAsync();
        return stay;
    }

    private static async Task<RoomStay> Seed(DeskHarness harness)
    {
        var stay = await harness.SeedStayAsync(new DateTimeOffset(
            Arrive.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero));

        stay.DepartureAt = StayTime.Observed(new DateTimeOffset(
            Depart.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero));

        await harness.Db.SaveChangesAsync();
        return stay;
    }

    private static async Task<DeskHarness> Ready()
    {
        var harness = await DeskHarness.CreateAsync(withEventStore: true);
        await harness.ConfigureAsync();

        await harness.MasterDataRoomsAsync(
        [
            new MasterDataRoomSource.Row
            {
                Id = Spare,
                PropertyId = DeskHarness.Property,
                RoomTypeId = DeskHarness.RoomType,
                RoomNumber = "305",
                Active = true,
            },
            new MasterDataRoomSource.Row
            {
                Id = Taken,
                PropertyId = DeskHarness.Property,
                RoomTypeId = DeskHarness.RoomType,
                RoomNumber = "306",
                Active = true,
            },
        ]);

        return harness;
    }

    private static T? Read<T>(object? answer, string name)
    {
        var json = JsonSerializer.SerializeToElement(answer);
        return json.TryGetProperty(name, out var value) ? value.Deserialize<T>() : default;
    }
}
