using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelOS.GuestOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TheReservationBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "guestops");

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expected_stay_count = table.Column<int>(type: "integer", nullable: true),
                    is_complete = table.Column<bool>(type: "boolean", nullable: true),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guests",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_given = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    name_family = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    name_as_given = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    preferences = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "held_facts",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    lifecycle = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_held_facts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rooms_out_of_order",
                schema: "guestops",
                columns: table => new
                {
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: true),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rooms_out_of_order", x => x.room_id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "guestops",
                columns: table => new
                {
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    home_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    required_for_home_country = table.Column<List<string>>(type: "text[]", nullable: false),
                    required_for_visitors = table.Column<List<string>>(type: "text[]", nullable: false),
                    accepted_id_types = table.Column<List<string>>(type: "text[]", nullable: false),
                    signature_required = table.Column<bool>(type: "boolean", nullable: false),
                    print_on_check_in = table.Column<bool>(type: "boolean", nullable: false),
                    card_number_prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    next_card_number = table.Column<long>(type: "bigint", nullable: false),
                    reporting_required = table.Column<bool>(type: "boolean", nullable: false),
                    reporting_applies_to = table.Column<int>(type: "integer", nullable: false),
                    reporting_authority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reporting_due_hours = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.property_id);
                    table.CheckConstraint("ck_settings__due_hours", "reporting_due_hours > 0");
                });

            migrationBuilder.CreateTable(
                name: "stop_sells",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    set_by = table.Column<Guid>(type: "uuid", nullable: true),
                    set_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stop_sells", x => x.id);
                    table.CheckConstraint("ck_stop_sells__range", "to_date >= from_date");
                });

            migrationBuilder.CreateTable(
                name: "booking_external_refs",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    identifier_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    external_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_external_refs", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_external_refs_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "guestops",
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_stays",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lifecycle = table.Column<int>(type: "integer", nullable: false),
                    arrival_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    arrival_basis = table.Column<int>(type: "integer", nullable: false),
                    departure_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    departure_basis = table.Column<int>(type: "integer", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: true),
                    walk_in = table.Column<bool>(type: "boolean", nullable: false),
                    pms_unknown = table.Column<bool>(type: "boolean", nullable: false),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_stays", x => x.id);
                    table.ForeignKey(
                        name: "fk_room_stays_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "guestops",
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contact_points",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    value_cipher = table.Column<byte[]>(type: "bytea", nullable: false),
                    value_index = table.Column<byte[]>(type: "bytea", nullable: false),
                    tech_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    use_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: true),
                    origin = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contact_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_contact_points_guests_guest_id",
                        column: x => x.guest_id,
                        principalSchema: "guestops",
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assignments",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignments_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commercial_terms",
                schema: "guestops",
                columns: table => new
                {
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    rate_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    amount_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    amount_tax_basis = table.Column<int>(type: "integer", nullable: true),
                    guarantee_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    guarantee_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    on_hold = table.Column<bool>(type: "boolean", nullable: false),
                    reserves_inventory = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    deposit_offset_days_from_booking = table.Column<int>(type: "integer", nullable: true),
                    cancel_offset_days_from_arrival = table.Column<int>(type: "integer", nullable: true),
                    cancel_drop_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    penalty_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    penalty_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    penalty_tax_basis = table.Column<int>(type: "integer", nullable: true),
                    penalty_nights = table.Column<int>(type: "integer", nullable: true),
                    penalty_basis = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commercial_terms", x => x.stay_id);
                    table.ForeignKey(
                        name: "fk_commercial_terms_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registrations",
                schema: "guestops",
                columns: table => new
                {
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    name_as_on_id = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    nationality = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    id_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    id_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    id_issuer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    id_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    arriving_from = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    proceeding_to = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    purpose_of_visit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    vehicle_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    passport_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    passport_issue = table.Column<DateOnly>(type: "date", nullable: true),
                    passport_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    passport_place = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    visa_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    visa_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    visa_issue = table.Column<DateOnly>(type: "date", nullable: true),
                    visa_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    arrived_in_country_on = table.Column<DateOnly>(type: "date", nullable: true),
                    port_of_arrival = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    document_refs = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    signature_ref = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    captured_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registrations", x => x.stay_id);
                    table.ForeignKey(
                        name: "fk_registrations_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_absences",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    raw_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_absences", x => x.id);
                    table.ForeignKey(
                        name: "fk_stay_absences_room_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_disagreements",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aspect = table.Column<int>(type: "integer", nullable: false),
                    our_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    pms_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    override_actor = table.Column<Guid>(type: "uuid", nullable: true),
                    override_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    pms_value_at_override = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    raised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    cleared_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cleared_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_disagreements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stay_disagreements_room_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_external_refs",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    identifier_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    external_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_external_refs", x => x.id);
                    table.ForeignKey(
                        name: "fk_stay_external_refs_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_guests",
                schema: "guestops",
                columns: table => new
                {
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: true),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    origin = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_guests", x => new { x.stay_id, x.guest_id });
                    table.ForeignKey(
                        name: "fk_stay_guests_guests_guest_id",
                        column: x => x.guest_id,
                        principalSchema: "guestops",
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stay_guests_room_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_link_candidates",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    held_fact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_score = table.Column<double>(type: "double precision", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    raised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_link_candidates", x => x.id);
                    table.ForeignKey(
                        name: "fk_stay_link_candidates_room_stays_local_stay_id",
                        column: x => x.local_stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_notes",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    author = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_stay_notes_room_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_reporting",
                schema: "guestops",
                columns: table => new
                {
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_by = table.Column<DateOnly>(type: "date", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    filed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    filed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    authority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_reporting", x => x.stay_id);
                    table.ForeignKey(
                        name: "fk_stay_reporting_room_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_requests",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    logged_by = table.Column<Guid>(type: "uuid", nullable: true),
                    logged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    handed_off = table.Column<bool>(type: "boolean", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_stay_requests_room_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_sources",
                schema: "guestops",
                columns: table => new
                {
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    travel_agent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    market_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    meal_plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    adults = table.Column<int>(type: "integer", nullable: false),
                    children = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_sources", x => x.stay_id);
                    table.ForeignKey(
                        name: "fk_stay_sources_room_stays_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "room_stays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_source_detail",
                schema: "guestops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    integration_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stay_source_detail", x => x.id);
                    table.ForeignKey(
                        name: "fk_stay_source_detail_stay_sources_stay_id",
                        column: x => x.stay_id,
                        principalSchema: "guestops",
                        principalTable: "stay_sources",
                        principalColumn: "stay_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assignments_room_id_released_at",
                schema: "guestops",
                table: "assignments",
                columns: new[] { "room_id", "released_at" });

            migrationBuilder.CreateIndex(
                name: "ix_assignments_stay_id",
                schema: "guestops",
                table: "assignments",
                column: "stay_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_external_refs_booking_id",
                schema: "guestops",
                table: "booking_external_refs",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "uq_booking_external_refs__identity",
                schema: "guestops",
                table: "booking_external_refs",
                columns: new[] { "integration_id", "identifier_kind", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_property_id",
                schema: "guestops",
                table: "bookings",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_points__blind_index",
                schema: "guestops",
                table: "contact_points",
                columns: new[] { "kind", "value_index" });

            migrationBuilder.CreateIndex(
                name: "ix_contact_points_guest_id",
                schema: "guestops",
                table: "contact_points",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_guests_property_id",
                schema: "guestops",
                table: "guests",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_held_facts_property_id_resolved_at",
                schema: "guestops",
                table: "held_facts",
                columns: new[] { "property_id", "resolved_at" });

            migrationBuilder.CreateIndex(
                name: "uq_registrations__card_number",
                schema: "guestops",
                table: "registrations",
                column: "card_number",
                unique: true,
                filter: "card_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_room_stays_booking_id",
                schema: "guestops",
                table: "room_stays",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_room_stays_current_room_id",
                schema: "guestops",
                table: "room_stays",
                column: "current_room_id");

            migrationBuilder.CreateIndex(
                name: "ix_room_stays_property_id_business_date",
                schema: "guestops",
                table: "room_stays",
                columns: new[] { "property_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_room_stays_property_id_lifecycle",
                schema: "guestops",
                table: "room_stays",
                columns: new[] { "property_id", "lifecycle" });

            migrationBuilder.CreateIndex(
                name: "ix_room_stays_property_id_room_type_id",
                schema: "guestops",
                table: "room_stays",
                columns: new[] { "property_id", "room_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rooms_out_of_order_property_id_from_date_to_date",
                schema: "guestops",
                table: "rooms_out_of_order",
                columns: new[] { "property_id", "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "uq_stay_absences__field",
                schema: "guestops",
                table: "stay_absences",
                columns: new[] { "stay_id", "field" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stay_disagreements_stay_id_state",
                schema: "guestops",
                table: "stay_disagreements",
                columns: new[] { "stay_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_stay_external_refs_stay_id",
                schema: "guestops",
                table: "stay_external_refs",
                column: "stay_id");

            migrationBuilder.CreateIndex(
                name: "uq_stay_external_refs__identity",
                schema: "guestops",
                table: "stay_external_refs",
                columns: new[] { "integration_id", "identifier_kind", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stay_guests_guest_id",
                schema: "guestops",
                table: "stay_guests",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_stay_link_candidates_local_stay_id_state",
                schema: "guestops",
                table: "stay_link_candidates",
                columns: new[] { "local_stay_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_stay_notes_stay_id",
                schema: "guestops",
                table: "stay_notes",
                column: "stay_id");

            migrationBuilder.CreateIndex(
                name: "ix_stay_reporting_state_required_by",
                schema: "guestops",
                table: "stay_reporting",
                columns: new[] { "state", "required_by" });

            migrationBuilder.CreateIndex(
                name: "ix_stay_requests_stay_id",
                schema: "guestops",
                table: "stay_requests",
                column: "stay_id");

            migrationBuilder.CreateIndex(
                name: "uq_stay_requests__correlation",
                schema: "guestops",
                table: "stay_requests",
                column: "correlation_id",
                unique: true,
                filter: "correlation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stay_source_detail_stay_id",
                schema: "guestops",
                table: "stay_source_detail",
                column: "stay_id");

            migrationBuilder.CreateIndex(
                name: "ix_stay_sources_channel",
                schema: "guestops",
                table: "stay_sources",
                column: "channel");

            migrationBuilder.CreateIndex(
                name: "ix_stop_sells_property_id_room_type_id_from_date_to_date",
                schema: "guestops",
                table: "stop_sells",
                columns: new[] { "property_id", "room_type_id", "from_date", "to_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignments",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "booking_external_refs",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "commercial_terms",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "contact_points",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "held_facts",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "registrations",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "rooms_out_of_order",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_absences",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_disagreements",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_external_refs",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_guests",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_link_candidates",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_notes",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_reporting",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_requests",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_source_detail",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stop_sells",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "guests",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "stay_sources",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "room_stays",
                schema: "guestops");

            migrationBuilder.DropTable(
                name: "bookings",
                schema: "guestops");
        }
    }
}
