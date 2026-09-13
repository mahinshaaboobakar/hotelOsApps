using HotelOS.RoomCare.Domain;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Infrastructure.Configuration.Vocabulary;

namespace HotelOS.RoomCare.Infrastructure.Configuration;

/// <summary>The task and the six tables beside it — chapter 03 §2.3, §2.4.</summary>
internal static class TaskTables
{
    public static void Configure(ModelBuilder model)
    {
        Task(model);
        Beside(model);
        Story(model);
    }

    private static void Task(ModelBuilder model) =>
        model.Entity<RoomTask>(task =>
        {
            task.ToTable("room_task", t =>
            {
                t.HasCheckConstraint("ck_room_task__window", OneOf("window", ServiceWindowName.All));
                t.HasCheckConstraint("ck_room_task__service", OneOf("service", Service.All));
                t.HasCheckConstraint("ck_room_task__priority", OneOf("priority", PriorityBand.All));
                t.HasCheckConstraint("ck_room_task__linen_due", OneOf("linen_due", LinenDue.All));
                t.HasCheckConstraint("ck_room_task__inspection_rule", OneOf("inspection_rule", InspectionRule.All));
                t.HasCheckConstraint("ck_room_task__status", OneOf("status", RoomTaskStatus.All));
                t.HasCheckConstraint("ck_room_task__outcome", NullOrOneOf("outcome", TaskOutcome.All));
                t.HasCheckConstraint("ck_room_task__partial_done", EachOneOf("partial_done", PartialPart.All));
                t.HasCheckConstraint("ck_room_task__decided_by", OneOf("decided_by", DecidedBy.All));
                t.HasCheckConstraint(
                    "ck_room_task__ended_has_outcome",
                    "status NOT IN ('ENDED', 'CLOSED_BY_POLICY') OR outcome IS NOT NULL");
                t.HasCheckConstraint(
                    "ck_room_task__area_or_room", "(service = 'AREA_CLEAN') = (room_id IS NULL)");
                t.HasCheckConstraint("ck_room_task__minutes", "minutes_expected >= 0 AND extra_minutes >= 0");
            });
            task.HasKey(x => x.Id);
            task.Property(x => x.Window).HasMaxLength(8).IsRequired();
            task.Property(x => x.Service).HasMaxLength(16).IsRequired();
            task.Property(x => x.Priority).HasMaxLength(16).IsRequired();
            task.Property(x => x.LinenDue).HasMaxLength(8).IsRequired();
            task.Property(x => x.InspectionRule).HasMaxLength(12).IsRequired();
            task.Property(x => x.ChecklistRef).HasMaxLength(120);
            task.Property(x => x.Status).HasMaxLength(16).IsRequired();
            task.Property(x => x.Outcome).HasMaxLength(24);
            task.Property(x => x.Reduction).HasMaxLength(300);
            task.Property(x => x.DecidedBy).HasMaxLength(12).IsRequired();
            task.Property(x => x.Credits).HasPrecision(6, 2);
            task.Property(x => x.DecisionInputs).AsJson();
            task.Property(x => x.Version).IsConcurrencyToken();
            task.Ignore(x => x.IsOpen);

            // One service per room per day per window — the reconcile's anchor.
            task.HasIndex(x => new { x.PropertyId, x.LocationId, x.OperatingDay, x.Window, x.Service, x.DueAt })
                .IsUnique()
                .AreNullsDistinct(false);
            task.HasIndex(x => new { x.PropertyId, x.OperatingDay, x.Status });
            task.HasIndex(x => new { x.PropertyId, x.AssignedToUserId, x.Status });
        });

