using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Summaries;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// The five dock widgets — <c>SHELL-Q35</c>, and the same capability the
/// screens read under.
/// </summary>
/// <remarks>
/// <para>
/// <b>A widget is served by this application, through the same door.</b> It is
/// hosted in its own realm by the shell rather than by the module, but it is
/// handed the same <c>HostApi</c> and calls <c>host.call</c> over the same
/// bridge — so there is no second surface to design, and inventing one would
/// have been a second way for this application to answer the same question.
/// </para>
/// <para>
/// <b>A widget shows a figure and a short list, and the two count different
/// things.</b> The figure counts the property; the list holds what fits in a
/// popover of one size. Content that does not fit is cut by the widget rather
/// than by the shell, so the number beside a four-row list may legitimately say
/// six — and a figure recomputed from the rows on screen would quietly report
/// the popover's height as the property's state.
/// </para>
/// <para>
/// <b>Every row carries where it opens.</b> The tap-through is
/// <c>shell.open</c>'s argument and it is composed here, beside the filter it
/// describes: a widget that built its own destination would be a second place
/// where a department code turns into a screen.
/// </para>
/// </remarks>
public static class WidgetViews
{
    /// <summary>Who is on shift now, by department.</summary>
    public static async Task<object?> ShiftBoard(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var view = await call.Service<ShiftBoardSummary>()
            .ReadAsync(call.Scope, cancellationToken);

        return new
        {
            onNow = view.OnNow,
            departments = view.Departments,
            rows = view.Rows.Select(one => Row(
                one.DepartmentCode,
                null,
                one.OnNow,
                Form.Count,
                "muted",
                "rota?department=" + one.DepartmentCode,
                Wire.Clock(one.StartsAt),
                Wire.Clock(one.EndsAt))).ToList(),
            nextChange = view.NextChange is null ? null : new
            {
                // The instant, in the form `formatInstant` reads. The widget
                // renders it in the property's zone; a server that had already
                // chosen "15:00" would be asserting a timezone nobody
                // established here.
                at = view.NextChange.At.ToString("O"),
                on = view.NextChange.On,
                off = view.NextChange.Off,
            },
        };
    }

    /// <summary>Who was rostered today, and who came.</summary>
    public static async Task<object?> AttendanceToday(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var view = await call.Service<AttendanceTodaySummary>()
            .ReadAsync(call.Scope, cancellationToken);

        return new
        {
            figures = new[]
            {
                Figure(view.Present, "present", "ink", of: view.Rostered),
                Figure(view.Late, "late", view.Late == 0 ? "muted" : "warn"),
                Figure(view.Absent, "absent", view.Absent == 0 ? "muted" : "bad"),
            },
            // The proportion bar is three segments over the rostered total, so
            // it adds up to what was planned rather than to what happened.
            share = new[]
            {
                Segment(view.Present - view.Late, "ok"),
                Segment(view.Late, "warn"),
                Segment(view.Absent, "bad"),
            },
            byDepartment = view.ByDepartment.Select(one => Row(
                one.DepartmentCode,
                null,
                one.Absent,
                Form.Count,
                one.Absent == 0 ? "muted" : "bad",
                "attendance?department=" + one.DepartmentCode,
                context: Context(one.Rostered, "rostered"))).ToList(),
            lateIn = view.LateIn.Select(one => Row(
                one.Person.Name,
                one.DepartmentCode,
                (int)one.LateBy.TotalMinutes,
                Form.Minutes,
                "warn",
                "attendance?department=" + one.DepartmentCode)).ToList(),
        };
    }

    /// <summary>What is waiting on somebody.</summary>
    public static async Task<object?> PendingRequests(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var view = await call.Service<PendingRequestsSummary>()
            .ReadAsync(call.Scope, cancellationToken);

        return new
        {
            figures = new[]
            {
                Figure(view.Leave, "leave", view.Leave == 0 ? "muted" : "ink"),
                Figure(view.Swaps, "swaps", view.Swaps == 0 ? "muted" : "ink"),
            },
            rows = view.Rows.Select(one => Row(
                one.Raiser.Name,
                one.Colleague?.Name is { } colleague
                    ? one.DepartmentCode + " · with " + colleague
                    : one.DepartmentCode,
                one.WaitingDays,
                Form.Days,
                one.WaitingDays >= 3 ? "warn" : "muted",
                "leave?department=" + one.DepartmentCode)).ToList(),
        };
    }

