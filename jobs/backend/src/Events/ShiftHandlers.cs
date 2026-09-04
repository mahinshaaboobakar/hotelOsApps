using System.Text.Json.Serialization;
using HotelOS.Jobs.Application.Settings;
using HotelOS.Platform;

namespace HotelOS.Jobs.Events;

/// <summary>Workforce's fan-out when a department's shift begins — requested of Workforce, S7.</summary>
public sealed record ShiftStarted(
    [property: JsonPropertyName("department_code")] string DepartmentCode,
    [property: JsonPropertyName("on_shift")] int OnShift);

/// <summary>Workforce's fan-out when a department's shift ends.</summary>
public sealed record ShiftEnded([property: JsonPropertyName("department_code")] string DepartmentCode);

/// <summary>Marks the department present. Idempotent: a replayed start says the same thing.</summary>
public sealed class ShiftStartedHandler(PresenceService presence) : IEventHandler<ShiftStarted>
{
    public Task HandleAsync(RequestScope scope, ShiftStarted payload, EventEnvelope envelope, CancellationToken cancellationToken) =>
        presence.ShiftStartedAsync(scope.PropertyId, payload.DepartmentCode, payload.OnShift, envelope.OccurredAt, cancellationToken);
}

/// <summary>Marks the department absent, unless service hours still cover the moment.</summary>
public sealed class ShiftEndedHandler(PresenceService presence) : IEventHandler<ShiftEnded>
{
    public Task HandleAsync(RequestScope scope, ShiftEnded payload, EventEnvelope envelope, CancellationToken cancellationToken) =>
        presence.ShiftEndedAsync(scope.PropertyId, payload.DepartmentCode, envelope.OccurredAt, cancellationToken);
}
