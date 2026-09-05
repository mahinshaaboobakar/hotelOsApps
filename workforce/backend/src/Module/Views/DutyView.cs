using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Duties;
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

        var now = clock.GetUtcNow();
        var anchor = call.Optional("week") is { } named
            ? DateOnly.Parse(named.GetString()!)
            : DateOnly.FromDateTime(now.UtcDateTime);

        var monday = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
        var from = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = from.AddDays(7);

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
            week = monday.ToString("d MMM") + " – " + monday.AddDays(6).ToString("d MMM"),
            days = Enumerable.Range(0, 7)
                .Select(offset => monday.AddDays(offset).ToString("ddd d"))
                .ToList(),
            now = Standing(holder, names),
            next = Standing(next, names),
            duties = Bands(week, monday, names),
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
        IReadOnlyDictionary<Guid, string> names)
    {
        var bands = new List<object>();

        for (var offset = 0; offset < 7; offset += 1)
        {
            var day = monday.AddDays(offset);

            foreach (var band in new[] { "day", "night" })
            {
                // Noon and midnight: the instant inside each band that no
                // ordinary span can miss. Testing the band's edges instead would
                // make a 08:00 handover belong to both bands or to neither.
                var probe = new DateTimeOffset(
                    day.ToDateTime(band == "day" ? new TimeOnly(12, 0) : new TimeOnly(23, 0)),
                    TimeSpan.Zero);

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
