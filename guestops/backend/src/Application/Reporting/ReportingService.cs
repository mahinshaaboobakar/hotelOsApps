using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Reporting;

/// <summary>
/// Recording that a guest filing was made — S19b.
/// </summary>
/// <remarks>
/// <para>
/// <b>HotelOS submits nothing.</b> Sending guest data to an authority is an
/// integration, and every integration on this platform is a connector — which
/// this would be the first <i>outbound</i> one of, landing on the write-back
/// capability CONN-Q5 deferred. What this service records is that a person
/// filed, when, with which authority, and under what reference.
/// </para>
/// <para>
/// <b>The reference is the receipt, and that is why the record exists ahead of
/// any connector.</b> The row is the property's evidence that it complied, so
/// its shape does not change when submission is automated: a person files and
/// records the receipt now, a connector records the same receipt later, on the
/// same row.
/// </para>
/// <para>
/// <b>Nothing here gates anything.</b> A stay with an outstanding filing checks
/// in, is served and checks out. There is deliberately no call from the
/// lifecycle service into this one.
/// </para>
/// </remarks>
public sealed class ReportingService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    TimeProvider clock)
{
    /// <summary>Record that this stay was filed with an authority.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="stayId">The stay filed.</param>
    /// <param name="authority">Which authority, as the property names it.</param>
    /// <param name="reference">The receipt the authority gave back.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The updated obligation row.</returns>
    /// <remarks>
    /// <para>
    /// <b>The receipt is required.</b> A filing recorded without one asserts
    /// compliance and carries no evidence of it, which is worse than an
    /// outstanding row — the outstanding row at least tells the truth.
    /// </para>
    /// <para>
    /// <b>Filing an obligation the policy does not impose is refused</b>, and
    /// with a diagnostic rather than silently: it means the desk and the
    /// configuration disagree about who must be filed, and quietly accepting it
    /// would hide a misconfiguration behind an apparently complete record.
    /// </para>
    /// </remarks>
    public async Task<StayReporting> RecordFilingAsync(
        RequestScope scope,
        Guid stayId,
        string authority,
        string reference,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ReportingFile, ResourceTypes.Stay, stayId, cancellationToken);

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new InvalidRequestException(
                "reference is required — a filing is a legal assertion and the authority's "
                + "receipt is part of the record, not a log line");
        }

        if (string.IsNullOrWhiteSpace(authority))
        {
            throw new InvalidRequestException("authority is required");
        }

        var stay = await db.Stays
            .FirstOrDefaultAsync(
                s => s.Id == stayId && s.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        var reporting = await db.Reporting
            .FirstOrDefaultAsync(r => r.StayId == stay.Id, cancellationToken)
            ?? throw new NotFoundException("stay_reporting", stayId);

        if (reporting.State == ReportingState.NotRequired)
        {
            throw new InvalidRequestException(
                "this stay is outside the property's reporting policy — filing it would record "
                + "an obligation the configuration says does not exist. Change the policy first "
                + "if the desk is right");
        }

        reporting.State = ReportingState.Filed;
        reporting.FiledAt = clock.GetUtcNow();
        reporting.FiledBy = scope.UserId;
        reporting.Authority = authority;
        reporting.Reference = reference;

        await db.SaveChangesAsync(cancellationToken);
        return reporting;
    }

    /// <summary>What this property still owes an authority.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="asOf">The day to judge overdue against.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>Outstanding obligations, the oldest deadline first.</returns>
    /// <remarks>
    /// <b>Rows with no deadline are still listed.</b> A stay whose arrival is
    /// unknown has no computable due date (R25 — an absence is not invented),
    /// and dropping it from the list would make the one filing nobody can date
    /// also the one nobody sees.
    /// </remarks>
    public async Task<IReadOnlyList<StayReporting>> OutstandingAsync(
        RequestScope scope, DateOnly asOf, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope,
            Permissions.ReservationRead,
            ResourceTypes.Property,
            scope.PropertyId,
            cancellationToken);

        return await db.Reporting
            .Where(r => r.State == ReportingState.Needed
                && r.Stay!.PropertyId == scope.PropertyId
                && (r.RequiredBy == null || r.RequiredBy <= asOf))
            .OrderBy(r => r.RequiredBy == null)
            .ThenBy(r => r.RequiredBy)
            .ToListAsync(cancellationToken);
    }
}
