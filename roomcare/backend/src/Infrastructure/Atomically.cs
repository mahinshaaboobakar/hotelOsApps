using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Infrastructure;

/// <summary>Runs a unit of work that saves more than once inside one transaction, under the retry strategy.</summary>
/// <remarks>
/// A hand-over ends one assignment row and inserts the next, and the
/// one-current-row index needs the first write to land before the second; the
/// transaction keeps both — and the event — in one commit. Under
/// <c>EnableRetryOnFailure</c> a user transaction must run inside the execution
/// strategy, or the provider refuses it.
/// </remarks>
public static class Atomically
{
    public static Task<T> AtomicallyAsync<T>(this RoomCareDbContext db, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            return work();
        }

        return db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var result = await work();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
