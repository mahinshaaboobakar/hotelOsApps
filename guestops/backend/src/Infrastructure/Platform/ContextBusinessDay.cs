using HotelOS.Contracts.Context.V1;
using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Infrastructure.Platform;

/// <summary>
/// The operating day, asked of the Context Service.
/// </summary>
/// <remarks>
/// <para>
/// <c>HUB-Q1</c> put the derivation there: the boundary is the property's
/// configuration and <c>operating_day(timestamp, boundary)</c> is Context's
/// answer over it, derived and stored by nobody. This application asks and
/// never computes — which is the same rule the Integration Hub follows when it
/// stamps <c>business_date</c> on a normalised fact.
/// </para>
/// <para>
/// <b>The check-in and check-out hours come from the same place</b>, and this
/// is why they are not read from a local column: Core Administration
/// establishes them (ADR 0052), and an application that cached them would be
/// one administrator edit away from turning a date into the wrong instant.
/// </para>
/// <para>
/// <b>What this cannot do yet.</b> Reaching Context needs a channel this
/// application is authenticated on, and an installed package has no service
/// certificate — nothing enrols one at install. Round 51 answers that; until
/// it does, these calls fail closed rather than falling back to a computed
/// answer, because a business date computed from the wrong boundary is exactly
/// R16's class of defect: silent, plausible, and wrong by a day.
/// </para>
/// </remarks>
public sealed class ContextBusinessDay(ContextService.ContextServiceClient context)
    : IBusinessDay
{
    public async Task<DateOnly?> CurrentAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        var day = await context.GetOperatingDayAsync(
            new GetOperatingDayRequest { Context = RequestContextFactory.ToRequestContext(scope) },
            cancellationToken: cancellationToken);

        return DateOnly.TryParse(day.BusinessDate, out var parsed) ? parsed : null;
    }

    public Task<StayTime> AtCheckInAsync(
        RequestScope scope, DateOnly date, CancellationToken cancellationToken)
        => AtAsync(scope, date, checkIn: true, cancellationToken);

    public Task<StayTime> AtCheckOutAsync(
        RequestScope scope, DateOnly date, CancellationToken cancellationToken)
        => AtAsync(scope, date, checkIn: false, cancellationToken);

    private async Task<StayTime> AtAsync(
        RequestScope scope, DateOnly date, bool checkIn, CancellationToken cancellationToken)
    {
        var summary = await context.GetPropertySummaryAsync(
            new GetPropertySummaryRequest { Context = RequestContextFactory.ToRequestContext(scope) },
            cancellationToken: cancellationToken);

        var clock = checkIn ? summary.CheckInTime : summary.CheckOutTime;

        // A property that has not configured one is not the same as one that
        // checks in at midnight — Master Data keeps them apart deliberately, so
        // an unset hour produces an unknown time rather than 00:00.
        if (!TimeOnly.TryParse(clock, out var hour))
        {
            return StayTime.None;
        }

        // The property's zone, from the property's own reference. Built here
        // rather than in UTC because a derived timestamp's date component must
        // be the date the desk typed — R12, and R16's reason for insisting the
        // zone is an IANA name and never an offset.
        var zone = TimeZoneInfo.FindSystemTimeZoneById(summary.Property.Timezone);
        var local = date.ToDateTime(hour);
        var offset = zone.GetUtcOffset(local);

        return new StayTime(new DateTimeOffset(local, offset), TimeBasis.Derived);
    }
}
