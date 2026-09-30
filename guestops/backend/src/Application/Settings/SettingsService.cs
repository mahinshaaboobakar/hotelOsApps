using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Application.Settings;

/// <summary>
/// This application's own configuration, and the card series it holds — §2.8.
/// </summary>
/// <remarks>
/// <para>
/// <b>Reading is not a permission-free operation.</b> The required-field sets
/// and the reporting policy are read by the desk on every check-in, so the read
/// asks for <see cref="Permissions.ReservationRead"/> rather than
/// <see cref="Permissions.Configure"/> — a receptionist must see the form they
/// have to fill in without being able to change what it demands.
/// </para>
/// <para>
/// <b>A property with no row is unconfigured, and that is reported.</b> A
/// property configured to require nothing and a property nobody has configured
/// are different facts, and only one of them is a reason to trust a blank card.
/// </para>
/// <para>
/// <b>The sentence above used to continue "this service invents no defaults",
/// and that is no longer true — GUEST-Q15, 2026-09-30.</b> An unconfigured read
/// now answers with <see cref="GuestOpsSettings.DefaultsFor"/>, because throwing
/// made the module envelope answer 404 across four surfaces including the only
/// screen that could create the row. What survives of the old sentence is the
/// part that mattered: <i>the two facts are still distinguished</i>, now by
/// <see cref="GuestOpsSettings.Stored"/> rather than by an exception — and the
/// values returned are the ones the manifest declared and an administrator
/// approved, not values this service made up.
/// </para>
/// </remarks>
public sealed class SettingsService(GuestOpsDbContext db, IKernelAuthorizer authorizer)
{
    /// <summary>This property's configuration.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>
    /// The configuration row, or the declared defaults where the property has
    /// none — <see cref="GuestOpsSettings.Stored"/> says which.
    /// </returns>
    /// <remarks>
    /// <b>This carried <c>&lt;exception cref="NotFoundException"&gt;The property
    /// has not been configured&lt;/exception&gt;</c> until 2026-09-30, and it can
    /// no longer throw it.</b> Recorded rather than deleted: a documented error
    /// that stopped being possible is contradicted by nothing — not the compiler,
    /// not a test, and not a caller, because callers do not handle errors that
    /// never arrive.
    /// </remarks>
    public async Task<GuestOpsSettings> GetAsync(
        RequestScope scope, CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope,
            Permissions.ReservationRead,
            ResourceTypes.Property,
            scope.PropertyId,
            cancellationToken);

