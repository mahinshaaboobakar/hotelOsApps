using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.RoomCare.Infrastructure.ReadModels;

/// <summary>Master Data's tables Room Care reads through the install grant — keyless, never migrated here.</summary>
/// <remarks>
/// <para>
/// ADR 0092 §4: an application reads canonical Master Data through
/// <c>hotelos_masterdata_reader</c>, in its own context, and Master Data never
/// sees a call. Three constraints make these read models rather than copies
/// (FF's pattern, recorded 2026-09-08): keyless and
/// <c>ExcludeFromMigrations</c>; a stated column count per table; one type per
/// table.
/// </para>
/// <para>
/// Every query names the property — these tables hold every hotel's rows, and a
/// lookup that skipped it would cross the tenancy boundary by omission.
/// </para>
/// </remarks>
public static class MasterDataHouse
{
    /// <summary>Register every read model on the model builder.</summary>
    public static void Configure(ModelBuilder model)
    {
        Keyless<MasterDataProperty>(model, "properties");
        Keyless<MasterDataRoom>(model, "rooms");
        Keyless<MasterDataRoomType>(model, "room_types");
        Keyless<MasterDataZone>(model, "zones");
        Keyless<MasterDataLocation>(model, "locations");
        Keyless<MasterDataDepartment>(model, "departments");
        Keyless<MasterDataStaff>(model, "staff");
    }

    private static void Keyless<T>(ModelBuilder model, string table)
        where T : class =>
        model.Entity<T>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable(table, "masterdata", t => t.ExcludeFromMigrations());
        });
}

/// <summary>A property's zone and day boundary — four columns.</summary>
public sealed class MasterDataProperty
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Timezone { get; set; } = string.Empty;

    public TimeOnly BusinessDayBoundary { get; set; }
}

/// <summary>A room's identity — seven columns; the number is shown, never stored here.</summary>
public sealed class MasterDataRoom
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid RoomTypeId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool Active { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>A room type's code and name — five columns.</summary>
public sealed class MasterDataRoomType
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>A zone's code and name — six columns.</summary>
public sealed class MasterDataZone
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool Active { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>A node of the property's location tree — six columns; public areas are nodes (S3).</summary>
public sealed class MasterDataLocation
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string LocationType { get; set; } = string.Empty;

    public bool Active { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>A department's id by code — four columns.</summary>
public sealed class MasterDataDepartment
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public string Code { get; set; } = string.Empty;

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>A person's display name by login — four columns; read at answer time, never kept.</summary>
public sealed class MasterDataStaff
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset? DeletedAt { get; set; }
}
