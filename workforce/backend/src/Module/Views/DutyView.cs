using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Calendar;
using HotelOS.Workforce.Application.Duties;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// The duty register — a week of Manager-on-Duty spans, and who holds it now.
/// </summary>
/// <remarks>
/// <para>
/// <b>A duty crosses midnight, so it belongs to two dates.</b> The register
/// draws a day band and a night band per day, and a 20:00–08:00 span appears in
/// the night band of the day it starts. Nothing here splits it into two spans:
/// a duty is one handover, and two rows would be two handovers.
/// </para>
/// <para>
/// <b>An uncovered night is a gap, not a blank.</b> Where no assignment covers
/// a band the row is present with a null holder — the screen draws "no MOD",
/// which is a fact somebody has to act on. Omitting the row would let a
/// property read an uncovered night as a night nobody had got to yet.
/// </para>
/// </remarks>
public static class DutyView
{
    /// <summary>One week of the register.</summary>
    public static async Task<object?> Register(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var duties = call.Service<DutyService>();
        var directory = call.Service<IStaffDirectory>();
        var clock = call.Service<TimeProvider>();

        // Days at the property, never UTC days — see PropertyCalendar.
        var calendar = await PropertyCalendar.ForAsync(
            directory, call.Scope.PropertyId, cancellationToken);

        var now = clock.GetUtcNow();

        // **Which week opens when nobody names one is *what day is it*, and
        // that is Context's answer** — ADR 0211. This was `calendar.DayOf(now)`:
        // the property's own wall clock, which is the right question for *has
        // 07:00 passed* and the wrong one for *which day are we on*. Left alone,
        // this view would keep answering from the calendar while every other
        // one answered from Context, and a property would have had two answers
        // to one question.
        //
        // `now` stays a real instant below: who holds the duty and who is next
        // are questions about a moment, not about a day.
        var anchor = call.Optional("week") is { } named
            ? DateOnly.Parse(named.GetString()!)
            : await PropertyDay.TodayAsync(call, cancellationToken);

        var monday = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
        var from = calendar.StartOf(monday);
        var to = calendar.StartOf(monday.AddDays(7));

        var week = await duties.ListAsync(call.Scope, from, to, cancellationToken);

        var holder = await duties.HolderAtAsync(call.Scope, now, cancellationToken);
        var next = await duties.NextAfterAsync(call.Scope, now, cancellationToken);

        var people = week.Select(one => one.StaffId)
            .Concat(holder is null ? [] : new[] { holder.StaffId })
            .Concat(next is null ? [] : new[] { next.StaffId })
            .Distinct()
            .ToList();

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, people, cancellationToken);

