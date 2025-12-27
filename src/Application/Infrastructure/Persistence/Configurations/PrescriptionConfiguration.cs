using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VerticalSliceArchitecture.Application.Domain;

namespace VerticalSliceArchitecture.Application.Infrastructure.Persistence.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.Property(p => p.MedicationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Dosage)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Directions)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Quantity)
            .IsRequired();

        builder.Property(p => p.NumberOfRefills)
            .IsRequired();

        builder.Property(p => p.RemainingRefills)
            .IsRequired();

        builder.Property(p => p.IssuedDateUtc)
            .IsRequired();

        builder.Property(p => p.ExpirationDateUtc)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Doctor)
            .WithMany()
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for querying patient's prescriptions
        builder.HasIndex(p => p.PatientId)
            .HasDatabaseName("IX_Prescriptions_PatientId");

        // Index for querying doctor's issued prescriptions
        builder.HasIndex(p => p.DoctorId)
            .HasDatabaseName("IX_Prescriptions_DoctorId");

        // Index for finding expired prescriptions
        builder.HasIndex(p => p.ExpirationDateUtc)
            .HasDatabaseName("IX_Prescriptions_ExpirationDate");
    }
}