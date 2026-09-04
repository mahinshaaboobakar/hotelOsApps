using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Application.Policies;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Events;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;

namespace HotelOS.Jobs.Application.Assignment;

/// <summary>
/// Who holds a job — S3 D1: a person or a team from the department's people on
/// shift on the execution date; AUTO picks the one with the fewest open jobs.
/// A reassignment ends the old row and opens a new one. Accepting is the
/// assignee's own act and needs no grant.
/// </summary>
public class AssignmentService(
    JobsDbContext db,
    IKernelAuthorizer authorizer,
    IPropertyDirectory directory,
    JobAnnouncer announcer,
    JobRecords records)
{
    /// <summary>The assignment made while raising — the raiser's pick, or AUTO.</summary>
    public async Task AssignOnRaiseAsync(
        RequestScope scope, Job job, RaiseJobCommand command, ResolvedPolicy resolved,
        CancellationToken cancellationToken)
    {
        if (command.AssignToUserId is { } user)
        {
            Open(job, user, null, AssignmentHow.Manual, scope.UserId);
            records.Move(scope, job, JobStatus.Assigned);
            announcer.Announce(scope, job, EventTypes.JobAssigned, records.Now, user.ToString());
            return;
        }

        var team = command.AssignToTeamId ?? resolved.AutoAssignTeamId;
        if (team is { } t)
        {
            Open(job, null, t, command.AssignToTeamId is null ? AssignmentHow.Auto : AssignmentHow.Manual, scope.UserId);
            records.Move(scope, job, JobStatus.Assigned);
            announcer.Announce(scope, job, EventTypes.JobAssigned, records.Now, $"team {t}");
            return;
        }

        var picked = await PickAsync(job, cancellationToken);
        if (picked is { } p)
        {
            Open(job, p, null, AssignmentHow.Auto, null);
            records.Move(scope, job, JobStatus.Assigned, byWhat: "AUTO");
            announcer.Announce(scope, job, EventTypes.JobAssigned, records.Now, p.ToString());
        }

        // Nobody on shift: the job stays RAISED, "AUTO · pending" on the board.
    }

    /// <summary>Assign or reassign — <c>job.assign</c>.</summary>
    public async Task<Job> AssignAsync(RequestScope scope, AssignCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(scope, Permissions.Assign, "property", scope.PropertyId, cancellationToken);
        if ((command.UserId is null) == (command.TeamId is null))
        {
            throw new InvalidRequestException("assign to exactly one of a user or a team");
        }

        var job = await records.LoadAsync(scope, command.JobId, cancellationToken);
        JobRecords.RequireVersion(job, command.ExpectedVersion);
        JobRecords.RequireOpen(job);

        var now = records.Now;
        if (await records.CurrentAssignmentAsync(job.Id, cancellationToken) is { } current)
        {
            current.EndedAt = now;
        }

        if (await records.OpenSessionAsync(job.Id, cancellationToken) is { } session)
        {
            session.Stop(now);
        }

        Open(job, command.UserId, command.TeamId, AssignmentHow.Manual, scope.UserId);
        records.Move(scope, job, JobStatus.Assigned);
        announcer.Announce(scope, job, EventTypes.JobAssigned, now, (command.UserId ?? command.TeamId).ToString());
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    /// <summary>The assignee accepts — their own act on their own job.</summary>
    public async Task<Job> AcceptAsync(RequestScope scope, Guid jobId, long expectedVersion, CancellationToken cancellationToken)
    {
        var job = await records.LoadAsync(scope, jobId, cancellationToken);
        JobRecords.RequireVersion(job, expectedVersion);
        var current = await records.CurrentAssignmentAsync(job.Id, cancellationToken)
            ?? throw new InvalidRequestException($"job {job.JobNumber} is not assigned");
        if (current.AssigneeUserId is { } assignee && assignee != scope.UserId)
        {
            throw new PermissionDeniedException("assignee", $"job {job.JobNumber}");
        }

        if (job.JobStatus != JobStatus.Assigned)
        {
            throw new InvalidRequestException($"job {job.JobNumber} is {job.JobStatus}, not ASSIGNED");
        }

        var now = records.Now;
        current.AcceptedAt = now;
        if (current.AssigneeUserId is null && scope.UserId is { } member)
        {
            // A team job: the member who accepts becomes the person.
            current.AssigneeUserId = member;
        }

        records.Move(scope, job, JobStatus.Accepted);
        announcer.Announce(scope, job, EventTypes.JobAccepted, now);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    private void Open(Job job, Guid? user, Guid? team, string how, Guid? by) =>
        db.Assignments.Add(new JobAssignment
        {
            Id = Uuid7.NewUuid7(),
            JobId = job.Id,
            PropertyId = job.PropertyId,
            AssigneeUserId = user,
            TeamId = team,
            How = how,
            AssignedBy = by,
            AssignedAt = records.Now,
        });

    /// <summary>AUTO: on shift on the execution date, fewest open jobs; nobody means null.</summary>
    private async Task<Guid?> PickAsync(Job job, CancellationToken cancellationToken)
    {
        var day = job.ScheduledFor ?? DateOnly.FromDateTime(records.Now.UtcDateTime);
        var people = await directory.OnShiftAsync(job.PropertyId, job.DepartmentCode, day, cancellationToken);
        return people.OrderBy(p => p.OpenJobs).ThenBy(p => p.Name).FirstOrDefault()?.UserId;
    }
}
