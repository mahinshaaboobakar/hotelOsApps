using HotelOS.Platform;

namespace HotelOS.GuestOps.Tests;

/// <summary>A clock the test moves by hand.</summary>
public sealed class ManualClock(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

/// <summary>Every event this service appended, in order.</summary>
/// <remarks>
/// <para>
/// A recording double rather than a real appender — ADR 0054: these tests own
/// behavioural coverage and are deliberately blind to the layers they do not
/// stand up. What they can assert exactly is <b>which facts were announced</b>,
/// which is most of what the rules here decide.
/// </para>
/// <para>
/// <b>Silence is an assertion too.</b> GUEST-Q4's silent confirmation and R7's
/// idempotent replay are both "nothing was published", and a double that only
/// counted successes could not tell them from a failure.
/// </para>
/// </remarks>
public sealed class RecordingAppender : IEventAppender
{
    public List<string> Types { get; } = [];

    public void Append<TPayload>(
        RequestScope scope,
        string eventType,
        string aggregateType,
        Guid aggregateId,
        long entityVersion,
        TPayload payload)
        => Types.Add(eventType);
}

/// <summary>An authorizer that says yes and remembers being asked.</summary>
public sealed class RecordingAuthorizer : IKernelAuthorizer
{
    public List<string> Permissions { get; } = [];

    public Task RequireAsync(
        RequestScope scope,
        string permission,
        string objectType,
        Guid objectId,
        CancellationToken cancellationToken)
    {
        Permissions.Add(permission);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<bool>> AllowedAsync(
        RequestScope scope,
        string permission,
        string objectType,
        IReadOnlyList<Guid> objectIds,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<bool>>([.. objectIds.Select(_ => true)]);
}
