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
/// <b>A property with no row is unconfigured, and that is reported.</b> This
/// service invents no defaults: a property configured to require nothing and a
/// property nobody has configured are different facts, and only one of them is
/// a reason to trust a blank card.
/// </para>
/// </remarks>
public sealed class SettingsService(GuestOpsDbContext db, IKernelAuthorizer authorizer)
{
    /// <summary>This property's configuration.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The configuration row.</returns>
    /// <exception cref="NotFoundException">The property has not been configured.</exception>
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

    /// <summary>The property's configuration, or a refusal that says what to do.</summary>
    /// <param name="propertyId">The property.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>The configuration row.</returns>
    /// <remarks>
    /// Internal to this assembly because the registration and reporting services
    /// need it without re-authorizing: they have already asked for their own
    /// permission on the stay, and asking twice would mean a person who may
    /// capture a card also needs a property-level read.
    /// </remarks>
    internal async Task<GuestOpsSettings> LoadAsync(
        Guid propertyId, CancellationToken cancellationToken)
        => await db.Settings
            .FirstOrDefaultAsync(s => s.PropertyId == propertyId, cancellationToken)
            ?? throw new NotFoundException("guestops_settings", propertyId);

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
    internal static string MintCardNumber(GuestOpsSettings settings)
    {
        var number = settings.NextCardNumber;
        settings.NextCardNumber++;
        settings.Version++;

        return $"{settings.CardNumberPrefix}{number}";
    }
}
