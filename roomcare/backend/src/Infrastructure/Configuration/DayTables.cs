using HotelOS.RoomCare.Domain;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Infrastructure.Configuration.Vocabulary;

namespace HotelOS.RoomCare.Infrastructure.Configuration;

/// <summary>The supervisor's lane, restocks, runs, deep cleans and what Workforce announced — chapter 03 §2.5–2.9.</summary>
internal static class DayTables
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<RoomSupervision>(lane =>
        {
            lane.ToTable("room_supervision", t =>
            {
                t.HasCheckConstraint("ck_room_supervision__reason", OneOf("reason", SupervisionReason.All));
                t.HasCheckConstraint("ck_room_supervision__decision", NullOrOneOf("decision", SupervisionDecision.All));
                t.HasCheckConstraint(
                    "ck_room_supervision__decision_complete",
                    "(decision IS NULL) = (decided_at IS NULL) AND (decision IS NULL) = (decided_by_user_id IS NULL)");
            });
            lane.HasKey(x => x.Id);
            lane.Property(x => x.Reason).HasMaxLength(24).IsRequired();
            lane.Property(x => x.Decision).HasMaxLength(16);
            lane.Property(x => x.Note).HasMaxLength(500);
            lane.Ignore(x => x.IsOpen);
            lane.HasIndex(x => new { x.PropertyId, x.RoomId, x.OperatingDay, x.Reason }).IsUnique();
            lane.HasIndex(x => new { x.PropertyId, x.DecidedAt });
        });

        model.Entity<Restock>(restock =>
        {
            restock.ToTable("restock");
            restock.HasKey(x => x.Id);
            restock.Property(x => x.Items).AsJson();
            restock.HasIndex(x => new { x.PropertyId, x.RoomId, x.At });
        });

        model.Entity<PrepareRun>(run =>
        {
            run.ToTable("prepare_run", t =>
            {
                t.HasCheckConstraint("ck_prepare_run__window", OneOf("window", ServiceWindowName.All));
                t.HasCheckConstraint("ck_prepare_run__by_kind", OneOf("by_kind", ActorKind.All));
                t.HasCheckConstraint("ck_prepare_run__via", OneOf("via", Via.All));
                t.HasCheckConstraint("ck_prepare_run__kind", OneOf("kind", RunKind.All));
            });
            run.HasKey(x => x.Id);
            run.Property(x => x.Window).HasMaxLength(8).IsRequired();
            run.Property(x => x.ByKind).HasMaxLength(12).IsRequired();
            run.Property(x => x.Via).HasMaxLength(12).IsRequired();
            run.Property(x => x.Kind).HasMaxLength(12).IsRequired();
            run.HasIndex(x => new { x.PropertyId, x.OperatingDay, x.Window, x.At });
        });

        model.Entity<DeepClean>(project =>
        {
            project.ToTable("deep_clean", t =>
            {
                t.HasCheckConstraint("ck_deep_clean__status", OneOf("status", DeepCleanStatus.All));
                t.HasCheckConstraint(
                    "ck_deep_clean__window_order", "window_from IS NULL OR window_to IS NULL OR window_from <= window_to");
            });
            project.HasKey(x => x.Id);
            project.Property(x => x.Status).HasMaxLength(16).IsRequired();
            project.Property(x => x.BlockCorrelationId).HasMaxLength(64);
            project.Property(x => x.JobCorrelationId).HasMaxLength(64);
            project.Property(x => x.JobStatusSeen).HasMaxLength(16);
            project.Property(x => x.Version).IsConcurrencyToken();
            project.HasIndex(x => new { x.PropertyId, x.RoomId, x.DueOn }).IsUnique();
            project.HasIndex(x => x.JobCorrelationId);
        });

        model.Entity<PostingSeen>(posting =>
        {
            posting.ToTable("posting_seen");
            posting.HasKey(x => x.PostingId);
            posting.Property(x => x.PostingId).ValueGeneratedNever();
            posting.Property(x => x.DepartmentCode).HasMaxLength(50).IsRequired();
            posting.HasIndex(x => new { x.PropertyId, x.DepartmentCode, x.EndedAt });
        });

        model.Entity<ShiftPresence>(presence =>
        {
            presence.ToTable("shift_presence");
            presence.HasKey(x => new { x.PropertyId, x.DepartmentCode });
            presence.Property(x => x.DepartmentCode).HasMaxLength(50);
        });

        model.Entity<RoomCareManagerGrant>(grant =>
        {
            grant.ToTable("roomcare_manager_grant");
            grant.HasKey(x => x.Id);
            grant.HasIndex(x => new { x.PropertyId, x.UserId }).IsUnique().HasFilter("revoked_at IS NULL");
        });
    }
}
