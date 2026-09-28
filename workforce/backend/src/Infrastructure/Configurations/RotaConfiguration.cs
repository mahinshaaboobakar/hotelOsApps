using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.Configurations;

/// <summary>
/// How the rota and the policy it is checked against are stored —
/// <c>Application/Rota</c>'s subject.
/// </summary>
/// <remarks>
/// Two types because <c>RotaService</c> and <c>PolicyService</c> are one
/// subject's two halves: an assignment is what the rota holds, and the policy is
/// what <c>OvertimeCheck</c> measures it against. They are written by nothing
/// else.
/// </remarks>
public sealed class RotaConfiguration :
    IEntityTypeConfiguration<ShiftAssignment>,
    IEntityTypeConfiguration<WorkforcePolicy>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShiftAssignment> assignment)
    {
        assignment.ToTable("shift_assignments", table =>
        {
            // Both stated or neither. A half-written one-off span is a cell
            // whose hours nobody can compute.
            table.HasCheckConstraint(
                "ck_shift_assignments__override_complete",
                "(override_starts_at IS NULL) = (override_ends_at IS NULL)");

            table.HasCheckConstraint(
                "ck_shift_assignments__department_present",
                "length(btrim(department_code)) > 0");
        });

        assignment.HasKey(a => a.Id);

        assignment.Property(a => a.DepartmentCode).HasMaxLength(50).IsRequired();
        assignment.Property(a => a.Version).IsConcurrencyToken();

        // **One shift per person per day.** A split shift is one catalogue
        // entry with two spans, so a second row on one day would be a second
        // shift — a thing the rota does not offer. Expressible as an index
        // because there is no window to compare, unlike a posting.
        assignment.HasIndex(a => new { a.PropertyId, a.StaffId, a.Date })
            .IsUnique()
            .HasDatabaseName("uq_shift_assignments__property_staff_date");

        // The week grid's query: one department, seven days.
        assignment.HasIndex(a => new { a.PropertyId, a.Date, a.DepartmentCode })
            .HasDatabaseName("ix_shift_assignments__property_date_department");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WorkforcePolicy> policy)
    {
        policy.ToTable("policies", table =>
        {
            // A threshold of zero flags every shift ever worked, and a
            // negative one cannot be meant. Refused in the service too, where
            // the message can say why; here so no other writer can bypass it.
            table.HasCheckConstraint(
                "ck_policies__overtime_daily_positive",
                "overtime_daily_hours IS NULL OR overtime_daily_hours > 0");

            table.HasCheckConstraint(
                "ck_policies__overtime_weekly_positive",
                "overtime_weekly_hours IS NULL OR overtime_weekly_hours > 0");
        });

        // The property id *is* the key: one property has one policy, and a
        // surrogate would admit a second row nothing could choose between.
        policy.HasKey(p => p.PropertyId);

        policy.Property(p => p.OvertimeDailyHours).HasPrecision(5, 2);
        policy.Property(p => p.OvertimeWeeklyHours).HasPrecision(5, 2);
        policy.Property(p => p.Version).IsConcurrencyToken();
    }
}
