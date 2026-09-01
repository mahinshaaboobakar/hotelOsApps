namespace HotelOS.Workforce.Application.Periods;

/// <summary>
/// One person's figures over one period — what payroll is handed.
/// </summary>
/// <param name="StaffId">Whose.</param>
/// <param name="From">First business day counted.</param>
/// <param name="To">Last business day counted.</param>
/// <param name="DaysPosted">Days the rota rostered them to work.</param>
/// <param name="DaysPresent">Days somebody recorded them arriving.</param>
/// <param name="DaysAbsent">Rostered to work, and no arrival recorded.</param>
/// <param name="LateCount">Days they arrived after the shift was due to start.</param>
/// <param name="HoursWorked">Actual, from attendance.</param>
/// <param name="OvertimeHours">Hours beyond the property's daily threshold.</param>
/// <param name="LeaveTakenByType">Approved leave days falling inside the period.</param>
/// <remarks>
/// <para>
/// <b>Workforce produces the numbers; it never calculates pay</b> — chapter 01
/// §3.7. Pay is a legal and compliance domain that differs by country (WPS, PF,
/// ESI) and by hotel, and building it wrong is a salary dispute. There is no
/// rate, no allowance and no deduction anywhere in this type, and that absence is
/// the design.
/// </para>
/// <para>
/// <b>Computed, never stored.</b> A period is a question asked of the rota,
/// attendance and the leave ledger — and every one of those can be corrected
/// after the fact. A stored total would be right until somebody fixed a
/// mispunched clock-out, which is the ordinary case rather than the exception.
/// </para>
/// <para>
/// <b>Holidays worked is not here, and its absence is a finding rather than an
/// omission.</b> §3.7 lists it; <c>WF-Q16</c> puts the holiday calendar in Core
/// Administration, which this application reads; and no such calendar exists
/// anywhere in the platform — no entity, no proto, no column. Inventing one here
/// would put a Core Administration concern in an installable application, which
/// is the boundary ADR 0051 exists for. Recorded as <c>F9</c>.
/// </para>
/// </remarks>
public sealed record WorkforcePeriod(
    Guid StaffId,
    DateOnly From,
    DateOnly To,
    int DaysPosted,
    int DaysPresent,
    int DaysAbsent,
    int LateCount,
    decimal HoursWorked,
    decimal OvertimeHours,
    IReadOnlyDictionary<Guid, decimal> LeaveTakenByType)
{
    /// <summary>Days present that the rota had not planned for.</summary>
    /// <remarks>
    /// Not one of §3.7's seven, and included because the period is what somebody
    /// checks before signing off a month: a day worked off-rota is exactly the
    /// row that needs a human to look at it, and leaving it out of the summary
    /// would mean finding it only by reading every day.
    /// </remarks>
    public int UnplannedDays { get; init; }
}
