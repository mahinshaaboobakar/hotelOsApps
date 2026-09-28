using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.Configurations;

/// <summary>
/// How leave — its kinds, its balances and its requests — is stored;
/// <c>Application/Leave</c>'s subject.
/// </summary>
/// <remarks>
/// Three types, one subject. A request is granted against a type and moves the
/// ledger, and <c>LeaveService</c> and <c>LeaveTypeService</c> are the only
/// writers of all three: a balance is the ledger's sum, so the three cannot be
/// reasoned about apart.
/// </remarks>
public sealed class LeaveConfiguration :
    IEntityTypeConfiguration<LeaveType>,
    IEntityTypeConfiguration<LeaveLedgerEntry>,
    IEntityTypeConfiguration<LeaveRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LeaveType> type)
    {
        type.ToTable("leave_types", table =>
            table.HasCheckConstraint(
                "ck_leave_types__accrual_not_negative",
                "accrual_per_month IS NULL OR accrual_per_month >= 0"));

        type.HasKey(t => t.Id);

        type.Property(t => t.Code).HasMaxLength(32).IsRequired();
        type.Property(t => t.Name).HasMaxLength(120).IsRequired();
        type.Property(t => t.AccrualPerMonth).HasPrecision(5, 2);
        type.Property(t => t.Version).IsConcurrencyToken();

        // The code is stable within the property and reports group on it, so
        // two types sharing one would merge a year of history. Unfiltered by
        // `active`, unlike the shift catalogue: a retired type's ledger
        // entries still name it, and reusing the code would attribute them to
        // whatever replaced it.
        type.HasIndex(t => new { t.PropertyId, t.Code })
            .IsUnique()
            .HasDatabaseName("uq_leave_types__property_code");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LeaveLedgerEntry> entry)
    {
        entry.ToTable("leave_ledger", table =>
            table.HasCheckConstraint(
                "ck_leave_ledger__days_not_zero",
                "days <> 0"));

        entry.HasKey(e => e.Id);

        entry.Property(e => e.Days).HasPrecision(6, 2);
        entry.Property(e => e.Note).HasMaxLength(500).IsRequired();

        // The balance query: one person, summed by type.
        entry.HasIndex(e => new { e.PropertyId, e.StaffId, e.LeaveTypeId })
            .HasDatabaseName("ix_leave_ledger__property_staff_type");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LeaveRequest> request)
    {
        request.ToTable("leave_requests", table =>
            table.HasCheckConstraint(
                "ck_leave_requests__range_ordered",
                "leave_to >= leave_from"));

        request.HasKey(r => r.Id);

        request.Property(r => r.Note).HasMaxLength(1000).IsRequired();
        request.Property(r => r.DecisionNote).HasMaxLength(1000).IsRequired();
        request.Property(r => r.Version).IsConcurrencyToken();

        // `Days` is computed from the two dates and has nowhere to be stored
        // — a count that could disagree with the range it came from is the
        // derived-projection defect one field wide.
        request.Ignore(r => r.Days);

        // Two column names, because `from` and `to` are reserved words in
        // enough dialects that a bare one is a migration failure waiting for
        // whichever database somebody tries next.
        request.Property(r => r.From).HasColumnName("leave_from");
        request.Property(r => r.To).HasColumnName("leave_to");

        // The approver's queue, and the overlap check.
        request.HasIndex(r => new { r.PropertyId, r.ApproverStaffId, r.State })
            .HasDatabaseName("ix_leave_requests__property_approver_state");

        request.HasIndex(r => new { r.PropertyId, r.StaffId, r.From })
            .HasDatabaseName("ix_leave_requests__property_staff_from");
    }
}
