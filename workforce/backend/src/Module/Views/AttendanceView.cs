using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Attendance;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// Attendance — who was posted, who came, and the difference measured.
/// </summary>
/// <remarks>
/// <para>
/// <b>Lateness is measured, never judged.</b> The row says "Late 20 min"
/// because that is the arithmetic between the shift's start and the arrival;
/// nothing here decides whether twenty minutes matters, which is a property's
/// policy and a supervisor's conversation.
/// </para>
/// <para>
/// <b>Present-and-not-rostered is a row, not an error.</b> Somebody covering at
/// short notice is a real fact and the missing rota cell is the gap. Dropping
/// the row would hide the hours somebody actually worked.
/// </para>
/// </remarks>
public static class AttendanceView
{
    /// <summary>One day, one department.</summary>
    public static async Task<object?> Day(ModuleCall call, CancellationToken cancellationToken)
    {
        var comparison = call.Service<DayComparison>();
        var directory = call.Service<IStaffDirectory>();
        var postings = call.Service<PostingService>();
        var records = call.Service<AttendanceService>();
        var clock = call.Service<TimeProvider>();

        var on = call.Optional("date") is { } named
            ? DateOnly.Parse(named.GetString()!)
            : DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var department = call.Optional("department")?.GetString();

        var rows = await comparison.CompareAsync(
            call.Scope,
            new AttendanceQuery { From = on, To = on, DepartmentCode = department },
            cancellationToken);

        var written = await records.ReadAsync(
            call.Scope,
            new AttendanceQuery { From = on, To = on, DepartmentCode = department },
            cancellationToken);

        var held = await postings.ListAsync(
            call.Scope,
            new ListPostingsQuery { DepartmentCode = department },
            cancellationToken);

        var role = held.GroupBy(one => one.StaffId)
            .ToDictionary(group => group.Key, group => group.First().JobRole);

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, rows.Select(one => one.StaffId).ToList(), cancellationToken);

        var byStaff = written.ToDictionary(one => one.StaffId);

        return new
        {
            date = on.ToString("dddd d MMMM") + " · business day",
            department,
            rows = rows.Select(one => Row(one, names, role, byStaff)).ToList(),
        };
    }

    /// <summary>Record an arrival or a departure.</summary>
    public static async Task<object?> Record(
        ModuleCall call, CancellationToken cancellationToken)
    {
        if (call.Method != "record")
        {
            throw new InvalidRequestException(call.Method + " is not an attendance method");
        }

        var written = await call.Service<AttendanceService>().RecordAsync(
            call.Scope,
            new RecordAttendanceCommand
            {
                StaffId = call.Id("staffId"),
                BusinessDate = call.Date("on"),
                InAt = Time(call, "in"),
                OutAt = Time(call, "out"),
                // Stamped by this handler, never read from the body: a UI can
                // write any source into its own JSON, and a record claiming a
                // device wrote it would be attributing a measurement to a
                // process that did not take it.
                Source = AttendanceSource.Manual,
            },
            cancellationToken);

        return new { id = written.Id, version = written.Version };
    }

    /// <summary>Correct a record somebody may already have been paid against.</summary>
    public static async Task<object?> Amend(ModuleCall call, CancellationToken cancellationToken)
    {
        if (call.Method != "amend")
        {
            throw new InvalidRequestException(call.Method + " is not an attendance method");
        }

        var written = await call.Service<AttendanceService>().AmendAsync(
            call.Scope,
            new AmendAttendanceCommand
            {
                Id = call.Id("id"),
                ExpectedVersion = call.Required("version").GetInt64(),
                InAt = Time(call, "in"),
                OutAt = Time(call, "out"),
                ClearIn = call.Optional("clearIn")?.GetBoolean() ?? false,
            },
            cancellationToken);

        return new { id = written.Id, version = written.Version };
    }

    /// <summary>One person's day.</summary>
    private static object Row(
        DayRow row,
        IReadOnlyDictionary<Guid, string> names,
        IReadOnlyDictionary<Guid, string> roles,
        IReadOnlyDictionary<Guid, AttendanceRecord> written)
    {
        written.TryGetValue(row.StaffId, out var record);
        var (against, tone) = Verdict(row);

        return new
        {
            who = names.TryGetValue(row.StaffId, out var name) ? name : null,
            role = roles.TryGetValue(row.StaffId, out var job) ? job : null,
            posted = row.Rostered && row.ScheduledStart is { } start
                ? start.ToString("HH:mm")
                : row.Rostered ? "rostered" : null,
            @in = row.ActualIn?.ToString("HH:mm"),
            @out = record?.OutAt?.ToString("HH:mm"),
            against,
            tone,
            // The source is the record's own, and null where no record exists —
            // an absent person has no source, and "manual" on their row would
            // say somebody entered an absence they did not enter.
            source = record is null ? null : record.Source.ToString().ToLowerInvariant(),
        };
    }

    /// <summary>What the comparison says, in the words the screen draws.</summary>
    private static (string Reads, string Tone) Verdict(DayRow row)
    {
        if (row.Absent)
        {
            return ("Absent", "bad");
        }

        if (!row.Rostered)
        {
            return ("Present, not rostered", "warn");
        }

        if (row.LateBy is { } late && late > TimeSpan.Zero)
        {
            return ("Late " + (int)late.TotalMinutes + " min", "warn");
        }

        return row.Worked is null ? ("On shift", "neu") : ("On time", "ok");
    }

    /// <summary>An optional time on the wire.</summary>
    private static TimeOnly? Time(ModuleCall call, string field)
        => call.Optional(field) is { } value ? TimeOnly.Parse(value.GetString()!) : null;
}
