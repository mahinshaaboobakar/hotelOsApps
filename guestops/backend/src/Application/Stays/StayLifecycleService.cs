using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Stays;

/// <summary>
/// What happens to a stay: check in, check out, cancel, no-show, correct.
/// </summary>
/// <remarks>
/// <para>
/// One aggregate, one file. Assignment is <see cref="StayAssignmentService"/>'s
/// because a room change is its own fact (R8) and folding it in here would put
/// the operation consumers must distinguish beside the one they must not.
/// </para>
/// <para>
/// <b>Every write here is the same permission</b> — <c>stay.override</c>. In a
/// PMS-connected property it is also what records an override, and GUEST-Q3
/// ruled that clearing a disagreement takes it too.
/// </para>
/// </remarks>
public sealed class StayLifecycleService(
    GuestOpsDbContext db,
    IKernelAuthorizer authorizer,
    IEventAppender events,
    TimeProvider clock)
{
    /// <summary>Check the guest in.</summary>
    /// <remarks>
    /// <para>
    /// <b>The one hard gate in this application, and it is our own fact:</b> a
    /// stay with no room cannot be checked in (S8, the GUEST-Q2 addendum). A
    /// person cannot be in no room.
    /// </para>
    /// <para>
    /// <b>It does not gate on a neighbour.</b> Room readiness is Room Care's,
    /// and an absent or dissenting neighbour loses its capability and never
    /// this flow (APPS-Q2) — so there is no readiness check here, and a
    /// resolver that arrives later is display-only.
    /// </para>
    /// </remarks>
    public async Task<RoomStay> CheckInAsync(
        RequestScope scope, Guid stayId, long version, CancellationToken cancellationToken)
    {
        var stay = await RequireWritableAsync(scope, stayId, version, cancellationToken);

        if (stay.CurrentRoomId is null)
        {
            throw new InvalidRequestException(
                "this stay has no room; assign one before checking the guest in");
        }

        var now = clock.GetUtcNow();

        var wasHeld = stay.Lifecycle;
        stay.Lifecycle = StayLifecycle.InHouse;
        stay.ArrivalAt = StayTime.Observed(now);

        // The arrival is now observed, so whatever recorded its absence is no
        // longer true. Clearing it here rather than leaving a stale row is the
        // difference between an Attention list that means something and one
        // that grows.
        await ClearAbsenceAsync(stayId, AbsentFields.ArrivalTime, cancellationToken);

        Bump(stay, scope);
        await RecordOverrideAsync(scope, stay, wasHeld, cancellationToken);

        events.Append(scope, "stay.arrived", "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            property_id = stay.PropertyId,
            room_id = stay.CurrentRoomId,
            arrival_at = now,
            business_date = stay.BusinessDate?.ToString("yyyy-MM-dd"),
        });

        await db.SaveChangesAsync(cancellationToken);
        return stay;
    }

    /// <summary>Check the guest out.</summary>
    /// <remarks>
    /// Room Care learns the room is vacated and <b>decides for itself</b>
    /// whether that becomes work: cleaning is policy-driven, and a checked-out
    /// room becoming a task is a hotel policy rather than an automatic
    /// consequence (APPS-Q1). This announces the departure and asserts nothing
    /// about cleaning.
    /// </remarks>
    public async Task<RoomStay> CheckOutAsync(
        RequestScope scope, Guid stayId, long version, CancellationToken cancellationToken)
    {
        var stay = await RequireWritableAsync(scope, stayId, version, cancellationToken);

        var now = clock.GetUtcNow();

        var wasHeld = stay.Lifecycle;
        stay.Lifecycle = StayLifecycle.Departed;
        stay.DepartureAt = StayTime.Observed(now);

        Bump(stay, scope);
        await RecordOverrideAsync(scope, stay, wasHeld, cancellationToken);

        events.Append(scope, "stay.departed", "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            property_id = stay.PropertyId,
            room_id = stay.CurrentRoomId,
            departure_at = now,
            business_date = stay.BusinessDate?.ToString("yyyy-MM-dd"),
        });

        await db.SaveChangesAsync(cancellationToken);
        return stay;
    }

    /// <summary>Cancel one stay.</summary>
    /// <remarks>
    /// <para>
    /// <b>One stay, never a group.</b> GUEST-Q2: cancelling a booking is <i>n</i>
    /// calls to this, which is what the model does and what lets either stay be
    /// reinstated on its own.
    /// </para>
    /// <para>
    /// A cancelled stay is <b>not a deleted stay</b>: it keeps its time, its
    /// reason and its penalty, and it stays in the list. ADR 0062's
    /// <c>active</c> / <c>deleted_at</c> answer whether a record exists, and a
    /// cancelled reservation exists.
    /// </para>
    /// <para>
    /// The penalty is <b>computed from the stored offset and recorded, never
    /// charged</b> — charging is Finance's (GUEST-Q6).
    /// </para>
    /// </remarks>
    public async Task<RoomStay> CancelAsync(
        RequestScope scope,
        Guid stayId,
        string reason,
        long version,
        CancellationToken cancellationToken)
    {
        var stay = await RequireWritableAsync(scope, stayId, version, cancellationToken);

        if (stay.Lifecycle is StayLifecycle.InHouse or StayLifecycle.Departed)
        {
            throw new InvalidRequestException(
                "this guest has already arrived; correct the stay rather than cancelling it");
        }

        var terms = await db.Terms.FirstOrDefaultAsync(t => t.StayId == stayId, cancellationToken);

        var wasHeld = stay.Lifecycle;
        stay.Lifecycle = StayLifecycle.Cancelled;
        Bump(stay, scope);
        await RecordOverrideAsync(scope, stay, wasHeld, cancellationToken);

        events.Append(scope, "stay.cancelled", "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            property_id = stay.PropertyId,
            reason,
            penalty = terms?.PenaltyAmount,
        });

        await db.SaveChangesAsync(cancellationToken);
        return stay;
    }

    /// <summary>Record that nobody arrived.</summary>
    /// <remarks>
    /// A <b>business fact, not a lifecycle verb</b> — ADR 0062's idiom, whose
    /// worked example is <c>RecordStaffExit</c>. It is chargeable and it is
    /// reportable, and it must stay distinguishable from a cancellation for both
    /// reasons.
    /// </remarks>
    public async Task<RoomStay> RecordNoShowAsync(
        RequestScope scope, Guid stayId, long version, CancellationToken cancellationToken)
    {
        var stay = await RequireWritableAsync(scope, stayId, version, cancellationToken);

        if (stay.Lifecycle is not (StayLifecycle.Booked or StayLifecycle.Pending
            or StayLifecycle.Waitlisted))
        {
            throw new InvalidRequestException(
                "only a stay that never arrived can be a no-show");
        }

        var wasHeld = stay.Lifecycle;
        stay.Lifecycle = StayLifecycle.NoShow;
        Bump(stay, scope);
        await RecordOverrideAsync(scope, stay, wasHeld, cancellationToken);

        events.Append(scope, "stay.no_show", "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            property_id = stay.PropertyId,
            business_date = stay.BusinessDate?.ToString("yyyy-MM-dd"),
        });

        await db.SaveChangesAsync(cancellationToken);
        return stay;
    }

    /// <summary>Move a stay's lifecycle deliberately, including backwards.</summary>
    /// <remarks>
    /// <para>
    /// <b>The one exception to the rule inbound facts obey</b> (S24). The guest
    /// checked out in error at 07:00 and still asleep in the room is a real
    /// morning, and a model that refused it would make the mistake permanent.
    /// Inbound facts are monotonic; people are not.
    /// </para>
    /// <para>
    /// It is a <b>recorded correction</b> rather than a rewriting of history:
    /// the room was announced vacated and consumers acted on it, so the
    /// correction is published as its own fact and the reason is required.
    /// </para>
    /// </remarks>
    public async Task<RoomStay> CorrectAsync(
        RequestScope scope,
        Guid stayId,
        StayLifecycle to,
        string reason,
        long version,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidRequestException(
                "a correction needs a reason; without one it is indistinguishable from a mistake");
        }

        var stay = await RequireWritableAsync(scope, stayId, version, cancellationToken);
        var from = stay.Lifecycle;

        stay.Lifecycle = to;
        Bump(stay, scope);
        await RecordOverrideAsync(scope, stay, from, cancellationToken);

        events.Append(scope, "stay.corrected", "stay", stay.Id, stay.Version, new
        {
            stay_id = stay.Id,
            property_id = stay.PropertyId,
            from = from.ToString(),
            to = to.ToString(),
            reason,
        });

        await db.SaveChangesAsync(cancellationToken);
        return stay;
    }

    /// <summary>
    /// Record that a person wrote a stay the PMS owns — GUEST-Q1's amendment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>PMS-connected is PMS-writes-first, staff-may-override — never
    /// read-only.</b> So a staff write is not refused and not silently applied:
    /// it is applied <i>and recorded as an override</i>, with who, when, and
    /// <b>what the PMS said at that moment</b>. That last one is what makes the
    /// override explicable months later, and it is not recoverable afterwards —
    /// the desk acted on what it could see.
    /// </para>
    /// <para>
    /// <b>The test is whether the PMS knows this stay</b>, not whether a
    /// connector is configured. A stay created here and never seen by the PMS
    /// has nothing to override; one the PMS sent does. GUEST-Q4 removed the
    /// second mode, so there is no property-level flag consulted anywhere.
    /// </para>
    /// <para>
    /// One row per aspect: a second override on the lifecycle while the first
    /// still stands is the same override continuing, not a new one, and two
    /// rows would make the Attention list count one stay twice.
    /// </para>
    /// </remarks>
    /// <param name="scope">The caller who wrote.</param>
    /// <param name="stay">The stay, already changed.</param>
    /// <param name="pmsValue">What the stay held before the write.</param>
    /// <param name="cancellationToken">The call's token.</param>
    private async Task RecordOverrideAsync(
        RequestScope scope,
        RoomStay stay,
        StayLifecycle pmsValue,
        CancellationToken cancellationToken)
    {
        if (stay.PmsUnknown)
        {
            return;
        }

        var standing = await db.Disagreements.AnyAsync(
            d => d.StayId == stay.Id
                 && d.Aspect == DisagreementAspect.Lifecycle
                 && (d.State == DisagreementState.Overridden
                     || d.State == DisagreementState.Standing),
            cancellationToken);

        if (standing)
        {
            return;
        }

        db.Disagreements.Add(new StayDisagreement
        {
            Id = Uuid7.NewUuid7(),
            StayId = stay.Id,
            Aspect = DisagreementAspect.Lifecycle,

            // Two values, and both are needed: what the desk wrote, and what
            // the PMS last told us — the value the desk was looking at when it
            // decided to override.
            OurValue = stay.Lifecycle.ToString(),
            PmsValueAtOverride = pmsValue.ToString(),

            OverrideActor = scope.UserId,
            OverrideAt = clock.GetUtcNow(),
            RaisedAt = clock.GetUtcNow(),
            State = DisagreementState.Overridden,
        });
    }

    private async Task<RoomStay> RequireWritableAsync(
        RequestScope scope, Guid stayId, long version, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.StayOverride, ResourceTypes.Stay, stayId, cancellationToken);

        var stay = await db.Stays
            .FirstOrDefaultAsync(
                s => s.Id == stayId && s.PropertyId == scope.PropertyId, cancellationToken)
            ?? throw new NotFoundException("stay", stayId);

        if (stay.Version != version)
        {
            throw new ConcurrencyException("stay", stayId, version);
        }

        return stay;
    }

    private async Task ClearAbsenceAsync(
        Guid stayId, string field, CancellationToken cancellationToken)
    {
        var absence = await db.Absences
            .FirstOrDefaultAsync(a => a.StayId == stayId && a.Field == field, cancellationToken);

        if (absence is not null)
        {
            db.Absences.Remove(absence);
        }
    }

    /// <summary>The version the event carries, bumped in the same transaction.</summary>
    /// <remarks>
    /// The Kernel refuses an <c>entity_version</c> that is not exactly
    /// <c>last + 1</c>, which is what makes a gap detectable rather than
    /// silently absorbed — so the bump and the append must see the same number.
    /// </remarks>
    private static void Bump(RoomStay stay, RequestScope scope)
    {
        stay.UpdatedBy = scope.UserId;
        stay.Version += 1;
    }
}
