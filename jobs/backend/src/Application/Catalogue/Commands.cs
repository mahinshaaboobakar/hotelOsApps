namespace HotelOS.Jobs.Application.Catalogue;

/// <summary>A new or changed category — frame 7 "＋ New".</summary>
public sealed record CategoryCommand
{
    public Guid? Id { get; init; }

    public long? ExpectedVersion { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string DepartmentCode { get; init; }

    public bool Active { get; init; } = true;
}

/// <summary>A new or changed item — frame 7's New-item dialog.</summary>
public sealed record ItemCommand
{
    public Guid? Id { get; init; }

    public long? ExpectedVersion { get; init; }

    public required Guid CategoryId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public string DefaultPriority { get; init; } = "P3";

    public int? DueWithinMinutes { get; init; }

    public bool RestrictedByDefault { get; init; }

    public bool GuestRequestable { get; init; } = true;

    public string PhotoOnCompletion { get; init; } = "OPTIONAL";

    public bool Active { get; init; } = true;

    /// <summary>Aliases to set — the whole list; absent means leave them.</summary>
    public IReadOnlyList<string>? Aliases { get; init; }
}

/// <summary>A resolution, added inline under an item or a category.</summary>
public sealed record ResolutionCommand
{
    public Guid? CategoryId { get; init; }

    public Guid? ItemId { get; init; }

    public required string Name { get; init; }

    public bool NoteRequired { get; init; }
}

/// <summary>A property's take on an item — frame 7's property tab (S1 D12).</summary>
public sealed record ItemPolicyCommand
{
    public required Guid ItemId { get; init; }

    public bool ActiveHere { get; init; } = true;

    public string? DisplayName { get; init; }

    public string? DefaultPriority { get; init; }

    public int? DueWithinMinutes { get; init; }

    public Guid? ConcernPolicyId { get; init; }

    public string AutoAssign { get; init; } = "USER";

    public Guid? AutoAssignTeamId { get; init; }
}
