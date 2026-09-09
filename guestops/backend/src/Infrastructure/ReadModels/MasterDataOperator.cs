using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.ReadModels;

/// <summary>
/// Who is signed in, and where — the two facts the bar attributes writes to.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two tables, one purpose.</b> Its neighbours take a file per table because
/// each answers its own question; these two answer one — <i>who is at this
/// desk</i> — and splitting them would put a question's halves in two places
/// and leave neither able to state it.
/// </para>
/// <para>
/// <b>Why this exists at all.</b> The bar used to draw a name and a property
/// written into the module: <c>"Anitha Menon"</c>, <c>"Front Office · Avenue
/// Regent"</c>. A realm cannot know either — <c>ModuleIdentity</c> carries the
/// package id, version and capabilities, and <c>PropertyEnvironment</c> carries
/// a timezone and a locale, by ruling (<c>SHELL-Q52</c>) rather than by
/// omission. What a realm cannot know, <b>its own backend can</b>: design page
/// 63 §3, <i>"a bundle calls only its own backend; everything else, its backend
/// does."</i> The session token this request carried was validated by the SDK,
/// so <c>RequestScope</c> holds the user and the property, and both names are
/// one <c>SELECT</c> away on a schema this application is already granted.
/// </para>
/// <para>
/// <b>Keyless, read-only, excluded from migrations</b> — the same terms as
/// every read model here. Nothing is stored: the answer is rendered and
/// discarded, so no <c>guestops</c> table learns a person's name.
/// </para>
/// <para>
/// <b>Both may be absent, and absent is not a default.</b> A user with no staff
/// record has no display name, and the caller may be a service rather than a
/// person. The view returns null and the bar says the operator is not
/// established — never a placeholder, because a name on this bar is an
/// attribution claim on every write the screens make.
/// </para>
/// </remarks>
public sealed class MasterDataStaffName
{
    /// <summary>The Identity user this staff record belongs to, when it does.</summary>
    public Guid? UserId { get; set; }

    /// <summary>What this person is called on a screen.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Deactivated staff are not drawn as the signed-in operator.</summary>
    public bool Active { get; set; }

    /// <summary>Soft deletion — ADR 0062's logical removal.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Keyless, over <c>masterdata.staff</c>. Never written.</summary>
public sealed class MasterDataStaffNameConfiguration
    : IEntityTypeConfiguration<MasterDataStaffName>
{
    public void Configure(EntityTypeBuilder<MasterDataStaffName> builder)
    {
        builder.HasNoKey();
        builder.ToTable("staff", "masterdata", table => table.ExcludeFromMigrations());
    }
}

/// <summary>What this property is called, for the bar to name the desk.</summary>
public sealed class MasterDataPropertyName
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>Keyless, over <c>masterdata.properties</c>. Never written.</summary>
public sealed class MasterDataPropertyNameConfiguration
    : IEntityTypeConfiguration<MasterDataPropertyName>
{
    public void Configure(EntityTypeBuilder<MasterDataPropertyName> builder)
    {
        builder.HasNoKey();
        builder.ToTable("properties", "masterdata", table => table.ExcludeFromMigrations());
    }
}
