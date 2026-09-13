using HotelOS.RoomCare.Domain;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Infrastructure.Configuration.Vocabulary;

namespace HotelOS.RoomCare.Infrastructure.Configuration;

/// <summary>The property's standard — windows, services, rules, areas, plans and zones (chapter 03 §2.8).</summary>
internal static class StandardTables
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<ServiceWindow>(window =>
        {
            window.ToTable("service_window", t =>
            {
                t.HasCheckConstraint("ck_service_window__window", OneOf("window", ServiceWindowName.All));
                t.HasCheckConstraint("ck_service_window__not_empty", "starts <> ends");
            });
            window.HasKey(x => x.Id);
            window.Property(x => x.Window).HasMaxLength(8).IsRequired();
            window.Property(x => x.Version).IsConcurrencyToken();
            window.HasIndex(x => new { x.PropertyId, x.Window }).IsUnique();
        });

        model.Entity<ServiceStandard>(standard =>
        {
            standard.ToTable("service_standard", t =>
            {
                t.HasCheckConstraint("ck_service_standard__service", OneOf("service", Service.All));
                t.HasCheckConstraint("ck_service_standard__inspection_rule", OneOf("inspection_rule", InspectionRule.All));
                t.HasCheckConstraint("ck_service_standard__phases", EachOneOf("phases", Phase.All));
                t.HasCheckConstraint("ck_service_standard__minutes", "minutes >= 0 AND credits >= 0");
            });
            standard.HasKey(x => x.Id);
            standard.Property(x => x.Service).HasMaxLength(16).IsRequired();
            standard.Property(x => x.InspectionRule).HasMaxLength(12).IsRequired();
            standard.Property(x => x.ChecklistRef).HasMaxLength(120);
            standard.Property(x => x.Credits).HasPrecision(6, 2);
            standard.Property(x => x.Version).IsConcurrencyToken();
            standard.HasIndex(x => new { x.PropertyId, x.RoomTypeId, x.Service }).IsUnique().AreNullsDistinct(false);
        });

        model.Entity<PropertyPolicy>(policy =>
        {
            policy.ToTable("property_policy", t =>
            {
                t.HasCheckConstraint("ck_property_policy__trigger_mode", OneOf("trigger_mode", TriggerMode.All));
                t.HasCheckConstraint("ck_property_policy__who_leads", OneOf("who_leads", WhoLeads.All));
                t.HasCheckConstraint("ck_property_policy__stay_source", OneOf("stay_source", StaySource.All));
                t.HasCheckConstraint("ck_property_policy__board_view", OneOf("board_default_view", BoardView.All));
                t.HasCheckConstraint("ck_property_policy__states_view", OneOf("states_default_view", StatesView.All));
                t.HasCheckConstraint(
                    "ck_property_policy__on_departure", OneOf("on_departure_condition", [Condition.Dirty]));
                t.HasCheckConstraint("ck_property_policy__linen_rule", OneOf("linen_rule_kind", LinenRuleKind.All));
                t.HasCheckConstraint("ck_property_policy__towels", OneOf("towels", TowelRule.All));
                t.HasCheckConstraint("ck_property_policy__ladder", EachOneOf("priority_ladder", PriorityBand.All));
                t.HasCheckConstraint("ck_property_policy__strategy", OneOf("assignment_strategy", AssignmentStrategy.All));
                t.HasCheckConstraint("ck_property_policy__unsold", OneOf("unsold_departure", UnsoldDeparture.All));
                t.HasCheckConstraint(
                    "ck_property_policy__numbers",
                    "linen_every_days > 0 AND refresh_after_days > 0 AND dnd_recheck_minutes > 0 AND supervisor_after_days > 0");
            });
            policy.HasKey(x => x.PropertyId);
            policy.Property(x => x.PropertyId).ValueGeneratedNever();
            policy.Property(x => x.TriggerMode).HasMaxLength(12).IsRequired();
            policy.Property(x => x.WhoLeads).HasMaxLength(12).IsRequired();
            policy.Property(x => x.StaySource).HasMaxLength(12).IsRequired();
            policy.Property(x => x.BoardDefaultView).HasMaxLength(8).IsRequired();
            policy.Property(x => x.StatesDefaultView).HasMaxLength(12).IsRequired();
            policy.Property(x => x.OnDepartureCondition).HasMaxLength(12).IsRequired();
            policy.Property(x => x.LinenRuleKind).HasMaxLength(24).IsRequired();
            policy.Property(x => x.Towels).HasMaxLength(20).IsRequired();
            policy.Property(x => x.AssignmentStrategy).HasMaxLength(16).IsRequired();
            policy.Property(x => x.UnsoldDeparture).HasMaxLength(12).IsRequired();
            policy.Property(x => x.DepartmentCode).HasMaxLength(50).IsRequired();
            policy.Property(x => x.Version).IsConcurrencyToken();
        });

        model.Entity<AreaSchedule>(area =>
        {
            area.ToTable("area_schedule", t => t.HasCheckConstraint("ck_area_schedule__minutes", "minutes > 0"));
            area.HasKey(x => x.Id);
            area.Property(x => x.Version).IsConcurrencyToken();
            area.HasIndex(x => new { x.PropertyId, x.LocationId }).IsUnique();
        });

        model.Entity<DeepCleanPlan>(plan =>
        {
            plan.ToTable("deep_clean_plan", t => t.HasCheckConstraint("ck_deep_clean_plan__months", "every_months > 0"));
            plan.HasKey(x => x.Id);
            plan.Property(x => x.Version).IsConcurrencyToken();
            plan.HasIndex(x => new { x.PropertyId, x.RoomTypeId }).IsUnique();
        });

        model.Entity<RoomZoneAssignment>(zone =>
        {
            zone.ToTable("room_zone_assignment", t => t.HasCheckConstraint(
                "ck_room_zone_assignment__order", "effective_until IS NULL OR effective_from <= effective_until"));
            zone.HasKey(x => x.Id);
            zone.HasIndex(x => new { x.PropertyId, x.RoomId }).IsUnique().HasFilter("effective_until IS NULL");
            zone.HasIndex(x => new { x.PropertyId, x.ZoneId });
        });
    }
}
