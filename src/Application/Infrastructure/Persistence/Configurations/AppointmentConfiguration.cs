using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VerticalSliceArchitecture.Application.Domain.Healthcare;

namespace VerticalSliceArchitecture.Application.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.Property(a => a.Notes)
            .HasMaxLength(1024);

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

        // Index for checking doctor availability
        builder.HasIndex(a => new { a.DoctorId, a.StartUtc, a.EndUtc })
            .HasDatabaseName("IX_Appointments_Doctor_TimeRange");

        // Index for patient appointments
        builder.HasIndex(a => new { a.PatientId, a.StartUtc })
            .HasDatabaseName("IX_Appointments_Patient_StartTime");
    }
}