        return new
        {
            // **The seven days, as the wire carries them** — ADR 0175. This sent
            // a rendered range (`1 Sep – 7 Sep`) and seven rendered headers
            // (`ddd d`), both in the culture of whichever account the service
            // runs under. The screen composes the range from the first and the
            // last of these, so the dash and the word order are the screen's
            // and the days are the service's.
            days = Enumerable.Range(0, 7)
                .Select(offset => monday.AddDays(offset).ToString("yyyy-MM-dd"))
                .ToList(),
            now = Standing(holder, names),
            next = Standing(next, names),
            duties = Bands(week, monday, names, calendar),

            // **Who could hold it.** The dialog listed three people written
            // into the module — Anjali Menon, Rahul Nair, Vishnu Das, with
            // their roles and department codes — so a property saw the same
            // three strangers whoever it employed.
            //
            // Every active posting, because that is the rule the command
            // states: *any active staff member, from any department*. Nothing
            // narrower is invented here, and nothing narrower is written down.
            candidates = await Candidates(call, cancellationToken),
        };
    }

    /// <summary>Assign, amend, or take a duty off the register.</summary>
    public static Task<object?> Write(ModuleCall call, CancellationToken cancellationToken)
        => call.Method switch
        {
            "assign" => Assign(call, cancellationToken),
            "amend" => Amend(call, cancellationToken),
            "withdraw" => Withdraw(call, cancellationToken),
            _ => throw new InvalidRequestException(call.Method + " is not a duty method"),
        };

    /// <summary>Everybody who could hold a duty, as the picker lists them.</summary>
    /// <remarks>
    /// Ordered by name for the reader’s eye — no, ordered by staff id, because
    /// the ORDER a person reads is the module’s and the locale is on its side
    /// of the bridge. Same division as the department picker.
    /// </remarks>
    private static async Task<object[]> Candidates(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var postings = await call.Service<PostingService>().ListAsync(
            call.Scope, new ListPostingsQuery(), cancellationToken);

        var directory = call.Service<IStaffDirectory>();

        var people = postings.Select(one => one.StaffId).Distinct().ToList();

        var names = await directory.FindNamesAsync(
            call.Scope.PropertyId, people, cancellationToken);

        var departments = await directory.FindDepartmentNamesAsync(
            call.Scope.PropertyId, cancellationToken);

        return [.. postings
            .GroupBy(one => one.StaffId)
            .Select(group => group.OrderByDescending(one => one.IsPrimary).First())
            .OrderBy(one => one.StaffId)
            .Select(one => new
            {
                staffId = one.StaffId,
                name = names.TryGetValue(one.StaffId, out var found) ? found : null,
                role = one.JobRole,
                department = departments.TryGetValue(one.DepartmentCode, out var known)
                    ? known
                    : one.DepartmentCode,
            })];
    }

    private static async Task<object?> Assign(ModuleCall call, CancellationToken cancellationToken)
    {
        var duty = await call.Service<DutyService>().AssignAsync(
            call.Scope,
            new AssignDutyCommand
            {
                StaffId = call.Id("staffId"),
                StartsAt = Instant(call, "from"),
                EndsAt = Instant(call, "to"),
                HandoverNote = call.Optional("note")?.GetString(),
            },
            cancellationToken);

        return new { id = duty.Id, version = duty.Version };
    }

    private static async Task<object?> Amend(ModuleCall call, CancellationToken cancellationToken)
    {
        var duty = await call.Service<DutyService>().AmendAsync(
            call.Scope,
            new AmendDutyCommand
            {
                Id = call.Id("id"),
                ExpectedVersion = call.Required("version").GetInt64(),
                StaffId = call.Optional("staffId")?.GetGuid(),
                StartsAt = call.Optional("from") is null ? null : Instant(call, "from"),
                EndsAt = call.Optional("to") is null ? null : Instant(call, "to"),
            },
            cancellationToken);

        return new { id = duty.Id, version = duty.Version };
    }

    private static async Task<object?> Withdraw(
        ModuleCall call, CancellationToken cancellationToken)
    {
        await call.Service<DutyService>().WithdrawAsync(
            call.Scope,
            new WithdrawDutyCommand
            {
                Id = call.Id("id"),
                ExpectedVersion = call.Required("version").GetInt64(),
            },
            cancellationToken);

        return new { withdrawn = call.Id("id") };
    }

    /// <summary>Fourteen bands — a day and a night for each of seven days.</summary>
    private static List<object> Bands(
        IReadOnlyList<DutyAssignment> week,
        DateOnly monday,
        IReadOnlyDictionary<Guid, string> names,
        PropertyCalendar calendar)
    {
        var bands = new List<object>();

        for (var offset = 0; offset < 7; offset += 1)
        {
            var day = monday.AddDays(offset);

            foreach (var band in new[] { "day", "night" })
            {
                // Noon and 23:00: the instant inside each band that no ordinary
                // span can miss. Testing the band's edges instead would make a
                // 08:00 handover belong to both bands or to neither.
                //
                // **At the property.** These were UTC noon and 23:00, so a Kochi
                // night band was probed at 04:30 the next morning and read a
                // covered night as "no MOD".
                var probe = calendar.At(
                    day, band == "day" ? new TimeOnly(12, 0) : new TimeOnly(23, 0));

                var covering = week.FirstOrDefault(one => one.CoversAt(probe));

                bands.Add(new
                {
                    who = covering is null
                        ? null
                        : names.TryGetValue(covering.StaffId, out var name) ? name : null,
                    where = (string?)null,
                    // **The span as instants, never as rendered hours.** These
                    // are stored as `DateTimeOffset` and were being written out
                    // with `ToString("HH")`, which renders in the offset the row
                    // carries — UTC. A Kochi property would read 20:00 for a
                    // handover that happens at 01:30 its own time, and nothing
                    // on the screen would say which clock it meant.
                    from = covering?.StartsAt.ToString("O"),
                    to = covering?.EndsAt.ToString("O"),
                    day = offset,
                    band,
                });
            }
        }

        return bands;
    }

    /// <summary>Who holds it, and the span they hold it for.</summary>
    /// <remarks>
    /// The words are the screen's. This used to compose "since 20:00 · ends
    /// 08:00 tomorrow" here, which put three decisions in the service that only
    /// the reader's property can make: the clock, the locale, and whether the
    /// end is tomorrow — and "tomorrow" is a different day in two timezones.
    /// </remarks>
    private static object? Standing(
        DutyAssignment? duty, IReadOnlyDictionary<Guid, string> names)
    {
        if (duty is null)
        {
            return null;
        }

        var name = names.TryGetValue(duty.StaffId, out var found) ? found : null;

        // Master Data did not answer for this person. The band is still drawn —
        // the duty exists — but the card that names somebody is not, because it
        // would be naming nobody.
        return name is null
            ? null
            : new { who = name, from = duty.StartsAt.ToString("O"), to = duty.EndsAt.ToString("O") };
    }

    /// <summary>An instant on the wire, in the form the SDK's formatter reads.</summary>
    private static DateTimeOffset Instant(ModuleCall call, string field)
        => DateTimeOffset.Parse(call.Text(field)).ToUniversalTime();
}
