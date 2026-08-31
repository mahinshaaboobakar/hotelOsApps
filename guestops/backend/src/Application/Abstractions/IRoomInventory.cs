namespace HotelOS.GuestOps.Application.Abstractions;

/// <summary>
/// How many rooms of each type this property has — Master Data's answer.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read, never copied.</b> An application may read master data; what it may
/// not do is keep its own version of a room. This returns a count at the moment
/// it is asked, so there is nothing here to go stale and nothing to reconcile.
/// </para>
/// <para>
/// <b>An interface because the source is not this application's business.</b>
/// Today it is a <c>SELECT</c> on <c>masterdata</c> with the grant an installed
/// application holds. If the platform later prefers a Master Data RPC or a
/// Context resolver, the availability calculation does not change — which is
/// the point of naming the question rather than the mechanism.
/// </para>
/// </remarks>
public interface IRoomInventory
{
    /// <summary>Rooms per type, for the types asked about.</summary>
    /// <param name="propertyId">The caller's property.</param>
    /// <param name="roomTypeIds">Empty means every type this property has.</param>
    /// <param name="cancellationToken">The call's token.</param>
    Task<IReadOnlyDictionary<Guid, int>> CountByTypeAsync(
        Guid propertyId,
        IReadOnlyCollection<Guid> roomTypeIds,
        CancellationToken cancellationToken);
}
