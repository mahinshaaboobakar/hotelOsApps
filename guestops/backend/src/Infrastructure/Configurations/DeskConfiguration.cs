using HotelOS.GuestOps.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.GuestOps.Infrastructure.Configurations;

/// <summary>
/// The desk's own records — section 2.7's "the rest of the desk".
/// </summary>
/// <remarks>
/// One purpose, and the design's own grouping: what the desk writes down about
/// a guest who is here. They share a lifetime (all die with the stay) and a
/// reason to change (the desk's paperwork), which is what makes them one file
/// rather than four — ADR 0038's test is a shared purpose, not a shared table
/// count.
/// </remarks>
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

        // The navigation is configured explicitly, and that is not decoration.
        // Left to convention EF paired `Stay` with neither the key nor an
        // inverse and invented a shadow `stay_id1` alongside it — a second,
        // always-empty foreign key that the schema carried from slice 1 until
        // the first test to insert a row hit it.
        builder.HasOne(r => r.Stay)
            .WithOne()
            .HasForeignKey<Registration>(r => r.StayId)
            .OnDelete(DeleteBehavior.Cascade);
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

        builder.HasOne(r => r.Stay)
            .WithOne()
            .HasForeignKey<StayReporting>(r => r.StayId)
            .OnDelete(DeleteBehavior.Cascade);
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

        builder.HasOne(r => r.Stay)
            .WithMany()
            .HasForeignKey(r => r.StayId)
            .OnDelete(DeleteBehavior.Cascade);
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

        builder.HasOne(n => n.Stay)
            .WithMany()
            .HasForeignKey(n => n.StayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
