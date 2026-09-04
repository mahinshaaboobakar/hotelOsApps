namespace HotelOS.Jobs.Application.Jobs;

/// <summary>What a caller sends to raise a job — frame 3, the guest app, or an application's event.</summary>
public sealed record RaiseJobCommand
{
    public required Guid ItemId { get; init; }

    public required Guid LocationId { get; init; }

    public Guid? AssetId { get; init; }

    public required string Summary { get; init; }

    public string? Details { get; init; }

    /// <summary>Set by hand: the manual link of the priority chain (S1 D4).</summary>
    public string? Priority { get; init; }

    /// <summary>Set by the flow that knows more — guest in room, PMS occupied.</summary>
    public string? FlowPriority { get; init; }

    public required string RaisedVia { get; init; }

    public required string RaisedKind { get; init; }

    public Guid? RaisedById { get; init; }

    public Guid? StayId { get; init; }

    /// <summary>Set means SCHEDULED until that day (S2 D3).</summary>
    public DateOnly? ScheduledFor { get; init; }

    public string? Cycle { get; init; }

    public bool? Restricted { get; init; }

    /// <summary>Pick a person now; null means AUTO on the execution date.</summary>
    public Guid? AssignToUserId { get; init; }

    public Guid? AssignToTeamId { get; init; }

    /// <summary>A child step of this parent (S1 D2).</summary>
    public Guid? ParentJobId { get; init; }
}

/// <summary>Assign or reassign — frame 2 Reassign, frame 3 Assign to.</summary>
public sealed record AssignCommand
{
    public required Guid JobId { get; init; }

    public required long ExpectedVersion { get; init; }

    public Guid? UserId { get; init; }

    public Guid? TeamId { get; init; }
}

/// <summary>Resolve — frame 4: one catalogue resolution, or "Other" with a note.</summary>
public sealed record ResolveCommand
{
    public required Guid JobId { get; init; }

    public required long ExpectedVersion { get; init; }

    public Guid? ResolutionId { get; init; }

    public string? Note { get; init; }
}

/// <summary>Put on hold with a reason and a date (S9 D2).</summary>
public sealed record HoldCommand
{
    public required Guid JobId { get; init; }

    public required long ExpectedVersion { get; init; }

    public required string Reason { get; init; }

    public DateTimeOffset? Until { get; init; }
}

/// <summary>End as CANCELLED with a reason; cascades to steps (S2, S1 D2).</summary>
public sealed record CancelCommand
{
    public required Guid JobId { get; init; }

    public required long ExpectedVersion { get; init; }

    public required string Reason { get; init; }
}

/// <summary>One of the course changes <c>job.amend</c> gates.</summary>
public sealed record AmendCommand
{
    public required Guid JobId { get; init; }

    public required long ExpectedVersion { get; init; }

    /// <summary>A new priority; the manual link of the chain.</summary>
    public string? Priority { get; init; }

    /// <summary>Reschedule; <c>Optional.Absent</c> leaves it, a null value clears it (raise now).</summary>
    public Optional<DateOnly?> ScheduledFor { get; init; } = Optional<DateOnly?>.Absent;

    public bool? Restricted { get; init; }

    public Guid? LinkJobId { get; init; }
}

/// <summary>A value that may be absent from a command, distinct from null.</summary>
public readonly record struct Optional<T>(bool IsPresent, T Value)
{
    public static Optional<T> Absent => new(false, default!);

    public static Optional<T> Of(T value) => new(true, value);
}
