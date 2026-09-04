using HotelOS.Jobs.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Infrastructure.Configuration;

/// <summary>
/// The job and its satellites as tables — design §2.1–2.2. Vocabularies are
/// CHECK constraints so the database refuses a spelling the code does not know.
/// </summary>
public static class JobTables
{
    private static string OneOf(string column, IReadOnlyList<string> values) =>
        $"{column} IN ({string.Join(", ", values.Select(v => $"'{v}'"))})";

    public static void Configure(ModelBuilder model)
    {
        Job(model);
        Work(model);
        Story(model);
        model.Entity<PropertyJobSequence>(s =>
        {
            s.ToTable("property_job_sequence");
            s.HasKey(x => x.PropertyId);
            s.Property(x => x.PropertyCode).HasMaxLength(50).IsRequired();
        });
    }

    private static void Job(ModelBuilder model)
    {
        model.Entity<Job>(job =>
        {
            job.ToTable("job", table =>
            {
                table.HasCheckConstraint("ck_job__status", OneOf("job_status", JobStatus.All));
                table.HasCheckConstraint("ck_job__priority", OneOf("priority", Priority.All));
                table.HasCheckConstraint(
                    "ck_job__priority_decided_by", OneOf("priority_decided_by", PriorityDecidedBy.All));
                table.HasCheckConstraint("ck_job__raised_via", OneOf("raised_via", RaisedVia.All));
                table.HasCheckConstraint("ck_job__raised_kind", OneOf("raised_kind", RaisedKind.All));
                table.HasCheckConstraint(
                    "ck_job__guest_has_stay", "raised_kind <> 'GUEST' OR stay_id IS NOT NULL");
                table.HasCheckConstraint(
                    "ck_job__hold_pair", "job_status <> 'ON_HOLD' OR hold_reason IS NOT NULL");
                table.HasCheckConstraint(
                    "ck_job__step_pair", "(parent_job_id IS NULL) = (step_no IS NULL)");
                table.HasCheckConstraint(
                    "ck_job__deleted_has_reason", "deleted_at IS NULL OR delete_reason IS NOT NULL");
            });
            job.HasKey(j => j.Id);
            job.Property(j => j.JobNumber).HasMaxLength(40).IsRequired();
            job.Property(j => j.DepartmentCode).HasMaxLength(50).IsRequired();
            job.Property(j => j.Summary).HasMaxLength(300).IsRequired();
            job.Property(j => j.Details).HasMaxLength(4000);
            job.Property(j => j.Priority).HasMaxLength(12).IsRequired();
            job.Property(j => j.PriorityDecidedBy).HasMaxLength(12).IsRequired();
            job.Property(j => j.RaisedVia).HasMaxLength(12).IsRequired();
            job.Property(j => j.RaisedKind).HasMaxLength(12).IsRequired();
            job.Property(j => j.JobStatus).HasMaxLength(12).IsRequired();
            job.Property(j => j.Cycle).HasMaxLength(80);
            job.Property(j => j.HoldReason).HasMaxLength(300);
            job.Property(j => j.DeleteReason).HasMaxLength(300);
            job.Property(j => j.Version).IsConcurrencyToken();
            job.Ignore(j => j.IsOpen);
            job.Ignore(j => j.IsStep);
            job.HasIndex(j => new { j.PropertyId, j.JobNumber }).IsUnique();
            job.HasIndex(j => new { j.PropertyId, j.JobStatus, j.DepartmentCode });
            job.HasIndex(j => new { j.PropertyId, j.ScheduledFor });
            job.HasIndex(j => j.ParentJobId);
            job.HasIndex(j => j.StayId);
        });
    }

