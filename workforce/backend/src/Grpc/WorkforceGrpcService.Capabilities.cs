using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HotelOS.Platform;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Contracts.V1;
using HotelOS.Workforce.Domain;

// Two `ExpiryBand`s exist and both are correct: the domain's is the judgment,
// the contract's is how it crosses a wire. Aliased rather than renamed, because
// renaming either would make the mapping below read as a translation between two
// different ideas instead of the same one in two places.
using DomainBand = HotelOS.Workforce.Domain.ExpiryBand;
using WireBand = HotelOS.Workforce.Contracts.V1.ExpiryBand;

namespace HotelOS.Workforce.Grpc;

/// <summary>Capability and compliance — the RPCs, and their wire conversion.</summary>
/// <remarks>
/// One file, one subject (ADR 0038). Its own conversion, because
/// <see cref="CapabilityView"/> has one caller and a "shared" file holding a
/// one-caller helper is a <c>helpers.cs</c> that has not been named yet.
/// </remarks>
public partial class WorkforceGrpcService
{
    /// <inheritdoc />
    public override async Task<CapabilityView> RecordCapability(
        RecordCapabilityRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var capability = await capabilities.RecordAsync(
            scope,
            new RecordCapabilityCommand
            {
                StaffId = ParseId(request.StaffId, "staff_id"),
                Name = request.Name,
                ValidUntil = ParseOptionalDate(request.ValidUntil, "valid_until"),
                Note = request.Note,
            },
            context.CancellationToken);

        return ToView(capability);
    }

    /// <inheritdoc />
    public override async Task<CapabilityView> AmendCapability(
        AmendCapabilityRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var capability = await capabilities.AmendAsync(
            scope,
            new AmendCapabilityCommand
            {
                Id = ParseId(request.Id, "id"),
                ExpectedVersion = request.ExpectedVersion,
                Name = request.HasName ? request.Name : null,
                Note = request.HasNote ? request.Note : null,

                // Three outcomes from one field: absent leaves the expiry,
                // present-with-a-date renews it, present-and-empty turns a
                // certification back into an ability. A plain string could carry
                // only two of the three.
                ValidUntil = request.HasValidUntil
                    ? Optional<DateOnly?>.Of(
                        ParseOptionalDate(request.ValidUntil, "valid_until"))
                    : Optional<DateOnly?>.Absent,
            },
            context.CancellationToken);

        return ToView(capability);
    }

    /// <inheritdoc />
    public override async Task<RemoveCapabilityResponse> RemoveCapability(
        RemoveCapabilityRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        await capabilities.RemoveAsync(
            scope,
            new RemoveCapabilityCommand
            {
                Id = ParseId(request.Id, "id"),
                ExpectedVersion = request.ExpectedVersion,
            },
            context.CancellationToken);

        return new RemoveCapabilityResponse();
    }

    /// <inheritdoc />
    public override async Task<ListCapabilitiesResponse> ListCapabilities(
        ListCapabilitiesRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var found = await capabilities.ListAsync(
            scope,
            new ListCapabilitiesQuery { StaffId = ParseOptionalId(request.StaffId, "staff_id") },
            context.CancellationToken);

        return Response(found);
    }

    /// <inheritdoc />
    public override async Task<ListCapabilitiesResponse> ListAttention(
        ListAttentionRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));

        var found = await capabilities.AttentionAsync(
            scope,
            new AttentionQuery
            {
                DepartmentCode = string.IsNullOrWhiteSpace(request.DepartmentCode)
                    ? null
                    : request.DepartmentCode,
            },
            context.CancellationToken);

        return Response(found);
    }

    /// <inheritdoc />
    public override async Task<ListCapabilitiesResponse> CertificationRegister(
        CertificationRegisterRequest request, ServerCallContext context)
    {
        var scope = request.Context.ToScope(CallerContext.Get(context));
        var found = await capabilities.RegisterAsync(scope, context.CancellationToken);

        return Response(found);
    }

    private ListCapabilitiesResponse Response(IReadOnlyList<Capability> found)
    {
        var response = new ListCapabilitiesResponse();
        response.Capabilities.AddRange(found.Select(ToView));

        return response;
    }

    private CapabilityView ToView(Capability capability) => new()
    {
        Id = capability.Id.ToString(),
        PropertyId = capability.PropertyId.ToString(),
        StaffId = capability.StaffId.ToString(),
        Name = capability.Name,
        ValidUntil = capability.ValidUntil?.ToString("O") ?? string.Empty,
        Note = capability.Note,

        // Computed here from the service's clock rather than by the caller, so
        // two screens reading one row on one day cannot disagree about whether
        // it has expired.
        Band = capabilities.BandOf(capability) switch
        {
            DomainBand.Valid => WireBand.Valid,
            DomainBand.Within60Days => WireBand.Within60Days,
            DomainBand.Within30Days => WireBand.Within30Days,
            DomainBand.Within7Days => WireBand.Within7Days,
            DomainBand.Expired => WireBand.Expired,
            _ => WireBand.DoesNotLapse,
        },

        CreatedAt = Timestamp.FromDateTimeOffset(capability.CreatedAt),
        UpdatedAt = Timestamp.FromDateTimeOffset(capability.UpdatedAt),
        Version = capability.Version,
    };

    /// <summary>An ISO date, or none.</summary>
    /// <remarks>
    /// Empty is not a failure here — it is the ordinary case, and it means this
    /// capability is an ability. A malformed date is a failure, because silently
    /// treating "12/03/27" as "no expiry" would put a certification on nobody's
    /// Attention list.
    /// </remarks>
    private static DateOnly? ParseOptionalDate(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParse(value, out var date)
            ? date
            : throw new InvalidRequestException($"{field} must be an ISO date (YYYY-MM-DD)");
    }
}
