using HotelOS.Jobs.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Infrastructure.Configuration;

/// <summary>
/// What the general manager has granted — design §4.2's
/// <c>property#jobs_manager</c>, as this application records having announced
/// it.
/// </summary>
/// <remarks>
/// Its own file rather than a fourth block in <c>PolicyTables</c>: a policy is
/// what this property decided about its work, and a grant is what one person
/// may do. They share a settings screen and nothing else, and sharing a screen
/// is not sharing a purpose.
/// </remarks>
public static class GrantTables
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<JobsManagerGrant>(g =>
        {
            g.ToTable("jobs_manager_grant");
            g.HasKey(x => x.Id);
            g.Ignore(x => x.IsLive);

            // **One live grant per person, and the history beside it.** A
            // partial unique index rather than a plain one: granting twice is a
            // double-click and must not create a second standing row, while
            // grant → revoke → grant again is an ordinary sequence and must.
            g.HasIndex(x => new { x.PropertyId, x.UserId })
                .IsUnique()
                .HasFilter("revoked_at IS NULL");

            // The screen's only listing question — who holds it here, now.
            g.HasIndex(x => new { x.PropertyId, x.RevokedAt });
        });
    }
}
