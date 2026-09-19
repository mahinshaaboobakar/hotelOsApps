namespace HotelOS.Jobs.Application;

/// <summary>
/// A value in the words of a sentence a person reads — a refusal is shown on the
/// screen as the service wrote it.
/// </summary>
/// <remarks>
/// A refusal that printed a job's status as the wire carries it read "MRN-ENG-142
/// is IN_PROGRESS and cannot be held"; with this it reads "… is in progress …".
/// <c>RefusalWordsGuardTests</c> allows a hole through <c>Said</c> and refuses a
/// raw one (owner, 2026-09-19; KK's Room Care finding the same day).
/// </remarks>
public static class Said
{
    /// <summary><c>IN_PROGRESS</c> → "in progress".</summary>
    public static string Status(string status) => status.ToLowerInvariant().Replace('_', ' ');
}
