using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
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

/// <summary>A property whose day and boundary a test states outright.</summary>
/// <remarks>
/// <para>
/// The port exists so a service under test can stand still at 03:59 without a
/// Context Service running — its own documentation says so — and this is that
/// double. The values are given per test rather than derived, because a stub
/// that computed the operating day would be a second implementation of the one
/// thing this application is forbidden to compute.
/// </para>
/// <para>
/// <c>bounds: null</c> is the property Context cannot answer for. It is a state
/// worth testing: the departures list must return nothing rather than guess a
/// window, and only a double can produce it on demand.
/// </para>
/// </remarks>
public sealed class StubBusinessDay(DateOnly? today, DayBounds? bounds = null) : IBusinessDay
{
    public Task<DateOnly?> CurrentAsync(RequestScope scope, CancellationToken cancellationToken)
        => Task.FromResult(today);

    public Task<StayTime> AtCheckInAsync(
        RequestScope scope, DateOnly date, CancellationToken cancellationToken)
        => Task.FromResult(StayTime.Observed(new DateTimeOffset(date.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero)));

    public Task<StayTime> AtCheckOutAsync(
        RequestScope scope, DateOnly date, CancellationToken cancellationToken)
        => Task.FromResult(StayTime.Observed(new DateTimeOffset(date.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero)));

    public Task<DayBounds?> BoundsAsync(
        RequestScope scope, DateOnly date, CancellationToken cancellationToken)
        => Task.FromResult(bounds);
}
