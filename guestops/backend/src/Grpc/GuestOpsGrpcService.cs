using Google.Protobuf.WellKnownTypes;
using HotelOS.GuestOps.Application.Availability;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Application.Registrations;
using HotelOS.GuestOps.Application.Reporting;
using HotelOS.GuestOps.Application.Requests;
using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Grpc;

/// <summary>
/// The gRPC surface. Delegation and mapping only.
/// </summary>
/// <remarks>
/// <para>
/// CLAUDE.md §"No business logic in API routes": this layer parses a request,
/// hands it to a service, and maps the result back. Authorization and the rules
/// live in <c>Application</c>; the reads and the schema in
/// <c>Infrastructure</c>; the dependency points one way.
/// </para>
/// <para>
/// Errors are not caught here — <c>DomainExceptionInterceptor</c> maps them once
/// for every RPC, so a method that forgot cannot return the wrong status.
/// </para>
/// <para>
/// <b>This file holds no RPC.</b> It is the shared surface — the four services
/// the partials delegate to, and the conversions more than one of them needs
/// (ADR 0038, ADR 0042). One topic per file:
/// </para>
/// <list type="table">
///   <item><term>Bookings</term><description><see cref="BookingService"/></description></item>
///   <item><term>Stays</term><description><see cref="StayLifecycleService"/></description></item>
///   <item><term>Assignment</term><description><see cref="StayAssignmentService"/></description></item>
///   <item><term>Availability</term><description><see cref="AvailabilityService"/></description></item>
///   <item><term>Registration</term><description><see cref="RegistrationService"/></description></item>
///   <item><term>Reporting</term><description><see cref="ReportingService"/></description></item>
///   <item><term>Requests</term><description><see cref="StayRequestService"/></description></item>
///   <item><term>Settings</term><description><see cref="SettingsService"/></description></item>
/// </list>
/// </remarks>
public partial class GuestOpsGrpcService(
    BookingService bookings,
    StayLifecycleService lifecycle,
    StayAssignmentService assignment,
    StayListService stays,
    AvailabilityService availability,
    RegistrationService registrations,
    ReportingService reporting,
    StayRequestService requests,
    SettingsService settings) : GuestOpsService.GuestOpsServiceBase
{
    /// <summary>Correlation, echoed back.</summary>
    /// <remarks>
    /// The same id the caller sent, so a support conversation about one request
    /// has one identifier on both sides of the wire.
    /// <para>
    /// <b>Added because eight responses declared <c>meta</c> and nothing set
    /// any of them</b> — CORE-Q13's original complaint, one repository over: a
    /// declared field that is never populated is worse than an absent one,
    /// because a client reads the schema and believes the capability exists.
    /// </para>
    /// </remarks>
    private static HotelOS.Contracts.Common.V1.ResponseMeta Meta(
        HotelOS.Contracts.Common.V1.RequestContext? context)
        => new() { RequestId = context?.RequestId ?? string.Empty };

    // --- parsing ----------------------------------------------------------

    private static Guid ParseRequired(string value, string field)
        => Guid.TryParse(value, out var parsed) && parsed != Guid.Empty
            ? parsed
            : throw new InvalidRequestException($"{field} is required and must be a UUID");

    /// <summary>An ISO-8601 date, or a refusal to guess one.</summary>
    /// <remarks>
    /// Never defaulted to today. A booking whose arrival failed to parse is a
    /// request the caller got wrong, and silently booking tonight would be a
    /// stay nobody asked for.
    /// </remarks>
    private static DateOnly ParseDate(string value, string field)
        => DateOnly.TryParse(value, out var parsed)
            ? parsed
            : throw new InvalidRequestException($"{field} must be an ISO-8601 date");

    // --- outbound ---------------------------------------------------------

    /// <summary>Proto3 has no null: absent is the empty string.</summary>
    private static string Or(Guid? id) => id?.ToString() ?? string.Empty;

    private static string ToIso(DateOnly? date) => date?.ToString("yyyy-MM-dd") ?? string.Empty;

    /// <summary>An instant on the wire, or absent.</summary>
    private static string ToIso(DateTimeOffset? at) => at?.ToString("O") ?? string.Empty;

    /// <summary>An optional ISO date from the wire — empty means absent.</summary>
    /// <remarks>
    /// Never defaulted. An unparseable date on a registration card is refused
    /// rather than guessed: a wrong passport expiry is worse than a blank one.
    /// </remarks>
    private static DateOnly? OptionalDate(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParse(value, out var parsed)
            ? parsed
            : throw new InvalidRequestException($"{field} must be an ISO-8601 date");
    }

    /// <summary>Proto3 has no null: absent is the empty string.</summary>
    private static string? OrNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>A moment and its basis, together — never one without the other.</summary>
    private static Contracts.V1.StayTime ToProto(Domain.StayTime time)
        => new()
        {
            At = time.At is { } at ? Timestamp.FromDateTimeOffset(at) : null,
            Basis = (Contracts.V1.TimeBasis)(int)time.Basis,
        };

    private static Contracts.V1.RoomStay ToProto(Domain.RoomStay stay)
    {
        var message = new Contracts.V1.RoomStay
        {
            Id = stay.Id.ToString(),
            BookingId = stay.BookingId.ToString(),
            PropertyId = stay.PropertyId.ToString(),
            RoomTypeId = stay.RoomTypeId.ToString(),
            CurrentRoomId = Or(stay.CurrentRoomId),
            Lifecycle = (Contracts.V1.StayLifecycle)(int)stay.Lifecycle,
            ArrivalAt = ToProto(stay.ArrivalAt),
            DepartureAt = ToProto(stay.DepartureAt),
            BusinessDate = ToIso(stay.BusinessDate),
            WalkIn = stay.WalkIn,
            PmsUnknown = stay.PmsUnknown,
            Version = stay.Version,
        };

        foreach (var absence in stay.Absences)
        {
            message.Absences.Add(new Absence
            {
                Field = absence.Field,
                Reason = (AbsenceReason)(int)absence.Reason,
                RawValue = absence.RawValue ?? string.Empty,
            });
        }

        return message;
    }
}
