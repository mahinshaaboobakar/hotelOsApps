using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Domain.Policy;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Infrastructure.Configuration;

/// <summary>The property's policies as tables — design §2.3 second half, settings frames 1–11.</summary>
public static class PolicyTables
{
    private static string OneOf(string column, IReadOnlyList<string> values) =>
        $"{column} IN ({string.Join(", ", values.Select(v => $"'{v}'"))})";

    public static void Configure(ModelBuilder model)
    {
        model.Entity<PropertyItemPolicy>(p =>
        {
            p.ToTable("property_item_policy", t =>
            {
                t.HasCheckConstraint("ck_item_policy__auto_assign", "auto_assign IN ('USER', 'TEAM')");
                t.HasCheckConstraint(
                    "ck_item_policy__priority",
                    "default_priority IS NULL OR default_priority IN ('P1', 'P2', 'P3')");
            });
            p.HasKey(x => x.Id);
            p.Property(x => x.DisplayName).HasMaxLength(120);
            p.Property(x => x.DefaultPriority).HasMaxLength(4);
            p.Property(x => x.AutoAssign).HasMaxLength(4).IsRequired();
            p.Property(x => x.Version).IsConcurrencyToken();
            p.HasIndex(x => new { x.PropertyId, x.ItemId }).IsUnique();
        });

        model.Entity<ConcernPolicy>(c =>
        {
            c.ToTable("concern_policy", t =>
            {
                t.HasCheckConstraint(
                    "ck_concern_policy__scope_nested",
                    "(category_id IS NULL OR department_code IS NOT NULL) AND (item_id IS NULL OR category_id IS NOT NULL)");
            });
            c.HasKey(x => x.Id);
            c.Property(x => x.Name).HasMaxLength(120).IsRequired();
            c.Property(x => x.DepartmentCode).HasMaxLength(50);
            c.Property(x => x.Version).IsConcurrencyToken();
            c.Ignore(x => x.Specificity);
            c.HasIndex(x => new { x.PropertyId, x.DepartmentCode, x.CategoryId, x.ItemId });
        });

        model.Entity<ConcernPolicyRule>(r =>
        {
            r.ToTable("concern_policy_rule", t =>
            {
                t.HasCheckConstraint("ck_concern_rule__priority", "priority IN ('P1', 'P2', 'P3')");
                t.HasCheckConstraint("ck_concern_rule__at_risk", "at_risk_percent BETWEEN 1 AND 99");
            });
            r.HasKey(x => x.Id);
            r.Property(x => x.Priority).HasMaxLength(4).IsRequired();
            r.HasIndex(x => new { x.PolicyId, x.Priority }).IsUnique();
            r.HasOne<ConcernPolicy>().WithMany().HasForeignKey(x => x.PolicyId);
        });

        model.Entity<ConcernLadderStep>(s =>
        {
            s.ToTable("concern_ladder_step", t =>
            {
                t.HasCheckConstraint("ck_ladder__role", OneOf("role", LadderRole.All));
                t.HasCheckConstraint("ck_ladder__trigger", "trigger IN ('AT_RISK', 'BREACHED')");
                t.HasCheckConstraint("ck_ladder__delay", "delay_minutes >= 0");
            });
            s.HasKey(x => x.Id);
            s.Property(x => x.Priority).HasMaxLength(4).IsRequired();
            s.Property(x => x.Role).HasMaxLength(16).IsRequired();
            s.Property(x => x.Trigger).HasMaxLength(12).IsRequired();
            s.HasIndex(x => new { x.PolicyId, x.Priority, x.StepNo }).IsUnique();
            s.HasOne<ConcernPolicy>().WithMany().HasForeignKey(x => x.PolicyId);
        });

        WhoWhenAndClosing(model);
    }

    /// <summary>Who is told, when a department is present, and how a job closes.</summary>
    private static void WhoWhenAndClosing(ModelBuilder model)
    {
        model.Entity<ConcernSubscription>(s =>
        {
            s.ToTable("concern_subscription", t => t.HasCheckConstraint(
                "ck_subscription__role", OneOf("role", LadderRole.All)));
            s.HasKey(x => x.Id);
            s.Property(x => x.Role).HasMaxLength(16).IsRequired();
            s.Property(x => x.Concern).HasMaxLength(12).IsRequired();
            s.Property(x => x.OnlyPriority).HasMaxLength(4);
            s.Property(x => x.DepartmentCode).HasMaxLength(50);
            s.HasIndex(x => new { x.PropertyId, x.Role });
        });

        model.Entity<ServiceHours>(h =>
        {
            h.ToTable("service_hours");
            h.HasKey(x => x.Id);
            h.Property(x => x.DepartmentCode).HasMaxLength(50);
            h.HasIndex(x => new { x.PropertyId, x.DepartmentCode }).IsUnique();
        });

        model.Entity<DepartmentPresence>(p =>
        {
            p.ToTable("department_presence");
            p.HasKey(x => x.Id);
            p.Property(x => x.DepartmentCode).HasMaxLength(50).IsRequired();
            p.HasIndex(x => new { x.PropertyId, x.DepartmentCode }).IsUnique();
        });

        model.Entity<ClosingPolicy>(c =>
        {
            c.ToTable("closing_policy", t => t.HasCheckConstraint(
                "ck_closing__hours", "auto_close_hours >= 0"));
            c.HasKey(x => x.Id);
            c.Property(x => x.DepartmentCode).HasMaxLength(50);
            c.HasIndex(x => new { x.PropertyId, x.DepartmentCode }).IsUnique();
        });

        model.Entity<HoldPolicy>(h =>
        {
            h.ToTable("hold_policy", t => t.HasCheckConstraint(
                "ck_hold__warn_role", OneOf("warn_role", LadderRole.All)));
            h.HasKey(x => x.Id);
            h.Property(x => x.WarnRole).HasMaxLength(16).IsRequired();
            h.HasIndex(x => x.PropertyId).IsUnique();
        });
    }
}
