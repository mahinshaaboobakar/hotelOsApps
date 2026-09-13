using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.RoomCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialRoomCare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "roomcare");

            migrationBuilder.CreateTable(
                name: "area_schedule",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    times = table.Column<List<TimeOnly>>(type: "time without time zone[]", nullable: false),
                    minutes = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_area_schedule", x => x.id);
                    table.CheckConstraint("ck_area_schedule__minutes", "minutes > 0");
                });

            migrationBuilder.CreateTable(
                name: "deep_clean",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    due_on = table.Column<DateOnly>(type: "date", nullable: false),
                    window_from = table.Column<DateOnly>(type: "date", nullable: true),
                    window_to = table.Column<DateOnly>(type: "date", nullable: true),
                    block_correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    block_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    block_applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    job_correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_status_seen = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    done_on = table.Column<DateOnly>(type: "date", nullable: true),
                    planned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deep_clean", x => x.id);
                    table.CheckConstraint("ck_deep_clean__status", "\"status\" IN ('PLANNED', 'BLOCK_REQUESTED', 'BLOCKED', 'IN_PROGRESS', 'RETURNING', 'DONE', 'CANCELLED')");
                    table.CheckConstraint("ck_deep_clean__window_order", "window_from IS NULL OR window_to IS NULL OR window_from <= window_to");
                });

            migrationBuilder.CreateTable(
                name: "deep_clean_plan",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    every_months = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deep_clean_plan", x => x.id);
                    table.CheckConstraint("ck_deep_clean_plan__months", "every_months > 0");
                });

            migrationBuilder.CreateTable(
                name: "posting_seen",
                schema: "roomcare",
                columns: table => new
                {
                    posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posting_seen", x => x.posting_id);
                });

            migrationBuilder.CreateTable(
                name: "prepare_run",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operating_day = table.Column<DateOnly>(type: "date", nullable: false),
                    window = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    by_kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    via = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    rooms_considered = table.Column<int>(type: "integer", nullable: false),
                    tasks_created = table.Column<int>(type: "integer", nullable: false),
                    tasks_updated = table.Column<int>(type: "integer", nullable: false),
                    tasks_skipped = table.Column<int>(type: "integer", nullable: false),
                    pending = table.Column<int>(type: "integer", nullable: false),
                    unassignable = table.Column<int>(type: "integer", nullable: false),
                    changes_since_previous = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prepare_run", x => x.id);
                    table.CheckConstraint("ck_prepare_run__by_kind", "\"by_kind\" IN ('USER', 'SYSTEM', 'APPLICATION')");
                    table.CheckConstraint("ck_prepare_run__kind", "\"kind\" IN ('FIRST', 'RECONCILE')");
                    table.CheckConstraint("ck_prepare_run__via", "\"via\" IN ('APP', 'HOSPILOT')");
                    table.CheckConstraint("ck_prepare_run__window", "\"window\" IN ('MORNING', 'EVENING')");
                });

            migrationBuilder.CreateTable(
                name: "property_policy",
                schema: "roomcare",
                columns: table => new
                {
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_mode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    who_leads = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    stay_source = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    board_default_view = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    states_default_view = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    on_departure_condition = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    linen_rule_kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    linen_every_days = table.Column<int>(type: "integer", nullable: false),
                    towels = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    turndown_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    refresh_after_days = table.Column<int>(type: "integer", nullable: false),
                    dnd_recheck_minutes = table.Column<int>(type: "integer", nullable: false),
                    supervisor_after_days = table.Column<int>(type: "integer", nullable: false),
                    priority_ladder = table.Column<List<string>>(type: "text[]", nullable: false),
                    assignment_strategy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    unsold_departure = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_policy", x => x.property_id);
                    table.CheckConstraint("ck_property_policy__board_view", "\"board_default_view\" IN ('MAP', 'WALL')");
                    table.CheckConstraint("ck_property_policy__ladder", "\"priority_ladder\" IS NULL OR \"priority_ladder\" <@ ARRAY['SOLD_TONIGHT', 'DEPARTURE', 'DAILY', 'REFRESH']::text[]");
                    table.CheckConstraint("ck_property_policy__linen_rule", "\"linen_rule_kind\" IN ('EVERY_N_DEFERRABLE', 'MUST_BY_N')");
                    table.CheckConstraint("ck_property_policy__numbers", "linen_every_days > 0 AND refresh_after_days > 0 AND dnd_recheck_minutes > 0 AND supervisor_after_days > 0");
                    table.CheckConstraint("ck_property_policy__on_departure", "\"on_departure_condition\" IN ('DIRTY')");
                    table.CheckConstraint("ck_property_policy__states_view", "\"states_default_view\" IN ('SHEET', 'TAP_GRID', 'COMPACT')");
                    table.CheckConstraint("ck_property_policy__stay_source", "\"stay_source\" IN ('PMS', 'GUESTOPS', 'MANUAL')");
                    table.CheckConstraint("ck_property_policy__strategy", "\"assignment_strategy\" IN ('CONTINUITY', 'SAME_ZONE', 'LOWEST_LOAD')");
                    table.CheckConstraint("ck_property_policy__towels", "\"towels\" IN ('DAILY', 'GREEN_PROGRAMME')");
                    table.CheckConstraint("ck_property_policy__trigger_mode", "\"trigger_mode\" IN ('PREPARE', 'AUTOMATIC')");
                    table.CheckConstraint("ck_property_policy__unsold", "\"unsold_departure\" IN ('TODAY', 'MAY_WAIT')");
                    table.CheckConstraint("ck_property_policy__who_leads", "\"who_leads\" IN ('ROOM_CARE', 'PMS')");
                });

            migrationBuilder.CreateTable(
                name: "restock",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    items = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_restock", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "room_observation",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    operating_day = table.Column<DateOnly>(type: "date", nullable: true),
                    occupancy = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    condition = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    stay_statuses = table.Column<List<string>>(type: "text[]", nullable: true),
                    next_sold_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_pseudo_room = table.Column<bool>(type: "boolean", nullable: true),
                    applied = table.Column<bool>(type: "boolean", nullable: false),
                    outcome = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    via = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_observation", x => x.id);
                    table.CheckConstraint("ck_room_observation__condition", "\"condition\" IS NULL OR \"condition\" IN ('DIRTY', 'CLEAN', 'INSPECTED')");
                    table.CheckConstraint("ck_room_observation__manual_has_person", "source <> 'MANUAL' OR by_user_id IS NOT NULL");
                    table.CheckConstraint("ck_room_observation__occupancy", "\"occupancy\" IS NULL OR \"occupancy\" IN ('VACANT', 'OCCUPIED', 'UNKNOWN')");
                    table.CheckConstraint("ck_room_observation__outcome", "\"outcome\" IN ('APPLIED', 'OLDER_THAN_ACT', 'DISAGREEMENT_FLAGGED', 'APPLIED_PMS_LEADS')");
                    table.CheckConstraint("ck_room_observation__source", "\"source\" IN ('PMS', 'GUESTOPS', 'MANUAL', 'ENGINEERING', 'SYSTEM')");
                    table.CheckConstraint("ck_room_observation__stay_statuses", "\"stay_statuses\" IS NULL OR \"stay_statuses\" <@ ARRAY['BOOKED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED', 'NO_SHOW', 'DUE_OUT', 'WAITLISTED', 'PENDING']::text[]");
                    table.CheckConstraint("ck_room_observation__via", "\"via\" IS NULL OR \"via\" IN ('APP', 'HOSPILOT')");
                });

            migrationBuilder.CreateTable(
                name: "room_state",
                schema: "roomcare",
                columns: table => new
                {
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    condition_set_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    condition_set_by_kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    condition_set_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    condition_source = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    occupancy = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    next_sold_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    stay_statuses = table.Column<List<string>>(type: "text[]", nullable: false),
                    is_pseudo_room = table.Column<bool>(type: "boolean", nullable: false),
                    last_observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    disagreement_observed_condition = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    disagreement_observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    disagreement_source = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    disagreement_cleared_by = table.Column<Guid>(type: "uuid", nullable: true),
                    disagreement_cleared_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    disagreement_cleared_kept = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    linen_last_changed_on = table.Column<DateOnly>(type: "date", nullable: true),
                    days_without_service = table.Column<int>(type: "integer", nullable: false),
                    days_counted_through = table.Column<DateOnly>(type: "date", nullable: true),
                    supervised_since = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_state", x => x.room_id);
                    table.CheckConstraint("ck_room_state__condition", "\"condition\" IN ('DIRTY', 'CLEAN', 'INSPECTED')");
                    table.CheckConstraint("ck_room_state__days_without_service", "days_without_service >= 0");
                    table.CheckConstraint("ck_room_state__disagreement_condition", "\"disagreement_observed_condition\" IS NULL OR \"disagreement_observed_condition\" IN ('DIRTY', 'CLEAN', 'INSPECTED')");
                    table.CheckConstraint("ck_room_state__disagreement_kept", "\"disagreement_cleared_kept\" IS NULL OR \"disagreement_cleared_kept\" IN ('OURS', 'THEIRS')");
                    table.CheckConstraint("ck_room_state__disagreement_source", "\"disagreement_source\" IS NULL OR \"disagreement_source\" IN ('PMS', 'GUESTOPS', 'MANUAL', 'ENGINEERING', 'SYSTEM')");
                    table.CheckConstraint("ck_room_state__occupancy", "\"occupancy\" IN ('VACANT', 'OCCUPIED', 'UNKNOWN')");
                    table.CheckConstraint("ck_room_state__set_by_kind", "\"condition_set_by_kind\" IN ('USER', 'SYSTEM', 'APPLICATION')");
                    table.CheckConstraint("ck_room_state__source", "\"condition_source\" IN ('ATTENDANT', 'INSPECTION', 'SUPERVISOR', 'PMS', 'GUESTOPS', 'MANUAL', 'SYSTEM')");
                    table.CheckConstraint("ck_room_state__stay_statuses", "\"stay_statuses\" IS NULL OR \"stay_statuses\" <@ ARRAY['BOOKED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED', 'NO_SHOW', 'DUE_OUT', 'WAITLISTED', 'PENDING']::text[]");
                });

            migrationBuilder.CreateTable(
                name: "room_supervision",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operating_day = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decision = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_supervision", x => x.id);
                    table.CheckConstraint("ck_room_supervision__decision", "\"decision\" IS NULL OR \"decision\" IN ('DND_APPROVED', 'CLEAN', 'OTHER')");
                    table.CheckConstraint("ck_room_supervision__decision_complete", "(decision IS NULL) = (decided_at IS NULL) AND (decision IS NULL) = (decided_by_user_id IS NULL)");
                    table.CheckConstraint("ck_room_supervision__reason", "\"reason\" IN ('DAYS_WITHOUT_SERVICE', 'DISAGREEMENT', 'ARRIVAL_BEFORE_WINDOW', 'NOBODY_AVAILABLE')");
                });

            migrationBuilder.CreateTable(
                name: "room_task",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operating_day = table.Column<DateOnly>(type: "date", nullable: false),
                    window = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    service = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    priority_rank = table.Column<int>(type: "integer", nullable: false),
                    earliest_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    linen_due = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    minutes_expected = table.Column<int>(type: "integer", nullable: false),
                    credits = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    inspection_rule = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    checklist_ref = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    outcome = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    partial_done = table.Column<List<string>>(type: "text[]", nullable: false),
                    reduction = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    decided_by = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    decision_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_inputs = table.Column<string>(type: "jsonb", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    extra_minutes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_task", x => x.id);
                    table.CheckConstraint("ck_room_task__area_or_room", "(service = 'AREA_CLEAN') = (room_id IS NULL)");
                    table.CheckConstraint("ck_room_task__decided_by", "\"decided_by\" IN ('PREPARE', 'AUTOMATIC', 'SUPERVISOR', 'SYSTEM')");
                    table.CheckConstraint("ck_room_task__ended_has_outcome", "status NOT IN ('ENDED', 'CLOSED_BY_POLICY') OR outcome IS NOT NULL");
                    table.CheckConstraint("ck_room_task__inspection_rule", "\"inspection_rule\" IN ('NONE', 'ALWAYS', 'ARRIVALS', 'EVERY_NTH', 'VIP')");
                    table.CheckConstraint("ck_room_task__linen_due", "\"linen_due\" IN ('NOT_DUE', 'DUE', 'MUST')");
                    table.CheckConstraint("ck_room_task__minutes", "minutes_expected >= 0 AND extra_minutes >= 0");
                    table.CheckConstraint("ck_room_task__outcome", "\"outcome\" IS NULL OR \"outcome\" IN ('DONE', 'PARTIAL', 'DECLINED', 'DND', 'SKIPPED_BY_GUEST', 'NOT_REACHED', 'SUPERVISOR_DND_APPROVED', 'SUPERVISOR_CLEANED', 'SUPERVISOR_DECIDED')");
                    table.CheckConstraint("ck_room_task__partial_done", "\"partial_done\" IS NULL OR \"partial_done\" <@ ARRAY['BATHROOM', 'TOWELS', 'RUBBISH', 'BED']::text[]");
                    table.CheckConstraint("ck_room_task__priority", "\"priority\" IN ('SOLD_TONIGHT', 'DEPARTURE', 'DAILY', 'REFRESH')");
                    table.CheckConstraint("ck_room_task__service", "\"service\" IN ('DEPARTURE_CLEAN', 'DAILY_SERVICE', 'TURNDOWN', 'REFRESH', 'AREA_CLEAN')");
                    table.CheckConstraint("ck_room_task__status", "\"status\" IN ('PENDING_POLICY', 'PLANNED', 'ASSIGNED', 'IN_PROGRESS', 'ENDED', 'CLOSED_BY_POLICY')");
                    table.CheckConstraint("ck_room_task__window", "\"window\" IN ('MORNING', 'EVENING')");
                });

            migrationBuilder.CreateTable(
                name: "room_zone_assignment",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_until = table.Column<DateOnly>(type: "date", nullable: true),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_zone_assignment", x => x.id);
                    table.CheckConstraint("ck_room_zone_assignment__order", "effective_until IS NULL OR effective_from <= effective_until");
                });

            migrationBuilder.CreateTable(
                name: "roomcare_manager_grant",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roomcare_manager_grant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_standard",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    minutes = table.Column<int>(type: "integer", nullable: false),
                    credits = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    inspection_rule = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    checklist_ref = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    phases = table.Column<List<string>>(type: "text[]", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_standard", x => x.id);
                    table.CheckConstraint("ck_service_standard__inspection_rule", "\"inspection_rule\" IN ('NONE', 'ALWAYS', 'ARRIVALS', 'EVERY_NTH', 'VIP')");
                    table.CheckConstraint("ck_service_standard__minutes", "minutes >= 0 AND credits >= 0");
                    table.CheckConstraint("ck_service_standard__phases", "\"phases\" IS NULL OR \"phases\" <@ ARRAY['STRIP', 'CLEAN', 'MAKE_UP', 'DONE', 'INSPECT']::text[]");
                    table.CheckConstraint("ck_service_standard__service", "\"service\" IN ('DEPARTURE_CLEAN', 'DAILY_SERVICE', 'TURNDOWN', 'REFRESH', 'AREA_CLEAN')");
                });

            migrationBuilder.CreateTable(
                name: "service_window",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    window = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    starts = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ends = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    allow_assignment_outside = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_window", x => x.id);
                    table.CheckConstraint("ck_service_window__not_empty", "starts <> ends");
                    table.CheckConstraint("ck_service_window__window", "\"window\" IN ('MORNING', 'EVENING')");
                });

            migrationBuilder.CreateTable(
                name: "shift_presence",
                schema: "roomcare",
                columns: table => new
                {
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    on_now = table.Column<int>(type: "integer", nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_presence", x => new { x.property_id, x.department_code });
                });

            migrationBuilder.CreateTable(
                name: "task_assignment",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_by_kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    assigned_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    via = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_reason = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    mode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_assignment", x => x.id);
                    table.CheckConstraint("ck_task_assignment__by_kind", "\"assigned_by_kind\" IN ('USER', 'SYSTEM', 'APPLICATION')");
                    table.CheckConstraint("ck_task_assignment__end_pair", "(ended_at IS NULL) = (end_reason IS NULL)");
                    table.CheckConstraint("ck_task_assignment__end_reason", "\"end_reason\" IS NULL OR \"end_reason\" IN ('REASSIGNED', 'ENDED', 'SHIFT_ENDED', 'STAFF_EXITED')");
                    table.CheckConstraint("ck_task_assignment__mode", "\"mode\" IN ('PROPOSED', 'ACCEPTED', 'MANUAL')");
                    table.CheckConstraint("ck_task_assignment__via", "\"via\" IN ('APP', 'HOSPILOT')");
                });

            migrationBuilder.CreateTable(
                name: "task_attempt",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    found = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    partial_done = table.Column<List<string>>(type: "text[]", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_attempt", x => x.id);
                    table.CheckConstraint("ck_task_attempt__found", "\"found\" IN ('DONE', 'PARTIAL', 'DECLINED', 'DND')");
                    table.CheckConstraint("ck_task_attempt__partial_done", "\"partial_done\" IS NULL OR \"partial_done\" <@ ARRAY['BATHROOM', 'TOWELS', 'RUBBISH', 'BED']::text[]");
                });

            migrationBuilder.CreateTable(
                name: "task_history",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    by_kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    via = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    from_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    to_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_history", x => x.id);
                    table.CheckConstraint("ck_task_history__by_kind", "\"by_kind\" IN ('USER', 'SYSTEM', 'APPLICATION')");
                    table.CheckConstraint("ck_task_history__kind", "\"kind\" IN ('TRANSITION', 'REDUCTION', 'REPRIORITISED', 'SUPERVISOR_DECISION', 'DISAGREEMENT_CLEARED', 'EXTRA_TIME')");
                    table.CheckConstraint("ck_task_history__via", "\"via\" IN ('APP', 'HOSPILOT')");
                });

            migrationBuilder.CreateTable(
                name: "task_issue",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_hint = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    media_id = table.Column<Guid>(type: "uuid", nullable: true),
                    by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_issue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_job_touch",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operating_day = table.Column<DateOnly>(type: "date", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_job_touch", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_phase",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_phase", x => x.id);
                    table.CheckConstraint("ck_task_phase__phase", "\"phase\" IN ('STRIP', 'CLEAN', 'MAKE_UP', 'DONE', 'INSPECT')");
                    table.CheckConstraint("ck_task_phase__status", "\"status\" IN ('PENDING', 'ACTIVE', 'DONE', 'SKIPPED', 'FAILED')");
                });

            migrationBuilder.CreateTable(
                name: "task_work_session",
                schema: "roomcare",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_reason = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_work_session", x => x.id);
                    table.CheckConstraint("ck_task_work_session__end_reason", "\"end_reason\" IS NULL OR \"end_reason\" IN ('PAUSE', 'END', 'REASSIGNED')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_area_schedule_property_id_location_id",
                schema: "roomcare",
                table: "area_schedule",
                columns: new[] { "property_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deep_clean_job_correlation_id",
                schema: "roomcare",
                table: "deep_clean",
                column: "job_correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_deep_clean_property_id_room_id_due_on",
                schema: "roomcare",
                table: "deep_clean",
                columns: new[] { "property_id", "room_id", "due_on" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deep_clean_plan_property_id_room_type_id",
                schema: "roomcare",
                table: "deep_clean_plan",
                columns: new[] { "property_id", "room_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_posting_seen_property_id_department_code_ended_at",
                schema: "roomcare",
                table: "posting_seen",
                columns: new[] { "property_id", "department_code", "ended_at" });

            migrationBuilder.CreateIndex(
                name: "ix_prepare_run_property_id_operating_day_window_at",
                schema: "roomcare",
                table: "prepare_run",
                columns: new[] { "property_id", "operating_day", "window", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_restock_property_id_room_id_at",
                schema: "roomcare",
                table: "restock",
                columns: new[] { "property_id", "room_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_room_observation_event_id_room_id",
                schema: "roomcare",
                table: "room_observation",
                columns: new[] { "event_id", "room_id" },
                unique: true,
                filter: "event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_room_observation_property_id_room_id_occurred_at",
                schema: "roomcare",
                table: "room_observation",
                columns: new[] { "property_id", "room_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_room_state_property_id_condition",
                schema: "roomcare",
                table: "room_state",
                columns: new[] { "property_id", "condition" });

            migrationBuilder.CreateIndex(
                name: "ix_room_supervision_property_id_decided_at",
                schema: "roomcare",
                table: "room_supervision",
                columns: new[] { "property_id", "decided_at" });

            migrationBuilder.CreateIndex(
                name: "ix_room_supervision_property_id_room_id_operating_day_reason",
                schema: "roomcare",
                table: "room_supervision",
                columns: new[] { "property_id", "room_id", "operating_day", "reason" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_room_task_property_id_assigned_to_user_id_status",
                schema: "roomcare",
                table: "room_task",
                columns: new[] { "property_id", "assigned_to_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_room_task_property_id_location_id_operating_day_window_serv",
                schema: "roomcare",
                table: "room_task",
                columns: new[] { "property_id", "location_id", "operating_day", "window", "service", "due_at" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_room_task_property_id_operating_day_status",
                schema: "roomcare",
                table: "room_task",
                columns: new[] { "property_id", "operating_day", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_room_zone_assignment_property_id_room_id",
                schema: "roomcare",
                table: "room_zone_assignment",
                columns: new[] { "property_id", "room_id" },
                unique: true,
                filter: "effective_until IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_room_zone_assignment_property_id_zone_id",
                schema: "roomcare",
                table: "room_zone_assignment",
                columns: new[] { "property_id", "zone_id" });

            migrationBuilder.CreateIndex(
                name: "ix_roomcare_manager_grant_property_id_user_id",
                schema: "roomcare",
                table: "roomcare_manager_grant",
                columns: new[] { "property_id", "user_id" },
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_service_standard_property_id_room_type_id_service",
                schema: "roomcare",
                table: "service_standard",
                columns: new[] { "property_id", "room_type_id", "service" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_service_window_property_id_window",
                schema: "roomcare",
                table: "service_window",
                columns: new[] { "property_id", "window" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_assignment_property_id_user_id_ended_at",
                schema: "roomcare",
                table: "task_assignment",
                columns: new[] { "property_id", "user_id", "ended_at" });

            migrationBuilder.CreateIndex(
                name: "ix_task_assignment_task_id",
                schema: "roomcare",
                table: "task_assignment",
                column: "task_id",
                unique: true,
                filter: "ended_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_task_attempt_task_id_at",
                schema: "roomcare",
                table: "task_attempt",
                columns: new[] { "task_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_task_history_task_id_at",
                schema: "roomcare",
                table: "task_history",
                columns: new[] { "task_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_task_issue_correlation_id",
                schema: "roomcare",
                table: "task_issue",
                column: "correlation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_issue_property_id_room_id_at",
                schema: "roomcare",
                table: "task_issue",
                columns: new[] { "property_id", "room_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_task_job_touch_event_id",
                schema: "roomcare",
                table: "task_job_touch",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_job_touch_property_id_room_id_operating_day",
                schema: "roomcare",
                table: "task_job_touch",
                columns: new[] { "property_id", "room_id", "operating_day" });

            migrationBuilder.CreateIndex(
                name: "ix_task_phase_task_id_sequence",
                schema: "roomcare",
                table: "task_phase",
                columns: new[] { "task_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_work_session_property_id_user_id_ended_at",
                schema: "roomcare",
                table: "task_work_session",
                columns: new[] { "property_id", "user_id", "ended_at" });

            migrationBuilder.CreateIndex(
                name: "ix_task_work_session_task_id",
                schema: "roomcare",
                table: "task_work_session",
                column: "task_id",
                unique: true,
                filter: "ended_at IS NULL");

            // The two reads chapter 03 §2.10 exposes and nothing else: what fills
            // RoomContext.room_condition, and a room's day for the desk (S5 c12).
            // DDL in a migration is the one place hand-written SQL belongs.
            migrationBuilder.Sql("""
                CREATE VIEW roomcare.room_condition_now AS
                SELECT room_id, property_id, condition, condition_source AS source, condition_set_at AS set_at, version
                FROM roomcare.room_state;
                """);
            migrationBuilder.Sql("""
                CREATE VIEW roomcare.room_day AS
                SELECT t.property_id, t.room_id, t.operating_day, t.id AS task_id, t.service, t."window", t.status, t.outcome,
                       t.partial_done, t.assigned_to_user_id,
                       (SELECT max(a.at) FROM roomcare.task_attempt a WHERE a.task_id = t.id) AS last_attempt_at,
                       (SELECT count(*) FROM roomcare.task_attempt a WHERE a.task_id = t.id) AS attempts,
                       (SELECT s.decision FROM roomcare.room_supervision s
                         WHERE s.room_id = t.room_id AND s.operating_day = t.operating_day AND s.decision IS NOT NULL
                         ORDER BY s.decided_at DESC LIMIT 1) AS supervisor_decision,
                       (SELECT count(*) FROM roomcare.task_job_touch j
                         WHERE j.room_id = t.room_id AND j.operating_day = t.operating_day) AS job_touches,
                       r.linen_last_changed_on
                FROM roomcare.room_task t
                LEFT JOIN roomcare.room_state r ON r.room_id = t.room_id
                WHERE t.room_id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS roomcare.room_day;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS roomcare.room_condition_now;");

            migrationBuilder.DropTable(
                name: "area_schedule",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "deep_clean",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "deep_clean_plan",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "posting_seen",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "prepare_run",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "property_policy",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "restock",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "room_observation",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "room_state",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "room_supervision",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "room_task",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "room_zone_assignment",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "roomcare_manager_grant",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "service_standard",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "service_window",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "shift_presence",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "task_assignment",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "task_attempt",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "task_history",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "task_issue",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "task_job_touch",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "task_phase",
                schema: "roomcare");

            migrationBuilder.DropTable(
                name: "task_work_session",
                schema: "roomcare");
        }
    }
}
