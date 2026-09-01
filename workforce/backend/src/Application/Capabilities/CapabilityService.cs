using HotelOS.Platform;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Domain;
using HotelOS.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Workforce.Application.Capabilities;

/// <summary>
/// Capabilities — what people can do, and what is about to lapse.
/// </summary>
/// <remarks>
/// <para>
/// Slice 2, and second rather than last: a fire-warden card expires whether or
/// not Jobs has shipped. It needs only slice 1 — the Attention list's audience
/// resolves from postings, and nothing here depends on a rota, on leave or on
/// attendance.
/// </para>
/// <para>
/// <b>Nothing in this service blocks anything.</b> `WF-Q16`: the platform
/// refuses the physically impossible and warns on a judgment. An expired
/// certificate is a judgment — the person can physically work the shift — so
/// this service records, computes and reports, and the decision stays the
/// hotel's. Our job is that nobody can say <i>"we didn't know"</i>.
/// </para>
/// </remarks>
public class CapabilityService(
    WorkforceDbContext db,
    IKernelAuthorizer authorizer,
    TimeProvider clock)
{
    /// <summary>Record something a person can do.</summary>
    public async Task<Capability> RecordAsync(
        RequestScope scope, RecordCapabilityCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.CapabilityManage, "property", scope.PropertyId, cancellationToken);

        var name = command.Name?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            throw new InvalidRequestException("name is required");
        }

        // The same person cannot hold one capability twice: a second "fire
        // warden" row is two expiry dates for one fact, and the register would
        // show the person as both current and lapsed. Renewing amends the row
        // that exists.
        var already = await db.Capabilities.AnyAsync(
            c => c.PropertyId == scope.PropertyId
                 && c.StaffId == command.StaffId
                 && c.Name == name,
            cancellationToken);

        if (already)
        {
            throw new InvalidRequestException(
                $"this person already holds '{name}' — renew it rather than recording it again");
        }

        var now = clock.GetUtcNow();
        var capability = new Capability
        {
            Id = Uuid7.NewUuid7(),
            PropertyId = scope.PropertyId,
            StaffId = command.StaffId,
            Name = name,
            ValidUntil = command.ValidUntil,
            Note = command.Note?.Trim() ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };

        db.Capabilities.Add(capability);
        await db.SaveChangesAsync(cancellationToken);

        return capability;
    }

    /// <summary>Amend a capability — including renewing it.</summary>
    public async Task<Capability> AmendAsync(
        RequestScope scope, AmendCapabilityCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.CapabilityManage, "property", scope.PropertyId, cancellationToken);

        var capability = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(capability, command.ExpectedVersion);

        if (command.Name is { } name)
        {
            var trimmed = name.Trim();
            capability.Name = trimmed.Length > 0
                ? trimmed
                : throw new InvalidRequestException("name cannot be cleared");
        }

        if (command.Note is { } note)
        {
            capability.Note = note.Trim();
        }

        if (command.ValidUntil.IsPresent)
        {
            // Renewing, or turning a certification back into an ability. Both
            // are this one assignment, because the date is the discriminator.
            capability.ValidUntil = command.ValidUntil.Value;
        }

        capability.UpdatedAt = clock.GetUtcNow();
        capability.Version += 1;

        await db.SaveChangesAsync(cancellationToken);
        return capability;
    }

    /// <summary>Remove a capability recorded in error.</summary>
    public async Task RemoveAsync(
        RequestScope scope, RemoveCapabilityCommand command, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.CapabilityManage, "property", scope.PropertyId, cancellationToken);

        var capability = await LoadAsync(scope, command.Id, cancellationToken);
        RequireVersion(capability, command.ExpectedVersion);

        // A hard delete, and deliberately. This is the row that should never
        // have existed; an expired capability is *kept*, because the register
        // showing what has lapsed is the whole point of having one.
        db.Capabilities.Remove(capability);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>One person's capabilities, or the property's.</summary>
    public async Task<IReadOnlyList<Capability>> ListAsync(
        RequestScope scope, ListCapabilitiesQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        var capabilities = db.Capabilities.Where(c => c.PropertyId == scope.PropertyId);

        if (query.StaffId is { } staffId)
        {
            capabilities = capabilities.Where(c => c.StaffId == staffId);
        }

        return await Ordered(capabilities).ToListAsync(cancellationToken);
    }

    /// <summary>What is about to lapse, or has.</summary>
    /// <remarks>
    /// <para>
    /// Computed, never a stored list and never a notification queue: the answer
    /// depends on today. Nothing is marked read or dismissed, because a
    /// certificate that has expired does not stop being expired when somebody
    /// looks at it.
    /// </para>
    /// <para>
    /// The window is the ruling's outermost band — <b>60 days</b> — and the
    /// finer 30 and 7 are the band each row reports, so one query serves all
    /// three without three round trips.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<Capability>> AttentionAsync(
        RequestScope scope, AttentionQuery query, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        var today = Today();
        var horizon = today.AddDays(60);

        var capabilities = db.Capabilities.Where(
            c => c.PropertyId == scope.PropertyId
                 && c.ValidUntil != null
                 && c.ValidUntil <= horizon);

        if (!string.IsNullOrWhiteSpace(query.DepartmentCode))
        {
            var code = query.DepartmentCode.Trim().ToUpperInvariant();

            // Whose people these are is a **posting** question, which is why this
            // application can answer it at all — ADR 0116 §6 makes department
            // membership derive from postings only, and this is the same
            // resolution the leave approver uses.
            var posted = db.Postings
                .Where(p => p.PropertyId == scope.PropertyId
                            && p.DepartmentCode == code
                            && (p.EffectiveTo == null || p.EffectiveTo >= today))
                .Select(p => p.StaffId);

            capabilities = capabilities.Where(c => posted.Contains(c.StaffId));
        }

        // Soonest first: a list a department head reads at 7 a.m. is a list whose
        // top row is the one that matters most.
        return await capabilities
            .OrderBy(c => c.ValidUntil)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Every dated capability — the sheet a safety inspector asks for.</summary>
    /// <remarks>
    /// Dated only. An ability is not a certification and putting <i>"speaks
    /// Arabic"</i> on a compliance register would bury the four rows an
    /// inspector came to see.
    /// </remarks>
    public async Task<IReadOnlyList<Capability>> RegisterAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.WorkforceRead, "property", scope.PropertyId, cancellationToken);

        return await db.Capabilities
            .Where(c => c.PropertyId == scope.PropertyId && c.ValidUntil != null)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.ValidUntil)
            .ToListAsync(cancellationToken);
    }

    /// <summary>The band a capability is in today, at this property.</summary>
    /// <remarks>
    /// On the service rather than computed by each caller, so one clock decides
    /// it. Two callers reading the same row on the same day must not disagree
    /// about whether it has expired.
    /// </remarks>
    /// <param name="capability">The capability to judge.</param>
    /// <returns>Its band.</returns>
    public ExpiryBand BandOf(Capability capability) => capability.BandOn(Today());

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    private static IQueryable<Capability> Ordered(IQueryable<Capability> capabilities) =>
        capabilities.OrderBy(c => c.Name).ThenBy(c => c.ValidUntil);

    private async Task<Capability> LoadAsync(
        RequestScope scope, Guid id, CancellationToken cancellationToken)
    {
        var capability = await db.Capabilities.FirstOrDefaultAsync(
            c => c.Id == id && c.PropertyId == scope.PropertyId, cancellationToken);

        return capability ?? throw new NotFoundException("capability", id);
    }

    private static void RequireVersion(Capability capability, long expected)
    {
        if (capability.Version != expected)
        {
            throw new ConcurrencyException("capability", capability.Id, expected);
        }
    }
}
