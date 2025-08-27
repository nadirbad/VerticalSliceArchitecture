using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VerticalSliceArchitecture.Application.Domain.Healthcare;

namespace VerticalSliceArchitecture.Application.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.Property(p => p.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Email)
            .HasMaxLength(320)
            .IsRequired()
            .HasConversion(
                e => e == null ? null : e.Trim().ToLowerInvariant(),
                e => e ?? string.Empty);

        builder.Property(p => p.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => p.Email)
            .IsUnique();
    }
}