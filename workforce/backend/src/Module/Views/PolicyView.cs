using HotelOS.Platform;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using HotelOS.Workforce.Infrastructure;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// Policy — the property's own shift catalogue, its leave types and the
/// overtime threshold.
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything on this screen is the property's, not the platform's.</b> A
/// closed list of shifts shipped by HotelOS would be a release every time a
/// hotel invented a split, so the catalogue is data and this is where it is
/// configured.
/// </para>
/// <para>
/// <b>"In use" is counted, never estimated.</b> The number beside a shift is
/// how many assignments actually reference it, because it is the number a
/// person uses to decide whether retiring it is safe.
/// </para>
/// </remarks>
public static class PolicyView
{
    /// <summary>The catalogue, the types and the threshold.</summary>
    public static async Task<object?> Read(ModuleCall call, CancellationToken cancellationToken)
    {
        var catalogue = call.Service<ShiftCatalogueService>();
        var types = call.Service<LeaveTypeService>();
        var policy = call.Service<PolicyService>();
        var db = call.Service<WorkforceDbContext>();

        // ADR 0211 — the day that decides which shifts and leave types are in
        // force today.
        var today = await PropertyDay.TodayAsync(call, cancellationToken);
        var shifts = await catalogue.ListAsync(call.Scope, false, cancellationToken);

        var usage = await db.ShiftAssignments
            .Where(one => one.PropertyId == call.Scope.PropertyId)
            .GroupBy(one => one.CatalogueEntryId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(one => one.Key, one => one.Count, cancellationToken);

        var rows = new List<object>();

        foreach (var shift in shifts)
        {
            var hours = await catalogue.HoursOnAsync(
                call.Scope, shift.Id, today, cancellationToken);

            rows.Add(new
            {
                name = shift.Name,
                code = shift.ShortCode,
                hours = Wire.Span(hours?.StartsAt, hours?.EndsAt),
                second = Wire.Span(hours?.SecondStartsAt, hours?.SecondEndsAt),
                colour = string.IsNullOrEmpty(shift.Colour) ? "None" : shift.Colour,

                // **The tone the rota and the schedule already draw this shift
                // in.** Both screens reading this render `row.tone` and nothing
                // sent it: against a real property the swatch and the code chip
                // carried `undefined` as a class, so a shift that is Rose on the
                // rota was unstyled here. `RotaView` and `ScheduleView` have
                // called `Wording.Tone` all along — this read is the one that
                // did not, and only the fixture ever had the field (ledger D3).
                // An unset colour is the empty string, which `Tone` answers
                // `neutral` for — the same call `RotaView` and `ScheduleView`
                // make, with no second reading of "no colour" invented here.
                tone = Wording.Tone(shift.Colour),
                kind = hours?.IsWorking == true ? "working" : "off",

                // **A count, not "88 assignments".** The word and the way the
                // number is written are the reader's: this composed an English
                // noun onto a figure in the service's own culture, so a property
                // reading Arabic read English and a property in Germany read
                // `1234` where it writes `1.234` — NUM-Q1, ADR 0174.
                inUse = usage.TryGetValue(shift.Id, out var count) ? count : 0,
            });
        }

        var leave = await types.ListAsync(call.Scope, false, cancellationToken);
        var threshold = await policy.GetAsync(call.Scope, cancellationToken);

        return new
        {
            property = (string?)null,
            catalogue = rows,
            leave = leave.Select(Type).ToList(),
            // The thresholds as numbers, null where a property set none - ADR
            // 0174. These were "9 h / day" built here, so the unit, the "/" and
            // the word for the period were all this service's, in one language.
            overtimeDaily = threshold?.OvertimeDailyHours,
            overtimeWeekly = threshold?.OvertimeWeeklyHours,
            // The declared-holiday list has no owner in this application and no
            // service answers it. Absent rather than an empty sentence: the
            // screen renders nothing where a fabricated "0 declared holidays"
            // would read as a property that had configured none.
            holidays = (string?)null,
        };
    }

    /// <summary>Define or amend a shift, a leave type, or the threshold.</summary>
    public static Task<object?> Write(ModuleCall call, CancellationToken cancellationToken)
        => call.Method switch
        {
            "defineShift" => DefineShift(call, cancellationToken),
            "renameShift" => RenameShift(call, cancellationToken),
            "rescheduleShift" => RescheduleShift(call, cancellationToken),
            "retireShift" => RetireShift(call, cancellationToken),
            "setLeaveType" => SetLeaveType(call, cancellationToken),
            "setOvertime" => SetOvertime(call, cancellationToken),
            _ => throw new InvalidRequestException(call.Method + " is not a policy method"),
        };

    private static async Task<object?> DefineShift(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var entry = await call.Service<ShiftCatalogueService>().CreateAsync(
            call.Scope,
            new CreateShiftCommand
            {
                Name = call.Text("name"),
                ShortCode = call.Text("code"),
                Colour = call.Optional("colour")?.GetString() ?? string.Empty,
                Hours = Hours(call),
                EffectiveFrom = call.Date("from"),
            },
            cancellationToken);

        return new { id = entry.Id, version = entry.Version };
    }

    private static async Task<object?> RenameShift(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var entry = await call.Service<ShiftCatalogueService>().RenameAsync(
            call.Scope,
            new RenameShiftCommand
            {
                Id = call.Id("id"),
                ExpectedVersion = call.Required("version").GetInt64(),
                Name = call.Optional("name")?.GetString(),
                ShortCode = call.Optional("code")?.GetString(),
                Colour = call.Optional("colour")?.GetString(),
            },
            cancellationToken);

        return new { id = entry.Id, version = entry.Version };
    }

    /// <summary>
    /// Change a shift's hours, forward from a date the caller chooses.
    /// </summary>
    /// <remarks>
    /// Rotas already worked keep the times they were worked under — which is
    /// why this is a new row rather than an edit, and why the date is the
    /// caller's rather than today's.
    /// </remarks>
    private static async Task<object?> RescheduleShift(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var hours = await call.Service<ShiftCatalogueService>().RescheduleAsync(
            call.Scope,
            new RescheduleShiftCommand
            {
                Id = call.Id("id"),
                ExpectedVersion = call.Required("version").GetInt64(),
                Hours = Hours(call),
                EffectiveFrom = call.Date("from"),
            },
            cancellationToken);

        return new { id = hours.Id, from = hours.EffectiveFrom };
    }

    private static async Task<object?> RetireShift(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var entry = await call.Service<ShiftCatalogueService>().RetireAsync(
            call.Scope,
            new RetireShiftCommand
            {
                Id = call.Id("id"),
                ExpectedVersion = call.Required("version").GetInt64(),
            },
            cancellationToken);

        return new { id = entry.Id, version = entry.Version, active = entry.Active };
    }

    private static async Task<object?> SetLeaveType(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var type = await call.Service<LeaveTypeService>().SetAsync(
            call.Scope,
            new SetLeaveTypeCommand
            {
                Id = call.Optional("id")?.GetGuid(),
                ExpectedVersion = call.Optional("version")?.GetInt64(),
                Code = call.Text("code"),
                Name = call.Text("name"),
                AccrualPerMonth = call.Optional("accrual")?.GetDecimal(),
            },
            cancellationToken);

        return new { id = type.Id, version = type.Version };
    }

    private static async Task<object?> SetOvertime(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var policy = await call.Service<PolicyService>().SetOvertimeAsync(
            call.Scope,
            new SetOvertimeThresholdCommand
            {
                DailyHours = call.Optional("daily")?.GetDecimal(),
                WeeklyHours = call.Optional("weekly")?.GetDecimal(),
            },
            cancellationToken);

        return new { version = policy.Version };
    }

    /// <summary>One leave type, as the table draws it.</summary>
    /// <remarks>
    /// <b>Numbers, and null where a type does not accrue</b> - ADR 0174. These
    /// were <c>"0.##"</c> through the machine's culture and carried their own
    /// units: <c>" / month"</c>, and an em-dash standing in for absence. The
    /// unit, the separator and what absence looks like are all the reader's,
    /// and a surface handed <c>"granted by HR"</c> cannot say it any other way.
    /// </remarks>
    private static object Type(LeaveType type) => new
    {
        type = type.Name,
        accruesPerMonth = type.AccrualPerMonth,
        perYear = type.AccrualPerMonth is { } rate ? rate * 12 : (decimal?)null,
        note = string.Empty,
    };



    /// <summary>The four times a shift may carry, as one command.</summary>
    /// <remarks>
    /// All four optional: an off shift has none, an ordinary shift has two, and
    /// a split has four. The service decides which combinations are legal —
    /// this only carries what was sent.
    /// </remarks>
    private static ShiftHoursCommand Hours(ModuleCall call) => new()
    {
        StartsAt = Time(call, "startsAt"),
        EndsAt = Time(call, "endsAt"),
        SecondStartsAt = Time(call, "secondStartsAt"),
        SecondEndsAt = Time(call, "secondEndsAt"),
    };

    /// <summary>An optional time on the wire.</summary>
    private static TimeOnly? Time(ModuleCall call, string field)
        => call.Optional(field) is { } value ? TimeOnly.Parse(value.GetString()!) : null;
}
