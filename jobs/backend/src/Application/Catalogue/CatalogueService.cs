using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Catalogue;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Catalogue;

/// <summary>
/// Curating the organisation's catalogue — <c>job.curate</c>, frame 7: categories,
/// items with their aliases, resolutions. Organisation-scoped (ruling 3 of
/// 2026-09-03); a property activates and overrides in
/// <see cref="PropertyCatalogueService"/>, never here.
/// </summary>
public class CatalogueService(JobsDbContext db, IKernelAuthorizer authorizer, TimeProvider clock)
{
    public async Task<Category> SaveCategoryAsync(RequestScope scope, CategoryCommand command, CancellationToken cancellationToken)
    {
        var organization = await CuratorAsync(scope, cancellationToken);
        var code = Code(command.Code);
        var department = Code(command.DepartmentCode);
        if (string.IsNullOrWhiteSpace(command.Name)) throw new InvalidRequestException("name is required");

        var now = clock.GetUtcNow();
        var category = command.Id is { } id
            ? await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organization, cancellationToken)
              ?? throw new NotFoundException("category", id)
            : new Category { Id = Guid.CreateVersion7(), OrganizationId = organization, CreatedAt = now };
        if (command.Id is not null && category.Version != command.ExpectedVersion)
        {
            throw new ConcurrencyException("category", category.Id, command.ExpectedVersion ?? 0);
        }

        if (await db.Categories.AnyAsync(c => c.OrganizationId == organization && c.Code == code && c.Id != category.Id, cancellationToken))
        {
            throw new InvalidRequestException($"a category coded {code} already exists");
        }

        category.Code = code;
        category.Name = command.Name.Trim();
        category.DepartmentCode = department;
        category.Active = command.Active;
        category.UpdatedAt = now;
        category.Version += 1;
        if (command.Id is null) db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<Item> SaveItemAsync(RequestScope scope, ItemCommand command, CancellationToken cancellationToken)
    {
        var organization = await CuratorAsync(scope, cancellationToken);
        ValidateItem(command);
        var code = Code(command.Code);
        _ = await db.Categories.FirstOrDefaultAsync(
                c => c.Id == command.CategoryId && c.OrganizationId == organization && c.DeletedAt == null, cancellationToken)
            ?? throw new InvalidRequestException("category_id is not a category of this organisation");

        var now = clock.GetUtcNow();
        var item = command.Id is { } id
            ? await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organization, cancellationToken)
              ?? throw new NotFoundException("item", id)
            : new Item { Id = Guid.CreateVersion7(), OrganizationId = organization, CreatedAt = now };
        if (command.Id is not null && item.Version != command.ExpectedVersion)
        {
            throw new ConcurrencyException("item", item.Id, command.ExpectedVersion ?? 0);
        }

        if (await db.Items.AnyAsync(i => i.OrganizationId == organization && i.Code == code && i.Id != item.Id, cancellationToken))
        {
            throw new InvalidRequestException($"an item coded {code} already exists");
        }

        item.CategoryId = command.CategoryId;
        item.Code = code;
        item.Name = command.Name.Trim();
        item.DefaultPriority = command.DefaultPriority;
        item.DueWithinMinutes = command.DueWithinMinutes;
        item.RestrictedByDefault = command.RestrictedByDefault;
        item.GuestRequestable = command.GuestRequestable;
        item.PhotoOnCompletion = command.PhotoOnCompletion;
        item.Active = command.Active;
        item.UpdatedAt = now;
        item.Version += 1;
        if (command.Id is null) db.Items.Add(item);
        if (command.Aliases is { } aliases) await ReplaceAliasesAsync(item, aliases, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<Resolution> AddResolutionAsync(RequestScope scope, ResolutionCommand command, CancellationToken cancellationToken)
    {
        var organization = await CuratorAsync(scope, cancellationToken);
        if (string.IsNullOrWhiteSpace(command.Name)) throw new InvalidRequestException("name is required");
        if (command.ItemId is { } itemId)
        {
            var item = await db.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.OrganizationId == organization, cancellationToken)
                ?? throw new InvalidRequestException("item_id is not an item of this organisation");
            if (command.CategoryId is { } given && given != item.CategoryId)
            {
                throw new InvalidRequestException("the item is not in that category");
            }
        }

        var resolution = new Resolution
        {
            Id = Guid.CreateVersion7(), OrganizationId = organization,
            CategoryId = command.CategoryId ?? (command.ItemId is { } i
                ? (await db.Items.FirstAsync(x => x.Id == i, cancellationToken)).CategoryId : null),
            ItemId = command.ItemId, Name = command.Name.Trim(), NoteRequired = command.NoteRequired,
        };
        db.CatalogueResolutions.Add(resolution);
        await db.SaveChangesAsync(cancellationToken);
        return resolution;
    }

    private async Task ReplaceAliasesAsync(Item item, IReadOnlyList<string> aliases, CancellationToken cancellationToken)
    {
        var existing = await db.ItemAliases.Where(a => a.ItemId == item.Id).ToListAsync(cancellationToken);
        db.ItemAliases.RemoveRange(existing);
        foreach (var alias in aliases.Select(a => a.Trim()).Where(a => a.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            db.ItemAliases.Add(new ItemAlias { Id = Guid.CreateVersion7(), ItemId = item.Id, Alias = alias });
        }
    }

    private static void ValidateItem(ItemCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) throw new InvalidRequestException("name is required");
        if (command.DefaultPriority is not (Priority.P1 or Priority.P2 or Priority.P3))
        {
            throw new InvalidRequestException("default_priority must be P1, P2 or P3");
        }

        if (!PhotoRule.All.Contains(command.PhotoOnCompletion))
        {
            throw new InvalidRequestException("photo_on_completion must be NONE, OPTIONAL or REQUIRED");
        }

        if (command.DueWithinMinutes is <= 0) throw new InvalidRequestException("due_within_minutes must be positive");
    }

    private static string Code(string raw)
    {
        var code = raw.Trim().ToUpperInvariant();
        return code.Length > 0 ? code : throw new InvalidRequestException("a code is required");
    }

    /// <summary>The organisation the caller curates for — the grant is organisation-level.</summary>
    private async Task<Guid> CuratorAsync(RequestScope scope, CancellationToken cancellationToken)
    {
        var organization = scope.OrganizationId
            ?? throw new InvalidRequestException("curating needs an organisation in scope");
        await authorizer.RequireAsync(scope, Permissions.Curate, "organization", organization, cancellationToken);
        return organization;
    }
}
