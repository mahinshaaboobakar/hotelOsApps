using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.Jobs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jobs");

            migrationBuilder.CreateTable(
                name: "category",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category", x => x.id);
                    table.CheckConstraint("ck_category__department_code_present", "length(btrim(department_code)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "closing_policy",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    auto_close_hours = table.Column<int>(type: "integer", nullable: false),
                    rating_on_close = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_closing_policy", x => x.id);
                    table.CheckConstraint("ck_closing__hours", "auto_close_hours >= 0");
                });

            migrationBuilder.CreateTable(
                name: "concern_policy",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    untriaged_stuck_minutes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concern_policy", x => x.id);
                    table.CheckConstraint("ck_concern_policy__scope_nested", "(category_id IS NULL OR department_code IS NOT NULL) AND (item_id IS NULL OR category_id IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "concern_subscription",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    concern = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    only_priority = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    repeat_minutes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concern_subscription", x => x.id);
                    table.CheckConstraint("ck_subscription__role", "role IN ('ASSIGNEE', 'SUPERVISOR', 'MANAGER', 'JOBS_MANAGER', 'GENERAL_MANAGER')");
                });

            migrationBuilder.CreateTable(
                name: "department_presence",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    follow_shifts = table.Column<bool>(type: "boolean", nullable: false),
                    staffed = table.Column<bool>(type: "boolean", nullable: false),
                    since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    on_shift = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_department_presence", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hold_policy",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_hold_days = table.Column<int>(type: "integer", nullable: false),
                    warn_days_before = table.Column<int>(type: "integer", nullable: false),
                    warn_role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    warn_assignee_on_day = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hold_policy", x => x.id);
                    table.CheckConstraint("ck_hold__warn_role", "warn_role IN ('ASSIGNEE', 'SUPERVISOR', 'MANAGER', 'JOBS_MANAGER', 'GENERAL_MANAGER')");
                });

            migrationBuilder.CreateTable(
                name: "job",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    priority = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    priority_decided_by = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    raised_via = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    raised_kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    raised_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduled_for = table.Column<DateOnly>(type: "date", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    job_status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    cycle = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    restricted = table.Column<bool>(type: "boolean", nullable: false),
                    hold_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    hold_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    parent_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    step_no = table.Column<int>(type: "integer", nullable: true),
                    concern_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    delete_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job", x => x.id);
                    table.CheckConstraint("ck_job__deleted_has_reason", "deleted_at IS NULL OR delete_reason IS NOT NULL");
                    table.CheckConstraint("ck_job__guest_has_stay", "raised_kind <> 'GUEST' OR stay_id IS NOT NULL");
                    table.CheckConstraint("ck_job__hold_pair", "job_status <> 'ON_HOLD' OR hold_reason IS NOT NULL");
                    table.CheckConstraint("ck_job__priority", "priority IN ('P1', 'P2', 'P3', 'NOT_TRIAGED')");
                    table.CheckConstraint("ck_job__priority_decided_by", "priority_decided_by IN ('MANUAL', 'FLOW', 'CATALOGUE', 'NONE')");
                    table.CheckConstraint("ck_job__raised_kind", "raised_kind IN ('STAFF', 'GUEST', 'APPLICATION')");
                    table.CheckConstraint("ck_job__raised_via", "raised_via IN ('APP', 'QR', 'GUEST_APP', 'WHATSAPP')");
                    table.CheckConstraint("ck_job__status", "job_status IN ('SCHEDULED', 'RAISED', 'ASSIGNED', 'ACCEPTED', 'IN_PROGRESS', 'ON_HOLD', 'RESOLVED', 'CLOSED', 'CANCELLED')");
                    table.CheckConstraint("ck_job__step_pair", "(parent_job_id IS NULL) = (step_no IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "job_assignment",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignee_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    how = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_assignment", x => x.id);
                    table.CheckConstraint("ck_job_assignment__how", "how IN ('MANUAL', 'AUTO')");
                    table.CheckConstraint("ck_job_assignment__one_target", "(assignee_user_id IS NOT NULL)::int + (team_id IS NOT NULL)::int <= 1");
                });

            migrationBuilder.CreateTable(
                name: "job_attachment",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bytes = table.Column<long>(type: "bigint", nullable: false),
                    added_by = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_attachment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_concern_history",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concern = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    accountable_role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ladder_step = table.Column<int>(type: "integer", nullable: false),
                    accountable_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    concern_policy_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_concern_history", x => x.id);
                    table.CheckConstraint("ck_job_concern__concern", "concern IN ('ON_TRACK', 'AT_RISK', 'BREACHED', 'STUCK')");
                    table.CheckConstraint("ck_job_concern__role", "accountable_role IN ('ASSIGNEE', 'SUPERVISOR', 'MANAGER', 'JOBS_MANAGER', 'GENERAL_MANAGER')");
                });

            migrationBuilder.CreateTable(
                name: "job_link",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_link", x => x.id);
                    table.CheckConstraint("ck_job_link__not_self", "job_id <> linked_job_id");
                });

            migrationBuilder.CreateTable(
                name: "job_note",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    @internal = table.Column<bool>(name: "internal", type: "boolean", nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_note", x => x.id);
                    table.CheckConstraint("ck_job_note__author_kind", "author_kind IN ('STAFF', 'GUEST', 'APPLICATION')");
                });

            migrationBuilder.CreateTable(
                name: "job_nudge",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concern = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    as_role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_nudge", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_rating",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stars = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_rating", x => x.id);
                    table.CheckConstraint("ck_job_rating__stars", "stars BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "job_reminder",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    for_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    remind_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    kind = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    fired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_reminder", x => x.id);
                    table.CheckConstraint("ck_job_reminder__kind", "kind IN ('MANUAL', 'HOLD')");
                });

            migrationBuilder.CreateTable(
                name: "job_resolution",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resolution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_resolution", x => x.id);
                    table.CheckConstraint("ck_job_resolution__other_needs_note", "resolution_id IS NOT NULL OR note IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "job_status_history",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    to_status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    by_what = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_status_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_work_session",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    paused_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pause_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    stopped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    worked_seconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_work_session", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "property_item_policy",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active_here = table.Column<bool>(type: "boolean", nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    default_priority = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    due_within_minutes = table.Column<int>(type: "integer", nullable: true),
                    concern_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    auto_assign = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    auto_assign_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_item_policy", x => x.id);
                    table.CheckConstraint("ck_item_policy__auto_assign", "auto_assign IN ('USER', 'TEAM')");
                    table.CheckConstraint("ck_item_policy__priority", "default_priority IS NULL OR default_priority IN ('P1', 'P2', 'P3')");
                });

            migrationBuilder.CreateTable(
                name: "property_job_sequence",
                schema: "jobs",
                columns: table => new
                {
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    next = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_job_sequence", x => x.property_id);
                });

            migrationBuilder.CreateTable(
                name: "resolution",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    note_required = table.Column<bool>(type: "boolean", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resolution", x => x.id);
                    table.CheckConstraint("ck_resolution__item_needs_category", "item_id IS NULL OR category_id IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "service_hours",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    from = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    to = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_hours", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "item",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    default_priority = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    due_within_minutes = table.Column<int>(type: "integer", nullable: true),
                    restricted_by_default = table.Column<bool>(type: "boolean", nullable: false),
                    guest_requestable = table.Column<bool>(type: "boolean", nullable: false),
                    photo_on_completion = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item", x => x.id);
                    table.CheckConstraint("ck_item__default_priority", "default_priority IN ('P1', 'P2', 'P3')");
                    table.CheckConstraint("ck_item__due_positive", "due_within_minutes IS NULL OR due_within_minutes > 0");
                    table.CheckConstraint("ck_item__photo", "photo_on_completion IN ('NONE', 'OPTIONAL', 'REQUIRED')");
                    table.ForeignKey(
                        name: "fk_item_category_category_id",
                        column: x => x.category_id,
                        principalSchema: "jobs",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "concern_ladder_step",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    step_no = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    trigger = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    delay_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concern_ladder_step", x => x.id);
                    table.CheckConstraint("ck_ladder__delay", "delay_minutes >= 0");
                    table.CheckConstraint("ck_ladder__role", "role IN ('ASSIGNEE', 'SUPERVISOR', 'MANAGER', 'JOBS_MANAGER', 'GENERAL_MANAGER')");
                    table.CheckConstraint("ck_ladder__trigger", "trigger IN ('AT_RISK', 'BREACHED')");
                    table.ForeignKey(
                        name: "fk_concern_ladder_step_concern_policy_policy_id",
                        column: x => x.policy_id,
                        principalSchema: "jobs",
                        principalTable: "concern_policy",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "concern_policy_rule",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    due_within_minutes = table.Column<int>(type: "integer", nullable: true),
                    at_risk_percent = table.Column<int>(type: "integer", nullable: false),
                    not_accepted_minutes = table.Column<int>(type: "integer", nullable: true),
                    no_session_minutes = table.Column<int>(type: "integer", nullable: true),
                    manager_at_risk = table.Column<bool>(type: "boolean", nullable: false),
                    runs_outside_presence = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concern_policy_rule", x => x.id);
                    table.CheckConstraint("ck_concern_rule__at_risk", "at_risk_percent BETWEEN 1 AND 99");
                    table.CheckConstraint("ck_concern_rule__priority", "priority IN ('P1', 'P2', 'P3')");
                    table.ForeignKey(
                        name: "fk_concern_policy_rule_concern_policy_policy_id",
                        column: x => x.policy_id,
                        principalSchema: "jobs",
                        principalTable: "concern_policy",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_alias",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alias = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_alias", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_alias_item_item_id",
                        column: x => x.item_id,
                        principalSchema: "jobs",
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_category_organization_id_code",
                schema: "jobs",
                table: "category",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_closing_policy_property_id_department_code",
                schema: "jobs",
                table: "closing_policy",
                columns: new[] { "property_id", "department_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_concern_ladder_step_policy_id_priority_step_no",
                schema: "jobs",
                table: "concern_ladder_step",
                columns: new[] { "policy_id", "priority", "step_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_concern_policy_property_id_department_code_category_id_item",
                schema: "jobs",
                table: "concern_policy",
                columns: new[] { "property_id", "department_code", "category_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_concern_policy_rule_policy_id_priority",
                schema: "jobs",
                table: "concern_policy_rule",
                columns: new[] { "policy_id", "priority" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_concern_subscription_property_id_role",
                schema: "jobs",
                table: "concern_subscription",
                columns: new[] { "property_id", "role" });

            migrationBuilder.CreateIndex(
                name: "ix_department_presence_property_id_department_code",
                schema: "jobs",
                table: "department_presence",
                columns: new[] { "property_id", "department_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hold_policy_property_id",
                schema: "jobs",
                table: "hold_policy",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_category_id",
                schema: "jobs",
                table: "item",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_organization_id_code",
                schema: "jobs",
                table: "item",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_alias_alias",
                schema: "jobs",
                table: "item_alias",
                column: "alias");

            migrationBuilder.CreateIndex(
                name: "ix_item_alias_item_id",
                schema: "jobs",
                table: "item_alias",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_parent_job_id",
                schema: "jobs",
                table: "job",
                column: "parent_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_property_id_job_number",
                schema: "jobs",
                table: "job",
                columns: new[] { "property_id", "job_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_property_id_job_status_department_code",
                schema: "jobs",
                table: "job",
                columns: new[] { "property_id", "job_status", "department_code" });

            migrationBuilder.CreateIndex(
                name: "ix_job_property_id_scheduled_for",
                schema: "jobs",
                table: "job",
                columns: new[] { "property_id", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_job_stay_id",
                schema: "jobs",
                table: "job",
                column: "stay_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_assignment_job_id_ended_at",
                schema: "jobs",
                table: "job_assignment",
                columns: new[] { "job_id", "ended_at" });

            migrationBuilder.CreateIndex(
                name: "ix_job_assignment_property_id_assignee_user_id_ended_at",
                schema: "jobs",
                table: "job_assignment",
                columns: new[] { "property_id", "assignee_user_id", "ended_at" });

            migrationBuilder.CreateIndex(
                name: "ix_job_attachment_job_id",
                schema: "jobs",
                table: "job_attachment",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_concern_history_job_id_since",
                schema: "jobs",
                table: "job_concern_history",
                columns: new[] { "job_id", "since" });

            migrationBuilder.CreateIndex(
                name: "ix_job_concern_history_property_id_concern_since",
                schema: "jobs",
                table: "job_concern_history",
                columns: new[] { "property_id", "concern", "since" });

            migrationBuilder.CreateIndex(
                name: "ix_job_link_job_id_linked_job_id",
                schema: "jobs",
                table: "job_link",
                columns: new[] { "job_id", "linked_job_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_note_job_id_at",
                schema: "jobs",
                table: "job_note",
                columns: new[] { "job_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_job_nudge_job_id_sent_at",
                schema: "jobs",
                table: "job_nudge",
                columns: new[] { "job_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_job_nudge_property_id_to_user_id_read_at",
                schema: "jobs",
                table: "job_nudge",
                columns: new[] { "property_id", "to_user_id", "read_at" });

            migrationBuilder.CreateIndex(
                name: "ix_job_rating_job_id",
                schema: "jobs",
                table: "job_rating",
                column: "job_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_reminder_property_id_fired_at_remind_at",
                schema: "jobs",
                table: "job_reminder",
                columns: new[] { "property_id", "fired_at", "remind_at" });

            migrationBuilder.CreateIndex(
                name: "ix_job_resolution_job_id",
                schema: "jobs",
                table: "job_resolution",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_status_history_job_id_at",
                schema: "jobs",
                table: "job_status_history",
                columns: new[] { "job_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_job_work_session_job_id_stopped_at",
                schema: "jobs",
                table: "job_work_session",
                columns: new[] { "job_id", "stopped_at" });

            migrationBuilder.CreateIndex(
                name: "ix_job_work_session_property_id_user_id_stopped_at",
                schema: "jobs",
                table: "job_work_session",
                columns: new[] { "property_id", "user_id", "stopped_at" });

            migrationBuilder.CreateIndex(
                name: "ix_property_item_policy_property_id_item_id",
                schema: "jobs",
                table: "property_item_policy",
                columns: new[] { "property_id", "item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_resolution_organization_id_category_id_item_id",
                schema: "jobs",
                table: "resolution",
                columns: new[] { "organization_id", "category_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_service_hours_property_id_department_code",
                schema: "jobs",
                table: "service_hours",
                columns: new[] { "property_id", "department_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "closing_policy",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "concern_ladder_step",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "concern_policy_rule",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "concern_subscription",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "department_presence",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "hold_policy",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "item_alias",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_assignment",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_attachment",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_concern_history",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_link",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_note",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_nudge",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_rating",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_reminder",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_resolution",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_status_history",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "job_work_session",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "property_item_policy",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "property_job_sequence",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "resolution",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "service_hours",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "concern_policy",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "item",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "category",
                schema: "jobs");
        }
    }
}
