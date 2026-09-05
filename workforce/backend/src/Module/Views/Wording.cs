namespace HotelOS.Workforce.Module.Views;

/// <summary>
/// How a domain value is said on a screen.
/// </summary>
/// <remarks>
/// <para>
/// Two conversions that more than one view needs and that belong to neither of
/// them: initials appear on a rota row, a team roll and a schedule header, and a
/// shift's colour becomes a tone wherever a shift is drawn.
/// </para>
/// <para>
/// A <b>domain-named</b> home rather than a helpers file — ADR 0042. What is
/// here is the module surface's vocabulary for saying things, and a conversion
/// that is not about saying something does not belong in it.
/// </para>
/// </remarks>
public static class Wording
{
    /// <summary>Two letters from a display name.</summary>
    /// <param name="name">What Master Data answered.</param>
    /// <returns>The initials an avatar shows.</returns>
    /// <remarks>
    /// Taken from the name and never from an id. A pair of letters derived from
    /// a UUID would be an identity this application invented for somebody, and
    /// it would look exactly as convincing as a real one.
    /// </remarks>
    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    /// <summary>The tone a shift's colour reads as.</summary>
    /// <param name="colour">The name the property chose for it.</param>
    /// <returns>One of the shell's tone words.</returns>
    /// <remarks>
    /// Mapped rather than passed through: the catalogue stores a colour a
    /// property typed, and the screen's palette is the shell's published tokens.
    /// A colour with no mapping is neutral, which is honest — the shift is drawn
    /// and simply carries no emphasis.
    /// </remarks>
    public static string Tone(string colour) => colour.ToUpperInvariant() switch
    {
        "CYAN" or "BLUE" or "INDIGO" => "brand",
        "EMERALD" or "GREEN" => "ok",
        "AMBER" or "ORANGE" => "warn",
        "RED" or "ROSE" => "bad",
        _ => "neutral",
    };
}