    /// <summary>What is about to need somebody's attention.</summary>
    public static async Task<object?> ComingUp(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var view = await call.Service<ComingUpSummary>().ReadAsync(call.Scope, cancellationToken);

        // The approved frame names the department in words — "Housekeeping",
        // not "HK" — because a card read at a glance should not ask a reader to
        // expand a code.
        var departments = await call.Service<IStaffDirectory>()
            .FindDepartmentNamesAsync(call.Scope.PropertyId, cancellationToken);

        return new
        {
            figures = new[]
            {
                // The approved frame's words. These said "overlaps" and
                // "expiring" — an implementation choice nobody ruled — while
                // the widget's own test and the owner's canvas both read
                // "overlapping leave" and "certs expiring".
                Figure(view.OverlappingLeave, "overlapping leave",
                    view.OverlappingLeave == 0 ? "muted" : "warn"),
                Figure(view.CertsExpiring, "certs expiring",
                    view.CertsExpiring == 0 ? "muted" : "warn"),
            },
            overlaps = view.Overlaps.Select(one => Dated(
                departments.TryGetValue(one.DepartmentCode, out var named)
                    ? named
                    : one.DepartmentCode,
                one.On.ToString("yyyy-MM-dd"),
                Context(one.Away, "away"),
                one.Posted,
                Form.OutOf,
                "warn",
                "leave?department=" + one.DepartmentCode)).ToList(),
            expiring = view.Expiring.Select(one => Row(
                one.Capability + " · " + one.Person.Name,
                null,
                one.InDays,
                Form.Days,
                one.InDays <= 7 ? "bad" : "warn",
                "people?capability=expiring")).ToList(),
        };
    }

    /// <summary>Who is away.</summary>
    public static async Task<object?> OnLeave(ModuleCall call, CancellationToken cancellationToken)
    {
        var view = await call.Service<OnLeaveSummary>().ReadAsync(call.Scope, cancellationToken);

        return new
        {
            figures = new[]
            {
                Figure(view.AwayToday, "away today",
                    view.AwayToday == 0 ? "muted" : "ink"),
                Figure(view.AwayThisWeek, "this week",
                    view.AwayThisWeek == 0 ? "muted" : "muted"),
            },
            today = view.Today.Select(Away).ToList(),
            restOfWeek = view.RestOfWeek.Select(Away).ToList(),
        };
    }

    /// <summary>One department's absence, named by whoever is in it.</summary>
    private static object Away(DepartmentAway away) => Row(
        away.DepartmentCode,
        // The people, where Master Data answered for them. An id would be an
        // identifier on a card that has room for a name and nothing else.
        string.Join(", ", away.People.Select(one => one.Name).Where(one => one is not null)),
        away.People.Count,
        Form.Count,
        "muted",
        "leave?department=" + away.DepartmentCode);

    /// <summary>
    /// How a row's number is written — the widget writes it, in the property's
    /// digits (NUM-Q1, ADR 0174). The words are the widget's too.
    /// </summary>
    /// <remarks>
    /// These were strings composed here — <c>"22 min"</c>, <c>"5d"</c>,
    /// <c>"of 5"</c> — in whatever culture this service runs under. The number
    /// travels now, and this says which of the drawing's forms it takes.
    /// </remarks>
    private static class Form
    {
        public const string Count = "count";
        public const string Minutes = "minutes";
        public const string Days = "days";
        public const string OutOf = "out-of";
    }

    /// <summary>A number that qualifies a row — "7 rostered", "3 away".</summary>
    private static object Context(int count, string word) => new { count, word };

    /// <summary>A row about a particular day — the day travels as ISO.</summary>
    private static object Dated(
        string name, string on, object context, int value, string form, string tone, string opens)
        => new { name, on, meta = (string?)null, context, value, form, tone, opens };

    /// <summary>One row of a widget's list.</summary>
    /// <remarks>
    /// <c>meta</c> is text only now; a number that used to live inside it
    /// travels as <c>context</c>, which the widget writes.
    /// </remarks>
    private static object Row(
        string? name,
        string? meta,
        int value,
        string form,
        string tone,
        string opens,
        string? from = null,
        string? to = null,
        object? context = null)
        => new
        {
            name,
            meta = string.IsNullOrWhiteSpace(meta) ? null : meta,
            context,
            value,
            form,
            tone,
            opens,

            // The two ends, carried rather than joined — ADR 0175, and the same
            // shape `on` already uses for a date. The panel that knows its rows
            // are spans composes them; every other widget leaves these null.
            from,
            to,
        };

    /// <summary>One of a card's headline numbers, and what it is out of where it is.</summary>
    /// <remarks>
    /// The number, not the text. This was documented as <i>"already formatted —
    /// a widget never computes one"</i>, which put every figure in the service's
    /// culture. <c>of</c> is the "of 6" in "5 of 6", null where there is none.
    /// </remarks>
    private static object Figure(int count, string label, string tone, int? of = null)
        => new { count, of, label, tone };

    /// <summary>One band of the proportion bar.</summary>
    private static object Segment(int count, string tone) => new { count = Math.Max(0, count), tone };
}
