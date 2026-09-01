namespace HotelOS.Workforce.Domain;

/// <summary>
/// How close a capability is to lapsing — the Attention list's vocabulary.
/// </summary>
/// <remarks>
/// <para>
/// A computed answer, never a column. It is a separate type from
/// <see cref="Capability"/> because it is a <i>judgment about</i> a capability
/// on a particular day, and the capability itself is the same record on every
/// day — ADR 0038's one-file-one-purpose applied to a vocabulary rather than to
/// a length.
/// </para>
/// <para>
/// The three warning bands are the ruling's <b>60 / 30 / 7</b>. They are
/// ordered so that a caller can say *"anything at or above
/// <see cref="Within60Days"/>"* and mean the Attention list, without knowing
/// which band a given row landed in.
/// </para>
/// </remarks>
public enum ExpiryBand
{
    /// <summary>An ability. There is nothing to renew.</summary>
    /// <remarks>
    /// First and zero, because it is the ordinary case: most capabilities are
    /// abilities, and an enum whose default value is a warning state would put
    /// every unset field on somebody's Attention list.
    /// </remarks>
    DoesNotLapse = 0,

    /// <summary>Lapses, but not soon enough to say anything about.</summary>
    Valid = 1,

    /// <summary>Two months out. The first time it is mentioned.</summary>
    Within60Days = 2,

    /// <summary>A month out.</summary>
    Within30Days = 3,

    /// <summary>A week out. The last warning before it is a problem.</summary>
    Within7Days = 4,

    /// <summary>The day has passed.</summary>
    /// <remarks>
    /// <b>It blocks nothing</b> — the rota warns and names it, and a person
    /// decides. `WF-Q16`: the platform refuses the physically impossible and
    /// warns on a judgment, and somebody with a lapsed certificate can
    /// physically work the shift. Whether they should is the hotel's call, and
    /// our job is that nobody can say *"we didn't know"*.
    /// </remarks>
    Expired = 5,
}
