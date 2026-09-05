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
                Span(one.StartsAt, one.EndsAt),
                one.OnNow.ToString(),
                "muted",
                "rota?department=" + one.DepartmentCode)).ToList(),
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
                Figure(view.Present + " of " + view.Rostered, "present", "ink"),
                Figure(view.Late.ToString(), "late", view.Late == 0 ? "muted" : "warn"),
                Figure(view.Absent.ToString(), "absent", view.Absent == 0 ? "muted" : "bad"),
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
                one.Rostered + " rostered",
                one.Absent.ToString(),
                one.Absent == 0 ? "muted" : "bad",
                "attendance?department=" + one.DepartmentCode)).ToList(),
            lateIn = view.LateIn.Select(one => Row(
                one.Person.Name,
                one.DepartmentCode,
                (int)one.LateBy.TotalMinutes + " min",
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
                Figure(view.Leave.ToString(), "leave", view.Leave == 0 ? "muted" : "ink"),
                Figure(view.Swaps.ToString(), "swaps", view.Swaps == 0 ? "muted" : "ink"),
            },
            rows = view.Rows.Select(one => Row(
                one.Raiser.Name,
                one.Colleague?.Name is { } colleague
                    ? one.DepartmentCode + " · with " + colleague
                    : one.DepartmentCode,
                one.WaitingDays + "d",
                one.WaitingDays >= 3 ? "warn" : "muted",
                "leave?department=" + one.DepartmentCode)).ToList(),
        };
    }

    /// <summary>What is about to need somebody's attention.</summary>
    public static async Task<object?> ComingUp(
        ModuleCall call, CancellationToken cancellationToken)
    {
        var view = await call.Service<ComingUpSummary>().ReadAsync(call.Scope, cancellationToken);

        return new
        {
            figures = new[]
            {
                Figure(view.OverlappingLeave.ToString(), "overlaps",
                    view.OverlappingLeave == 0 ? "muted" : "warn"),
                Figure(view.CertsExpiring.ToString(), "expiring",
                    view.CertsExpiring == 0 ? "muted" : "warn"),
            },
            overlaps = view.Overlaps.Select(one => Row(
                one.DepartmentCode,
                one.On.ToString("O"),
                one.Away + " of " + one.Posted,
                "warn",
                "leave?department=" + one.DepartmentCode)).ToList(),
            expiring = view.Expiring.Select(one => Row(
                one.Person.Name,
                one.Capability,
                one.InDays + "d",
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
                Figure(view.AwayToday.ToString(), "away today",
                    view.AwayToday == 0 ? "muted" : "ink"),
                Figure(view.AwayThisWeek.ToString(), "this week",
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
        away.People.Count.ToString(),
        "muted",
        "leave?department=" + away.DepartmentCode);

    /// <summary>One row of a widget's list.</summary>
    private static object Row(string? name, string? meta, string value, string tone, string opens)
        => new
        {
            name,
            meta = string.IsNullOrWhiteSpace(meta) ? null : meta,
            value,
            tone,
            opens,
        };

    /// <summary>One of a card's headline numbers.</summary>
    private static object Figure(string value, string label, string tone)
        => new { value, label, tone };

    /// <summary>One band of the proportion bar.</summary>
    private static object Segment(int count, string tone) => new { count = Math.Max(0, count), tone };

    /// <summary>"07:00–15:00", the property's own wall clock.</summary>
    /// <remarks>
    /// A shift's hours are clock times rather than instants: they have no date
    /// and no zone, because a Morning shift starts at 07:00 wherever the
    /// property is. Rendering them through an instant formatter would attach a
    /// timezone to something that never had one.
    /// </remarks>
    private static string Span(TimeOnly from, TimeOnly to)
        => from.ToString("HH:mm") + "–" + to.ToString("HH:mm");
}