    private static void Beside(ModelBuilder model)
    {
        model.Entity<TaskPhase>(phase =>
        {
            phase.ToTable("task_phase", t =>
            {
                t.HasCheckConstraint("ck_task_phase__phase", OneOf("phase", Phase.All));
                t.HasCheckConstraint("ck_task_phase__status", OneOf("status", PhaseStatus.All));
            });
            phase.HasKey(x => x.Id);
            phase.Property(x => x.Phase).HasMaxLength(8).IsRequired();
            phase.Property(x => x.Status).HasMaxLength(8).IsRequired();
            phase.Property(x => x.Note).HasMaxLength(300);
            phase.HasIndex(x => new { x.TaskId, x.Sequence }).IsUnique();
        });

        model.Entity<TaskAttempt>(attempt =>
        {
            attempt.ToTable("task_attempt", t =>
            {
                t.HasCheckConstraint("ck_task_attempt__found", OneOf("found", AttemptFound.All));
                t.HasCheckConstraint("ck_task_attempt__partial_done", EachOneOf("partial_done", PartialPart.All));
            });
            attempt.HasKey(x => x.Id);
            attempt.Property(x => x.Found).HasMaxLength(8).IsRequired();
            attempt.Property(x => x.Note).HasMaxLength(300);
            attempt.HasIndex(x => new { x.TaskId, x.At });
        });

        model.Entity<TaskAssignment>(assignment =>
        {
            assignment.ToTable("task_assignment", t =>
            {
                t.HasCheckConstraint("ck_task_assignment__by_kind", OneOf("assigned_by_kind", ActorKind.All));
                t.HasCheckConstraint("ck_task_assignment__via", OneOf("via", Via.All));
                t.HasCheckConstraint("ck_task_assignment__mode", OneOf("mode", AssignmentMode.All));
                t.HasCheckConstraint("ck_task_assignment__end_reason", NullOrOneOf("end_reason", AssignmentEnd.All));
                t.HasCheckConstraint("ck_task_assignment__end_pair", "(ended_at IS NULL) = (end_reason IS NULL)");
            });
            assignment.HasKey(x => x.Id);
            assignment.Property(x => x.AssignedByKind).HasMaxLength(12).IsRequired();
            assignment.Property(x => x.Via).HasMaxLength(12).IsRequired();
            assignment.Property(x => x.Mode).HasMaxLength(8).IsRequired();
            assignment.Property(x => x.EndReason).HasMaxLength(16);
            assignment.Ignore(x => x.IsCurrent);

            // At most one current assignment per task — two supervisors in the
            // same second produce one row and one refusal, never two (§7.8).
            assignment.HasIndex(x => x.TaskId).IsUnique().HasFilter("ended_at IS NULL");
            assignment.HasIndex(x => new { x.PropertyId, x.UserId, x.EndedAt });
        });

        model.Entity<TaskWorkSession>(session =>
        {
            session.ToTable("task_work_session", t => t.HasCheckConstraint(
                "ck_task_work_session__end_reason", NullOrOneOf("end_reason", SessionEnd.All)));
            session.HasKey(x => x.Id);
            session.Property(x => x.EndReason).HasMaxLength(12);
            session.Ignore(x => x.IsRunning);
            session.HasIndex(x => x.TaskId).IsUnique().HasFilter("ended_at IS NULL");
            session.HasIndex(x => new { x.PropertyId, x.UserId, x.EndedAt });
        });
    }

    private static void Story(ModelBuilder model)
    {
        model.Entity<TaskHistory>(history =>
        {
            history.ToTable("task_history", t =>
            {
                t.HasCheckConstraint("ck_task_history__by_kind", OneOf("by_kind", ActorKind.All));
                t.HasCheckConstraint("ck_task_history__via", OneOf("via", Via.All));
                t.HasCheckConstraint("ck_task_history__kind", OneOf("kind", HistoryKind.All));
            });
            history.HasKey(x => x.Id);
            history.Property(x => x.ByKind).HasMaxLength(12).IsRequired();
            history.Property(x => x.Via).HasMaxLength(12).IsRequired();
            history.Property(x => x.Kind).HasMaxLength(24).IsRequired();
            history.Property(x => x.FromStatus).HasMaxLength(16);
            history.Property(x => x.ToStatus).HasMaxLength(16);
            history.Property(x => x.Reason).HasMaxLength(500);
            history.HasIndex(x => new { x.TaskId, x.At });
        });

        model.Entity<TaskJobTouch>(touch =>
        {
            touch.ToTable("task_job_touch");
            touch.HasKey(x => x.Id);
            touch.Property(x => x.JobNumber).HasMaxLength(40);
            touch.Property(x => x.Summary).HasMaxLength(300);
            touch.HasIndex(x => x.EventId).IsUnique();
            touch.HasIndex(x => new { x.PropertyId, x.RoomId, x.OperatingDay });
        });

        model.Entity<TaskIssue>(issue =>
        {
            issue.ToTable("task_issue");
            issue.HasKey(x => x.Id);
            issue.Property(x => x.ItemHint).HasMaxLength(80);
            issue.Property(x => x.Note).HasMaxLength(1000).IsRequired();
            issue.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
            issue.HasIndex(x => x.CorrelationId).IsUnique();
            issue.HasIndex(x => new { x.PropertyId, x.RoomId, x.At });
        });
    }
}
