using Grpc.Core;
using HotelOS.Contracts.Context.V1;
using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;

namespace HotelOS.Workforce.Infrastructure.Platform;

/// <summary>
/// The property's operating day, asked of the Context Service — ADR 0211.
/// </summary>
/// <remarks>
/// <para>
/// <b>No property id is sent.</b> Context binds the property to the
/// authenticated caller (<c>AUTHZ-Q18b</c>), so the request carries the scope's
/// request context and nothing else. Sending one would be this application
/// naming a property it was not asked about.
/// </para>
/// <para>
/// <b>Nothing is computed here.</b> `GetOperatingDay` echoes the boundary and
/// the zone so a caller can show its working; this application needs the date
/// and reads only that. The roll hour is the property's configuration and is
/// Context's to apply — an application that re-derived the day from the echoed
/// boundary would be the second derivation ADR 0128 §6 refuses.
/// </para>
/// <para>
/// <b>A day that cannot be read is null, never a substitute.</b> An
/// unreachable Context, a refusal, and an unparseable date all answer the same
/// way, because all three mean this application does not know what day it is at
/// the property. The caller turns that into a failure the screen shows.
/// GuestOps' <c>ContextBusinessDay</c> fails closed for the same reason, in the
/// same words: a business date computed from the wrong boundary is silent,
/// plausible, and wrong by a day.
/// </para>
/// <para>
/// <b>Why this application holds a client at all, when it holds no Master Data
/// client.</b> <c>Program.cs</c> records why that one was removed: an installed
/// application's certificate is <c>kind=application</c>, and Master Data's
/// authenticator admits only a service without a token. ADR 0210 settles this
/// door differently — <c>GetOperatingDay</c> is platform/service-scoped and its
/// caller may be an <c>Application</c> — so the call is legitimate where the
/// other was not. <b>That it is admitted in a running property is not yet
/// proved</b>: it is proved when an installed application's Context call
/// succeeds live, and until then this implementation is registered by nobody.
/// </para>
/// </remarks>
/// <param name="context">The Context Service, as a client.</param>
public sealed class ContextOperatingDay(ContextService.ContextServiceClient context)
    : IOperatingDay
{
    /// <inheritdoc />
    public async Task<DateOnly?> TodayAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var day = await context.GetOperatingDayAsync(
                new GetOperatingDayRequest
                {
                    Context = RequestContextFactory.ToRequestContext(scope),
                },
                cancellationToken: cancellationToken);

            return DateOnly.TryParse(day.BusinessDate, out var parsed) ? parsed : null;
        }
        catch (RpcException)
        {
            // Unreachable, refused, or timed out. The caller says the day could
            // not be read; it does not say which, because every one of them
            // leaves this application equally ignorant of the date.
            return null;
        }
    }
}
