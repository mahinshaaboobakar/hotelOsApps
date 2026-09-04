using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Duties;

/// <summary>
/// The Manager on Duty register — spans, and the one answer at every instant.
/// </summary>
/// <remarks>
/// <c>WF-Q8</c>: a duty is a span crossing midnight as naturally as a night
/// shift does, and <i>"who is MOD right now"</i> is the clock against it —
/// computed, never stored.
/// </remarks>
public class DutyService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    TimeProvider clock)
{
    /// <summary>Assign the duty over a span.</summary>
    public async Task<DutyAssignment> AssignAsync(
        RequestScope scope, AssignDutyCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.DutyAssign, "property", scope.PropertyId, cancellationToken);

        // A span that ends before it starts cannot be true — refused, not
        // warned. Equal is refused too: a duty nobody holds for any instant is
        // a row that answers no question.
        if (command.EndsAt <= command.StartsAt)
        {
            throw new InvalidRequestException("a duty ends after it starts");
        }

        await RefuseOverlapAsync(scope.PropertyId, command, null, cancellationToken);

        var now = clock.GetUtcNow();
        var duty = new DutyAssignment
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            StaffId = command.StaffId,
            DutyType = DutyTypes.ManagerOnDuty,
            StartsAt = command.StartsAt,
            EndsAt = command.EndsAt,
            HandoverNote = command.HandoverNote?.Trim() ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        db.Duties.Add(duty);
        await db.SaveChangesAsync(cancellationToken);

        return duty;
    }

    /// <summary>Amend a duty — its holder, its span, or its handover note.</summary>
    public async Task<DutyAssignment> AmendAsync(
        RequestScope scope, AmendDutyCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.DutyAssign, "property", scope.PropertyId, cancellationToken);

        var duty = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(duty, command.ExpectedVersion);

        var starts = command.StartsAt ?? duty.StartsAt;
        var ends = command.EndsAt ?? duty.EndsAt;

        if (ends <= starts)
        {
            throw new InvalidRequestException("a duty ends after it starts");
        }

        if (starts != duty.StartsAt || ends != duty.EndsAt)
        {
            await RefuseOverlapAsync(
                scope.PropertyId,
                new AssignDutyCommand
                {
                    StaffId = duty.StaffId,
                    StartsAt = starts,
                    EndsAt = ends,
                },
                duty.Id,
                cancellationToken);
        }

        duty.StartsAt = starts;
        duty.EndsAt = ends;

        if (command.StaffId is { } staffId)
        {
            duty.StaffId = staffId;
        }

        if (command.HandoverNote is { } note)
        {
            duty.HandoverNote = note.Trim();
        }

        duty.UpdatedAt = clock.GetUtcNow();
        duty.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return duty;
    }

    /// <summary>Take a duty off the register.</summary>
    /// <remarks>
    /// A hard delete, and the gap it leaves is drawn rather than hidden: the
    /// register shows a dashed <i>no MOD</i> for the hours nobody holds, because
    /// a blank could mean <i>nobody</i> or <i>not entered yet</i> and those are
    /// different. Keeping a tombstone would say neither.
    /// </remarks>
    public async Task WithdrawAsync(
        RequestScope scope, WithdrawDutyCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.DutyAssign, "property", scope.PropertyId, cancellationToken);

        var duty = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(duty, command.ExpectedVersion);

        db.Duties.Remove(duty);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Who holds the duty at an instant — usually now.</summary>
    /// <remarks>
    /// <b>The question the register is actually opened to answer</b>, at 3 a.m.,
    /// by somebody who needs to call whoever it is. Computed from the clock
    /// against the stored spans; there is no <c>is_current_mod</c> flag to read
    /// and none to go stale.
    /// </remarks>
    public async Task<DutyAssignment?> HolderAtAsync(
        RequestScope scope, DateTimeOffset instant, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        return await db.Duties
            .Where(d => d.PropertyId == scope.PropertyId
                        && d.StartsAt <= instant
                        && d.EndsAt > instant)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>The next duty to begin after an instant.</summary>
    /// <remarks>
    /// Beside <see cref="HolderAtAsync"/> because the register's top line is
    /// <i>now and next</i>: knowing who is on and who follows is the whole of
    /// what a duty manager needs from this screen.
    /// </remarks>
    public async Task<DutyAssignment?> NextAfterAsync(
        RequestScope scope, DateTimeOffset instant, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        return await db.Duties
            .Where(d => d.PropertyId == scope.PropertyId && d.StartsAt > instant)
            .OrderBy(d => d.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Every duty overlapping a window — the week strip.</summary>
    public async Task<IReadOnlyList<DutyAssignment>> ListAsync(
        RequestScope scope,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        // Overlapping, not contained: a duty that began before the window and
        // runs into it is on the strip, which is the whole reason the strip is a
        // timeline rather than a row of day cells.
        return await db.Duties
            .Where(d => d.PropertyId == scope.PropertyId && d.StartsAt < to && d.EndsAt > from)
            .OrderBy(d => d.StartsAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Two duties may not be in force at one instant.</summary>
    /// <remarks>
    /// <para>
    /// <b>Refused, not warned</b> — <c>WF-Q16</c>. <i>"Who is MOD now"</i> with
    /// two answers is a corrupt record, not a judgment call.
    /// </para>
    /// <para>
    /// An overlap check rather than a unique key, because a span cannot be one:
    /// the behaviour chapter 01 described survives, and what detects the clash
    /// is a different database object. Half-open on both sides, so a duty ending
    /// at 08:00 and the next beginning at 08:00 do not collide — that is the
    /// ordinary handover and refusing it would make the register unusable.
    /// </para>
    /// </remarks>
    private async Task RefuseOverlapAsync(
        Guid propertyId,
        AssignDutyCommand command,
        Guid? excluding,
        CancellationToken cancellationToken)
    {
        var clashing = await db.Duties
            .Where(d => d.PropertyId == propertyId
                        && d.StartsAt < command.EndsAt
                        && command.StartsAt < d.EndsAt
                        && (excluding == null || d.Id != excluding))
            .FirstOrDefaultAsync(cancellationToken);

        if (clashing is not null)
        {
            throw new InvalidRequestException(
                "another Manager on Duty already holds part of that span "
                + $"({clashing.StartsAt:u} to {clashing.EndsAt:u})");
        }
    }

    private async Task<DutyAssignment> LoadAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken)
    {
        var duty = await db.Duties.FirstOrDefaultAsync(
            d => d.Id == id && d.PropertyId == scope.PropertyId, cancellationToken);

        return duty ?? throw new NotFoundException("duty", id);
    }

    private static void RequireVersion(DutyAssignment duty, long expected)
    {
        if (duty.Version != expected)
        {
            throw new ConcurrencyException("duty", duty.Id, expected);
        }
    }
}
