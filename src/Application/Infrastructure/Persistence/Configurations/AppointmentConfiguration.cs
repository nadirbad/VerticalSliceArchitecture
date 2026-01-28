using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VerticalSliceArchitecture.Application.Domain;

namespace VerticalSliceArchitecture.Application.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.Property(a => a.Notes)
            .HasMaxLength(1024);

        // Use PostgreSQL's xmin system column for optimistic concurrency
        // In Npgsql 7.0+, configure uint properties with IsRowVersion() to map to xmin
        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index for checking doctor availability and preventing exact duplicate bookings
        // Note: This prevents the same (DoctorId, StartUtc, EndUtc) combination but does NOT
        // prevent overlapping time ranges. Full overlap detection requires application-level checks.
        builder.HasIndex(a => new { a.DoctorId, a.StartUtc, a.EndUtc })
            .IsUnique()
            .HasDatabaseName("IX_Appointments_Doctor_TimeRange");

        // Index for patient appointments
        builder.HasIndex(a => new { a.PatientId, a.StartUtc })
            .HasDatabaseName("IX_Appointments_Patient_StartTime");
    }
}