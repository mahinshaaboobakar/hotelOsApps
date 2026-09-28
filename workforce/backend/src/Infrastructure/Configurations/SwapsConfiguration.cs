using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.Configurations;

/// <summary>
/// How a shift exchange is stored — <c>Application/Swaps</c>'s subject.
/// </summary>
public sealed class SwapsConfiguration : IEntityTypeConfiguration<SwapProposal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SwapProposal> proposal)
    {
        proposal.ToTable("swap_proposals", table =>
            table.HasCheckConstraint(
                "ck_swap_proposals__two_people",
                "proposer_staff_id <> colleague_staff_id"));

        proposal.HasKey(p => p.Id);

        proposal.Property(p => p.Note).HasMaxLength(1000).IsRequired();
        proposal.Property(p => p.DecisionNote).HasMaxLength(1000).IsRequired();
        proposal.Property(p => p.Version).IsConcurrencyToken();

        // "What needs me" — one query serving the colleague and the approver,
        // because in a small hotel they are the same person as often as not.
        proposal.HasIndex(p => new { p.PropertyId, p.State, p.ColleagueStaffId })
            .HasDatabaseName("ix_swap_proposals__property_state_colleague");

        proposal.HasIndex(p => new { p.PropertyId, p.State, p.ApproverStaffId })
            .HasDatabaseName("ix_swap_proposals__property_state_approver");
    }
}
