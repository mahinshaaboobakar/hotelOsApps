using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Availability;

/// <summary>What the operator is closing, and for when — ADR 0377.</summary>
/// <param name="RoomTypeId">The type. Required.</param>
/// <param name="RoomId">
/// One room OF THAT TYPE, or null for the whole type. A room with no type is not
/// expressible, which is why this record cannot carry one.
/// </param>
/// <param name="FromDate">Inclusive.</param>
/// <param name="ToDate">Inclusive.</param>
/// <param name="Reason">Why. Required — ADR 0377's schema.</param>
public sealed record StopSellEdit(
    Guid RoomTypeId,
    Guid? RoomId,
    DateOnly FromDate,
    DateOnly ToDate,
    string Reason);

/// <summary>
/// Closing a room type, or one room within it, from selling — ADR 0377.
/// </summary>
/// <remarks>
/// <para>
/// <b>The owner's ruling, 2026-10-04:</b> <i>"Both — they can choose room type and
/// or speciic room in a room type."</i> So the two are hierarchical: the type is
/// required, the room is optional, and the room is chosen WITHIN the type.
/// </para>
/// <para>
/// <b>Availability already subtracted stop-sells and nothing could create
/// one.</b> The table, the entity and the subtraction have existed since
/// <c>TheReservationBook</c>; the seller's control was readable and unwritable,
/// so <c>＋ Close a room type for dates</c> drew off on the approved page's own
/// Stop-sell tab.
/// </para>
/// <para>
/// <b>The room-within-type invariant is validated here</b> rather than by a
/// foreign key, because the rooms are Master Data's and this application
/// references them without owning them. That is also ADR 0356's shape — the
/// owning application validates at the write boundary — and it is the only place
/// that can refuse a room belonging to a different type with a sentence naming
/// both.
/// </para>
/// <para>
/// <b>No lift, and that is deliberate.</b> The approved page draws one control,
/// <c>＋ Close a room type for dates</c>, and nothing that reopens a closed
/// range. Building a removal would be a capability nobody drew and nobody ruled;
/// it is named here so the next reader meets the reason rather than the absence.
/// </para>
/// </remarks>
/// <param name="db">This application's own store, and its Master Data reads.</param>
/// <param name="authorizer">The Kernel.</param>
/// <param name="clock">When it was set.</param>
public sealed class StopSellService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    TimeProvider clock)
{
    /// <summary>Close a type, or one of its rooms, for a date range.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="edit">What is being closed, and why.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The stored hold.</returns>
    /// <exception cref="InvalidRequestException">
    /// A range that ends before it starts, an empty reason, a type this property
    /// has no rooms of, or a room that is not of that type.
    /// </exception>
    public async Task<StopSell> SetAsync(
        RequestScope scope,
        StopSellEdit edit,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.Configure, ResourceTypes.Property, scope.PropertyId,
            cancellationToken);

        if (edit.ToDate < edit.FromDate)
        {
            throw new InvalidRequestException("the last date is before the first");
        }

        if (string.IsNullOrWhiteSpace(edit.Reason))
        {
            throw new InvalidRequestException(
                "a stop-sell needs a reason — it is the seller's own decision, and a hold "
                + "nobody can explain is one nobody can lift with confidence");
        }

        await EnsureWithinTypeAsync(scope, edit, cancellationToken);

        var row = new StopSell
        {
            Id = Guid.CreateVersion7(),
            PropertyId = scope.PropertyId,
            RoomTypeId = edit.RoomTypeId,
            RoomId = edit.RoomId,
            FromDate = edit.FromDate,
            ToDate = edit.ToDate,
            Reason = edit.Reason.Trim(),
            SetBy = scope.UserId,
            SetAt = clock.GetUtcNow(),
        };

        db.StopSells.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        return row;
    }

    /// <summary>The invariant ADR 0377 names: a room, if given, is OF that type.</summary>
    /// <remarks>
    /// <para>
    /// <b>The type is checked even when no room is named</b>, because a hold on a
    /// type this property has no rooms of withholds nothing and reads on the
    /// screen as though it withholds everything — and nothing else in the flow
    /// would ever contradict it.
    /// </para>
    /// <para>
    /// <b>Deactivated and deleted rooms are excluded</b>, matching
    /// <c>FreeRoomsAsync</c>'s own reading of the same table: a room a property
    /// has closed is not one the desk can be holding back from sale.
    /// </para>
    /// </remarks>
    private async Task EnsureWithinTypeAsync(
        RequestScope scope, StopSellEdit edit, CancellationToken cancellationToken)
    {
        var rooms = db.Set<MasterDataRoom>()
            .Where(r => r.PropertyId == scope.PropertyId && r.Active && r.DeletedAt == null);

        if (!await rooms.AnyAsync(r => r.RoomTypeId == edit.RoomTypeId, cancellationToken))
        {
            throw new InvalidRequestException(
                $"this property has no rooms of type {edit.RoomTypeId}, so closing it "
                + "would withhold nothing while reading as though it withheld everything");
        }

        if (edit.RoomId is not { } roomId)
        {
            return;
        }

        var ofType = await rooms.AnyAsync(
            r => r.Id == roomId && r.RoomTypeId == edit.RoomTypeId, cancellationToken);

        if (!ofType)
        {
            throw new InvalidRequestException(
                $"room {roomId} is not a room of type {edit.RoomTypeId} — a stop-sell names "
                + "a room WITHIN a type, so a room of another type is not a narrower hold "
                + "but a different one");
        }
    }
}
