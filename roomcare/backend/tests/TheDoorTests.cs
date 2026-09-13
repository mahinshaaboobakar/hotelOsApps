using HotelOS.Platform;
using HotelOS.RoomCare.Application.Assignment;
using HotelOS.RoomCare.Application.Day;
using HotelOS.RoomCare.Application.Work;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelOS.RoomCare.Tests;

/// <summary>S5 case 1 — the four ways a room ends at the door, and done as the condition write (§7.1).</summary>
[Collection(RoomCareCollection.Name)]
public sealed class TheDoorTests(RoomCareFixture fixture)
{
    [Fact]
    public async Task Done_is_the_room_clean_announced_with_the_task_in_one_commit_and_resets_the_linen_date()
    {
        var (h, anita, task) = await AssignedAsync(Service.DepartureClean);

        await h.Get<AttendantWork>().StartAsync(h.As(anita), task.Id, default);
        h.Clock.Advance(TimeSpan.FromMinutes(35));
        var ended = await h.Get<AttendantWork>().AttemptAsync(h.As(anita), new AttemptCommand(task.Id, AttemptFound.Done), default);

        Assert.Equal((RoomTaskStatus.Ended, TaskOutcome.Done), (ended.Status, ended.Outcome));
        await using var db = h.Db();
        var room = await db.RoomStates.SingleAsync(r => r.RoomId == task.RoomId);
        Assert.Equal((Condition.Clean, ConditionSource.Attendant, anita), (room.Condition, room.ConditionSource, room.ConditionSetById!.Value));
        Assert.Equal(new DateOnly(2026, 9, 5), room.LinenLastChangedOn);
        var cleaned = (await h.EventsAsync(room.RoomId)).Last();
        Assert.Equal((EventTypes.RoomCleaned, room.Version), (cleaned.Type, cleaned.Version));
        Assert.Equal(task.Id.ToString(), cleaned.Payload.GetProperty("task_id").GetString());
        Assert.Equal(35, (await db.WorkSessions.SingleAsync(s => s.TaskId == task.Id)).Minutes);
        var taskEvents = await h.EventsAsync(task.Id);
        Assert.Equal(taskEvents.Count, taskEvents.Select(e => e.Version).Distinct().Count());
    }

    [Fact]
    public async Task The_DND_board_keeps_the_room_on_the_list_and_records_the_attempt()
    {
        var (h, anita, task) = await AssignedAsync(Service.DailyService);

        var after = await h.Get<AttendantWork>().AttemptAsync(h.As(anita), new AttemptCommand(task.Id, AttemptFound.Dnd), default);

        Assert.True(after.IsOpen);
        await using var db = h.Db();
        Assert.Equal(AttemptFound.Dnd, (await db.Attempts.SingleAsync(a => a.TaskId == task.Id)).Found);
        Assert.Contains(await h.EventsAsync(task.Id), e => e.Type == EventTypes.TaskAttempted && e.Payload.GetProperty("found").GetString() == AttemptFound.Dnd);
    }

    [Fact]
    public async Task A_partial_service_must_say_what_was_done()
    {
        var (h, anita, task) = await AssignedAsync(Service.DailyService);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            h.Get<AttendantWork>().AttemptAsync(h.As(anita), new AttemptCommand(task.Id, AttemptFound.Partial), default));
        var ended = await h.Get<AttendantWork>().AttemptAsync(
            h.As(anita), new AttemptCommand(task.Id, AttemptFound.Partial) { PartialDone = [PartialPart.Bathroom] }, default);

        Assert.Equal(TaskOutcome.Partial, ended.Outcome);
        Assert.Equal([PartialPart.Bathroom], ended.PartialDone);
    }

    [Fact]
    public async Task Only_the_attendant_the_room_is_assigned_to_may_work_it()
    {
        var (h, _, task) = await AssignedAsync(Service.DailyService);

        await Assert.ThrowsAsync<PermissionDeniedException>(() => h.Get<AttendantWork>().StartAsync(h.As(Guid.CreateVersion7()), task.Id, default));
    }

    [Fact]
    public async Task Found_an_issue_is_recorded_and_published_with_the_correlation_id_Jobs_answers_on()
    {
        var (h, anita, task) = await AssignedAsync(Service.DepartureClean);

        var issue = await h.Get<RoomActs>().IssueAsync(h.As(anita), task.Id, "Blowing warm since I came in", "AC_NOT_COOLING", null, default);

        var published = (await h.EventsAsync(issue.Id)).Single();
        Assert.Equal((EventTypes.IssueFound, issue.CorrelationId), (published.Type, published.CorrelationId));
        Assert.Equal(issue.CorrelationId, published.Payload.GetProperty("correlation_id").GetString());

        await h.InScopeAsync(async s =>
        {
            var job = Guid.CreateVersion7();
            // Built as the consumer builds a handler: from the delivery's scope.
            await ActivatorUtilities.CreateInstance<JobCreatedHandler>(s).HandleAsync(h.Tick with { CorrelationId = issue.CorrelationId },
                new JobAnnounced(job, "J-1190", task.RoomId, "Not cooling", null, h.Clock.GetUtcNow()),
                new EventEnvelope { EventId = Guid.CreateVersion7(), EventType = EventTypes.JobCreated, CorrelationId = issue.CorrelationId, OccurredAt = h.Clock.GetUtcNow() },
                default);
            return job;
        });
        await using var db = h.Db();
        Assert.NotNull((await db.Issues.SingleAsync(i => i.Id == issue.Id)).JobId);
    }

    /// <summary>One room decided, proposed to the only attendant posted, and accepted.</summary>
    internal static async Task<(RoomCareHarness H, Guid Attendant, RoomTask Task)> AssignedAsync(string service, RoomCareFixture fixture)
    {
        var h = new RoomCareHarness(fixture);
        var anita = await h.PostAsync("Anita Pillai");
        var room = h.House.Room("214");
        await h.SeedStateAsync(room, s =>
        {
            s.Condition = Condition.Dirty;
            if (service == Service.DepartureClean)
            {
                s.Occupancy = Occupancy.Vacant;
                s.StayStatuses = [StayStatus.CheckedOut];
            }
        });
        await h.Get<PrepareService>().PrepareAsync(h.Tick, null, default);
        await h.Get<AssignmentService>().AcceptAllAsync(h.As(Guid.CreateVersion7()), new DateOnly(2026, 9, 5), ServiceWindowName.Morning, default);
        await using var db = h.Db();
        return (h, anita, await db.Tasks.SingleAsync(t => t.RoomId == room));
    }

    private Task<(RoomCareHarness H, Guid Attendant, RoomTask Task)> AssignedAsync(string service) => AssignedAsync(service, fixture);
}