    /// <summary>Who holds it and how it is worked: assignment, sessions, resolution.</summary>
    private static void Work(ModelBuilder model)
    {
        model.Entity<JobAssignment>(a =>
        {
            a.ToTable("job_assignment", t =>
            {
                t.HasCheckConstraint("ck_job_assignment__how", "how IN ('MANUAL', 'AUTO')");
                t.HasCheckConstraint(
                    "ck_job_assignment__one_target",
                    "(assignee_user_id IS NOT NULL)::int + (team_id IS NOT NULL)::int <= 1");
            });
            a.HasKey(x => x.Id);
            a.Property(x => x.How).HasMaxLength(8).IsRequired();
            a.Ignore(x => x.IsCurrent);
            a.HasIndex(x => new { x.JobId, x.EndedAt });
            a.HasIndex(x => new { x.PropertyId, x.AssigneeUserId, x.EndedAt });
        });

        model.Entity<JobStatusHistory>(h =>
        {
            h.ToTable("job_status_history");
            h.HasKey(x => x.Id);
            h.Property(x => x.FromStatus).HasMaxLength(12).IsRequired();
            h.Property(x => x.ToStatus).HasMaxLength(12).IsRequired();
            h.Property(x => x.ByWhat).HasMaxLength(12);
            h.Property(x => x.Note).HasMaxLength(300);
            h.HasIndex(x => new { x.JobId, x.At });
        });

        model.Entity<JobWorkSession>(w =>
        {
            w.ToTable("job_work_session");
            w.HasKey(x => x.Id);
            w.Property(x => x.PauseReason).HasMaxLength(200);
            w.Ignore(x => x.IsRunning);
            w.Ignore(x => x.IsPaused);
            w.HasIndex(x => new { x.JobId, x.StoppedAt });
            w.HasIndex(x => new { x.PropertyId, x.UserId, x.StoppedAt });
        });

        model.Entity<JobResolution>(r =>
        {
            r.ToTable("job_resolution", t => t.HasCheckConstraint(
                "ck_job_resolution__other_needs_note", "resolution_id IS NOT NULL OR note IS NOT NULL"));
            r.HasKey(x => x.Id);
            r.Property(x => x.Note).HasMaxLength(2000);
            r.HasIndex(x => x.JobId);
        });
    }

    /// <summary>What is said and recorded about it: notes, attachments, links, concern, nudges, reminders, rating.</summary>
    private static void Story(ModelBuilder model)
    {
        model.Entity<JobNote>(n =>
        {
            n.ToTable("job_note", t => t.HasCheckConstraint(
                "ck_job_note__author_kind", OneOf("author_kind", RaisedKind.All)));
            n.HasKey(x => x.Id);
            n.Property(x => x.AuthorKind).HasMaxLength(12).IsRequired();
            n.Property(x => x.Text).HasMaxLength(4000).IsRequired();
            n.HasIndex(x => new { x.JobId, x.At });
        });

        model.Entity<JobAttachment>(a =>
        {
            a.ToTable("job_attachment");
            a.HasKey(x => x.Id);
            a.Property(x => x.Name).HasMaxLength(200).IsRequired();
            a.HasIndex(x => x.JobId);
        });

        model.Entity<JobLink>(l =>
        {
            l.ToTable("job_link", t => t.HasCheckConstraint(
                "ck_job_link__not_self", "job_id <> linked_job_id"));
            l.HasKey(x => x.Id);
            l.HasIndex(x => new { x.JobId, x.LinkedJobId }).IsUnique();
        });

        model.Entity<JobConcernHistory>(c =>
        {
            c.ToTable("job_concern_history", t =>
            {
                t.HasCheckConstraint("ck_job_concern__concern", OneOf("concern", Concern.All));
                t.HasCheckConstraint("ck_job_concern__role", OneOf("accountable_role", LadderRole.All));
            });
            c.HasKey(x => x.Id);
            c.Property(x => x.Concern).HasMaxLength(12).IsRequired();
            c.Property(x => x.AccountableRole).HasMaxLength(16).IsRequired();
            c.Property(x => x.Reason).HasMaxLength(200).IsRequired();
            c.HasIndex(x => new { x.JobId, x.Since });
            c.HasIndex(x => new { x.PropertyId, x.Concern, x.Since });
        });

        model.Entity<JobNudge>(n =>
        {
            n.ToTable("job_nudge");
            n.HasKey(x => x.Id);
            n.Property(x => x.Concern).HasMaxLength(12).IsRequired();
            n.Property(x => x.AsRole).HasMaxLength(16).IsRequired();
            n.HasIndex(x => new { x.PropertyId, x.ToUserId, x.ReadAt });
            n.HasIndex(x => new { x.JobId, x.SentAt });
        });

        model.Entity<JobReminder>(r =>
        {
            r.ToTable("job_reminder", t => t.HasCheckConstraint(
                "ck_job_reminder__kind", "kind IN ('MANUAL', 'HOLD')"));
            r.HasKey(x => x.Id);
            r.Property(x => x.Note).HasMaxLength(300).IsRequired();
            r.Property(x => x.Kind).HasMaxLength(8).IsRequired();
            r.HasIndex(x => new { x.PropertyId, x.FiredAt, x.RemindAt });
        });

        model.Entity<JobRating>(r =>
        {
            r.ToTable("job_rating", t => t.HasCheckConstraint(
                "ck_job_rating__stars", "stars BETWEEN 1 AND 5"));
            r.HasKey(x => x.Id);
            r.Property(x => x.Text).HasMaxLength(1000);
            r.HasIndex(x => x.JobId).IsUnique();
        });
    }
}