        return await LoadAsync(scope.PropertyId, cancellationToken);
    }

    /// <summary>Write the property's configuration, creating it the first time.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="edit">The values to apply.</param>
    /// <param name="version">The version the caller last read; 0 to create.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The stored configuration.</returns>
    /// <remarks>
    /// <b>The card series is not editable here.</b> <c>edit</c> carries a
    /// prefix but no next-number: a property that could set the counter
    /// backwards would issue a card number twice, and two guests signing one
    /// number is the records defect the unique index exists to prevent.
    /// </remarks>
    public async Task<GuestOpsSettings> SaveAsync(
        RequestScope scope,
        SettingsEdit edit,
        long version,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAsync(
            scope, Permissions.Configure, ResourceTypes.Property, scope.PropertyId, cancellationToken);

        if (edit.HomeCountry.Length != 2)
        {
            throw new InvalidRequestException(
                "home_country must be an ISO 3166-1 alpha-2 code — it decides which guests "
                + "count as from outside, and a wrong one changes what every card demands");
        }

        if (edit.ReportingDueHours <= 0)
        {
            throw new InvalidRequestException(
                "reporting_due_hours must be positive — it is an offset from arrival (R18), "
                + "and a deadline before the guest arrives is not a deadline");
        }

        var settings = await db.Settings
            .FirstOrDefaultAsync(s => s.PropertyId == scope.PropertyId, cancellationToken);

        if (settings is null)
        {
            settings = new GuestOpsSettings { PropertyId = scope.PropertyId };
            db.Settings.Add(settings);

            // Tracked from here, so the instance this returns must not read as the
            // declared defaults — the save is the moment it stops being them.
            settings.Stored = true;
        }
        else if (settings.Version != version)
        {
            throw new ConcurrencyException("guestops_settings", scope.PropertyId, version);
        }

        settings.HomeCountry = edit.HomeCountry.ToUpperInvariant();
        settings.RequiredForHomeCountry = [.. edit.RequiredForHomeCountry];
        settings.RequiredForVisitors = [.. edit.RequiredForVisitors];
        settings.AcceptedIdTypes = [.. edit.AcceptedIdTypes];
        settings.SignatureRequired = edit.SignatureRequired;
        settings.PrintOnCheckIn = edit.PrintOnCheckIn;
        settings.CardNumberPrefix = edit.CardNumberPrefix;
        settings.ReportingRequired = edit.ReportingRequired;
        settings.ReportingAppliesTo = edit.ReportingAppliesTo;
        settings.ReportingAuthority = edit.ReportingAuthority;
        settings.ReportingDueHours = edit.ReportingDueHours;
        settings.Version++;

        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    /// <summary>The property's configuration, or the declared defaults.</summary>
    /// <param name="propertyId">The property.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>
    /// The row, with <see cref="GuestOpsSettings.Stored"/> true; or
    /// <see cref="GuestOpsSettings.DefaultsFor"/> where the property has none.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Internal to this assembly because the registration and reporting services
    /// need it without re-authorizing: they have already asked for their own
    /// permission on the stay, and asking twice would mean a person who may
    /// capture a card also needs a property-level read.
    /// </para>
    /// <para>
    /// <b>This threw <c>NotFoundException("guestops_settings", …)</c> until
    /// 2026-09-30, and that is the deadlock GUEST-Q15 closed.</b> The module
    /// envelope answers a domain not-found with 404, and this method has five
    /// call sites — the Setup screen, the registration card, the capture write
    /// and its read-back — so one absent row 404'd four surfaces, <i>including
    /// the only screen that could create the row</i>. An unconfigured property is
    /// not a missing one: it is a property nobody has configured yet, which is an
    /// answer rather than a failure.
    /// </para>
    /// <para>
    /// <b><c>Stored</c> is set here, on both arms, because it is not a
    /// column.</b> EF materialises a row with the CLR default — <c>false</c> — so
    /// a found row would otherwise claim to be the defaults. Setting it where the
    /// two arms meet is the one place that cannot disagree with itself.
    /// </para>
    /// </remarks>
    internal async Task<GuestOpsSettings> LoadAsync(
        Guid propertyId, CancellationToken cancellationToken)
    {
        var row = await db.Settings
            .FirstOrDefaultAsync(s => s.PropertyId == propertyId, cancellationToken);

        if (row is null) return GuestOpsSettings.DefaultsFor(propertyId);

        row.Stored = true;
        return row;
    }

    /// <summary>Take the next number in the property's series.</summary>
    /// <param name="settings">The property's configuration, tracked by the context.</param>
    /// <returns>The formatted card number.</returns>
    /// <remarks>
    /// <para>
    /// <b>Minted inside the caller's transaction</b>, by incrementing the row
    /// the caller already holds — so the number and the card it belongs to are
    /// written in one commit. A number taken in its own transaction would leave
    /// a gap whenever the card failed to save, and a gap in a registration
    /// series is a question a property gets asked at an inspection.
    /// </para>
    /// <para>
    /// The row's optimistic version is what serialises two desks minting at
    /// once: the second commit fails and is retried rather than reusing a
    /// number.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <b>It refuses the declared defaults by name</b> — added 2026-09-30 with
    /// <see cref="GuestOpsSettings.DefaultsFor"/>. Since an unconfigured read now
    /// answers with an <i>untracked</i> instance, incrementing that instance would
    /// mint a number and persist nothing: the context has no entity to save, so
    /// the next card would take the same number. <i>A gap in a registration series
    /// is a question a property gets asked at an inspection; a REPEAT is worse.</i>
    /// <para>
    /// Nothing calls this today — the capture path does not mint — so the hazard
    /// is not live. It is refused rather than documented because the day somebody
    /// wires it, a comment would be the only thing standing between them and a
    /// duplicate, and this platform has already paid for that trade.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <b>Public rather than internal, by ADR 0025's own order</b> — the same
    /// reasoning <c>ApplicationContext</c> settled in this application: its test
    /// reaches it, no application in this repository uses
    /// <c>InternalsVisibleTo</c>, and inventing one to keep a keyword would be a
    /// new mechanism where widening is what the rule asks for. The refusal below
    /// is the deliverable and a guard asserted by nothing is the defect this
    /// whole change exists to stop repeating.
    /// </remarks>
    public static string MintCardNumber(GuestOpsSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Stored)
        {
            throw new InvalidOperationException(
                "a card number cannot be minted from the declared defaults — this property "
                + "has no settings row, so the incremented number would not be saved and the "
                + "next card would repeat it. Save the property's settings first.");
        }

        var number = settings.NextCardNumber;
        settings.NextCardNumber++;
        settings.Version++;

        return $"{settings.CardNumberPrefix}{number}";
    }
}
