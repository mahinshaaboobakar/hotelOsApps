using HotelOS.GuestOps.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.Configurations;

/// <summary>The person as this property knows them.</summary>
public class GuestIdentityConfiguration : IEntityTypeConfiguration<GuestIdentity>
{
    public void Configure(EntityTypeBuilder<GuestIdentity> builder)
    {
        builder.ToTable("guests");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.NameGiven).HasMaxLength(200);
        builder.Property(g => g.NameFamily).HasMaxLength(200);
        builder.Property(g => g.NameAsGiven).HasMaxLength(400).IsRequired();
        builder.Property(g => g.Preferences).HasMaxLength(2000);
        builder.Property(g => g.Version).IsConcurrencyToken();

        builder.HasIndex(g => g.PropertyId);

        builder.HasMany(g => g.Contacts)
            .WithOne(c => c.Guest)
            .HasForeignKey(c => c.GuestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Contact points: ciphertext, and a blind index beside it.
/// </summary>
/// <remarks>
/// <para>
/// The mechanism is the platform's, already designed: the value is encrypted,
/// and an indexed HMAC of its normalised form makes exact-match resolution a
/// single index seek. GuestOps owns this index because ADR 0089 §CTX-Q2 left it
/// waiting for *"the domain that owns the phone number"*, and that is this one.
/// </para>
/// <para>
/// <b>Exact match only.</b> No prefix search, no partial lookup — the accepted
/// cost of encrypting the column, and the reason Chapter 21's WhatsApp flow
/// resolves on a complete number rather than a fragment.
/// </para>
/// </remarks>
public class ContactPointConfiguration : IEntityTypeConfiguration<ContactPoint>
{
    public void Configure(EntityTypeBuilder<ContactPoint> builder)
    {
        builder.ToTable("contact_points");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ValueCipher).IsRequired();
        builder.Property(c => c.ValueIndex).IsRequired();
        builder.Property(c => c.TechType).HasMaxLength(32);
        builder.Property(c => c.UseType).HasMaxLength(32);

        // What phone → guest seeks on. Not unique: two guests legitimately share
        // a number — a couple, a company line — and a unique index here would
        // refuse the second booking rather than answer the question.
        builder.HasIndex(c => new { c.Kind, c.ValueIndex })
            .HasDatabaseName("ix_contact_points__blind_index");
    }
}

/// <summary>The terms, and the source detail beside them.</summary>
public class CommercialTermsConfiguration : IEntityTypeConfiguration<CommercialTerms>
{
    public void Configure(EntityTypeBuilder<CommercialTerms> builder)
    {
        builder.ToTable("commercial_terms");
        builder.HasKey(t => t.StayId);

        builder.Property(t => t.RateCode).HasMaxLength(64);
        builder.Property(t => t.RateName).HasMaxLength(200);
        builder.Property(t => t.GuaranteeCode).HasMaxLength(64);
        builder.Property(t => t.GuaranteeDescription).HasMaxLength(500);
        builder.Property(t => t.PenaltyBasis).HasMaxLength(64);

        // Money is three columns or it is not money (R19). Owned rather than
        // flattened by hand so the currency and the basis cannot be dropped
        // from one call site and kept in another.
        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.MinorUnits).HasColumnName("amount_minor_units");
            money.Property(m => m.Currency).HasColumnName("amount_currency").HasMaxLength(3);
            money.Property(m => m.Basis).HasColumnName("amount_tax_basis");
        });

        builder.OwnsOne(t => t.PenaltyAmount, money =>
        {
            money.Property(m => m.MinorUnits).HasColumnName("penalty_minor_units");
            money.Property(m => m.Currency).HasColumnName("penalty_currency").HasMaxLength(3);
            money.Property(m => m.Basis).HasColumnName("penalty_tax_basis");
        });
    }
}

/// <summary>The kept set, and what is retained beyond it — GUEST-Q7.</summary>
public class StaySourceConfiguration : IEntityTypeConfiguration<StaySource>
{
    public void Configure(EntityTypeBuilder<StaySource> builder)
    {
        builder.ToTable("stay_sources");
        builder.HasKey(s => s.StayId);

        builder.Property(s => s.Channel).HasMaxLength(64);
        builder.Property(s => s.TravelAgent).HasMaxLength(200);
        builder.Property(s => s.MarketCode).HasMaxLength(64);
        builder.Property(s => s.MealPlan).HasMaxLength(32);

        // Channel mix is the report every hotel runs, and it is the reason this
        // field is captured at all rather than reconstructed later.
        builder.HasIndex(s => s.Channel);

        builder.HasMany(s => s.Detail)
            .WithOne(d => d.Source)
            .HasForeignKey(d => d.StayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Retention — never read to drive behaviour.</summary>
public class StaySourceDetailConfiguration : IEntityTypeConfiguration<StaySourceDetail>
{
    public void Configure(EntityTypeBuilder<StaySourceDetail> builder)
    {
        builder.ToTable("stay_source_detail");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Key).HasMaxLength(128).IsRequired();
        builder.Property(d => d.Value).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.IntegrationId).HasMaxLength(64).IsRequired();

        // Indexed by stay only. **No index on `key`**, deliberately: an index
        // is what a query is built on, and a query here would be this table
        // driving behaviour — which the ruling forbids. A field that decides
        // something gets modelled first.
        builder.HasIndex(d => d.StayId);
    }
}
