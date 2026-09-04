using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Attendance;

/// <summary>
/// What actually happened — recorded, corrected, and never inferred.
/// </summary>
/// <remarks>
/// <para>
/// <c>WF-Q13</c>: v1 attendance is <b>manual</b>, entered by a supervisor.
/// Devices arrive later through the Integration Hub, and the shape of the record
/// does not change when they do — which is what <see cref="AttendanceSource"/>
/// and the external reference are for.
/// </para>
/// <para>
/// <b>Nothing here reads the rota.</b> An attendance record is a fact about a
/// person and a day, and it stands whether or not they were rostered — somebody
/// who came in on a day off was present, and a record that refused to exist
/// because no cell matched would lose the very thing worth knowing. Comparing the
/// two is <see cref="DayComparison"/>, and it is a view.
/// </para>
/// </remarks>
public class AttendanceService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    TimeProvider clock)
{
    /// <summary>Record, or replace, one person's day.</summary>
    public async Task<AttendanceRecord> RecordAsync(
        RequestScope scope, RecordAttendanceCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.AttendanceRecord, "property", scope.PropertyId, cancellationToken);

        Validate(command);

        var now = clock.GetUtcNow();
        var existing = await FindAsync(
            scope.PropertyId, command.StaffId, command.BusinessDate, cancellationToken);

        if (existing is not null)
        {
            existing.InAt = command.InAt;
            existing.OutAt = command.OutAt;
            existing.Source = command.Source;
            existing.RecordedByUserId = Recorder(scope, command.Source);
            existing.ExternalReference = command.ExternalReference?.Trim();
            existing.Note = command.Note?.Trim() ?? existing.Note;
            existing.UpdatedAt = now;
            existing.Version += 1;

            await db.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var record = new AttendanceRecord
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            StaffId = command.StaffId,
            BusinessDate = command.BusinessDate,
            InAt = command.InAt,
            OutAt = command.OutAt,
            Source = command.Source,
            RecordedByUserId = Recorder(scope, command.Source),
            ExternalReference = command.ExternalReference?.Trim(),
            Note = command.Note?.Trim() ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        db.Attendance.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        return record;
    }

    /// <summary>Correct a record.</summary>
    public async Task<AttendanceRecord> AmendAsync(
        RequestScope scope, AmendAttendanceCommand command, CancellationToken cancellationToken)
    {
        // **Amending is its own permission**, and that is the registry's design
        // rather than this service's: entering today's sheet is routine and a
        // supervisor does it; correcting a record somebody may already have been
        // paid against is not, and a property may want those in different hands.
        await authorizer.RequireAsync(
            scope, Permissions.AttendanceAmend, "property", scope.PropertyId, cancellationToken);

        var record = await LoadAsync(scope, command.Id, cancellationToken);

        if (record.Version != command.ExpectedVersion)
        {
            throw new ConcurrencyException("attendance record", record.Id, command.ExpectedVersion);
        }

        // Clearing is its own instruction, because null already means "leave it
        // alone" on the two time fields. Without it a mistaken arrival could
        // never be undone except by deleting the record, which loses the trail.
        var arrived = command.ClearIn ? null : command.InAt ?? record.InAt;
        var left = command.ClearOut ? null : command.OutAt ?? record.OutAt;

        if (left is not null && arrived is null)
        {
            throw new InvalidRequestException(
                "a departure with no arrival is not a day anybody worked");
        }

        record.InAt = arrived;
        record.OutAt = left;
        record.Note = command.Note?.Trim() ?? record.Note;

        // A correction is still an entry, and the account that made it is the one
        // now answerable. Leaving the original recorder would attribute a
        // later fix to whoever first typed the sheet.
        record.RecordedByUserId = Recorder(scope, record.Source);
        record.UpdatedAt = clock.GetUtcNow();
        record.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return record;
    }

    /// <summary>Remove a record that should never have existed.</summary>
    /// <remarks>
    /// Narrow, and <b>not</b> how an absence is expressed. An absence is a record
    /// with no arrival, which says somebody looked and they were not there.
    /// Deleting says only that nobody looked, and those are different answers to
    /// a payroll question.
    /// </remarks>
    public async Task DeleteAsync(
        RequestScope scope, Guid id, long expectedVersion, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.AttendanceAmend, "property", scope.PropertyId, cancellationToken);

        var record = await LoadAsync(scope, id, cancellationToken);

        if (record.Version != expectedVersion)
        {
            throw new ConcurrencyException("attendance record", record.Id, expectedVersion);
        }

        db.Attendance.Remove(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The records in a window.</summary>
    public async Task<IReadOnlyList<AttendanceRecord>> ReadAsync(
        RequestScope scope, AttendanceQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        var records = db.Attendance.Where(
            r => r.PropertyId == scope.PropertyId
                 && r.BusinessDate >= query.From
                 && r.BusinessDate <= query.To);

        if (query.StaffId is { } staffId)
        {
            records = records.Where(r => r.StaffId == staffId);
        }

        return await records
            .OrderBy(r => r.BusinessDate)
            .ThenBy(r => r.StaffId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Who is still signed in.</summary>
    /// <remarks>
    /// What a duty manager asks at midnight and a fire warden asks at any time.
    /// Derived from the records rather than kept in a column, like every other
    /// question of the form <i>right now</i> in this application.
    /// </remarks>
    public async Task<IReadOnlyList<AttendanceRecord>> StillInAsync(
        RequestScope scope, DateOnly businessDate, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.RosterRead, "property", scope.PropertyId, cancellationToken);

        return await db.Attendance
            .Where(r => r.PropertyId == scope.PropertyId
                        && r.BusinessDate == businessDate
                        && r.InAt != null
                        && r.OutAt == null)
            .ToListAsync(cancellationToken);
    }

    /// <summary>What a record may and may not say.</summary>
    /// <remarks>
    /// Both refusals are records that cannot be true rather than judgments —
    /// <c>WF-Q16</c>. A departure with no arrival is not a day anybody worked, and
    /// a non-manual source without its reference is a reading nobody can trace.
    /// </remarks>
    private static void Validate(RecordAttendanceCommand command)
    {
        if (command.OutAt is not null && command.InAt is null)
        {
            throw new InvalidRequestException(
                "a departure with no arrival is not a day anybody worked");
        }

        if (command.Source is not AttendanceSource.Manual
            && string.IsNullOrWhiteSpace(command.ExternalReference))
        {
            throw new InvalidRequestException(
                "a device or imported record carries the reference it came from — "
                + "a reading nobody can trace cannot be reconciled when two machines disagree");
        }
    }

    /// <summary>Who is answerable for this record.</summary>
    /// <remarks>
    /// The acting account for a manual entry; <c>null</c> for a device, where the
    /// external reference is the provenance and naming a person would attribute a
    /// machine reading to whoever happened to be signed in.
    /// </remarks>
    private static Guid? Recorder(RequestScope scope, AttendanceSource source) =>
        source is AttendanceSource.Manual ? scope.UserId : null;

    private async Task<AttendanceRecord?> FindAsync(
        Guid propertyId, Guid staffId, DateOnly date, CancellationToken cancellationToken) =>
        await db.Attendance.FirstOrDefaultAsync(
            r => r.PropertyId == propertyId && r.StaffId == staffId && r.BusinessDate == date,
            cancellationToken);

    private async Task<AttendanceRecord> LoadAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken)
    {
        var record = await db.Attendance.FirstOrDefaultAsync(
            r => r.Id == id && r.PropertyId == scope.PropertyId, cancellationToken);

        return record ?? throw new NotFoundException("attendance record", id);
    }
}
