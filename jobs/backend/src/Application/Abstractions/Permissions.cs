namespace HotelOS.Jobs.Application.Abstractions;

/// <summary>
/// The eight permissions this application requests — design §4.1, ruled by the
/// architect on 2026-09-04: concrete verbs in the registry's own idiom, never a
/// tier.
/// </summary>
public static class Permissions
{
    /// <summary>Open the board and a job in my department; my own jobs anywhere.</summary>
    public const string Read = "job.read";

    /// <summary>Raise a job, now or for a day.</summary>
    public const string Create = "job.create";

    /// <summary>Assign or reassign to a person or a team.</summary>
    public const string Assign = "job.assign";

    /// <summary>Resolve, close, reopen inside the window.</summary>
    public const string Complete = "job.complete";

    /// <summary>End a job as CANCELLED with a reason.</summary>
    public const string Cancel = "job.cancel";

    /// <summary>Hold, reschedule, re-prioritise, restrict, link, add a step.</summary>
    public const string Amend = "job.amend";

    /// <summary>This property's policies, presence and closing rules.</summary>
    public const string Configure = "job.configure";

    /// <summary>The organisation's catalogue.</summary>
    public const string Curate = "job.curate";
}
