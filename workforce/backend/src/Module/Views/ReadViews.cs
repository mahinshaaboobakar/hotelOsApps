using HotelOS.Platform;

namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// Which read answers which method — <c>roster.read</c>, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// One capability serves every screen's read because one permission governs
/// them: <i>"See the rota, the duty register, balances, attendance and the
/// month's numbers"</i> is a single decision an administrator makes, and
/// splitting it into nine would ask them nine times about one thing.
/// </para>
/// <para>
/// <b>A method this table does not know is a refusal, not an empty answer.</b>
/// It leaves as <c>invalid</c>, so a bundle asking for something that does not
/// exist is told so — where a null would have the screen draw its recorded
/// fixture and report itself live.
/// </para>
/// </remarks>
public static class ReadViews
{
    private static readonly IReadOnlyDictionary<
        string, Func<ModuleCall, CancellationToken, Task<object?>>> Answers =
        new Dictionary<string, Func<ModuleCall, CancellationToken, Task<object?>>>
        {
            ["people"] = PeopleView.Page,
            ["teams"] = TeamsView.List,
            ["week"] = RotaView.Week,
            ["schedule"] = ScheduleView.Month,
            ["leave"] = LeaveView.Board,
            ["day"] = AttendanceView.Day,
            ["register"] = DutyView.Register,
            ["month"] = ReportsView.Month,
            ["policy"] = PolicyView.Read,
        };

    /// <summary>Answer one read.</summary>
    public static Task<object?> Answer(ModuleCall call, CancellationToken cancellationToken)
        => Answers.TryGetValue(call.Method, out var answer)
            ? answer(call, cancellationToken)
            : throw new InvalidRequestException(call.Method + " is not a Workforce read");
}
