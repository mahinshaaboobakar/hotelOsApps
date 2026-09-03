using HotelOS.GuestOps.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.Configurations;

/// <summary>Overrides, disagreements and the candidate link — the PMS mode.</summary>
public class StayDisagreementConfiguration : IEntityTypeConfiguration<StayDisagreement>
{
    public void Configure(EntityTypeBuilder<StayDisagreement> builder)
    {
        builder.ToTable("stay_disagreements");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.OurValue).HasMaxLength(200).IsRequired();
        builder.Property(d => d.PmsValue).HasMaxLength(200).IsRequired();
        builder.Property(d => d.PmsValueAtOverride).HasMaxLength(200);

        // The Attention list reads the standing ones. Filtered rather than
        // whole, because settled and confirmed rows are history and there are
        // far more of them — a confirmation is silent and frequent, a
        // disagreement is loud and rare (GUEST-Q4).
        builder.HasIndex(d => new { d.StayId, d.State });
    }
}

/// <summary>A PMS fact that might be a stay this property already created.</summary>
public class StayLinkCandidateConfiguration : IEntityTypeConfiguration<StayLinkCandidate>
{
    public void Configure(EntityTypeBuilder<StayLinkCandidate> builder)
    {
        builder.ToTable("stay_link_candidates");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.LocalStayId, c.State });

        builder.HasOne(c => c.LocalStay)
            .WithMany()
            .HasForeignKey(c => c.LocalStayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>A fact received and deliberately not applied — GUEST-Q5.</summary>
public class HeldFactConfiguration : IEntityTypeConfiguration<HeldFact>
{
    public void Configure(EntityTypeBuilder<HeldFact> builder)
    {
        builder.ToTable("held_facts");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.IntegrationId).HasMaxLength(64).IsRequired();
        builder.Property(f => f.Payload).IsRequired();

        // Unresolved first: the Attention list reads them, and a property that
        // has run for a year has far more resolved rows than open ones.
        builder.HasIndex(f => new { f.PropertyId, f.ResolvedAt });
    }
}

/// <summary>The feed's pulse, one row per property and integration.</summary>
/// <remarks>
/// Keyed on both, because a property with two feeds has two answers and a
/// single combined stamp hides one of them going quiet.
/// </remarks>
public class InboundFeedMarkConfiguration : IEntityTypeConfiguration<InboundFeedMark>
{
    public void Configure(EntityTypeBuilder<InboundFeedMark> builder)
    {
        builder.ToTable("inbound_feed_marks");
        builder.HasKey(m => new { m.PropertyId, m.IntegrationId });
        builder.Property(m => m.IntegrationId).HasMaxLength(64);
    }
}
