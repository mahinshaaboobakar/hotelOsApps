using HotelOS.RoomCare.Domain;
using Microsoft.EntityFrameworkCore;

using static HotelOS.RoomCare.Infrastructure.Configuration.Vocabulary;

namespace HotelOS.RoomCare.Infrastructure.Configuration;

/// <summary>The room's condition and what was observed about it — chapter 03 §2.1, §2.2.</summary>
internal static class RoomTables
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<RoomState>(room =>
        {
            room.ToTable("room_state", t =>
            {
                t.HasCheckConstraint("ck_room_state__condition", OneOf("condition", Condition.All));
                t.HasCheckConstraint("ck_room_state__source", OneOf("condition_source", ConditionSource.All));
                t.HasCheckConstraint("ck_room_state__set_by_kind", OneOf("condition_set_by_kind", ActorKind.All));
                t.HasCheckConstraint("ck_room_state__occupancy", OneOf("occupancy", Occupancy.All));
                t.HasCheckConstraint("ck_room_state__stay_statuses", EachOneOf("stay_statuses", StayStatus.All));
                t.HasCheckConstraint(
                    "ck_room_state__disagreement_condition", NullOrOneOf("disagreement_observed_condition", Condition.All));
                t.HasCheckConstraint(
                    "ck_room_state__disagreement_source", NullOrOneOf("disagreement_source", ObservationSource.All));
                t.HasCheckConstraint(
                    "ck_room_state__disagreement_kept", NullOrOneOf("disagreement_cleared_kept", DisagreementKept.All));
                t.HasCheckConstraint("ck_room_state__days_without_service", "days_without_service >= 0");
            });
            room.HasKey(r => r.RoomId);
            room.Property(r => r.RoomId).ValueGeneratedNever();
            room.Property(r => r.Condition).HasMaxLength(12).IsRequired();
            room.Property(r => r.ConditionSource).HasMaxLength(12).IsRequired();
            room.Property(r => r.ConditionSetByKind).HasMaxLength(12).IsRequired();
            room.Property(r => r.Occupancy).HasMaxLength(12).IsRequired();
            room.Property(r => r.DisagreementObservedCondition).HasMaxLength(12);
            room.Property(r => r.DisagreementSource).HasMaxLength(12);
            room.Property(r => r.DisagreementClearedKept).HasMaxLength(8);
            room.Property(r => r.Version).IsConcurrencyToken();
            room.Ignore(r => r.HasDisagreement);
            room.HasIndex(r => new { r.PropertyId, r.Condition });
        });

        model.Entity<RoomObservation>(seen =>
        {
            seen.ToTable("room_observation", t =>
            {
                t.HasCheckConstraint("ck_room_observation__source", OneOf("source", ObservationSource.All));
                t.HasCheckConstraint("ck_room_observation__outcome", OneOf("outcome", ObservationOutcome.All));
                t.HasCheckConstraint("ck_room_observation__condition", NullOrOneOf("condition", Condition.All));
                t.HasCheckConstraint("ck_room_observation__occupancy", NullOrOneOf("occupancy", Occupancy.All));
                t.HasCheckConstraint("ck_room_observation__stay_statuses", EachOneOf("stay_statuses", StayStatus.All));
                t.HasCheckConstraint("ck_room_observation__via", NullOrOneOf("via", Via.All));
                t.HasCheckConstraint(
                    "ck_room_observation__manual_has_person", "source <> 'MANUAL' OR by_user_id IS NOT NULL");
            });
            seen.HasKey(o => o.Id);
            seen.Property(o => o.Source).HasMaxLength(12).IsRequired();
            seen.Property(o => o.Outcome).HasMaxLength(24).IsRequired();
            seen.Property(o => o.Condition).HasMaxLength(12);
            seen.Property(o => o.Occupancy).HasMaxLength(12);
            seen.Property(o => o.Via).HasMaxLength(12);
            seen.HasIndex(o => new { o.PropertyId, o.RoomId, o.OccurredAt });
            seen.HasIndex(o => new { o.EventId, o.RoomId }).IsUnique().HasFilter("event_id IS NOT NULL");
        });
    }
}
