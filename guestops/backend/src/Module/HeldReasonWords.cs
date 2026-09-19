using HotelOS.GuestOps.Domain;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Why a fact is held, in the words a person at the desk reads.
/// </summary>
/// <remarks>
/// Both the Attention card and the From the PMS widget sent
/// <c>HeldReason.ToString()</c> until 2026-09-19 — the enum's own name,
/// <c>CandidateLink</c>, on a staff screen.
/// <para>
/// <b>A new reason is not caught by the compiler</b> — a switch over an enum
/// needs its discard arm, so nothing fails when one is added. A value this
/// build has no words for is said as exactly that, rather than failing the
/// whole read or showing the enum's name.
/// </para>
/// </remarks>
public static class HeldReasonWords
{
    /// <summary>The reason, said.</summary>
    /// <param name="reason">Why the fact is held.</param>
    /// <returns>A sentence fragment for a person.</returns>
    public static string Said(HeldReason reason) => reason switch
    {
        HeldReason.CandidateLink => "May be a stay you already created",
        _ => "Held for a reason this version cannot describe",
    };
}
