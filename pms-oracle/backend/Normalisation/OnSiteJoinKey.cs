namespace PmsOracle.Normalisation;

/// <summary>
/// What the two halves of an on-site check-in are paired on.
/// </summary>
/// <param name="Surname">Family name, as the agent sent it.</param>
/// <param name="FirstName">Given name, as the agent sent it.</param>
/// <param name="ArrivalDate">The arrival date, as a date rather than a string.</param>
/// <remarks>
/// <para>
/// <b>This is entity resolution by name, and it is not a design choice.</b> The
/// on-site agent sends a check-in as two messages, one carrying contact details
/// and one carrying the room (R6), and it supplies no correlation identifier
/// for them — the reservation id is absent from at least one half. Three fields
/// is what there is.
/// </para>
/// <para>
/// The risk is real and worth stating where the code is: two guests with the
/// same name arriving the same day at the same property would join wrongly, and
/// a wrong join merges two stays. It is narrowed by the property and the
/// arrival date, and it is bounded by the pending-join window rather than left
/// open indefinitely — but it is not eliminated, and no arrangement of these
/// three fields eliminates it.
/// </para>
/// <para>
/// The reference did the same correlation with a Mongo query written inline at
/// the call site, over its own private copy of the data. Naming it as a type
/// does not make it safer; it makes it visible, testable, and something a
/// future connector version can replace the moment the agent offers a better
/// key.
/// </para>
/// </remarks>
public readonly record struct OnSiteJoinKey(
    string Surname,
    string FirstName,
    DateOnly ArrivalDate)
{
    /// <summary>
    /// Build a join key, if the message carries all three parts.
    /// </summary>
    /// <param name="surname">The <c>Surname</c> field.</param>
    /// <param name="firstName">The <c>FirstName</c> field.</param>
    /// <param name="arrivalDate">The parsed arrival date.</param>
    /// <returns>The key, or <c>null</c> when a part is missing.</returns>
    /// <remarks>
    /// A key with a blank name would match every other message with a blank
    /// name — which is to say it would join unrelated guests. Refusing to build
    /// one is what keeps that from being expressible.
    /// </remarks>
    public static OnSiteJoinKey? For(string? surname, string? firstName, DateOnly? arrivalDate)
    {
        if (string.IsNullOrWhiteSpace(surname)
            || string.IsNullOrWhiteSpace(firstName)
            || arrivalDate is null)
        {
            return null;
        }

        return new OnSiteJoinKey(surname.Trim(), firstName.Trim(), arrivalDate.Value);
    }
}
