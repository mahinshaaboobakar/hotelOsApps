using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HotelOS.Platform;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Contracts.V1;
using HotelOS.Workforce.Domain;

namespace HotelOS.Workforce.Grpc;

/// <summary>Postings — the RPCs, and the wire conversion they need.</summary>
/// <remarks>
/// One file, one subject (ADR 0038). The conversion below is here rather than in
/// a shared file because it has exactly one caller; a "shared" file holding a
/// one-caller helper is a <c>helpers.cs</c> that has not been named yet.
/// </remarks>
public partial class WorkforceGrpcService
{
    /// <inheritdoc />
    public override async Task<PostingView> CreatePosting(
        CreatePostingRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var posting = await postings.CreateAsync(
            scope,
            new CreatePostingCommand
            {
                StaffId = ParseId(request.StaffId, "staff_id"),
                DepartmentCode = request.DepartmentCode,
                JobRole = request.JobRole,
                IsPrimary = request.IsPrimary,
                IsDepartmentHead = request.IsDepartmentHead,
                ZoneId = ParseOptionalId(request.ZoneId, "zone_id"),
                ReportingManagerStaffId = ParseOptionalId(
                    request.ReportingManagerStaffId, "reporting_manager_staff_id"),
                EffectiveFrom = ParseDate(request.EffectiveFrom, "effective_from"),
            },
            context.CancellationToken);

        return ToView(posting);
    }

    /// <inheritdoc />
    public override async Task<PostingView> UpdatePosting(
        UpdatePostingRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var posting = await postings.UpdateAsync(
            scope,
            new UpdatePostingCommand
            {
                Id = ParseId(request.Id, "id"),
                ExpectedVersion = request.ExpectedVersion,
                JobRole = request.HasJobRole ? request.JobRole : null,
                IsPrimary = request.HasIsPrimary ? request.IsPrimary : null,
                IsDepartmentHead = request.HasIsDepartmentHead ? request.IsDepartmentHead : null,

                // `optional` in proto3 gives a real presence bit, which is what
                // lets "clear the zone" and "leave the zone alone" be different
                // requests. Collapsing them into one nullable field would make
                // the ambiguous state expressible on the wire.
                ZoneId = request.HasZoneId
                    ? Optional<Guid?>.Of(ParseOptionalId(request.ZoneId, "zone_id"))
                    : Optional<Guid?>.Absent,
                ReportingManagerStaffId = request.HasReportingManagerStaffId
                    ? Optional<Guid?>.Of(ParseOptionalId(
                        request.ReportingManagerStaffId, "reporting_manager_staff_id"))
                    : Optional<Guid?>.Absent,
            },
            context.CancellationToken);

        return ToView(posting);
    }

    /// <inheritdoc />
    public override async Task<PostingView> EndPosting(
        EndPostingRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var posting = await postings.EndAsync(
            scope,
            new EndPostingCommand
            {
                Id = ParseId(request.Id, "id"),
                ExpectedVersion = request.ExpectedVersion,
                EffectiveTo = ParseDate(request.EffectiveTo, "effective_to"),
            },
            context.CancellationToken);

        return ToView(posting);
    }

    /// <inheritdoc />
    public override async Task<PostingView> GetPosting(
        GetPostingRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));
        var posting = await postings.GetAsync(
            scope, ParseId(request.Id, "id"), context.CancellationToken);

        return ToView(posting);
    }

    /// <inheritdoc />
    public override async Task<ListPostingsResponse> ListPostings(
        ListPostingsRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var found = await postings.ListAsync(
            scope,
            new ListPostingsQuery
            {
                StaffId = ParseOptionalId(request.StaffId, "staff_id"),
                DepartmentCode = string.IsNullOrWhiteSpace(request.DepartmentCode)
                    ? null
                    : request.DepartmentCode,
                ZoneId = ParseOptionalId(request.ZoneId, "zone_id"),
                IncludeEnded = request.IncludeEnded,
            },
            context.CancellationToken);

        var response = new ListPostingsResponse();
        response.Postings.AddRange(found.Select(ToView));
        return response;
    }

    private static PostingView ToView(Posting posting) => new()
    {
        Id = posting.Id.ToString(),
        PropertyId = posting.PropertyId.ToString(),
        StaffId = posting.StaffId.ToString(),
        DepartmentCode = posting.DepartmentCode,
        JobRole = posting.JobRole,
        IsPrimary = posting.IsPrimary,
        IsDepartmentHead = posting.IsDepartmentHead,
        ZoneId = posting.ZoneId?.ToString() ?? string.Empty,
        ReportingManagerStaffId = posting.ReportingManagerStaffId?.ToString() ?? string.Empty,
        EffectiveFrom = posting.EffectiveFrom.ToString("O"),
        EffectiveTo = posting.EffectiveTo?.ToString("O") ?? string.Empty,
        CreatedAt = Timestamp.FromDateTimeOffset(posting.CreatedAt),
        UpdatedAt = Timestamp.FromDateTimeOffset(posting.UpdatedAt),
        Version = posting.Version,
    };

    private static Guid ParseId(string value, string field) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id
            : throw new InvalidRequestException($"{field} is required and must be a UUID");

    private static Guid? ParseOptionalId(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id
            : throw new InvalidRequestException($"{field} must be a UUID when present");
    }

    /// <summary>An ISO date, in the property's own day rather than a timestamp.</summary>
    /// <remarks>
    /// A posting starts on a <i>day</i>, not at an instant: "from the 1st of
    /// September" means the same thing in every timezone the property is not in.
    /// Sending a timestamp would make the start depend on where the caller is
    /// standing.
    /// </remarks>
    private static DateOnly ParseDate(string value, string field) =>
        DateOnly.TryParse(value, out var date)
            ? date
            : throw new InvalidRequestException($"{field} must be an ISO date (YYYY-MM-DD)");
}
