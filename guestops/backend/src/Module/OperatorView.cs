using HotelOS.GuestOps.Infrastructure;
using HotelOS.GuestOps.Infrastructure.ReadModels;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Who is at this desk — the bar's right-hand slot.
/// </summary>
/// <remarks>
/// <para>
/// <b>The module asks its backend, because a realm cannot know.</b> Design page
/// 63 §3: <i>"a bundle calls only its own backend; everything else, its backend
/// does."</i> <c>SHELL-Q52</c> ruled the host contract stays as it is —
/// capabilities presented to a module are not identity — so the answer comes
/// from here, where the session token was validated and <c>RequestScope</c>
/// carries the user and the property.
/// </para>
/// <para>
/// <b>Every field is nullable and every null is drawn.</b> The bar this
/// replaced held <c>"Anitha Menon"</c> and <c>"Front Office · Avenue Regent"</c>
/// as literals, which is an attribution claim about every write the screens
/// make. A name that cannot be established is reported absent, never softened:
/// <c>null</c> means <i>the platform could not establish this here</i>, never
/// <i>use something plausible</i>.
/// </para>
/// <para>
/// <b>Three ways the person is legitimately unknown</b>, and none is an error:
/// a service caller rather than a person, a user with no staff record, and a
/// staff record deactivated or soft-deleted (ADR 0062 — <c>active</c> and
/// <c>deleted_at</c> are the lifecycle, and a departed colleague is not who is
/// standing at the desk).
/// </para>
/// </remarks>
public sealed class OperatorView(GuestOpsDbContext db)
{
    /// <summary>Who is signed in and where, as far as Master Data can say.</summary>
    public async Task<object?> AnswerAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var name = scope.UserId is null
            ? null
            : await db.Set<MasterDataStaffName>()
                .Where(s => s.UserId == scope.UserId && s.Active && s.DeletedAt == null)
                .Select(s => s.DisplayName)
                .FirstOrDefaultAsync(cancellationToken);

        var property = await db.Set<MasterDataPropertyName>()
            .Where(p => p.Id == scope.PropertyId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return new OperatorAnswer(
            string.IsNullOrWhiteSpace(name) ? null : name,
            string.IsNullOrWhiteSpace(property) ? null : property);
    }

    /// <summary>What the bar draws, with absence expressible.</summary>
    public sealed record OperatorAnswer(string? Name, string? Where);
}
