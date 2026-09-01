using Grpc.Core;
using HotelOS.GuestOps.Application.Settings;
using HotelOS.GuestOps.Contracts.V1;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Grpc;

/// <summary>This application's own configuration — section 2.8.</summary>
/// <remarks>
/// <b>Reading and writing take different permissions.</b> A receptionist must
/// see the form they have to fill in without being able to change what it
/// demands, so the read asks for <c>reservation.read</c> and only the write asks
/// for <c>desk.configure</c>.
/// </remarks>
public partial class GuestOpsGrpcService
{
    /// <summary>This property's configuration.</summary>
    /// <param name="request">The call's context.</param>
    /// <param name="context">The call.</param>
    /// <returns>The configuration.</returns>
    public override async Task<Contracts.V1.GuestOpsSettings> GetSettings(
        GetSettingsRequest request, ServerCallContext context)
        => ToProto(await settings.GetAsync(
            request.Context.ToScope(CallerContext.Get(context)), context.CancellationToken));

    /// <summary>Write this property's configuration.</summary>
    /// <param name="request">The values to apply.</param>
    /// <param name="context">The call.</param>
    /// <returns>The stored configuration.</returns>
    public override async Task<Contracts.V1.GuestOpsSettings> SaveSettings(
        SaveSettingsRequest request, ServerCallContext context)
    {
        var edit = new SettingsEdit(
            request.HomeCountry,
            [.. request.RequiredForHomeCountry],
            [.. request.RequiredForVisitors],
            [.. request.AcceptedIdTypes],
            request.SignatureRequired,
            request.PrintOnCheckIn,
            request.CardNumberPrefix,
            request.ReportingRequired,
            ParseScope(request.ReportingAppliesTo),
            OrNull(request.ReportingAuthority),
            request.ReportingDueHours);

        var saved = await settings.SaveAsync(
            request.Context.ToScope(CallerContext.Get(context)),
            edit,
            request.Version,
            context.CancellationToken);

        return ToProto(saved);
    }

    /// <summary>Who the reporting obligation covers.</summary>
    /// <remarks>
    /// An unrecognised value is refused rather than defaulted. Defaulting to
    /// from-outside would quietly narrow a property's obligation, and the
    /// property would not find out until an inspection.
    /// </remarks>
    private static ReportingScope ParseScope(string value) => value switch
    {
        "from_outside" => ReportingScope.FromOutside,
        "every_guest" => ReportingScope.EveryGuest,
        _ => throw new InvalidRequestException(
            "reporting_applies_to must be from_outside or every_guest"),
    };

    private static Contracts.V1.GuestOpsSettings ToProto(Domain.GuestOpsSettings row)
    {
        var message = new Contracts.V1.GuestOpsSettings
        {
            PropertyId = row.PropertyId.ToString(),
            HomeCountry = row.HomeCountry,
            SignatureRequired = row.SignatureRequired,
            PrintOnCheckIn = row.PrintOnCheckIn,
            CardNumberPrefix = row.CardNumberPrefix,
            ReportingRequired = row.ReportingRequired,
            ReportingAppliesTo = row.ReportingAppliesTo == ReportingScope.EveryGuest
                ? "every_guest"
                : "from_outside",
            ReportingAuthority = row.ReportingAuthority ?? string.Empty,
            ReportingDueHours = row.ReportingDueHours,
            Version = row.Version,
        };

        message.RequiredForHomeCountry.AddRange(row.RequiredForHomeCountry);
        message.RequiredForVisitors.AddRange(row.RequiredForVisitors);
        message.AcceptedIdTypes.AddRange(row.AcceptedIdTypes);
        return message;
    }
}
