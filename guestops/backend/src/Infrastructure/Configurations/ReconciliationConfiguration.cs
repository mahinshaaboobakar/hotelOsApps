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

/// <summary>The seller's control — GUEST-Q7.</summary>
public class StopSellConfiguration : IEntityTypeConfiguration<StopSell>
{
    public void Configure(EntityTypeBuilder<StopSell> builder)
    {
        builder.ToTable("stop_sells");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Reason).HasMaxLength(500);

        // Availability subtracts these per type over a date range.
        builder.HasIndex(s => new { s.PropertyId, s.RoomTypeId, s.FromDate, s.ToDate });

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_stop_sells__range", "to_date >= from_date"));
    }
}

/// <summary>
/// What EngineeringOps has taken out of order, as this application heard it.
/// </summary>
/// <remarks>
/// Keyed on the room: one open out-of-order period per room at a time, which is
/// what an event stream of *placed* and *returned* produces. A second row for
/// one room would mean two facts nobody reconciled.
/// </remarks>
public class RoomOutOfOrderConfiguration : IEntityTypeConfiguration<RoomOutOfOrder>
{
    public void Configure(EntityTypeBuilder<RoomOutOfOrder> builder)
    {
        builder.ToTable("rooms_out_of_order");
        builder.HasKey(r => r.RoomId);

        builder.HasIndex(r => new { r.PropertyId, r.FromDate, r.ToDate });
    }
}

/// <summary>The card, and the filing obligation beside it.</summary>
public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("registrations");
        builder.HasKey(r => r.StayId);

        builder.Property(r => r.CardNumber).HasMaxLength(64);
        builder.Property(r => r.NameAsOnId).HasMaxLength(400);
        builder.Property(r => r.Nationality).HasMaxLength(2);
        builder.Property(r => r.AddressLine).HasMaxLength(500);
        builder.Property(r => r.City).HasMaxLength(100);
        builder.Property(r => r.State).HasMaxLength(100);
        builder.Property(r => r.Country).HasMaxLength(2);
        builder.Property(r => r.PostalCode).HasMaxLength(20);
        builder.Property(r => r.IdType).HasMaxLength(64);
        builder.Property(r => r.IdNumber).HasMaxLength(100);
        builder.Property(r => r.IdIssuer).HasMaxLength(200);
        builder.Property(r => r.ArrivingFrom).HasMaxLength(200);
        builder.Property(r => r.ProceedingTo).HasMaxLength(200);
        builder.Property(r => r.PurposeOfVisit).HasMaxLength(100);
        builder.Property(r => r.VehicleNumber).HasMaxLength(32);

        builder.Property(r => r.PassportNumber).HasMaxLength(100);
        builder.Property(r => r.PassportPlace).HasMaxLength(200);
        builder.Property(r => r.VisaType).HasMaxLength(64);
        builder.Property(r => r.VisaNumber).HasMaxLength(100);
        builder.Property(r => r.PortOfArrival).HasMaxLength(200);

        builder.Property(r => r.DocumentRefs).HasMaxLength(2000);
        builder.Property(r => r.SignatureRef).HasMaxLength(500);

        // The property's own series, unique within the property. Two guests
        // signing one card number is a records defect a hotel would be asked
        // about at an inspection.
        builder.HasIndex(r => r.CardNumber)
            .IsUnique()
            .HasFilter("card_number IS NOT NULL")
            .HasDatabaseName("uq_registrations__card_number");
    }
}

/// <summary>Telling an authority — the flag, and the receipt.</summary>
public class StayReportingConfiguration : IEntityTypeConfiguration<StayReporting>
{
    public void Configure(EntityTypeBuilder<StayReporting> builder)
    {
        builder.ToTable("stay_reporting");
        builder.HasKey(r => r.StayId);

        builder.Property(r => r.Authority).HasMaxLength(200);
        builder.Property(r => r.Reference).HasMaxLength(200);

        // The due list: what is outstanding, and what is overdue.
        builder.HasIndex(r => new { r.State, r.RequiredBy });
    }
}

/// <summary>Requests and notes.</summary>
public class StayRequestConfiguration : IEntityTypeConfiguration<StayRequest>
{
    public void Configure(EntityTypeBuilder<StayRequest> builder)
    {
        builder.ToTable("stay_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Text).HasMaxLength(2000).IsRequired();
        builder.HasIndex(r => r.StayId);

        // EVT-Q3: the reply arrives as an event carrying this id, and the
        // consumer looks the request up by it. Unique, because two requests
        // announced under one correlation id would make the reply ambiguous —
        // and the symptom would be a job attached to the wrong guest.
        builder.HasIndex(r => r.CorrelationId)
            .IsUnique()
            .HasFilter("correlation_id IS NOT NULL")
            .HasDatabaseName("uq_stay_requests__correlation");
    }
}

/// <summary>A remark that dies with the stay.</summary>
public class StayNoteConfiguration : IEntityTypeConfiguration<StayNote>
{
    public void Configure(EntityTypeBuilder<StayNote> builder)
    {
        builder.ToTable("stay_notes");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Text).HasMaxLength(2000).IsRequired();
        builder.HasIndex(n => n.StayId);
    }
}
