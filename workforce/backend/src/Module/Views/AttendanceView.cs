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

        // The property's operating day, asked of Context — ADR 0211. This was
        // the UTC calendar day, so between local midnight and 05:30 an Indian
        // property's attendance screen showed yesterday.
        var on = call.Optional("date") is { } named
            ? DateOnly.Parse(named.GetString()!)
            : await PropertyDay.TodayAsync(call, cancellationToken);

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
            // **The day as a day, and the words are the screen's** — ADR 0175.
            // This was `ToString("dddd d MMMM")`: the weekday and the month
            // name in whatever culture the service happens to run under, on
            // every property's screen, with " · business day" welded on after
            // it. The screen writes that clause in the property's own language.
            date = Wire.Day(on),
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
        var (state, lateBy, tone) = Verdict(row);

        return new
        {
            who = names.TryGetValue(row.StaffId, out var name) ? name : null,
            role = roles.TryGetValue(row.StaffId, out var job) ? job : null,
            // **Two fields, because it was three states in one string** — a
            // clock, the sentinel word `rostered`, and null. A surface handed
            // that cannot tell a time it must render from a word it must not,
            // and could not say "rostered" in any other language. ADR 0175.
            rostered = row.Rostered,
            postedAt = row.Rostered ? Wire.Clock(row.ScheduledStart) : null,
            @in = Wire.Clock(row.ActualIn),
            @out = Wire.Clock(record?.OutAt),

            // **The state as a word this service owns, and the lateness as a
            // number** — NUM-Q1, ADR 0174. `against` was a sentence composed
            // here — "Late 20 min" — so a property in another language read the
            // server's English, AND the screen counted late people by testing
            // `against.startsWith("Late")`: the service's own prose, parsed
            // back as data. The screen writes the sentence; the count reads
            // `state`.
            state,
            lateBy,
            tone,
            // The source is the record's own, and null where no record exists —
            // an absent person has no source, and "manual" on their row would
            // say somebody entered an absence they did not enter.
            source = record is null ? null : record.Source.ToString().ToLowerInvariant(),
        };
    }

    /// <summary>What the comparison found — a state, and the minutes when late.</summary>
    /// <remarks>
    /// <para>
    /// <b>A state this service owns, never a sentence.</b> It returned the
    /// words — <i>"Late 20 min"</i>, <i>"Present, not rostered"</i> — composed
    /// here in the service's own culture, with the number and its unit inside
    /// them. Two things followed: a property in another language read English,
    /// and the screen counted late people with
    /// <c>against.startsWith("Late")</c>, which is this service's prose being
    /// parsed back as data (NUM-Q1, ADR 0174).
    /// </para>
    /// <para>
    /// The states are the five the comparison can find. <c>lateBy</c> is null
    /// on all of them but <c>late</c> — a zero would be a person who arrived
    /// exactly on time, which is <c>onTime</c>.
    /// </para>
    /// </remarks>
    private static (string State, int? LateBy, string Tone) Verdict(DayRow row)
    {
        if (row.Absent)
        {
            return ("absent", null, "bad");
        }

        if (!row.Rostered)
        {
            return ("unrostered", null, "warn");
        }

        if (row.LateBy is { } late && late > TimeSpan.Zero)
        {
            return ("late", (int)late.TotalMinutes, "warn");
        }

        return row.Worked is null ? ("onShift", null, "neu") : ("onTime", null, "ok");
    }

    /// <summary>An optional time on the wire.</summary>
    private static TimeOnly? Time(ModuleCall call, string field)
        => call.Optional(field) is { } value ? TimeOnly.Parse(value.GetString()!) : null;
}
