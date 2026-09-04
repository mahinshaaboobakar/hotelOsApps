using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Stays;

/// <summary>Which of the four lists the desk is looking at.</summary>
/// <remarks>
/// The wire's <c>StayView</c> in the domain's own vocabulary, so the
/// application layer does not depend on the contract. There is no
/// <c>Unspecified</c> member: the gRPC layer refuses that by name, and a state
/// this enum cannot express is a state no query has to handle.
/// </remarks>
public enum StayView
{
    /// <summary>Due in on the business day.</summary>
    Arrivals = 1,

    /// <summary>Occupying a room now.</summary>
    InHouse = 2,

    /// <summary>Due out on the business day, and those already gone.</summary>
    Departures = 3,

    /// <summary>Things a person has to decide.</summary>
    Attention = 4,
}

/// <summary>What the desk asked for.</summary>
/// <param name="View">One of the four lists.</param>
/// <param name="BusinessDate">
/// Null means the property's current business day, which this asks
/// <see cref="IBusinessDay"/> for and never computes — a day that rolls at
/// 04:00 is the property's configuration, not this service's arithmetic.
/// </param>
/// <param name="Page">Already clamped by <see cref="Paging.Of"/>.</param>
public sealed record StayQuery(
    StayView View,
    DateOnly? BusinessDate,
    Paging.Window Page);

/// <summary>
/// The four lists — <c>CORE-Q13</c>, paged with a real total.
/// </summary>
/// <remarks>
/// <para>
/// Its own file because reading is its own purpose:
/// <c>StayLifecycleService</c> moves a stay through its states and
/// <c>StayAssignmentService</c> gives it a room, and a query is neither. Adding
/// it to either would give that file two audiences and two reasons to change —
/// ADR 0038.
/// </para>
/// <para>
/// <b>Paged, not cursored</b> — a business day's stays are bounded and their
/// count is a fact, so the desk gets <i>"showing 1–25 of 47"</i>. A cursor can
/// express neither the ordinal nor the total, which is why <c>common.v1</c> now
/// carries both patterns.
/// </para>
/// </remarks>
public sealed class StayListService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    IBusinessDay businessDay)
{
    /// <summary>One page of a list, and how many stays the whole list holds.</summary>
    /// <remarks>
    /// The count comes from the same query the page is taken from. Building the
    /// predicate twice is how a pager offers pages the list cannot produce — the
    /// count and the rows must not be able to disagree.
    /// </remarks>
    public async Task<PagedResult<RoomStay>> ListAsync(
        RequestScope scope, StayQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.ReservationRead, ResourceTypes.Property, scope.PropertyId,
            cancellationToken);

        var day = query.BusinessDate
            ?? await businessDay.CurrentAsync(scope, cancellationToken);

        var stays = await FilterAsync(
            scope,
            db.Stays.Where(s => s.PropertyId == scope.PropertyId),
            query.View,
            day,
            cancellationToken);

        var total = await stays.CountAsync(cancellationToken);

        // Ordered by arrival, then by id. The tie-break is not decoration: with
        // `Skip` reading page two, two stays sharing an arrival instant could
        // otherwise swap between the two reads — one shown twice, one missed,
        // and nothing on screen to say so.
        var rows = await stays
            .OrderBy(s => s.ArrivalAt.At)
            .ThenBy(s => s.Id)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .Include(s => s.Party)
            .Include(s => s.Source)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResult<RoomStay>(rows, total);
    }

    /// <summary>What each of the four lists actually selects.</summary>
    /// <remarks>
    /// <para>
    /// <b>There is no <c>DueOut</c> state to filter on</b> — <see cref="StayLifecycle"/>
    /// says so outright, and departures are composed instead: still in house or
    /// already gone, with a departure falling inside the day. A second
    /// vocabulary for an axis two fields already state is the drift
    /// <c>CONN-Q11</c> refused one level up.
    /// </para>
    /// <para>
    /// <b>Cancelled and no-show stays stay in Arrivals.</b> They were expected on
    /// the day and the desk is accountable for both — a cancelled reservation
    /// exists and a no-show is reportable (ADR 0062).
    /// </para>
    /// </remarks>
    private async Task<IQueryable<RoomStay>> FilterAsync(
        RequestScope scope,
        IQueryable<RoomStay> stays,
        StayView view,
        DateOnly? day,
        CancellationToken cancellationToken)
    {
        switch (view)
        {
            case StayView.Arrivals:
                return stays.Where(s => s.BusinessDate == day);

            case StayView.InHouse:
                return stays.Where(s => s.Lifecycle == StayLifecycle.InHouse);

            case StayView.Departures:
                return await DeparturesAsync(scope, stays, day, cancellationToken);

            case StayView.Attention:
                // `PmsUnknown` is a stay this property created that the feed has
                // never confirmed. A disagreement nobody has cleared is the
                // other half. Written against the set rather than a navigation
                // because `RoomStay` has none — the aggregate deliberately does
                // not own its disagreements.
                return stays.Where(s =>
                    s.PmsUnknown
                    || db.Disagreements.Any(d => d.StayId == s.Id && d.ClearedAt == null));

            default:
                throw new InvalidRequestException("view is required");
        }
    }

    /// <summary>Due out on the day, and those already gone.</summary>
    /// <remarks>
    /// <para>
    /// <b>Both halves need the operating day's instants, not its date.</b> A
    /// departure is stored only as a timestamp — there is no departure-date
    /// column, and <see cref="StayTime.Date"/> is computed in C# and cannot be
    /// translated to SQL. So the filter is a half-open instant range, and the
    /// range comes from <see cref="IBusinessDay.BoundsAsync"/> rather than from
    /// arithmetic here.
    /// </para>
    /// <para>
    /// <b>An unknown boundary yields an empty list, never a whole-day guess.</b>
    /// A property whose zone or roll time Context cannot answer would otherwise
    /// get a plausible list that is wrong by a day near midnight — the failure
    /// <c>IBusinessDay</c>'s own documentation calls out as having the widest
    /// blast radius because it looks like correct data.
    /// </para>
    /// </remarks>
    private async Task<IQueryable<RoomStay>> DeparturesAsync(
        RequestScope scope,
        IQueryable<RoomStay> stays,
        DateOnly? day,
        CancellationToken cancellationToken)
    {
        if (day is not { } date)
        {
            return stays.Where(_ => false);
        }

        var bounds = await businessDay.BoundsAsync(scope, date, cancellationToken);

        if (bounds is not { } window)
        {
            return stays.Where(_ => false);
        }

        return stays.Where(s =>
            (s.Lifecycle == StayLifecycle.InHouse || s.Lifecycle == StayLifecycle.Departed)
            && s.DepartureAt.At >= window.Start
            && s.DepartureAt.At < window.End);
    }
}
