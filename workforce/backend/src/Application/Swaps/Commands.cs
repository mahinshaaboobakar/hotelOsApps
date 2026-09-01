namespace HotelOS.Workforce.Application.Swaps;

/// <summary>Ask a colleague to exchange shifts.</summary>
/// <remarks>
/// The two cells are named rather than the two people and two dates: a cell is
/// what exists, and naming a person and a day would ask this service to guess
/// which shift was meant when the answer is already a row.
/// </remarks>
public sealed record ProposeSwapCommand
{
    /// <summary>The proposer's cell.</summary>
    public required Guid ProposerAssignmentId { get; init; }

    /// <summary>The colleague's cell.</summary>
    public required Guid ColleagueAssignmentId { get; init; }

    /// <summary>Why, in the proposer's words.</summary>
    public string? Note { get; init; }
}

/// <summary>Accept, decline, approve or cancel a proposal.</summary>
/// <remarks>
/// One command for four steps, because they carry the same three things and
/// differ only in what they mean. Four near-identical records would drift apart
/// the first time one of them gained a field.
/// </remarks>
public sealed record DecideSwapCommand
{
    /// <summary>The proposal.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>What the decider wants recorded.</summary>
    public string? Note { get; init; }
}
