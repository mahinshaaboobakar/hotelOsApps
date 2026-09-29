using Google.Protobuf.WellKnownTypes;
using HotelOS.Formats;
using HotelOS.Jobs.Application.Assignment;
using HotelOS.Jobs.Application.Cancellation;
using HotelOS.Jobs.Application.Catalogue;
using HotelOS.Jobs.Application.Completion;
using HotelOS.Jobs.Application.Course;
using HotelOS.Jobs.Application.Jobs;
using HotelOS.Jobs.Application.Notes;
using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Application.Rating;
using HotelOS.Jobs.Application.Settings;
using HotelOS.Jobs.Application.Work;
using HotelOS.Jobs.Contracts.V1;
using HotelOS.Jobs.Domain;
using HotelOS.Platform;

namespace HotelOS.Jobs.Grpc;

/// <summary>
/// The gRPC surface — validate, delegate, map. Each partial holds one group of
/// verbs; this file composes and holds the parsing every RPC shares. A domain
/// failure becomes a status code in the interceptor, never here.
/// </summary>
public partial class JobsGrpcService(
    JobService jobs,
    AssignmentService assignment,
    WorkSessionService work,
    CompletionService completion,
    CancellationService cancellation,
    CourseService course,
    NoteService notes,
    RatingService rating,
    JobQueries queries,
    CatalogueService catalogue,
    PropertyCatalogueService propertyCatalogue,
    ConcernPolicyService concernPolicies,
    PresenceService presence)
    : JobsService.JobsServiceBase
{
    private static Guid ParseId(string raw, string field) =>
        Guid.TryParse(raw, out var id) ? id : throw new InvalidRequestException($"{field} must be a UUID");

    private static Guid? ParseOptionalId(string raw, string field) =>
        string.IsNullOrWhiteSpace(raw) ? null : ParseId(raw, field);

    private static DateOnly? ParseOptionalDate(string raw, string field) =>
        string.IsNullOrWhiteSpace(raw) ? null
        // ADR 0174 §NUM-Q4 — invariant, and `Iso8601.Day` is what the message
        // already promised. A pinned format does NOT pin the calendar: the old
        // TryParseExact answered TRUE and 1483 CE for "2026-09-23" under th-TH,
        // so this refused nothing and quietly returned a date 543 years out.
        : Iso8601.Day(raw) is { } day ? day
        : throw new InvalidRequestException($"{field} must be an ISO date");

    private static string? Blank(string raw) => string.IsNullOrWhiteSpace(raw) ? null : raw;

    private static DateTimeOffset? At(Timestamp? stamp) => stamp?.ToDateTimeOffset();

    /// <summary>A job's row after a write: re-read so the wire carries the derived columns.</summary>
    private async Task<JobView> RowAsync(RequestScope scope, Job job, CancellationToken cancellationToken) =>
        Views.Detail(await queries.DetailAsync(scope, job.Id, cancellationToken), queries.Now, scope.UserId).Job;
}
