using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Catalogue;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Infrastructure.Configuration;

/// <summary>The organisation-scoped catalogue as tables — design §2.3, ruling 3 of 2026-09-03.</summary>
public static class CatalogueTables
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<Category>(c =>
        {
            c.ToTable("category", t => t.HasCheckConstraint(
                "ck_category__department_code_present", "length(btrim(department_code)) > 0"));
            c.HasKey(x => x.Id);
            c.Property(x => x.Code).HasMaxLength(50).IsRequired();
            c.Property(x => x.Name).HasMaxLength(120).IsRequired();
            c.Property(x => x.DepartmentCode).HasMaxLength(50).IsRequired();
            c.Property(x => x.Version).IsConcurrencyToken();
            c.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        });

        model.Entity<Item>(i =>
        {
            i.ToTable("item", t =>
            {
                t.HasCheckConstraint(
                    "ck_item__default_priority", "default_priority IN ('P1', 'P2', 'P3')");
                t.HasCheckConstraint(
                    "ck_item__photo", "photo_on_completion IN ('NONE', 'OPTIONAL', 'REQUIRED')");
                t.HasCheckConstraint(
                    "ck_item__due_positive", "due_within_minutes IS NULL OR due_within_minutes > 0");
            });
            i.HasKey(x => x.Id);
            i.Property(x => x.Code).HasMaxLength(50).IsRequired();
            i.Property(x => x.Name).HasMaxLength(120).IsRequired();
            i.Property(x => x.DefaultPriority).HasMaxLength(4).IsRequired();
            i.Property(x => x.PhotoOnCompletion).HasMaxLength(10).IsRequired();
            i.Property(x => x.Version).IsConcurrencyToken();
            i.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            i.HasIndex(x => x.CategoryId);
            i.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId);
        });

        model.Entity<ItemAlias>(a =>
        {
            a.ToTable("item_alias");
            a.HasKey(x => x.Id);
            a.Property(x => x.Alias).HasMaxLength(120).IsRequired();
            a.Property(x => x.Language).HasMaxLength(16);
            a.HasIndex(x => x.ItemId);
            a.HasIndex(x => x.Alias);
            a.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId);
        });

        model.Entity<Resolution>(r =>
        {
            r.ToTable("resolution", t => t.HasCheckConstraint(
                "ck_resolution__item_needs_category", "item_id IS NULL OR category_id IS NOT NULL"));
            r.HasKey(x => x.Id);
            r.Property(x => x.Name).HasMaxLength(120).IsRequired();
            r.HasIndex(x => new { x.OrganizationId, x.CategoryId, x.ItemId });
        });
    }
}
