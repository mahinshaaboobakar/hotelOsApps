namespace HotelOS.Workforce.Application.Assignment;

/// <summary>What is worth telling a manager before they fill a cell.</summary>
/// <remarks>
/// Every one of these is a <b>warning</b>. <c>WF-Q16</c> draws the line: the
/// platform refuses the physically impossible and warns on a judgment, and every
/// case here is a judgment a hotel makes daily — covering a shift outside your
/// department, working while a certificate has lapsed, being called in on
/// approved leave. A system that refused them would be overruled with a
/// spreadsheet.
/// </remarks>
public enum AdviceKind
{
    /// <summary>They have approved leave covering this day.</summary>
    OnApprovedLeave = 0,

    /// <summary>They have requested leave covering this day, undecided.</summary>
    LeaveRequested = 1,

    /// <summary>They hold no open posting to the department this cell is for.</summary>
    NotPostedToDepartment = 2,

    /// <summary>A certification of theirs has expired.</summary>
    CertificationExpired = 3,

    /// <summary>A certification expires within the horizon.</summary>
    CertificationExpiring = 4,

    /// <summary>They are already rostered that day.</summary>
    AlreadyRostered = 5,
}

/// <summary>One thing worth knowing before assigning.</summary>
/// <param name="Kind">Which.</param>
/// <param name="Detail">What to show — the specifics, never just the fact.</param>
/// <remarks>
/// <b>The detail carries the number or the name</b>, for the reason the overtime
/// warning does: <i>"Anjali has a certification issue"</i> tells a manager
/// nothing they can act on; <i>"Fire warden expired 12 Mar"</i> tells them
/// whether it matters for this shift.
/// </remarks>
public sealed record Advice(AdviceKind Kind, string Detail);
